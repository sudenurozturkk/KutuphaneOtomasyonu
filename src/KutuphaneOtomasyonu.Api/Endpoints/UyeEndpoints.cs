using System.Security.Claims;
using KutuphaneOtomasyonu.Api.Altyapi;
using KutuphaneOtomasyonu.Api.Dto;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;

namespace KutuphaneOtomasyonu.Api.Endpoints;

public static class UyeEndpoints
{
    public static void MapUyeler(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api/uyeler").WithTags("Üyeler")
            .RequireAuthorization(Oturum.Politikalar.Personel);

        grup.MapGet("/", AraAsync).WithSummary("Üye arama (ad, e-posta, öğrenci no, T.C.).");
        grup.MapGet("/{id:int}", DetayAsync)
            .WithSummary("Üye ayrıntısı; kimlik ve telefon Yardımcı Personel için maskelenir.");
        grup.MapPost("/", EkleAsync).RequireAuthorization(Oturum.Politikalar.Kutuphaneci)
            .WithSummary("Yeni üye kaydı (T.C./YKN veya pasaport zorunlu).");
        grup.MapPut("/{id:int}", GuncelleAsync).RequireAuthorization(Oturum.Politikalar.Kutuphaneci)
            .WithSummary("Üye bilgilerini günceller.");
        grup.MapPost("/{id:int}/ceza-ode", CezaOdeAsync).RequireAuthorization(Oturum.Politikalar.Kutuphaneci)
            .WithSummary("Bankoda alınan ceza ödemesini kaydeder (en eski borçtan başlayarak).");
    }

    private static async Task<IResult> AraAsync(
        KutuphaneDbContext db, string? arama, UyeTipi? tip, UyeDurumu? durum, int sayfa = 1, int boyut = 20)
    {
        sayfa = Math.Max(1, sayfa);
        boyut = Math.Clamp(boyut, 1, KatalogEndpoints.AzamiSayfaBoyutu);

        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var uyeler = await kapsam.Uye.SearchAsync(arama?.Trim(), tip, durum);
        var kayitlar = uyeler.Skip((sayfa - 1) * boyut).Take(boyut).Select(UyeOzetDto.Olustur).ToList();
        return Results.Ok(new SayfaliSonuc<UyeOzetDto>(uyeler.Count, sayfa, boyut, kayitlar));
    }

    private static async Task<IResult> DetayAsync(int id, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var uye = await kapsam.Uye.GetByIdAsync(id);
        return uye == null ? Yanit.Bulunamadi("Üye bulunamadı.") : Results.Ok(UyeDetayDto.Olustur(uye, kullanici.Rol()));
    }

    private static async Task<IResult> EkleAsync(UyeIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Uye.EkleAsync(istek.VarligaDonustur(), kullanici.KullaniciId());
        return sonuc.BasariliMi
            ? Results.Created($"/api/uyeler/{sonuc.Uye!.Id}", UyeOzetDto.Olustur(sonuc.Uye))
            : Yanit.Hata(sonuc.Mesaj);
    }

    private static async Task<IResult> GuncelleAsync(int id, UyeIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Uye.GuncelleAsync(istek.VarligaDonustur(id), kullanici.KullaniciId());
        if (!sonuc.BasariliMi)
            return sonuc.Mesaj == "Üye bulunamadı." ? Yanit.Bulunamadi(sonuc.Mesaj) : Yanit.Hata(sonuc.Mesaj);
        return Results.Ok(UyeOzetDto.Olustur(sonuc.Uye!));
    }

    private static async Task<IResult> CezaOdeAsync(int id, CezaOdemeIstegi istek, ClaimsPrincipal kullanici, KutuphaneDbContext db)
    {
        using var kapsam = await ServisKapsami.OlusturAsync(db);
        var sonuc = await kapsam.Uye.CezaOdeAsync(id, istek.Tutar, kullanici.KullaniciId());
        return sonuc.BasariliMi ? Results.Ok(UyeOzetDto.Olustur(sonuc.Uye!)) : Yanit.Hata(sonuc.Mesaj);
    }
}
