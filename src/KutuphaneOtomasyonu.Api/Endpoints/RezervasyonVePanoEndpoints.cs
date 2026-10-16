using System.Security.Claims;
using KutuphaneOtomasyonu.Api.Altyapi;
using KutuphaneOtomasyonu.Api.Dto;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Api.Endpoints;

public static class RezervasyonVePanoEndpoints
{
    public static void MapRezervasyonlarVePano(this IEndpointRouteBuilder app)
    {
        var rez = app.MapGroup("/api/rezervasyonlar").WithTags("Rezervasyon")
            .RequireAuthorization(Oturum.Politikalar.Personel);

        rez.MapGet("/", ListeleAsync).WithSummary("Rezervasyonlar; durum ile süzülebilir (Bekliyor, Onaylandi, ...).");
        rez.MapPost("/", OlusturAsync).WithSummary("Üyeyi kitabın bekleme sırasına ekler.");
        rez.MapPost("/{id:int}/iptal", IptalAsync).WithSummary("Rezervasyonu iptal eder; ayrılmış nüsha sıradakine geçer.");
        rez.MapPost("/{id:int}/odunce-donustur", OdunceDonusturAsync)
            .WithSummary("Ayrılmış rezervasyonu tek işlemde ödünce çevirir.");

        app.MapGet("/api/pano", PanoAsync).WithTags("Pano")
            .RequireAuthorization(Oturum.Politikalar.Personel)
            .WithSummary("Personel panosu için özet sayılar.");
    }

    private static async Task<IResult> ListeleAsync(KutuphaneDbContext db, RezervasyonDurumu? durum)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var liste = await kapsam.Rezervasyon.GetTumuAsync();
        if (durum.HasValue) liste = liste.Where(r => r.Durumu == durum.Value).ToList();
        return Results.Ok(liste.Take(500).Select(r => RezervasyonDto.Olustur(r)).ToList());
    }

    private static async Task<IResult> OlusturAsync(RezervasyonIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Rezervasyon.OlusturAsync(istek.KitapId, istek.UyeId, istek.Notlar, kullanici.KullaniciId());
        if (!sonuc.BasariliMi) return Yanit.Hata(sonuc.Mesaj);
        return Results.Created($"/api/rezervasyonlar/{sonuc.Rezervasyon!.Id}", await DtoAsync(db, sonuc.Rezervasyon.Id));
    }

    private static async Task<IResult> IptalAsync(int id, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Rezervasyon.IptalEtAsync(id, kullanici.KullaniciId());
        return sonuc.BasariliMi ? Results.Ok(await DtoAsync(db, id)) : Yanit.Hata(sonuc.Mesaj);
    }

    private static async Task<IResult> OdunceDonusturAsync(int id, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Rezervasyon.OdunceDonusturAsync(id, kullanici.KullaniciId());
        if (!sonuc.Basarili) return Yanit.Hata(sonuc.Mesaj);
        var odunc = await db.Oduncler.AsNoTracking()
            .Include(o => o.Kitap).Include(o => o.Uye).Include(o => o.Nusha)
            .SingleAsync(o => o.Id == sonuc.Odunc!.Id);
        return Results.Ok(new { mesaj = sonuc.Mesaj, odunc = OduncDto.Olustur(odunc) });
    }

    private static async Task<RezervasyonDto> DtoAsync(KutuphaneDbContext db, int id)
    {
        var r = await db.Rezervasyonlar.AsNoTracking().Include(x => x.Kitap).Include(x => x.Uye).SingleAsync(x => x.Id == id);
        return RezervasyonDto.Olustur(r);
    }

    private static async Task<IResult> PanoAsync(KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var kitap = await kapsam.Kitap.GetIstatistikAsync();
        var uye = await kapsam.Uye.GetIstatistikAsync();
        var odunc = await kapsam.Odunc.GetIstatistikAsync();
        var (bekleyenCeza, _) = await kapsam.Uye.GetBekleyenCezaAsync();
        var yaklasan = (await kapsam.Odunc.GetAktifOdunclerAsync()).Take(8).Select(o => OduncDto.Olustur(o)).ToList();

        return Results.Ok(new PanoDto(
            kitap.ToplamKitap, kitap.ToplamKopyaSayisi, kitap.MevcutKopya, uye.ToplamUye, uye.AktifUye,
            uye.CezaliUyeSayisi, odunc.AktifOduncSayisi, odunc.GecikmisSayisi, odunc.BugunOduncSayisi,
            odunc.BugunIadeSayisi, bekleyenCeza, yaklasan));
    }
}
