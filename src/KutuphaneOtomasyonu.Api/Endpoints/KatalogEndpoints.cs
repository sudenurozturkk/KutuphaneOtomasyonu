using System.Security.Claims;
using KutuphaneOtomasyonu.Api.Altyapi;
using KutuphaneOtomasyonu.Api.Dto;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;

namespace KutuphaneOtomasyonu.Api.Endpoints;

/// <summary>Herkese açık katalog (OPAC) ve personel için kitap yönetimi.</summary>
public static class KatalogEndpoints
{
    public const int AzamiSayfaBoyutu = 100;

    public static void MapKatalog(this IEndpointRouteBuilder app)
    {
        var katalog = app.MapGroup("/api/katalog").WithTags("Katalog").AllowAnonymous();

        katalog.MapGet("/", AraAsync).WithSummary("Kitap arama (başlık, yazar, ISBN, kategori, yayınevi).");
        katalog.MapGet("/kategoriler", async (KutuphaneDbContext db) =>
            {
                using var kapsam = await ServisKapsami.OlusturAsync(db);
                return Results.Ok(await kapsam.Kitap.GetKategorilerAsync());
            })
            .WithSummary("Katalogdaki kategoriler.");
        katalog.MapGet("/{id:int}", DetayAsync).WithSummary("Kitap ayrıntısı ve nüshaların durumu.");

        var kitaplar = app.MapGroup("/api/kitaplar").WithTags("Kitap yönetimi")
            .RequireAuthorization(Oturum.Politikalar.Kutuphaneci);

        kitaplar.MapPost("/", EkleAsync).WithSummary("Yeni kitap ve nüshalarını ekler.");
        kitaplar.MapPut("/{id:int}", GuncelleAsync).WithSummary("Kitap bilgilerini ve toplam nüsha sayısını günceller.");
        kitaplar.MapDelete("/{id:int}", ArsivleAsync).WithSummary("Kitabı arşivler (ödünçteki nüshası varsa reddedilir).");
    }

    private static async Task<IResult> AraAsync(
        KutuphaneDbContext db, string? arama, string? kategori, bool? sadeceMevcut, int sayfa = 1, int boyut = 20)
    {
        sayfa = Math.Max(1, sayfa);
        boyut = Math.Clamp(boyut, 1, AzamiSayfaBoyutu);

        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuclar = await kapsam.Kitap.SearchAsync(arama?.Trim(), kategori, null, sadeceMevcut == true ? true : null);
        var kayitlar = sonuclar.Skip((sayfa - 1) * boyut).Take(boyut).Select(KitapDto.Olustur).ToList();
        return Results.Ok(new SayfaliSonuc<KitapDto>(sonuclar.Count, sayfa, boyut, kayitlar));
    }

    private static async Task<IResult> DetayAsync(int id, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var kitap = await kapsam.Kitap.GetByIdAsync(id);
        if (kitap is not { Aktif: true }) return Yanit.Bulunamadi("Kitap bulunamadı.");

        var nushalar = kitap.Nushalar.OrderBy(n => n.Barkod).Select(NushaDto.Olustur).ToList();
        return Results.Ok(new KitapDetayDto(KitapDto.Olustur(kitap), nushalar));
    }

    private static async Task<IResult> EkleAsync(KitapIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Kitap.EkleAsync(istek.VarligaDonustur(), kullanici.KullaniciId());
        return sonuc.BasariliMi
            ? Results.Created($"/api/katalog/{sonuc.Kitap!.Id}", KitapDto.Olustur(sonuc.Kitap))
            : Yanit.Hata(sonuc.Mesaj);
    }

    private static async Task<IResult> GuncelleAsync(int id, KitapIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Kitap.GuncelleAsync(istek.VarligaDonustur(id), kullanici.KullaniciId());
        if (!sonuc.BasariliMi)
            return sonuc.Mesaj == "Kitap bulunamadı." ? Yanit.Bulunamadi(sonuc.Mesaj) : Yanit.Hata(sonuc.Mesaj);
        return Results.Ok(KitapDto.Olustur(sonuc.Kitap!));
    }

    private static async Task<IResult> ArsivleAsync(int id, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Kitap.DeleteAsync(id, kullanici.KullaniciId());
        return sonuc.BasariliMi ? Results.NoContent() : Yanit.Hata(sonuc.Mesaj);
    }
}
