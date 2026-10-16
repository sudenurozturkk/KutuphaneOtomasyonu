using System.Security.Claims;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Api.Altyapi;

/// <summary>
/// Personel oturumu: HttpOnly, SameSite=Strict çerez. Her istekte kullanıcının hâlâ aktif olduğu ve rolünün
/// değişmediği veritabanından doğrulanır; pasifleştirilen personelin oturumu hemen düşer.
/// </summary>
public static class Oturum
{
    public const string Sema = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string CerezAdi = "kutuphane.oturum";

    /// <summary>Varsayılan/kısa parolayla giriş yapan kullanıcı, parolasını değiştirene kadar yalnızca oturum uçlarını kullanabilir.</summary>
    public const string SifreDegistirTalebi = "sifre_degistir";
    public const string AdSoyadTalebi = "ad_soyad";

    public static class Politikalar
    {
        /// <summary>Her rol (Yardımcı Personel, Kütüphaneci, Yönetici).</summary>
        public const string Personel = "Personel";
        /// <summary>Kütüphaneci ve Yönetici.</summary>
        public const string Kutuphaneci = "Kutuphaneci";
        public const string Yonetici = "Yonetici";
    }

    public static ClaimsPrincipal Olustur(Kullanici kullanici, bool sifreDegistirmeli)
    {
        var talepler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new(ClaimTypes.Name, kullanici.KullaniciAdi),
            new(ClaimTypes.Role, kullanici.Rol.ToString()),
            new(AdSoyadTalebi, $"{kullanici.Ad} {kullanici.Soyad}".Trim()),
        };
        if (sifreDegistirmeli)
            talepler.Add(new Claim(SifreDegistirTalebi, "1"));
        return new ClaimsPrincipal(new ClaimsIdentity(talepler, Sema));
    }

    public static int KullaniciId(this ClaimsPrincipal kullanici) =>
        int.Parse(kullanici.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static KullaniciRolu Rol(this ClaimsPrincipal kullanici) =>
        Enum.Parse<KullaniciRolu>(kullanici.FindFirstValue(ClaimTypes.Role)!);

    public static bool SifreDegistirmeli(this ClaimsPrincipal kullanici) =>
        kullanici.HasClaim(t => t.Type == SifreDegistirTalebi);

    public static IServiceCollection AddKutuphaneOturumu(this IServiceCollection services, IWebHostEnvironment ortam)
    {
        services.AddAuthentication(Sema).AddCookie(Sema, secenekler =>
        {
            secenekler.Cookie.Name = CerezAdi;
            secenekler.Cookie.HttpOnly = true;
            secenekler.Cookie.SameSite = SameSiteMode.Strict;
            secenekler.Cookie.SecurePolicy = ortam.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            secenekler.ExpireTimeSpan = TimeSpan.FromHours(8);
            secenekler.SlidingExpiration = true;

            // API istemcileri yönlendirme değil durum kodu bekler.
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
            .AddPolicy(Politikalar.Personel, p => p
                .RequireAuthenticatedUser()
                .RequireAssertion(b => !b.User.SifreDegistirmeli()))
            .AddPolicy(Politikalar.Kutuphaneci, p => p
                .RequireRole(nameof(KullaniciRolu.Kutuphaneci), nameof(KullaniciRolu.Admin))
                .RequireAssertion(b => !b.User.SifreDegistirmeli()))
            .AddPolicy(Politikalar.Yonetici, p => p
                .RequireRole(nameof(KullaniciRolu.Admin))
                .RequireAssertion(b => !b.User.SifreDegistirmeli()));

        return services;
    }

    private static async Task GecerliligiDenetleAsync(CookieValidatePrincipalContext baglam)
    {
        var principal = baglam.Principal;
        var gecerli = false;
        if (principal != null && int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            var db = baglam.HttpContext.RequestServices.GetRequiredService<KutuphaneDbContext>();
            var kullanici = await db.Kullanicilar.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
            gecerli = kullanici is { Aktif: true } && kullanici.Rol.ToString() == principal.FindFirstValue(ClaimTypes.Role);
        }

        if (!gecerli)
        {
            baglam.RejectPrincipal();
            await baglam.HttpContext.SignOutAsync(Sema);
        }
    }
}
