using System.Security.Claims;
using KutuphaneOtomasyonu.Api.Altyapi;
using KutuphaneOtomasyonu.Api.Dto;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Api.Endpoints;

/// <summary>Ödünç verme, iade ve süre uzatma. Kurallar <see cref="OduncService"/> içindedir ve tek transaction'da uygulanır.</summary>
public static class OduncEndpoints
{
    public static void MapOduncler(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api/oduncler").WithTags("Ödünç")
            .RequireAuthorization(Oturum.Politikalar.Personel);

        grup.MapGet("/", ListeleAsync).WithSummary("Aktif ödünçler; durum=gecikmis ile yalnızca gecikenler, uyeId ile üyenin geçmişi.");
        grup.MapPost("/", OduncVerAsync).WithSummary("Kitap/nüsha barkodu ve üye ile ödünç verir.");
        grup.MapPost("/{id:int}/iade", IadeAsync).WithSummary("Ödünç kaydını iade alır (gecikme/hasar/kayıp cezası hesaplanır).");
        grup.MapPost("/iade-barkod", BarkodlaIadeAsync).WithSummary("Nüsha barkodunu okutarak iade alır.");
        grup.MapPost("/{id:int}/uzat", UzatAsync).WithSummary("Ödünç süresini uzatır.");
    }

    private static async Task<IResult> ListeleAsync(KutuphaneDbContext db, string? durum, int? uyeId)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        if (uyeId.HasValue)
        {
            var uye = await db.Uyeler.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uyeId.Value);
            if (uye == null) return Yanit.Bulunamadi("Üye bulunamadı.");
            var gecmis = await kapsam.Odunc.GetUyeOdunclerAsync(uye.Id);
            return Results.Ok(gecmis.Select(o => OduncDto.Olustur(o, uye)).ToList());
        }

        var liste = string.Equals(durum, "gecikmis", StringComparison.OrdinalIgnoreCase)
            ? await kapsam.Odunc.GetGecikmisTeslerAsync()
            : await kapsam.Odunc.GetAktifOdunclerAsync();
        return Results.Ok(liste.Select(o => OduncDto.Olustur(o)).ToList());
    }

    private static async Task<IResult> OduncVerAsync(OduncVerIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);

        var kitapId = istek.KitapId;
        var barkod = string.IsNullOrWhiteSpace(istek.NushaBarkodu) ? null : istek.NushaBarkodu.Trim();
        if (barkod != null)
        {
            var nusha = await kapsam.Banko.NushaBulAsync(barkod);
            if (nusha == null) return Yanit.Hata("Bu barkoda ait nüsha bulunamadı.");
            if (kitapId.HasValue && kitapId != nusha.KitapId) return Yanit.Hata("Barkod seçilen kitaba ait değil.");
            kitapId = nusha.KitapId;
        }
        if (kitapId == null) return Yanit.Hata("Kitap veya nüsha barkodu belirtilmelidir.");

        var uyeId = istek.UyeId;
        if (uyeId == null)
        {
            if (string.IsNullOrWhiteSpace(istek.Uye)) return Yanit.Hata("Üye belirtilmelidir.");
            var bulunan = await kapsam.Banko.UyeBulAsync(istek.Uye);
            if (bulunan.Kayit == null) return Yanit.Hata(bulunan.Mesaj);
            uyeId = bulunan.Kayit.Id;
        }

        var sonuc = await kapsam.Odunc.OduncVerAsync(kitapId.Value, uyeId.Value, kullanici.KullaniciId(), barkod);
        if (!sonuc.Basarili) return Yanit.Hata(sonuc.Mesaj);
        return Results.Created($"/api/oduncler/{sonuc.Odunc!.Id}", await DtoAsync(db, sonuc.Odunc.Id));
    }

    private static async Task<IResult> IadeAsync(int id, IadeIstegi? istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        istek ??= new IadeIstegi();
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Odunc.IadeAlAsync(id, istek.Durum, istek.Notlar, kullanici.KullaniciId());
        return await SonucAsync(db, sonuc);
    }

    private static async Task<IResult> BarkodlaIadeAsync(BarkodIadeIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        if (string.IsNullOrWhiteSpace(istek.Barkod)) return Yanit.Hata("Barkod zorunludur.");

        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var nusha = await kapsam.Banko.NushaBulAsync(istek.Barkod.Trim());
        if (nusha == null) return Yanit.Hata("Bu barkoda ait nüsha bulunamadı.");
        var odunc = await kapsam.Banko.NushaninAktifOduncuAsync(nusha.Id);
        if (odunc.Kayit == null) return Yanit.Hata(odunc.Mesaj);

        var sonuc = await kapsam.Odunc.IadeAlAsync(odunc.Kayit.Id, istek.Durum, istek.Notlar, kullanici.KullaniciId());
        return await SonucAsync(db, sonuc);
    }

    private static async Task<IResult> UzatAsync(int id, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Odunc.UzatAsync(id, kullanici.KullaniciId());
        return await SonucAsync(db, sonuc);
    }

    private static async Task<IResult> SonucAsync(KutuphaneDbContext db, OduncSonuc sonuc) =>
        sonuc.Basarili
            ? Results.Ok(new { mesaj = sonuc.Mesaj, odunc = await DtoAsync(db, sonuc.Odunc!.Id) })
            : sonuc.Mesaj == "Ödünç kaydı bulunamadı." ? Yanit.Bulunamadi(sonuc.Mesaj) : Yanit.Hata(sonuc.Mesaj);

    /// <summary>Yanıt, işlemden sonra veritabanında kalıcı olan hâli yansıtır.</summary>
    private static async Task<OduncDto> DtoAsync(KutuphaneDbContext db, int oduncId)
    {
        var odunc = await db.Oduncler.AsNoTracking()
            .Include(o => o.Kitap).Include(o => o.Uye).Include(o => o.Nusha)
            .SingleAsync(o => o.Id == oduncId);
        return OduncDto.Olustur(odunc);
    }
}
