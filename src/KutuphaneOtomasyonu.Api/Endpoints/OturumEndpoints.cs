using System.Security.Claims;
using KutuphaneOtomasyonu.Api.Altyapi;
using KutuphaneOtomasyonu.Api.Dto;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using Microsoft.AspNetCore.Authentication;

namespace KutuphaneOtomasyonu.Api.Endpoints;

public static class OturumEndpoints
{
    public const string GirisHizSiniri = "giris";

    public static RouteGroupBuilder MapOturum(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api/oturum").WithTags("Oturum");

        grup.MapPost("/giris", GirisAsync)
            .AllowAnonymous()
            .RequireRateLimiting(GirisHizSiniri)
            .WithSummary("Personel girişi; başarılıysa oturum çerezi verir.");

        grup.MapPost("/cikis", async (HttpContext baglam) =>
            {
                await baglam.SignOutAsync(Oturum.Sema);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithSummary("Oturumu kapatır.");

        grup.MapGet("/ben", (ClaimsPrincipal kullanici) => Results.Ok(OturumBilgisi(kullanici)))
            .RequireAuthorization()
            .WithSummary("Oturumdaki kullanıcı.");

        grup.MapPost("/sifre", SifreDegistirAsync)
            .RequireAuthorization()
            .WithSummary("Oturumdaki kullanıcının parolasını değiştirir.");

        return grup;
    }

    private static async Task<IResult> GirisAsync(
        GirisIstegi istek, KimlikDogrulamaService dogrulayici, KutuphaneDbContext db,
        IConfiguration ayarlar, HttpContext baglam)
    {
        if (string.IsNullOrWhiteSpace(istek.KullaniciAdi) || string.IsNullOrEmpty(istek.Sifre))
            return Yanit.Hata("Kullanıcı adı ve şifre zorunludur.");

        var (sonuc, kullanici) = await dogrulayici.DogrulaAsync(istek.KullaniciAdi, istek.Sifre, db);
        if (!sonuc.Basarili || kullanici == null)
            return Yanit.Hata(sonuc.Mesaj, StatusCodes.Status401Unauthorized);

        var zorunlu = ayarlar.GetValue("Guvenlik:VarsayilanSifreDegisimiZorunlu", true) && sonuc.SifreDegistirmeGerekli;
        var principal = Oturum.Olustur(kullanici, zorunlu);
        await baglam.SignInAsync(Oturum.Sema, principal);
        return Results.Ok(OturumBilgisi(principal));
    }

    private static async Task<IResult> SifreDegistirAsync(
        SifreDegistirIstegi istek, ClaimsPrincipal kullanici, KimlikDogrulamaService dogrulayici,
        KutuphaneDbContext db, HttpContext baglam)
    {
        var sonuc = await dogrulayici.SifreDegistirAsync(
            db, kullanici.KullaniciId(), istek.MevcutSifre ?? string.Empty, istek.YeniSifre ?? string.Empty);
        if (!sonuc.Basarili) return Yanit.Hata(sonuc.Mesaj);

        // Parola değişti: zorunlu değişim işareti kalkmış yeni bir oturum çerezi verilir.
        var guncel = await db.Kullanicilar.FindAsync(kullanici.KullaniciId());
        var principal = Oturum.Olustur(guncel!, sifreDegistirmeli: false);
        await baglam.SignInAsync(Oturum.Sema, principal);
        return Results.Ok(OturumBilgisi(principal));
    }

    private static OturumDto OturumBilgisi(ClaimsPrincipal p) => new(
        p.KullaniciId(),
        p.Identity?.Name ?? string.Empty,
        p.FindFirstValue(Oturum.AdSoyadTalebi) ?? string.Empty,
        p.Rol(),
        p.SifreDegistirmeli());
}
