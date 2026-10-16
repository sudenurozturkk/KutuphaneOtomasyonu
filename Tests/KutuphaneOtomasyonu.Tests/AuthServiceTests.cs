using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();
    private readonly ElleZaman _zaman = new(new DateTime(2026, 10, 1, 9, 0, 0));

    public void Dispose() => _db.Dispose();

    private async Task<Kullanici> KullaniciEkle(string sifreOzeti, KullaniciRolu rol = KullaniciRolu.Kutuphaneci)
    {
        using var context = _db.YeniContext();
        return await TestVeri.EkleAsync(context, new Kullanici
        {
            Ad = "Test", Soyad = "Personel", KullaniciAdi = "personel", SifreHash = sifreOzeti, Rol = rol
        });
    }

    [Fact]
    public async Task Giris_EskiOzetiDogrularVePbkdf2IleYeniden_Ozetler()
    {
        var kullanici = await KullaniciEkle(SifreHasher.EskiOzet("parola"));
        var auth = new AuthService(_zaman);

        using (var context = _db.YeniContext())
            Assert.True((await auth.GirisDetayliAsync("personel", "parola", context)).Basarili);

        using var dogrulama = _db.YeniContext();
        var kayit = await dogrulama.Kullanicilar.SingleAsync(k => k.Id == kullanici.Id);
        Assert.StartsWith("PBKDF2-SHA256$", kayit.SifreHash);
        Assert.False(SifreHasher.YukseltmeGerekli(kayit.SifreHash));
        Assert.True(SifreHasher.Dogrula("parola", kayit.SifreHash));
        Assert.NotNull(kayit.SonGirisTarihi);
        Assert.Single(await dogrulama.AktiviteLoglar.Where(l => l.Islem == DenetimService.Islemler.GirisBasarili).ToListAsync());
        Assert.Equal(kullanici.Id, auth.AktifKullanici?.Id);
    }

    [Fact]
    public async Task Giris_YanlisSifreVePasifKullaniciReddedilir_BilinmeyenAdKaydedilmez()
    {
        await KullaniciEkle(SifreHasher.Hashle("dogru", 1_000));
        var auth = new AuthService(_zaman);
        using var context = _db.YeniContext();

        Assert.False((await auth.GirisDetayliAsync("personel", "yanlis", context)).Basarili);
        Assert.False((await auth.GirisDetayliAsync("yok-boyle-biri", "dogru", context)).Basarili);
        Assert.Null(auth.AktifKullanici);

        var loglar = await context.AktiviteLoglar.Where(l => l.Islem == DenetimService.Islemler.GirisBasarisiz).ToListAsync();
        Assert.Equal(2, loglar.Count);
        Assert.DoesNotContain(loglar, l => l.Detay.Contains("yok-boyle-biri"));
    }

    [Fact]
    public async Task Giris_ArtArdaHatalardanSonraGeciciKilitlenir_SureBitinceAcilir()
    {
        await KullaniciEkle(SifreHasher.Hashle("dogru", 1_000));
        var auth = new AuthService(_zaman);
        using var context = _db.YeniContext();

        for (var i = 0; i < AuthService.MaksimumHataliDeneme; i++)
            await auth.GirisDetayliAsync("personel", "yanlis", context);

        var kilitli = await auth.GirisDetayliAsync("personel", "dogru", context);
        Assert.False(kilitli.Basarili);
        Assert.Contains("saniye", kilitli.Mesaj);

        _zaman.Ilerlet(AuthService.KilitSuresi + TimeSpan.FromSeconds(1));
        Assert.True((await auth.GirisDetayliAsync("personel", "dogru", context)).Basarili);
    }

    [Fact]
    public async Task SifreDegistir_KurallariUygular_YeniSifreyleGirisYapilir()
    {
        await KullaniciEkle(SifreHasher.Hashle("eski-sifre", 1_000));
        var auth = new AuthService(_zaman);
        using (var context = _db.YeniContext())
        {
            Assert.False((await auth.SifreDegistirAsync(context, "eski-sifre", "yeni-sifre-1")).Basarili); // oturum yok
            Assert.True((await auth.GirisDetayliAsync("personel", "eski-sifre", context)).Basarili);

            Assert.Contains("Mevcut", (await auth.SifreDegistirAsync(context, "yanlis", "yeni-sifre-1")).Mesaj);
            Assert.Contains("en az", (await auth.SifreDegistirAsync(context, "eski-sifre", "kisa")).Mesaj);
            Assert.Contains("aynı olamaz", (await auth.SifreDegistirAsync(context, "eski-sifre", "eski-sifre")).Mesaj);
            Assert.Contains("kullanıcı adı", (await auth.SifreDegistirAsync(context, "eski-sifre", "PERSONEL")).Mesaj);
            Assert.True((await auth.SifreDegistirAsync(context, "eski-sifre", "yeni-sifre-1")).Basarili);
        }

        var yeniOturum = new AuthService(_zaman);
        using var dogrulama = _db.YeniContext();
        Assert.False((await yeniOturum.GirisDetayliAsync("personel", "eski-sifre", dogrulama)).Basarili);
        Assert.True((await yeniOturum.GirisDetayliAsync("personel", "yeni-sifre-1", dogrulama)).Basarili);
        Assert.Single(await dogrulama.AktiviteLoglar.Where(l => l.Islem == DenetimService.Islemler.SifreDegistirildi).ToListAsync());
    }

    [Fact]
    public async Task YetkiGerektir_RolHiyerarsisineUyar()
    {
        await KullaniciEkle(SifreHasher.Hashle("dogru", 1_000), KullaniciRolu.Kutuphaneci);
        var auth = new AuthService(_zaman);

        Assert.Throws<UnauthorizedAccessException>(() => auth.YetkiGerektir(KullaniciRolu.YardimciPersonel));

        using var context = _db.YeniContext();
        await auth.GirisDetayliAsync("personel", "dogru", context);

        auth.YetkiGerektir(KullaniciRolu.Kutuphaneci);
        Assert.Throws<UnauthorizedAccessException>(() => auth.YetkiGerektir(KullaniciRolu.Admin));
    }
}
