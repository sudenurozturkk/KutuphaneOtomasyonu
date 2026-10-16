using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class OduncServiceTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();
    private readonly ElleZaman _zaman = new(new DateTime(2026, 10, 1, 10, 0, 0));

    public void Dispose() => _db.Dispose();

    private OduncService Servis(KutuphaneDbContext context, SistemAyarlari? ayarlar = null) =>
        TestVeri.OduncService(context, ayarlar, _zaman);

    private static async Task StokDegismeziniDogrula(KutuphaneDbContext context, int kitapId)
    {
        var kitap = await context.Kitaplar.AsNoTracking().SingleAsync(k => k.Id == kitapId);
        var aktif = await context.Oduncler.CountAsync(o => o.KitapId == kitapId && o.Durumu == OduncDurumu.Aktif);
        Assert.Equal(kitap.ToplamAdet - kitap.MevcutAdet, aktif);
    }

    [Fact]
    public async Task OduncVer_BasariliIslemStokuDusurur_AyarlardakiUzatmaHakkiniVeUyeSuresiniKullanir()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2));
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye(maxSure: 30));

        var sonuc = await Servis(context, TestVeri.Ayarlar(maxUzatma: 4)).OduncVerAsync(kitap.Id, uye.Id, 0);

        Assert.True(sonuc.Basarili, sonuc.Mesaj);
        Assert.Equal(1, (await context.Kitaplar.AsNoTracking().SingleAsync(k => k.Id == kitap.Id)).MevcutAdet);
        Assert.Equal(4, sonuc.Odunc!.MaxUzatmaSayisi);
        Assert.Equal(_zaman.Simdi.AddDays(30), sonuc.Odunc.PlanlananIadeTarihi);
        Assert.Single(await context.Bildirimler.Where(b => b.UyeId == uye.Id).ToListAsync());
        Assert.Single(await context.AktiviteLoglar.Where(l => l.Islem == DenetimService.Islemler.OduncVerildi).ToListAsync());
        await StokDegismeziniDogrula(context, kitap.Id);
    }

    [Fact]
    public async Task OduncVer_ArsivlenmisKitabiReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(aktif: false));
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());

        var sonuc = await Servis(context).OduncVerAsync(kitap.Id, uye.Id, 0);

        Assert.False(sonuc.Basarili);
        Assert.Contains("arşiv", sonuc.Mesaj);
        Assert.Empty(await context.Oduncler.ToListAsync());
    }

    [Fact]
    public async Task OduncVer_UyelikSuresiDolmusUyeyiReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = TestVeri.Uye();
        uye.UyelikBitisTarihi = _zaman.Simdi.AddDays(-1);
        await TestVeri.EkleAsync(context, uye);

        var sonuc = await Servis(context).OduncVerAsync(kitap.Id, uye.Id, 0);

        Assert.False(sonuc.Basarili);
        Assert.Contains("Üyelik süresi", sonuc.Mesaj);
    }

    [Theory]
    [InlineData(UyeDurumu.Pasif)]
    [InlineData(UyeDurumu.Askida)]
    [InlineData(UyeDurumu.Yasakli)]
    public async Task OduncVer_AktifOlmayanUyeyiReddeder(UyeDurumu durum)
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye(durum: durum));

        var sonuc = await Servis(context).OduncVerAsync(kitap.Id, uye.Id, 0);

        Assert.False(sonuc.Basarili);
        Assert.Equal(1, (await context.Kitaplar.AsNoTracking().SingleAsync()).MevcutAdet);
    }

    [Fact]
    public async Task OduncVer_CezaliUyeyiLimitiAsaniVeAyniKitabiIkinciKezAlaniReddeder()
    {
        using var context = _db.YeniContext();
        var kitap1 = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 3));
        var kitap2 = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 3));
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye(maxAdet: 1));
        var cezali = TestVeri.Uye();
        cezali.ToplamCeza = 10;
        await TestVeri.EkleAsync(context, cezali);
        var servis = Servis(context);

        Assert.True((await servis.OduncVerAsync(kitap1.Id, uye.Id, 0)).Basarili);
        var ayniKitap = await servis.OduncVerAsync(kitap1.Id, uye.Id, 0);
        var limit = await servis.OduncVerAsync(kitap2.Id, uye.Id, 0);
        var ceza = await servis.OduncVerAsync(kitap1.Id, cezali.Id, 0);

        Assert.False(ayniKitap.Basarili);
        Assert.False(limit.Basarili);
        Assert.Contains("limit", limit.Mesaj);
        Assert.False(ceza.Basarili);
        Assert.Contains("cezası", ceza.Mesaj);
        await StokDegismeziniDogrula(context, kitap1.Id);
    }

    [Theory]
    [InlineData(KitapDurumu.Iyi, 2, 2, 0)]
    [InlineData(KitapDurumu.Orta, 2, 2, 0)]
    [InlineData(KitapDurumu.Hasarli, 1, 1, 50)]
    [InlineData(KitapDurumu.Kayip, 1, 1, 100)]
    public async Task IadeAl_HerDurumdaStokDegismeziKorunur(KitapDurumu durum, int beklenenToplam, int beklenenMevcut, decimal beklenenCeza)
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2, fiyat: 100m));
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = Servis(context);
        var odunc = (await servis.OduncVerAsync(kitap.Id, uye.Id, 0)).Odunc!;

        var iade = await servis.IadeAlAsync(odunc.Id, durum);

        Assert.True(iade.Basarili, iade.Mesaj);
        var guncel = await context.Kitaplar.AsNoTracking().SingleAsync(k => k.Id == kitap.Id);
        Assert.Equal(beklenenToplam, guncel.ToplamAdet);
        Assert.Equal(beklenenMevcut, guncel.MevcutAdet);
        Assert.Equal(beklenenCeza, (await context.Uyeler.AsNoTracking().SingleAsync(u => u.Id == uye.Id)).ToplamCeza);
        await StokDegismeziniDogrula(context, kitap.Id);
    }

    [Fact]
    public async Task IadeAl_GecikmeCezasiTamGunUzerindenHesaplanirVeKaydedilir()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye(maxSure: 14));
        var servis = Servis(context, TestVeri.Ayarlar(gunlukCeza: 3));
        var odunc = (await servis.OduncVerAsync(kitap.Id, uye.Id, 0)).Odunc!;

        _zaman.Ilerlet(TimeSpan.FromDays(14 + 5) + TimeSpan.FromHours(6));
        var iade = await servis.IadeAlAsync(odunc.Id, KitapDurumu.Iyi);

        Assert.True(iade.Basarili);
        var kayit = Assert.Single(await context.CezaKayitlari.ToListAsync());
        Assert.Equal(15m, kayit.Tutar);
        Assert.Equal(CezaTipi.Gecikme, kayit.Tipi);
        Assert.Equal(15m, (await context.Uyeler.AsNoTracking().SingleAsync(u => u.Id == uye.Id)).ToplamCeza);
    }

    [Fact]
    public async Task IadeAl_IkinciKezIadeyiReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = Servis(context);
        var odunc = (await servis.OduncVerAsync(kitap.Id, uye.Id, 0)).Odunc!;

        Assert.True((await servis.IadeAlAsync(odunc.Id, KitapDurumu.Iyi)).Basarili);
        var ikinci = await servis.IadeAlAsync(odunc.Id, KitapDurumu.Iyi);

        Assert.False(ikinci.Basarili);
        Assert.Equal(1, (await context.Kitaplar.AsNoTracking().SingleAsync()).MevcutAdet);
    }

    [Fact]
    public async Task IadeAl_KopyayiSiradakiUyeyeAyirir_BaskasiAlamaz_SiradakiAlabilir()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 1));
        var okuyan = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var bekleyen = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var baskasi = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = Servis(context, TestVeri.Ayarlar(rezervasyonGun: 3));
        var odunc = (await servis.OduncVerAsync(kitap.Id, okuyan.Id, 0)).Odunc!;
        var rezervasyon = await TestVeri.EkleAsync(context, new Rezervasyon
        {
            KitapId = kitap.Id,
            UyeId = bekleyen.Id,
            RezervasyonTarihi = _zaman.Simdi,
            GecerlilikTarihi = _zaman.Simdi.AddDays(1),
            Durumu = RezervasyonDurumu.Bekliyor
        });

        _zaman.Ilerlet(TimeSpan.FromHours(12));
        Assert.True((await servis.IadeAlAsync(odunc.Id, KitapDurumu.Iyi)).Basarili);

        var ayrilan = await context.Rezervasyonlar.AsNoTracking().SingleAsync(r => r.Id == rezervasyon.Id);
        Assert.Equal(RezervasyonDurumu.Onaylandi, ayrilan.Durumu);
        Assert.Equal(_zaman.Simdi.AddDays(3), ayrilan.GecerlilikTarihi);

        var baskasiSonuc = await servis.OduncVerAsync(kitap.Id, baskasi.Id, 0);
        Assert.False(baskasiSonuc.Basarili);
        Assert.Contains("ayrılmış", baskasiSonuc.Mesaj);

        var bekleyenSonuc = await servis.OduncVerAsync(kitap.Id, bekleyen.Id, 0);
        Assert.True(bekleyenSonuc.Basarili, bekleyenSonuc.Mesaj);
        Assert.Equal(RezervasyonDurumu.Tamamlandi,
            (await context.Rezervasyonlar.AsNoTracking().SingleAsync(r => r.Id == rezervasyon.Id)).Durumu);
        await StokDegismeziniDogrula(context, kitap.Id);
    }

    [Fact]
    public async Task IadeAl_SuresiDolanBekleyenRezervasyonuAtlarVeIsaretler()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 1));
        var okuyan = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var eski = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var yeni = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = Servis(context);
        var odunc = (await servis.OduncVerAsync(kitap.Id, okuyan.Id, 0)).Odunc!;
        var suresiDolan = await TestVeri.EkleAsync(context, new Rezervasyon
        {
            KitapId = kitap.Id, UyeId = eski.Id, RezervasyonTarihi = _zaman.Simdi.AddDays(-10),
            GecerlilikTarihi = _zaman.Simdi.AddDays(-3), Durumu = RezervasyonDurumu.Bekliyor
        });
        var gecerli = await TestVeri.EkleAsync(context, new Rezervasyon
        {
            KitapId = kitap.Id, UyeId = yeni.Id, RezervasyonTarihi = _zaman.Simdi.AddDays(-1),
            GecerlilikTarihi = _zaman.Simdi.AddDays(6), Durumu = RezervasyonDurumu.Bekliyor
        });

        Assert.True((await servis.IadeAlAsync(odunc.Id, KitapDurumu.Iyi)).Basarili);

        Assert.Equal(RezervasyonDurumu.Suresi_Doldu, (await context.Rezervasyonlar.AsNoTracking().SingleAsync(r => r.Id == suresiDolan.Id)).Durumu);
        Assert.Equal(RezervasyonDurumu.Onaylandi, (await context.Rezervasyonlar.AsNoTracking().SingleAsync(r => r.Id == gecerli.Id)).Durumu);
    }

    [Fact]
    public async Task OduncVer_AyirmaSuresiGecinceKopyaHerkeseAcilir()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 1));
        var sahibi = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var baskasi = await TestVeri.EkleAsync(context, TestVeri.Uye());
        await TestVeri.EkleAsync(context, new Rezervasyon
        {
            KitapId = kitap.Id, UyeId = sahibi.Id, RezervasyonTarihi = _zaman.Simdi,
            GecerlilikTarihi = _zaman.Simdi.AddDays(3), Durumu = RezervasyonDurumu.Onaylandi
        });
        var servis = Servis(context);

        Assert.False((await servis.OduncVerAsync(kitap.Id, baskasi.Id, 0)).Basarili);
        _zaman.Ilerlet(TimeSpan.FromDays(4));
        Assert.True((await servis.OduncVerAsync(kitap.Id, baskasi.Id, 0)).Basarili);
    }

    [Fact]
    public async Task Uzat_HakKadarUzatir_SonraVeGecikmisseReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2));
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye(maxSure: 14));
        var uye2 = await TestVeri.EkleAsync(context, TestVeri.Uye(maxSure: 14));
        var servis = Servis(context, TestVeri.Ayarlar(maxUzatma: 1, oduncSuresi: 7));
        var odunc = (await servis.OduncVerAsync(kitap.Id, uye.Id, 0)).Odunc!;
        var gecikecek = (await servis.OduncVerAsync(kitap.Id, uye2.Id, 0)).Odunc!;
        var ilkTarih = odunc.PlanlananIadeTarihi;

        var ilk = await servis.UzatAsync(odunc.Id);
        var ikinci = await servis.UzatAsync(odunc.Id);

        Assert.True(ilk.Basarili, ilk.Mesaj);
        Assert.Equal(ilkTarih.AddDays(7), (await context.Oduncler.AsNoTracking().SingleAsync(o => o.Id == odunc.Id)).PlanlananIadeTarihi);
        Assert.False(ikinci.Basarili);
        Assert.Contains("Uzatma hakkı", ikinci.Mesaj);

        _zaman.Ilerlet(TimeSpan.FromDays(15));
        var gecikmis = await servis.UzatAsync(gecikecek.Id);
        Assert.False(gecikmis.Basarili);
        Assert.Contains("Gecikmiş", gecikmis.Mesaj);
    }

    [Fact]
    public async Task Veritabani_NegatifStokVeYinelenenAktifOduncuReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2));
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());

        kitap.MevcutAdet = -1;
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        kitap = await context.Kitaplar.SingleAsync();
        kitap.MevcutAdet = 3;
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        context.Oduncler.Add(new Odunc { KitapId = kitap.Id, UyeId = uye.Id, Durumu = OduncDurumu.Aktif });
        context.Oduncler.Add(new Odunc { KitapId = kitap.Id, UyeId = uye.Id, Durumu = OduncDurumu.Aktif });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task OduncVer_EsZamanliIsteklerSonKopyayiYalnizcaBirKezVerir()
    {
        using var dosyaDb = TestVeritabani.Dosya();
        const int kopya = 3;
        const int istekSayisi = 8;
        int kitapId;
        var uyeIdleri = new List<int>();
        using (var context = dosyaDb.YeniContext())
        {
            kitapId = (await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: kopya))).Id;
            for (var i = 0; i < istekSayisi; i++)
                uyeIdleri.Add((await TestVeri.EkleAsync(context, TestVeri.Uye())).Id);
        }

        using var baslangic = new ManualResetEventSlim(false);
        var gorevler = uyeIdleri.Select(uyeId => Task.Run(async () =>
        {
            baslangic.Wait();
            using var context = dosyaDb.YeniContext();
            return await TestVeri.OduncService(context).OduncVerAsync(kitapId, uyeId, 0);
        })).ToList();
        baslangic.Set();
        var sonuclar = await Task.WhenAll(gorevler);

        Assert.Equal(kopya, sonuclar.Count(s => s.Basarili));
        using var dogrulama = dosyaDb.YeniContext();
        Assert.Equal(0, (await dogrulama.Kitaplar.SingleAsync()).MevcutAdet);
        Assert.Equal(kopya, await dogrulama.Oduncler.CountAsync(o => o.Durumu == OduncDurumu.Aktif));
    }

    [Fact]
    public async Task IadeAl_EsZamanliCiftIadeStoguBirKezArtirirVeTekCezaYazar()
    {
        using var dosyaDb = TestVeritabani.Dosya();
        int kitapId, oduncId;
        var gecmis = new ElleZaman(DateTime.Now.AddDays(-20));
        using (var context = dosyaDb.YeniContext())
        {
            kitapId = (await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2))).Id;
            var uyeId = (await TestVeri.EkleAsync(context, TestVeri.Uye(maxSure: 14))).Id;
            oduncId = (await TestVeri.OduncService(context, zaman: gecmis).OduncVerAsync(kitapId, uyeId, 0)).Odunc!.Id;
        }

        using var baslangic = new ManualResetEventSlim(false);
        var gorevler = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            baslangic.Wait();
            using var context = dosyaDb.YeniContext();
            return await TestVeri.OduncService(context).IadeAlAsync(oduncId, KitapDurumu.Iyi);
        })).ToList();
        baslangic.Set();
        var sonuclar = await Task.WhenAll(gorevler);

        Assert.Equal(1, sonuclar.Count(s => s.Basarili));
        using var dogrulama = dosyaDb.YeniContext();
        Assert.Equal(2, (await dogrulama.Kitaplar.SingleAsync()).MevcutAdet);
        Assert.Single(await dogrulama.CezaKayitlari.ToListAsync());
    }

    [Fact]
    public async Task OduncVer_AyniUyeninEsZamanliIstekleriLimitiAsamaz()
    {
        using var dosyaDb = TestVeritabani.Dosya();
        int uyeId;
        var kitapIdleri = new List<int>();
        using (var context = dosyaDb.YeniContext())
        {
            uyeId = (await TestVeri.EkleAsync(context, TestVeri.Uye(maxAdet: 2))).Id;
            for (var i = 0; i < 6; i++)
                kitapIdleri.Add((await TestVeri.EkleAsync(context, TestVeri.Kitap())).Id);
        }

        using var baslangic = new ManualResetEventSlim(false);
        var gorevler = kitapIdleri.Select(kitapId => Task.Run(async () =>
        {
            baslangic.Wait();
            using var context = dosyaDb.YeniContext();
            return await TestVeri.OduncService(context).OduncVerAsync(kitapId, uyeId, 0);
        })).ToList();
        baslangic.Set();
        var sonuclar = await Task.WhenAll(gorevler);

        Assert.Equal(2, sonuclar.Count(s => s.Basarili));
        using var dogrulama = dosyaDb.YeniContext();
        Assert.Equal(2, await dogrulama.Oduncler.CountAsync(o => o.UyeId == uyeId && o.Durumu == OduncDurumu.Aktif));
    }
}
