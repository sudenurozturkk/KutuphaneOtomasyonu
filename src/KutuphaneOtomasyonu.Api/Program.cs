using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using KutuphaneOtomasyonu.Api.Altyapi;
using KutuphaneOtomasyonu.Api.Endpoints;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Veritabanı ----------
// Yol verilmezse masaüstü uygulamasıyla aynı dosya kullanılır (%APPDATA%\KutuphaneOtomasyonu\kutuphane.db).
var ayarliYol = builder.Configuration["Veritabani:Yol"];
var veritabaniYolu = string.IsNullOrWhiteSpace(ayarliYol)
    ? KutuphaneDbContext.VeritabaniYolu
    : Path.GetFullPath(ayarliYol, builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(veritabaniYolu)!);
builder.Services.AddDbContext<KutuphaneDbContext>(o => o.UseSqlite($"Data Source={veritabaniYolu}"));

// ---------- Servisler ----------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new KimlikDogrulamaService(sp.GetRequiredService<TimeProvider>()));
builder.Services.AddKutuphaneOturumu(builder.Environment);

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Hesap kilidine ek olarak IP başına giriş denemesi sınırı (parola deneme saldırılarına karşı).
    o.AddPolicy(OturumEndpoints.GirisHizSiniri, baglam => RateLimitPartition.GetFixedWindowLimiter(
        baglam.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<YetkiHatasiIsleyici>();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((belge, _, _) =>
{
    belge.Info.Title = "Kütüphane Otomasyonu Web API";
    belge.Info.Description = "Çevrimiçi kütüphane yönetim sistemi: katalog, üyeler, ödünç/iade ve rezervasyon.";
    return Task.CompletedTask;
}));

var app = builder.Build();

// ---------- Şema ve demo verisi ----------
using (var kapsam = app.Services.CreateScope())
{
    var db = kapsam.ServiceProvider.GetRequiredService<KutuphaneDbContext>();
    if (app.Configuration.GetValue<bool>("Veritabani:DemoVerisi"))
        DatabaseSeeder.Seed(db);          // migration'lar + boş veritabanına demo verisi
    else
        VeritabaniSemasi.Hazirla(db);     // yalnızca migration'lar
    app.Logger.LogInformation("Veritabanı hazır: {Yol}", veritabaniYolu);
}

// ---------- HTTP hattı ----------
app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.UseGuvenlikBasliklari();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "Kütüphane Otomasyonu Web API");
        o.DocumentTitle = "Kütüphane Web API";
    });
}

app.MapOturum();
app.MapKatalog();
app.MapUyeler();
app.MapOduncler();
app.MapRezervasyonlarVePano();
app.MapGet("/api/saglik", () => Results.Ok(new { durum = "calisiyor" })).AllowAnonymous().WithTags("Sistem");

app.Run();

/// <summary>Entegrasyon testlerinin (WebApplicationFactory) uygulamaya erişimi için.</summary>
public partial class Program;
