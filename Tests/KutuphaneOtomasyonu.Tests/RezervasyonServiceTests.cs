using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class RezervasyonServiceTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();
    private readonly ElleZaman _zaman = new(new DateTime(2026, 10, 1, 10, 0, 0));

    public void Dispose() => _db.Dispose();

    private RezervasyonService Servis(KutuphaneDbContext context) =>
        new(context, new BildirimService(context), TestVeri.Ayarlar(rezervasyonGun: 3), _zaman);

    private Task<Rezervasyon> RezervasyonEkle(KutuphaneDbContext context, int kitapId, int uyeId,
        RezervasyonDurumu durum, int gecerlilikGun, int dakikaOnce = 0) =>
        TestVeri.EkleAsync(context, new Rezervasyon
        {
            KitapId = kitapId,
            UyeId = uyeId,
            RezervasyonTarihi = _zaman.Simdi.AddMinutes(-dakikaOnce),
            GecerlilikTarihi = _zaman.Simdi.AddDays(gecerlilikGun),
            Durumu = durum
        });

    private static async Task<RezervasyonDurumu> Durum(KutuphaneDbContext context, int id) =>
        (await context.Rezervasyonlar.AsNoTracking().SingleAsync(r => r.Id == id)).Durumu;

    [Fact]
    public async Task Olustur_SiraNumarasiVerir_YineleneniReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye1 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var uye2 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = Servis(context);

        var ilk = await servis.OlusturAsync(kitap.Id, uye1.Id, " not ", null);
        var ikinci = await servis.OlusturAsync(kitap.Id, uye2.Id, null, null);
        var yinelenen = await servis.OlusturAsync(kitap.Id, uye1.Id, null, null);

        Assert.True(ilk.BasariliMi);
        Assert.Equal(1, ilk.Rezervasyon!.SiraNumarasi);
        Assert.Equal("not", ilk.Rezervasyon.Notlar);
        Assert.Equal(_zaman.Simdi.AddDays(RezervasyonService.BeklemeSuresiGun), ilk.Rezervasyon.GecerlilikTarihi);
        Assert.Equal(2, ikinci.Rezervasyon!.SiraNumarasi);
        Assert.False(yinelenen.BasariliMi);
    }

    [Fact]
    public async Task Olustur_ArsivKitabi_PasifUyeyi_VeKitabiZatenAlmisUyeyiReddeder()
    {
        using var context = _db.YeniContext();
        var arsiv = await TestVeri.EkleAsync(context, TestVeri.Kitap(aktif: false));
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2));
        var pasif = await TestVeri.EkleAsync(context, TestVeri.Uye(durum: UyeDurumu.Pasif));
        var okuyan = await TestVeri.EkleAsync(context, TestVeri.Uye());
        Assert.True((await TestVeri.OduncService(context, zaman: _zaman).OduncVerAsync(kitap.Id, okuyan.Id, 0)).Basarili);
        var servis = Servis(context);

        Assert.False((await servis.OlusturAsync(arsiv.Id, okuyan.Id, null, null)).BasariliMi);
        Assert.False((await servis.OlusturAsync(kitap.Id, pasif.Id, null, null)).BasariliMi);
        Assert.False((await servis.OlusturAsync(kitap.Id, okuyan.Id, null, null)).BasariliMi);
    }

    [Fact]
    public async Task IptalEt_AyrilmisKopyayiSiradakineGecirir_TamamlanmisiReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye1 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var uye2 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var ayrilmis = await RezervasyonEkle(context, kitap.Id, uye1.Id, RezervasyonDurumu.Onaylandi, 2, dakikaOnce: 30);
        var bekleyen = await RezervasyonEkle(context, kitap.Id, uye2.Id, RezervasyonDurumu.Bekliyor, 5, dakikaOnce: 10);
        var tamamlanan = await RezervasyonEkle(context, kitap.Id, uye2.Id, RezervasyonDurumu.Tamamlandi, 5);
        var servis = Servis(context);

        Assert.True((await servis.IptalEtAsync(ayrilmis.Id, null)).BasariliMi);
        Assert.False((await servis.IptalEtAsync(tamamlanan.Id, null)).BasariliMi);

        Assert.Equal(RezervasyonDurumu.Iptal, await Durum(context, ayrilmis.Id));
        Assert.Equal(RezervasyonDurumu.Onaylandi, await Durum(context, bekleyen.Id));
        Assert.Equal(RezervasyonDurumu.Tamamlandi, await Durum(context, tamamlanan.Id));
    }

    [Fact]
    public async Task OdunceDonustur_TekIslemdeOduncVerirVeRezervasyonuTamamlar()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var rezervasyon = await RezervasyonEkle(context, kitap.Id, uye.Id, RezervasyonDurumu.Onaylandi, 2);

        var sonuc = await Servis(context).OdunceDonusturAsync(rezervasyon.Id, 0);

        Assert.True(sonuc.Basarili, sonuc.Mesaj);
        Assert.Equal(RezervasyonDurumu.Tamamlandi, await Durum(context, rezervasyon.Id));
        Assert.Equal(0, (await context.Kitaplar.AsNoTracking().SingleAsync()).MevcutAdet);
        Assert.Single(await context.AktiviteLoglar.Where(l => l.Islem == DenetimService.Islemler.RezervasyonOdunceDonustu).ToListAsync());
    }

    [Fact]
    public async Task OdunceDonustur_OduncReddedilirseHicbirSeyDegismez()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = TestVeri.Uye();
        uye.ToplamCeza = 5;
        await TestVeri.EkleAsync(context, uye);
        var rezervasyon = await RezervasyonEkle(context, kitap.Id, uye.Id, RezervasyonDurumu.Bekliyor, 2);

        var sonuc = await Servis(context).OdunceDonusturAsync(rezervasyon.Id, 0);

        Assert.False(sonuc.Basarili);
        Assert.Equal(RezervasyonDurumu.Bekliyor, await Durum(context, rezervasyon.Id));
        Assert.Empty(await context.Oduncler.ToListAsync());
        Assert.Equal(1, (await context.Kitaplar.AsNoTracking().SingleAsync()).MevcutAdet);
    }

    [Fact]
    public async Task OdunceDonustur_SuresiDolmusRezervasyonuReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var rezervasyon = await RezervasyonEkle(context, kitap.Id, uye.Id, RezervasyonDurumu.Bekliyor, -1);

        Assert.False((await Servis(context).OdunceDonusturAsync(rezervasyon.Id, 0)).Basarili);
    }

    [Fact]
    public async Task SuresiDolanlariIsle_HerDolanAyirmaIcinFarkliBirSiradakiniOnaylar()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2));
        var uyeler = new List<Uye>();
        for (var i = 0; i < 4; i++)
            uyeler.Add(await TestVeri.EkleAsync(context, TestVeri.Uye()));
        var dolan1 = await RezervasyonEkle(context, kitap.Id, uyeler[0].Id, RezervasyonDurumu.Onaylandi, -1, dakikaOnce: 40);
        var dolan2 = await RezervasyonEkle(context, kitap.Id, uyeler[1].Id, RezervasyonDurumu.Onaylandi, -1, dakikaOnce: 30);
        var sira1 = await RezervasyonEkle(context, kitap.Id, uyeler[2].Id, RezervasyonDurumu.Bekliyor, 5, dakikaOnce: 20);
        var sira2 = await RezervasyonEkle(context, kitap.Id, uyeler[3].Id, RezervasyonDurumu.Bekliyor, 5, dakikaOnce: 10);

        var dolanSayisi = await Servis(context).SuresiDolanlariIsleAsync();

        Assert.Equal(2, dolanSayisi);
        Assert.Equal(RezervasyonDurumu.Suresi_Doldu, await Durum(context, dolan1.Id));
        Assert.Equal(RezervasyonDurumu.Suresi_Doldu, await Durum(context, dolan2.Id));
        Assert.Equal(RezervasyonDurumu.Onaylandi, await Durum(context, sira1.Id));
        Assert.Equal(RezervasyonDurumu.Onaylandi, await Durum(context, sira2.Id));
    }
}
