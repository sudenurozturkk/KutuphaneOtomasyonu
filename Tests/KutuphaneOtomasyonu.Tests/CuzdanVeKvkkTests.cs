using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class CuzdanVeKvkkTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();
    private static readonly Kullanici Admin = new() { Id = 1, Rol = KullaniciRolu.Admin, Aktif = true };

    public CuzdanVeKvkkTests()
    {
        using var context = _db.YeniContext();
        context.Kullanicilar.Add(new Kullanici { Id = 1, Ad = "A", Soyad = "B", KullaniciAdi = "admin", SifreHash = "x", Rol = KullaniciRolu.Admin });
        context.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Cuzdan_YuklemeBakiyeyiArtirir_CezaOdemeBorcuKapatirVeHareketYazar()
    {
        using var context = _db.YeniContext();
        var uye = TestVeri.Uye();
        uye.ToplamCeza = 30;
        await TestVeri.EkleAsync(context, uye);
        var servis = new CuzdanService(context);

        Assert.False((await servis.CezaOdeAsync(uye.Id, 1)).BasariliMi); // bakiye yok
        Assert.True((await servis.YukleAsync(uye.Id, 20, 1)).BasariliMi);
        var yetersiz = await servis.CezaOdeAsync(uye.Id, 1);
        Assert.Contains("yetersiz", yetersiz.Mesaj);
        Assert.Equal(50m, (await servis.YukleAsync(uye.Id, 30, 1)).YeniBakiye);

        var odeme = await servis.CezaOdeAsync(uye.Id, 1);

        Assert.True(odeme.BasariliMi, odeme.Mesaj);
        Assert.Equal(20m, odeme.YeniBakiye);
        Assert.Equal(20m, await servis.BakiyeAsync(uye.Id));
        Assert.Equal(0m, (await context.Uyeler.AsNoTracking().SingleAsync(u => u.Id == uye.Id)).KalanCeza);
        var hareketler = await servis.HareketlerAsync(uye.Id);
        Assert.Equal(3, hareketler.Count);
        Assert.Equal(-30m, hareketler[0].Tutar);
        Assert.False((await servis.CezaOdeAsync(uye.Id, 1)).BasariliMi); // borç kalmadı
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(5001)]
    public async Task Cuzdan_GecersizYuklemeTutariniReddeder(decimal tutar)
    {
        using var context = _db.YeniContext();
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());

        Assert.False((await new CuzdanService(context).YukleAsync(uye.Id, tutar, 1)).BasariliMi);
        Assert.Empty(await context.CuzdanHareketleri.ToListAsync());
    }

    [Fact]
    public async Task Anonimlestir_KisiselVeriyiSiler_GecmisiKorur_BildirimleriVeRezervasyonlariTemizler()
    {
        using var context = _db.YeniContext();
        var uye = TestVeri.Uye();
        uye.Telefon = "05551234567";
        uye.Adres = "Örnek Mah.";
        uye.OgrenciNo = "2024000001";
        await TestVeri.EkleAsync(context, uye);
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2));
        var servis = TestVeri.OduncService(context);
        var odunc = (await servis.OduncVerAsync(kitap.Id, uye.Id, 0)).Odunc!;
        Assert.True((await servis.IadeAlAsync(odunc.Id, KitapDurumu.Iyi)).Basarili);
        await TestVeri.EkleAsync(context, new Rezervasyon { KitapId = kitap.Id, UyeId = uye.Id, GecerlilikTarihi = DateTime.Now.AddDays(3) });

        var sonuc = await new UyeService(context).AnonimlestirAsync(uye.Id, Admin);

        Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
        var kayit = await context.Uyeler.AsNoTracking().SingleAsync(u => u.Id == uye.Id);
        Assert.True(kayit.AnonimMi);
        Assert.Equal("Anonim", kayit.Ad);
        Assert.Equal(string.Empty, kayit.TCKimlikNo);
        Assert.Equal(string.Empty, kayit.Telefon);
        Assert.Equal(string.Empty, kayit.Adres);
        Assert.Equal(string.Empty, kayit.OgrenciNo);
        Assert.EndsWith("@anonim.invalid", kayit.Email);
        Assert.Equal(UyeDurumu.Pasif, kayit.Durumu);
        Assert.Empty(await context.Bildirimler.Where(b => b.UyeId == uye.Id).ToListAsync());
        Assert.Single(await context.Oduncler.Where(o => o.UyeId == uye.Id).ToListAsync());
        Assert.All(await context.Rezervasyonlar.Where(r => r.UyeId == uye.Id).ToListAsync(), r => Assert.Equal(RezervasyonDurumu.Iptal, r.Durumu));
        Assert.True(await context.AktiviteLoglar.AnyAsync(l => l.Islem == DenetimService.Islemler.UyeAnonimlestirildi));

        var tekrar = await new UyeService(context).GuncelleAsync(new Uye { Id = uye.Id, Ad = "X", Soyad = "Y", Email = "x@y.zz", TCKimlikNo = "10000000146" }, 1);
        Assert.False(tekrar.BasariliMi);
    }

    [Fact]
    public async Task Anonimlestir_YoneticiOlmayan_AktifOdunc_VeBorcVarkenReddedilir()
    {
        using var context = _db.YeniContext();
        var okuyan = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var borclu = TestVeri.Uye();
        borclu.ToplamCeza = 5;
        await TestVeri.EkleAsync(context, borclu);
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        Assert.True((await TestVeri.OduncService(context).OduncVerAsync(kitap.Id, okuyan.Id, 0)).Basarili);
        var servis = new UyeService(context);

        Assert.False((await servis.AnonimlestirAsync(okuyan.Id, new Kullanici { Id = 1, Rol = KullaniciRolu.Kutuphaneci, Aktif = true })).BasariliMi);
        Assert.Contains("aktif ödünc", (await servis.AnonimlestirAsync(okuyan.Id, Admin)).Mesaj);
        Assert.Contains("borcu", (await servis.AnonimlestirAsync(borclu.Id, Admin)).Mesaj);
    }

    [Theory]
    [InlineData(KullaniciRolu.YardimciPersonel, "•••••••0146", "•••••••4567")]
    [InlineData(KullaniciRolu.Kutuphaneci, "10000000146", "05551234567")]
    [InlineData(KullaniciRolu.Admin, "10000000146", "05551234567")]
    public void KisiselVeri_RoleGoreMaskelenir(KullaniciRolu rol, string beklenenTc, string beklenenTelefon)
    {
        var kullanici = new Kullanici { Rol = rol };

        Assert.Equal(beklenenTc, KisiselVeri.Goster("10000000146", kullanici));
        Assert.Equal(beklenenTelefon, KisiselVeri.Goster("05551234567", kullanici));
        Assert.Equal("•••", KisiselVeri.Maskele("abc"));
        Assert.Equal(string.Empty, KisiselVeri.Goster("", kullanici));
    }

    [Fact]
    public async Task Uye_PasaportIleKayitOlur_TcBosBirdenCokUyeOlabilir_PasaportBenzersizdir()
    {
        using var context = _db.YeniContext();
        var servis = new UyeService(context);
        Uye Yabanci(string pasaport, string email) =>
            new() { Ad = "Jane", Soyad = "Doe", Email = email, PasaportNo = pasaport, UyeTipi = UyeTipi.Misafir };

        var ilk = await servis.EkleAsync(Yabanci("x1234567", "jane@example.org"), null);
        var ikinci = await servis.EkleAsync(Yabanci("Y7654321", "john@example.org"), null);
        var ayniPasaport = await servis.EkleAsync(Yabanci("X1234567", "other@example.org"), null);
        var kimliksiz = await servis.EkleAsync(Yabanci("", "none@example.org"), null);
        var gecersiz = await servis.EkleAsync(Yabanci("AB-12", "bad@example.org"), null);

        Assert.True(ilk.BasariliMi, ilk.Mesaj);
        Assert.Equal("X1234567", ilk.Uye!.PasaportNo);
        Assert.True(ikinci.BasariliMi, ikinci.Mesaj);
        Assert.Contains("Pasaport No ile", ayniPasaport.Mesaj);
        Assert.Contains("veya Pasaport", kimliksiz.Mesaj);
        Assert.Contains("6–20", gecersiz.Mesaj);
        Assert.Equal(ilk.Uye.Id, (await new BankoAramaService(context).UyeBulAsync("x1234567")).Kayit?.Id);
    }

    [Theory]
    [InlineData("admin123", true)]
    [InlineData("123456", true)]
    [InlineData("kisa", true)]
    [InlineData("Yeterince-Uzun-1", false)]
    public void SifreDegistirilmeli_VarsayilanVeKisaParolalariYakalar(string sifre, bool beklenen) =>
        Assert.Equal(beklenen, AuthService.SifreDegistirilmeli(sifre));

    [Fact]
    public async Task ServisKapsami_AyarlariBirKezOkur_VerilenBaglamiKapatmaz()
    {
        using var context = _db.YeniContext();
        context.SistemAyarlari.Add(new SistemAyarlari { MaxUzatmaSayisi = 7 });
        await context.SaveChangesAsync();

        using (var kapsam = await ServisKapsami.OlusturAsync(context))
        {
            Assert.Equal(7, kapsam.Ayarlar.MaxUzatmaSayisi);
            Assert.Same(kapsam.Odunc, kapsam.Odunc);
            Assert.Same(context, kapsam.Context);
        }

        Assert.Equal(1, await context.SistemAyarlari.CountAsync()); // bağlam hâlâ kullanılabilir
    }

    [Fact]
    public async Task Salon_GunlukOzetAktifRezervasyonVeMasaSayisiniVerir()
    {
        using var context = _db.YeniContext();
        var servis = new SalonRezervasyonService(context);
        var bugun = new DateTime(2026, 10, 1);
        await servis.OlusturAsync("D-101", "Sessiz", "A", "09:00 - 11:00", "2 saat", "", bugun);
        await servis.OlusturAsync("D-101", "Sessiz", "B", "11:00 - 13:00", "2 saat", "", bugun);
        var iptal = await servis.OlusturAsync("B-201", "Bireysel", "C", "09:00 - 11:00", "2 saat", "", bugun);
        await servis.IptalEtAsync(iptal.Rezervasyon!.Id);
        await servis.OlusturAsync("G-301", "Grup", "D", "09:00 - 11:00", "2 saat", "", bugun.AddDays(1));

        Assert.Equal((2, 1), await servis.GunlukOzetAsync(bugun));
    }
}
