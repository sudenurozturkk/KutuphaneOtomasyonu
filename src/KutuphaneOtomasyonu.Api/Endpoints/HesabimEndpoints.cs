using System.Security.Claims;
using KutuphaneOtomasyonu.Api.Altyapi;
using KutuphaneOtomasyonu.Api.Dto;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Api.Endpoints;

/// <summary>
/// Öğrenci portalı: üye yalnızca kendi ödünçlerini, rezervasyonlarını ve bildirimlerini görür ve yönetir.
/// Kayıt kimliği istekten değil oturumdan gelir; başka üyenin kaydı "bulunamadı" olarak yanıtlanır.
/// Kurallar personel tarafıyla aynıdır (aynı servisler).
/// </summary>
public static class HesabimEndpoints
{
    public static void MapHesabim(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api/hesabim").WithTags("Öğrenci portalı (Hesabım)")
            .RequireAuthorization(UyeOturumu.Politikalar.Uye);

        grup.MapGet("/", OzetAsync).WithSummary("Üyenin kendi bilgileri, kotası ve borcu.");
        grup.MapGet("/oduncler", OdunclerAsync).WithSummary("Üyenin aktif ödünçleri ve geçmişi.");
        grup.MapPost("/oduncler/{id:int}/uzat", UzatAsync).WithSummary("Kendi ödüncünün süresini uzatır.");
        grup.MapGet("/rezervasyonlar", RezervasyonlarAsync).WithSummary("Üyenin rezervasyonları.");
        grup.MapPost("/rezervasyonlar", RezerveEtAsync).WithSummary("Kitap için bekleme sırasına girer.");
        grup.MapPost("/rezervasyonlar/{id:int}/iptal", IptalAsync).WithSummary("Kendi rezervasyonunu iptal eder.");
        grup.MapGet("/bildirimler", BildirimlerAsync).WithSummary("Son 30 bildirim.");
        grup.MapPost("/bildirimler/{id:int}/okundu", OkunduAsync).WithSummary("Bildirimi okundu işaretler.");
    }

    private static async Task<IResult> OzetAsync(ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        var uyeId = kullanici.UyeId();
        var u = await db.Uyeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == uyeId);
        if (u == null) return Yanit.Bulunamadi("Üye bulunamadı.");

        var simdi = DateTime.Now;
        var aktif = await db.Oduncler.CountAsync(o => o.UyeId == uyeId && o.Durumu == OduncDurumu.Aktif);
        var gecikmis = await db.Oduncler.CountAsync(o =>
            o.UyeId == uyeId && o.Durumu == OduncDurumu.Aktif && o.PlanlananIadeTarihi < simdi);
        var okunmamis = await db.Bildirimler.CountAsync(b => b.UyeId == uyeId && !b.Okundu);

        return Results.Ok(new HesapDto(u.Id, u.TamAd, u.OgrenciNo, u.Email, u.Bolum, u.UyeTipi, u.Durumu,
            u.MaxOduncAdet, u.MaxOduncSuresi, aktif, gecikmis, u.KalanCeza, u.UyelikBitisTarihi, okunmamis));
    }

    private static async Task<IResult> OdunclerAsync(ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        var uyeId = kullanici.UyeId();
        var uye = await db.Uyeler.AsNoTracking().FirstAsync(u => u.Id == uyeId);
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var oduncler = await kapsam.Odunc.GetUyeOdunclerAsync(uyeId);
        return Results.Ok(oduncler.Take(100).Select(o => OduncDto.Olustur(o, uye)).ToList());
    }

    private static async Task<IResult> UzatAsync(int id, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        var uyeId = kullanici.UyeId();
        if (!await db.Oduncler.AnyAsync(o => o.Id == id && o.UyeId == uyeId))
            return Yanit.Bulunamadi("Ödünç kaydı bulunamadı.");

        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Odunc.UzatAsync(id, kullaniciId: null);
        if (!sonuc.Basarili) return Yanit.Hata(sonuc.Mesaj);

        var odunc = await db.Oduncler.AsNoTracking()
            .Include(o => o.Kitap).Include(o => o.Uye).Include(o => o.Nusha)
            .SingleAsync(o => o.Id == id);
        return Results.Ok(new { mesaj = sonuc.Mesaj, odunc = OduncDto.Olustur(odunc) });
    }

    private static async Task<IResult> RezervasyonlarAsync(ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        var uyeId = kullanici.UyeId();
        var liste = await db.Rezervasyonlar.AsNoTracking()
            .Include(r => r.Kitap).Include(r => r.Uye)
            .Where(r => r.UyeId == uyeId)
            .OrderByDescending(r => r.RezervasyonTarihi)
            .Take(100)
            .ToListAsync();
        return Results.Ok(liste.Select(r => RezervasyonDto.Olustur(r)).ToList());
    }

    private static async Task<IResult> RezerveEtAsync(HesapRezervasyonIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Rezervasyon.OlusturAsync(istek.KitapId, kullanici.UyeId(), notlar: null, kullaniciId: null);
        if (!sonuc.BasariliMi) return Yanit.Hata(sonuc.Mesaj);

        var r = await db.Rezervasyonlar.AsNoTracking().Include(x => x.Kitap).Include(x => x.Uye)
            .SingleAsync(x => x.Id == sonuc.Rezervasyon!.Id);
        return Results.Created($"/api/hesabim/rezervasyonlar/{r.Id}", RezervasyonDto.Olustur(r));
    }

    private static async Task<IResult> IptalAsync(int id, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        var uyeId = kullanici.UyeId();
        if (!await db.Rezervasyonlar.AnyAsync(r => r.Id == id && r.UyeId == uyeId))
            return Yanit.Bulunamadi("Rezervasyon bulunamadı.");

        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Rezervasyon.IptalEtAsync(id, kullaniciId: null);
        if (!sonuc.BasariliMi) return Yanit.Hata(sonuc.Mesaj);

        var r = await db.Rezervasyonlar.AsNoTracking().Include(x => x.Kitap).Include(x => x.Uye).SingleAsync(x => x.Id == id);
        return Results.Ok(RezervasyonDto.Olustur(r));
    }

    private static async Task<IResult> BildirimlerAsync(ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        var uyeId = kullanici.UyeId();
        var liste = await db.Bildirimler.AsNoTracking()
            .Where(b => b.UyeId == uyeId)
            .OrderByDescending(b => b.OlusturmaTarihi)
            .Take(30)
            .ToListAsync();
        return Results.Ok(liste.Select(BildirimDto.Olustur).ToList());
    }

    private static async Task<IResult> OkunduAsync(int id, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        var uyeId = kullanici.UyeId();
        var bildirim = await db.Bildirimler.FirstOrDefaultAsync(b => b.Id == id && b.UyeId == uyeId);
        if (bildirim == null) return Yanit.Bulunamadi("Bildirim bulunamadı.");
        if (!bildirim.Okundu)
        {
            bildirim.Okundu = true;
            bildirim.OkunmaTarihi = DateTime.Now;
            await db.SaveChangesAsync();
        }
        return Results.NoContent();
    }
}
