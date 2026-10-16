using Microsoft.AspNetCore.Diagnostics;

namespace KutuphaneOtomasyonu.Api.Altyapi;

/// <summary>Hata yanıtları RFC 9457 "problem details" biçimindedir; kullanıcıya gösterilecek mesaj <c>detail</c> alanındadır.</summary>
public static class Yanit
{
    public static IResult Hata(string mesaj, int durum = StatusCodes.Status400BadRequest) =>
        Results.Problem(title: "İşlem yapılamadı", detail: mesaj, statusCode: durum);

    public static IResult Bulunamadi(string mesaj = "Kayıt bulunamadı.") =>
        Hata(mesaj, StatusCodes.Status404NotFound);
}

/// <summary>Servis katmanındaki yetki savunması (<see cref="UnauthorizedAccessException"/>) 403 olarak döner.</summary>
internal sealed class YetkiHatasiIsleyici : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public YetkiHatasiIsleyici(IProblemDetailsService problemDetails) => _problemDetails = problemDetails;

    public async ValueTask<bool> TryHandleAsync(HttpContext baglam, Exception hata, CancellationToken iptal)
    {
        if (hata is not UnauthorizedAccessException) return false;

        baglam.Response.StatusCode = StatusCodes.Status403Forbidden;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = baglam,
            ProblemDetails = { Title = "Yetkisiz işlem", Detail = hata.Message, Status = StatusCodes.Status403Forbidden },
        });
    }
}

/// <summary>Tarayıcı arayüzü için temel güvenlik başlıkları.</summary>
internal static class GuvenlikBasliklari
{
    private const string IcerikPolitikasi =
        "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; " +
        "connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public static IApplicationBuilder UseGuvenlikBasliklari(this IApplicationBuilder app) =>
        app.Use(async (baglam, sonraki) =>
        {
            var basliklar = baglam.Response.Headers;
            basliklar.XContentTypeOptions = "nosniff";
            basliklar.XFrameOptions = "DENY";
            basliklar["Referrer-Policy"] = "no-referrer";
            // Swagger arayüzü satır içi betik kullandığı için içerik politikası yalnızca uygulama sayfalarına uygulanır.
            if (!baglam.Request.Path.StartsWithSegments("/swagger"))
                basliklar.ContentSecurityPolicy = IcerikPolitikasi;
            await sonraki();
        });
}
