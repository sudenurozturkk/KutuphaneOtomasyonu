using KutuphaneOtomasyonu.Api.Altyapi;
using KutuphaneOtomasyonu.Api.Dto;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using Microsoft.AspNetCore.Authentication;

namespace KutuphaneOtomasyonu.Api.Endpoints;

public static class UyeOturumEndpoints
{
    public static void MapUyeOturumu(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api/uye-oturum").WithTags("Üye oturumu");

        grup.MapPost("/giris", GirisAsync)
            .AllowAnonymous()
            .RequireRateLimiting(OturumEndpoints.GirisHizSiniri)
            .WithSummary("Üye öğrenci numarası/e-posta ve parolasıyla giriş yapar.");
        grup.MapPost("/cikis", async (HttpContext baglam) =>
            {
                await baglam.SignOutAsync(UyeOturumu.Sema);
                return Results.NoContent();
            })
            .RequireAuthorization(UyeOturumu.Politikalar.UyeOturum)
            .WithSummary("Üye oturumunu kapatır.");
        grup.MapGet("/ben", (System.Security.Claims.ClaimsPrincipal kullanici) =>
                Results.Ok(new UyeOturumDto(kullanici.UyeId(), kullanici.Identity?.Name ?? string.Empty,
                    kullanici.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? string.Empty,
                    kullanici.UyeSifreDegistirmeli())))
            .RequireAuthorization(UyeOturumu.Politikalar.UyeOturum)
            .WithSummary("Oturumdaki üye.");
        grup.MapPost("/sifre", SifreDegistirAsync)
            .RequireAuthorization(UyeOturumu.Politikalar.UyeOturum)
            .WithSummary("Üyenin kendi parolasını değiştirir (geçici parola sonrası zorunlu).");
    }

    private static async Task<IResult> GirisAsync(
        UyeGirisIstegi istek, UyeHesapService hesap, KutuphaneDbContext db, IConfiguration ayarlar, HttpContext baglam)
    {
        if (string.IsNullOrWhiteSpace(istek.Giris) || string.IsNullOrEmpty(istek.Sifre))
            return Yanit.Hata("Öğrenci numarası/e-posta ve şifre zorunludur.");

        var (sonuc, uye) = await hesap.DogrulaAsync(istek.Giris, istek.Sifre, db);
        if (!sonuc.Basarili || uye == null)
            return Yanit.Hata(sonuc.Mesaj, StatusCodes.Status401Unauthorized);

        // Personelin verdiği geçici parola her ortamda değiştirilmelidir; demo/kısa parolalar personel kuralıyla aynı ayara bağlıdır.
        var degistirmeli = uye.PortalSifreGecici ||
                           (ayarlar.GetValue("Guvenlik:VarsayilanSifreDegisimiZorunlu", true) && sonuc.SifreDegistirmeGerekli);
        await baglam.SignInAsync(UyeOturumu.Sema, UyeOturumu.Olustur(uye, degistirmeli));
        return Results.Ok(new UyeOturumDto(uye.Id, uye.OgrenciNo, uye.TamAd, degistirmeli));
    }

    private static async Task<IResult> SifreDegistirAsync(
        UyeSifreDegistirIstegi istek, System.Security.Claims.ClaimsPrincipal kullanici,
        UyeHesapService hesap, KutuphaneDbContext db, HttpContext baglam)
    {
        var sonuc = await hesap.SifreDegistirAsync(db, kullanici.UyeId(),
            istek.MevcutSifre ?? string.Empty, istek.YeniSifre ?? string.Empty);
        if (!sonuc.Basarili) return Yanit.Hata(sonuc.Mesaj);

        var uye = await db.Uyeler.FindAsync(kullanici.UyeId());
        if (uye == null) return Yanit.Bulunamadi("Üye bulunamadı.");
        await baglam.SignInAsync(UyeOturumu.Sema, UyeOturumu.Olustur(uye, false));
        return Results.Ok(new UyeOturumDto(uye.Id, uye.OgrenciNo, uye.TamAd, false));
    }
}
