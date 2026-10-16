using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class KatalogVeUyeServiceTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();

    public void Dispose() => _db.Dispose();

    private static Kitap YeniKitap(string isbn = "978-0-306-40615-7", int adet = 2) => new()
    {
        Baslik = "  Veri Yapıları ",
        Yazar = "Yazar",
        ISBN = isbn,
        ToplamAdet = adet,
        YayinYili = 2020
    };

    [Fact]
    public async Task KitapEkle_IsbnNormallestirilir_MevcutAdetToplamaEsitlenir()
    {
        using var context = _db.YeniContext();
        var kitap = YeniKitap();
        kitap.MevcutAdet = 99;

        var sonuc = await new KitapService(context).EkleAsync(kitap, null);

        Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
        Assert.Equal("9780306406157", sonuc.Kitap!.ISBN);
        Assert.Equal(2, sonuc.Kitap.MevcutAdet);
        Assert.Single(await context.AktiviteLoglar.Where(l => l.Islem == DenetimService.Islemler.KitapEklendi).ToListAsync());
    }

    [Theory]
    [InlineData("978-0-306-40615-8", "ISBN geçersiz")]
    [InlineData("", null)]
    public async Task KitapEkle_IsbnKurali(string isbn, string? beklenenHata)
    {
        using var context = _db.YeniContext();

        var sonuc = await new KitapService(context).EkleAsync(YeniKitap(isbn), null);

        if (beklenenHata == null) Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
        else Assert.Contains(beklenenHata, sonuc.Mesaj);
    }

    [Fact]
    public async Task KitapEkle_ArsivdekiKayitDahilYinelenenIsbnReddedilir()
    {
        using var context = _db.YeniContext();
        var servis = new KitapService(context);
        var ilk = (await servis.EkleAsync(YeniKitap(), null)).Kitap!;
        Assert.True((await servis.DeleteAsync(ilk.Id)).BasariliMi);

        var ikinci = await servis.EkleAsync(YeniKitap("9780306406157"), null);

        Assert.False(ikinci.BasariliMi);
        Assert.Contains("arşivlenmiş", ikinci.Mesaj);
    }

    [Fact]
    public async Task KitapEkle_GecersizYilVeNegatifAdetReddedilir()
    {
        using var context = _db.YeniContext();
        var servis = new KitapService(context);
        var yil = YeniKitap();
        yil.YayinYili = 3000;
        var adet = YeniKitap(isbn: "");
        adet.ToplamAdet = -1;

        Assert.False((await servis.EkleAsync(yil, null)).BasariliMi);
        Assert.False((await servis.EkleAsync(adet, null)).BasariliMi);
    }

    [Fact]
    public async Task KitapGuncelle_ToplamOduncteSayisininAltinaInemez_MevcutYenidenHesaplanir()
    {
        using var context = _db.YeniContext();
        var servis = new KitapService(context);
        var kitap = (await servis.EkleAsync(YeniKitap(adet: 3), null)).Kitap!;
        var uye1 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var uye2 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var odunc = TestVeri.OduncService(context);
        Assert.True((await odunc.OduncVerAsync(kitap.Id, uye1.Id, 0)).Basarili);
        Assert.True((await odunc.OduncVerAsync(kitap.Id, uye2.Id, 0)).Basarili);

        var asagi = new Kitap { Id = kitap.Id, Baslik = "B", Yazar = "Y", ISBN = kitap.ISBN, ToplamAdet = 1 };
        var yukari = new Kitap { Id = kitap.Id, Baslik = "B", Yazar = "Y", ISBN = kitap.ISBN, ToplamAdet = 5 };
        var asagiSonuc = await servis.GuncelleAsync(asagi, null);
        var yukariSonuc = await servis.GuncelleAsync(yukari, null);

        Assert.False(asagiSonuc.BasariliMi);
        Assert.Contains("ödünçteki", asagiSonuc.Mesaj);
        Assert.True(yukariSonuc.BasariliMi, yukariSonuc.Mesaj);
        var guncel = await context.Kitaplar.AsNoTracking().SingleAsync();
        Assert.Equal(5, guncel.ToplamAdet);
        Assert.Equal(3, guncel.MevcutAdet);
    }

    [Fact]
    public async Task KitapGuncelle_EskiGecersizIsbnDegismedikceEngellemez()
    {
        using var context = _db.YeniContext();
        var eski = await TestVeri.EkleAsync(context, new Kitap { Baslik = "Eski", Yazar = "Y", ISBN = "9789780001000001", ToplamAdet = 1, MevcutAdet = 1 });

        var sonuc = await new KitapService(context).GuncelleAsync(
            new Kitap { Id = eski.Id, Baslik = "Yeni Başlık", Yazar = "Y", ISBN = "9789780001000001", ToplamAdet = 1 }, null);

        Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
    }

    [Fact]
    public async Task KitapArsivle_OduncteKopyaVarsaReddeder_YoksaRezervasyonlariIptalEder()
    {
        using var context = _db.YeniContext();
        var servis = new KitapService(context);
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var okuyan = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var bekleyen = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var odunc = (await TestVeri.OduncService(context).OduncVerAsync(kitap.Id, okuyan.Id, 0)).Odunc!;
        var rezervasyon = await TestVeri.EkleAsync(context, new Rezervasyon
        {
            KitapId = kitap.Id, UyeId = bekleyen.Id, GecerlilikTarihi = DateTime.Now.AddDays(5)
        });

        Assert.False((await servis.DeleteAsync(kitap.Id)).BasariliMi);
        Assert.True((await TestVeri.OduncService(context).IadeAlAsync(odunc.Id, KitapDurumu.Iyi)).Basarili);
        Assert.True((await servis.DeleteAsync(kitap.Id)).BasariliMi);

        Assert.False((await context.Kitaplar.AsNoTracking().SingleAsync()).Aktif);
        Assert.Equal(RezervasyonDurumu.Iptal, (await context.Rezervasyonlar.AsNoTracking().SingleAsync(r => r.Id == rezervasyon.Id)).Durumu);
    }

    [Fact]
    public async Task PopulerKitaplar_OduncSayisinaGoreSiralanir()
    {
        using var context = _db.YeniContext();
        var az = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var cok = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var orta = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        foreach (var (kitap, sayi) in new[] { (az, 1), (cok, 4), (orta, 2) })
            for (var i = 0; i < sayi; i++)
                context.Oduncler.Add(new Odunc { KitapId = kitap.Id, UyeId = uye.Id, Durumu = OduncDurumu.Iade });
        await context.SaveChangesAsync();

        var populer = await new KitapService(context).GetPopulerKitaplarAsync();

        Assert.Equal(new[] { cok.Id, orta.Id, az.Id }, populer.Select(k => k.Id));
    }

    private static Uye YeniUye(int no, UyeTipi tip = UyeTipi.Ogrenci) => new()
    {
        Ad = " Ada ",
        Soyad = "Deneme",
        Email = $"uye{no}@ornek.edu.tr",
        TCKimlikNo = DogrulamaVeYardimciTests.GecerliTc(no),
        UyeTipi = tip
    };

    [Theory]
    [InlineData(UyeTipi.Ogrenci, 3, 14)]
    [InlineData(UyeTipi.Misafir, 3, 14)]
    [InlineData(UyeTipi.Akademisyen, 5, 30)]
    [InlineData(UyeTipi.Personel, 5, 30)]
    public async Task UyeEkle_TipeGoreKotaAtanir(UyeTipi tip, int adet, int sure)
    {
        using var context = _db.YeniContext();

        var sonuc = await new UyeService(context).EkleAsync(YeniUye(11, tip), null);

        Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
        Assert.Equal("Ada", sonuc.Uye!.Ad);
        Assert.Equal(adet, sonuc.Uye.MaxOduncAdet);
        Assert.Equal(sure, sonuc.Uye.MaxOduncSuresi);
    }

    [Fact]
    public async Task UyeEkle_GecersizTcVeEpostaIleYinelenenKayitlarReddedilir()
    {
        using var context = _db.YeniContext();
        var servis = new UyeService(context);
        Assert.True((await servis.EkleAsync(YeniUye(1), null)).BasariliMi);

        var gecersizTc = YeniUye(2);
        gecersizTc.TCKimlikNo = "12345678901";
        var gecersizEmail = YeniUye(3);
        gecersizEmail.Email = "eksik@alan";
        var ayniEmail = YeniUye(4);
        ayniEmail.Email = "UYE1@ornek.edu.tr";
        var ayniTc = YeniUye(5);
        ayniTc.TCKimlikNo = DogrulamaVeYardimciTests.GecerliTc(1);

        Assert.Contains("TC Kimlik", (await servis.EkleAsync(gecersizTc, null)).Mesaj);
        Assert.Contains("E-posta", (await servis.EkleAsync(gecersizEmail, null)).Mesaj);
        Assert.Contains("e-posta", (await servis.EkleAsync(ayniEmail, null)).Mesaj);
        Assert.Contains("TC Kimlik No ile", (await servis.EkleAsync(ayniTc, null)).Mesaj);
        Assert.Equal(1, await context.Uyeler.CountAsync());
    }

    [Fact]
    public async Task UyeGuncelle_TipDegisinceKotaGuncellenir_EskiTcDegismedikceKabulEdilir()
    {
        using var context = _db.YeniContext();
        var eski = TestVeri.Uye();
        eski.TCKimlikNo = "00012345678"; // demo verisindeki gibi algoritmaya uymayan eski kayıt
        await TestVeri.EkleAsync(context, eski);

        var degisiklik = new Uye
        {
            Id = eski.Id, Ad = eski.Ad, Soyad = eski.Soyad, Email = eski.Email,
            TCKimlikNo = eski.TCKimlikNo, UyeTipi = UyeTipi.Akademisyen
        };
        var sonuc = await new UyeService(context).GuncelleAsync(degisiklik, null);

        Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
        Assert.Equal(5, sonuc.Uye!.MaxOduncAdet);
        Assert.Equal(30, sonuc.Uye.MaxOduncSuresi);
    }

    [Fact]
    public async Task CezaOde_TutarDogrulanir_KayitlarEskidenYeniyeKapatilir()
    {
        using var context = _db.YeniContext();
        var uye = TestVeri.Uye();
        uye.ToplamCeza = 30;
        await TestVeri.EkleAsync(context, uye);
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var odunc = await TestVeri.EkleAsync(context, new Odunc { KitapId = kitap.Id, UyeId = uye.Id, Durumu = OduncDurumu.Iade });
        context.CezaKayitlari.AddRange(
            new CezaKaydi { UyeId = uye.Id, OduncId = odunc.Id, Tutar = 20, OlusmaTarihi = DateTime.Now.AddDays(-2) },
            new CezaKaydi { UyeId = uye.Id, OduncId = odunc.Id, Tutar = 10, OlusmaTarihi = DateTime.Now.AddDays(-1) });
        await context.SaveChangesAsync();
        var servis = new UyeService(context);

        Assert.False((await servis.CezaOdeAsync(uye.Id, 0)).BasariliMi);
        Assert.False((await servis.CezaOdeAsync(uye.Id, 31)).BasariliMi);
        Assert.True((await servis.CezaOdeAsync(uye.Id, 15)).BasariliMi);

        // 20'lik en eski kayıt karşılanamadığı için sonraki 10'luk kayıt sıra dışı kapatılmaz.
        Assert.Empty(await context.CezaKayitlari.Where(c => c.Odendi).ToListAsync());
        Assert.Equal((15m, 1), await servis.GetBekleyenCezaAsync());
    }
}
