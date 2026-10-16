using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class UyeHesapServiceTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();
    private readonly ElleZaman _zaman = new(new DateTime(2026, 10, 1, 9, 0, 0));

    public void Dispose() => _db.Dispose();

    private async Task<Uye> OgrenciEkle(string ogrenciNo = "2026123456")
    {
        using var context = _db.YeniContext();
        var uye = TestVeri.Uye();
        uye.OgrenciNo = ogrenciNo;
        return await TestVeri.EkleAsync(context, uye);
    }

    [Fact]
    public async Task GeciciSifre_IleGirisYapilir_DegistirilenekadarGeciciIsaretliKalir()
    {
        var uye = await OgrenciEkle();
        var servis = new UyeHesapService(_zaman);
        using var context = _db.YeniContext();

        var (olustur, gecici) = await servis.GeciciSifreOlusturAsync(context, uye.Id, personelId: null);
        Assert.True(olustur.BasariliMi, olustur.Mesaj);
        Assert.Equal(UyeHesapService.GeciciSifreUzunlugu, gecici!.Length);

        var (giris, girenUye) = await servis.DogrulaAsync("2026123456", gecici, context);
        Assert.True(giris.Basarili, giris.Mesaj);
        Assert.Equal(uye.Id, girenUye!.Id);
        Assert.True(girenUye.PortalSifreGecici);

        // E-posta ile de (büyük/küçük harf duyarsız) giriş yapılabilir.
        Assert.True((await servis.DogrulaAsync(uye.Email.ToUpperInvariant(), gecici, context)).Sonuc.Basarili);

        Assert.Contains("en az", (await servis.SifreDegistirAsync(context, uye.Id, gecici, "kisa")).Mesaj);
        Assert.Contains("demo", (await servis.SifreDegistirAsync(context, uye.Id, gecici, "ogrenci123")).Mesaj);
        Assert.True((await servis.SifreDegistirAsync(context, uye.Id, gecici, "Kendi-Sifrem-1")).Basarili);

        using var dogrulama = _db.YeniContext();
        var kayit = await dogrulama.Uyeler.AsNoTracking().SingleAsync(u => u.Id == uye.Id);
        Assert.False(kayit.PortalSifreGecici);
        Assert.True(SifreHasher.Dogrula("Kendi-Sifrem-1", kayit.PortalSifreHash));
        Assert.False((await servis.DogrulaAsync("2026123456", gecici, dogrulama)).Sonuc.Basarili);

        var islemler = await dogrulama.AktiviteLoglar.Select(l => l.Islem).ToListAsync();
        Assert.Contains(DenetimService.Islemler.UyePortalSifresiOlusturuldu, islemler);
        Assert.Contains(DenetimService.Islemler.UyeGirisBasarili, islemler);
        Assert.Contains(DenetimService.Islemler.UyeSifreDegistirildi, islemler);
        Assert.All(await dogrulama.AktiviteLoglar.Select(l => l.Detay).ToListAsync(), d => Assert.DoesNotContain(gecici, d));
    }

    [Fact]
    public async Task Giris_HesabiOlmayan_YanlisSifre_VeAyniOgrenciNoReddedilir()
    {
        var hesapsiz = await OgrenciEkle("2026000777");
        var servis = new UyeHesapService(_zaman);
        using var context = _db.YeniContext();

        Assert.False((await servis.DogrulaAsync("2026000777", "herhangi", context)).Sonuc.Basarili);

        var uye1 = await OgrenciEkle("2026000888");
        var uye2 = await OgrenciEkle("2026000888");
        var (_, sifre1) = await servis.GeciciSifreOlusturAsync(context, uye1.Id, null);
        await servis.GeciciSifreOlusturAsync(context, uye2.Id, null);

        // Aynı öğrenci numarasına sahip iki hesap varsa numara ile giriş belirsizdir; e-posta ile girilmelidir.
        Assert.False((await servis.DogrulaAsync("2026000888", sifre1!, context)).Sonuc.Basarili);
        Assert.True((await servis.DogrulaAsync(uye1.Email, sifre1!, context)).Sonuc.Basarili);
        Assert.False((await servis.DogrulaAsync(uye1.Email, "yanlis-sifre", context)).Sonuc.Basarili);
        Assert.NotEqual(hesapsiz.Id, uye1.Id);
    }

    [Fact]
    public async Task Giris_ArtArdaHatalardaKilitlenir_YasakliVeAnonimUyeGiremez()
    {
        var uye = await OgrenciEkle();
        var servis = new UyeHesapService(_zaman);
        using var context = _db.YeniContext();
        var (_, sifre) = await servis.GeciciSifreOlusturAsync(context, uye.Id, null);

        for (var i = 0; i < GirisKilidi.MaksimumHataliDeneme; i++)
            await servis.DogrulaAsync("2026123456", "yanlis", context);
        var kilitli = await servis.DogrulaAsync("2026123456", sifre!, context);
        Assert.False(kilitli.Sonuc.Basarili);
        Assert.Contains("saniye", kilitli.Sonuc.Mesaj);

        _zaman.Ilerlet(GirisKilidi.KilitSuresi + TimeSpan.FromSeconds(1));
        var kayit = await context.Uyeler.SingleAsync(u => u.Id == uye.Id);
        kayit.Durumu = UyeDurumu.Yasakli;
        await context.SaveChangesAsync();
        var yasakli = await servis.DogrulaAsync("2026123456", sifre!, context);
        Assert.False(yasakli.Sonuc.Basarili);
        Assert.Contains("kapalı", yasakli.Sonuc.Mesaj);
    }

    [Fact]
    public async Task Anonimlestirme_PortalHesabiniKapatir()
    {
        var uye = await OgrenciEkle();
        var servis = new UyeHesapService(_zaman);
        using var context = _db.YeniContext();
        await servis.GeciciSifreOlusturAsync(context, uye.Id, null);

        var admin = await TestVeri.EkleAsync(context, new Kullanici
        {
            Ad = "A", Soyad = "B", KullaniciAdi = "yonetici", SifreHash = "x", Rol = KullaniciRolu.Admin
        });
        Assert.True((await new UyeService(context).AnonimlestirAsync(uye.Id, admin)).BasariliMi);

        var kayit = await context.Uyeler.AsNoTracking().SingleAsync(u => u.Id == uye.Id);
        Assert.False(kayit.PortalHesabiVar);
        Assert.False((await servis.GeciciSifreOlusturAsync(context, uye.Id, null)).Sonuc.BasariliMi);
    }
}
