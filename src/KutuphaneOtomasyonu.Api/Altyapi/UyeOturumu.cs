using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Api.Altyapi;

/// <summary>
/// Üye (öğrenci) portalı oturumu. Personel oturumundan ayrı bir çerez şemasıdır: personel uç noktaları yalnızca
/// varsayılan (personel) şemasını doğruladığından üye çerezi personel yetkisi kazandırmaz, tersi de geçerlidir.
/// Her istekte üyenin hâlâ giriş yapabildiği ve parolasının değişmediği (parola damgası) denetlenir.
/// </summary>
public static class UyeOturumu
{
    public const string Sema = "UyeOturumu";
    public const string CerezAdi = "kutuphane.uye.oturum";
    public const string UyeIdTalebi = "uye_id";
    public const string SifreDegistirTalebi = "uye_sifre_degistir";
    public const string SifreDamgasiTalebi = "uye_sifre_damgasi";

    public static class Politikalar
    {
        /// <summary>Parolasını değiştirmesi gerekmeyen, oturum açmış üye.</summary>
        public const string Uye = "Uye";
        /// <summary>Oturum açmış her üye (zorunlu parola değişimi bekleyenler dahil).</summary>
        public const string UyeOturum = "UyeOturum";
    }

    public static ClaimsPrincipal Olustur(Uye uye, bool sifreDegistirmeli)
    {
        var talepler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, uye.Id.ToString()),
            new(ClaimTypes.Name, string.IsNullOrEmpty(uye.OgrenciNo) ? uye.Email : uye.OgrenciNo),
            new(UyeIdTalebi, uye.Id.ToString()),
            new(ClaimTypes.GivenName, uye.TamAd),
            new(SifreDamgasiTalebi, SifreDamgasi(uye.PortalSifreHash)),
        };
        if (sifreDegistirmeli)
            talepler.Add(new Claim(SifreDegistirTalebi, "1"));
        return new ClaimsPrincipal(new ClaimsIdentity(talepler, Sema));
    }

    /// <summary>Parola özetinden türetilen kısa damga; parola değişince eski oturumlar geçersiz olur.</summary>
    public static string SifreDamgasi(string ozet) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ozet ?? string.Empty)))[..16];

    public static int UyeId(this ClaimsPrincipal kullanici) =>
        int.Parse(kullanici.FindFirstValue(UyeIdTalebi)!);

    public static bool UyeSifreDegistirmeli(this ClaimsPrincipal kullanici) =>
        kullanici.HasClaim(t => t.Type == SifreDegistirTalebi);

    public static IServiceCollection AddUyeOturumu(this IServiceCollection services, IWebHostEnvironment ortam)
    {
        services.AddAuthentication()
            .AddCookie(Sema, secenekler =>
            {
                secenekler.Cookie.Name = CerezAdi;
                secenekler.Cookie.HttpOnly = true;
                secenekler.Cookie.SameSite = SameSiteMode.Strict;
                secenekler.Cookie.SecurePolicy = ortam.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                secenekler.ExpireTimeSpan = TimeSpan.FromHours(8);
                secenekler.SlidingExpiration = true;
                secenekler.Events.OnRedirectToLogin = baglam =>
                {
                    baglam.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                secenekler.Events.OnRedirectToAccessDenied = baglam =>
                {
                    baglam.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                secenekler.Events.OnValidatePrincipal = GecerliligiDenetleAsync;
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politikalar.Uye, p => p
                .AddAuthenticationSchemes(Sema)
                .RequireAuthenticatedUser()
                .RequireAssertion(b => !b.User.UyeSifreDegistirmeli()))
            .AddPolicy(Politikalar.UyeOturum, p => p
                .AddAuthenticationSchemes(Sema)
                .RequireAuthenticatedUser());
        return services;
    }

    private static async Task GecerliligiDenetleAsync(CookieValidatePrincipalContext baglam)
    {
        var principal = baglam.Principal;
        var gecerli = false;
        if (principal != null && int.TryParse(principal.FindFirstValue(UyeIdTalebi), out var id))
        {
            var db = baglam.HttpContext.RequestServices.GetRequiredService<KutuphaneDbContext>();
            var uye = await db.Uyeler.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            gecerli = uye != null && UyeHesapService.GirisYapabilirMi(uye) &&
                      principal.FindFirstValue(SifreDamgasiTalebi) == SifreDamgasi(uye.PortalSifreHash);
        }

        if (!gecerli)
        {
            baglam.RejectPrincipal();
            await baglam.HttpContext.SignOutAsync(Sema);
        }
    }
}
