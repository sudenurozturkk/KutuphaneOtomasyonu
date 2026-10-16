using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class NushaVeUzatmaTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();
    private readonly ElleZaman _zaman = new(new DateTime(2026, 10, 1, 10, 0, 0));

    public void Dispose() => _db.Dispose();

    private static Kitap YeniKitap(int adet, string raf = "A-01") =>
        new() { Baslik = "Nüsha Testi", Yazar = "Y", ToplamAdet = adet, RafKonumu = raf };

    [Fact]
    public async Task KitapEkle_HerKopyaIcinSiraliBarkodluNushaOlusturur()
    {
        using var context = _db.YeniContext();

        var kitap = (await new KitapService(context).EkleAsync(YeniKitap(3), null)).Kitap!;

        var barkodlar = await context.KitapNushalari.Where(n => n.KitapId == kitap.Id).OrderBy(n => n.Id).Select(n => n.Barkod).ToListAsync();
        Assert.Equal(Enumerable.Range(1, 3).Select(i => $"KB{kitap.Id:D6}{i:D4}"), barkodlar);
        Assert.All(await context.KitapNushalari.ToListAsync(), n => Assert.Equal(NushaDurumu.Rafta, n.Durum));
    }

    [Fact]
    public async Task OduncVer_RaftakiNushayiVerir_BarkodlaBelirliNushaSecilebilir_GecersizleriReddeder()
    {
        using var context = _db.YeniContext();
        var kitap = (await new KitapService(context).EkleAsync(YeniKitap(3), null)).Kitap!;
        var baska = (await new KitapService(context).EkleAsync(YeniKitap(1), null)).Kitap!;
        var uye1 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var uye2 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var uye3 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = TestVeri.OduncService(context, zaman: _zaman);
        var ucuncu = NushaIslemleri.BarkodUret(kitap.Id, 3);

        var otomatik = await servis.OduncVerAsync(kitap.Id, uye1.Id, 0);
        var secili = await servis.OduncVerAsync(kitap.Id, uye2.Id, 0, ucuncu.ToLowerInvariant());
        var ayniNusha = await servis.OduncVerAsync(kitap.Id, uye3.Id, 0, ucuncu);
        var baskaKitabin = await servis.OduncVerAsync(kitap.Id, uye3.Id, 0, NushaIslemleri.BarkodUret(baska.Id, 1));

        Assert.Equal(NushaIslemleri.BarkodUret(kitap.Id, 1), otomatik.Odunc!.Nusha!.Barkod);
        Assert.Equal(ucuncu, secili.Odunc!.Nusha!.Barkod);
        Assert.False(ayniNusha.Basarili);
        Assert.Contains("ödünç verilemez", ayniNusha.Mesaj);
        Assert.False(baskaKitabin.Basarili);
        Assert.Contains("bu kitaba ait değil", baskaKitabin.Mesaj);
        Assert.Equal(2, await context.KitapNushalari.CountAsync(n => n.KitapId == kitap.Id && n.Durum == NushaDurumu.Oduncte));
        Assert.Equal(1, (await context.Kitaplar.AsNoTracking().SingleAsync(k => k.Id == kitap.Id)).MevcutAdet);
    }

    [Theory]
    [InlineData(KitapDurumu.Iyi, NushaDurumu.Rafta)]
    [InlineData(KitapDurumu.Orta, NushaDurumu.Rafta)]
    [InlineData(KitapDurumu.Hasarli, NushaDurumu.Hasarli)]
    [InlineData(KitapDurumu.Kayip, NushaDurumu.Kayip)]
    public async Task IadeAl_NushaDurumunuIadeDurumunaGoreGunceller(KitapDurumu iade, NushaDurumu beklenen)
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2));
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = TestVeri.OduncService(context, zaman: _zaman);
        var odunc = (await servis.OduncVerAsync(kitap.Id, uye.Id, 0)).Odunc!;

        Assert.True((await servis.IadeAlAsync(odunc.Id, iade)).Basarili);

        var nusha = await context.KitapNushalari.AsNoTracking().SingleAsync(n => n.Id == odunc.NushaId);
        Assert.Equal(beklenen, nusha.Durum);
        var guncel = await context.Kitaplar.AsNoTracking().SingleAsync();
        var nushalar = await context.KitapNushalari.AsNoTracking().Where(n => n.KitapId == kitap.Id).ToListAsync();
        Assert.Equal(nushalar.Count(n => n.DolasimdaMi), guncel.ToplamAdet);
        Assert.Equal(nushalar.Count(n => n.Durum == NushaDurumu.Rafta), guncel.MevcutAdet);
    }

    [Fact]
    public async Task KitapGuncelle_ArtistaNushaEkler_AzalistaRaftakiSonNushayiCikarir_RafiTasir()
    {
        using var context = _db.YeniContext();
        var servis = new KitapService(context);
        var kitap = (await servis.EkleAsync(YeniKitap(2), null)).Kitap!;

        var arti = await servis.GuncelleAsync(new Kitap { Id = kitap.Id, Baslik = "B", Yazar = "Y", ToplamAdet = 4, RafKonumu = "C-09" }, null);
        Assert.True(arti.BasariliMi, arti.Mesaj);
        var nushalar = await context.KitapNushalari.AsNoTracking().Where(n => n.KitapId == kitap.Id).OrderBy(n => n.Id).ToListAsync();
        Assert.Equal(4, nushalar.Count);
        Assert.Equal(NushaIslemleri.BarkodUret(kitap.Id, 4), nushalar[^1].Barkod);
        Assert.All(nushalar, n => Assert.Equal("C-09", n.RafKonumu));

        var eksi = await servis.GuncelleAsync(new Kitap { Id = kitap.Id, Baslik = "B", Yazar = "Y", ToplamAdet = 3, RafKonumu = "C-09" }, null);
        Assert.True(eksi.BasariliMi, eksi.Mesaj);
        var son = await context.KitapNushalari.AsNoTracking().SingleAsync(n => n.Barkod == NushaIslemleri.BarkodUret(kitap.Id, 4));
        Assert.Equal(NushaDurumu.Cikarildi, son.Durum);
        var guncel = await context.Kitaplar.AsNoTracking().SingleAsync();
        Assert.Equal((3, 3), (guncel.ToplamAdet, guncel.MevcutAdet));
    }

    [Fact]
    public async Task NushasiOlmayanKitapIlkIslemdeOnarilir_AktifOduncNushayaBaglanir()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, new Kitap { Baslik = "Eski", Yazar = "Y", ToplamAdet = 2, MevcutAdet = 1 });
        var eskiOkur = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var yeniOkur = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var eskiOdunc = await TestVeri.EkleAsync(context, new Odunc { KitapId = kitap.Id, UyeId = eskiOkur.Id, Durumu = OduncDurumu.Aktif });

        var sonuc = await TestVeri.OduncService(context, zaman: _zaman).OduncVerAsync(kitap.Id, yeniOkur.Id, 0);

        Assert.True(sonuc.Basarili, sonuc.Mesaj);
        Assert.NotNull((await context.Oduncler.AsNoTracking().SingleAsync(o => o.Id == eskiOdunc.Id)).NushaId);
        Assert.Equal(2, await context.KitapNushalari.CountAsync(n => n.KitapId == kitap.Id && n.Durum == NushaDurumu.Oduncte));
        Assert.Equal(0, (await context.Kitaplar.AsNoTracking().SingleAsync()).MevcutAdet);
    }

    [Fact]
    public async Task Banko_NushaBarkoduKitabiVeAktifOduncuKesinBulur()
    {
        using var context = _db.YeniContext();
        var kitap = (await new KitapService(context).EkleAsync(YeniKitap(2), null)).Kitap!;
        var uye1 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var uye2 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = TestVeri.OduncService(context, zaman: _zaman);
        await servis.OduncVerAsync(kitap.Id, uye1.Id, 0);
        var ikinci = (await servis.OduncVerAsync(kitap.Id, uye2.Id, 0)).Odunc!;
        var arama = new BankoAramaService(context);

        var nusha = await arama.NushaBulAsync(" " + ikinci.Nusha!.Barkod.ToLowerInvariant() + " ");

        Assert.Equal(kitap.Id, nusha?.Kitap?.Id);
        Assert.Equal(ikinci.Id, (await arama.NushaninAktifOduncuAsync(nusha!.Id)).Kayit?.Id);
        Assert.Null(await arama.NushaBulAsync("KB9999999999"));
    }

    [Fact]
    public async Task Uzat_BaskaUyeBekliyorVeBosKopyaYoksaReddeder_BosKopyaVarsaIzinVerir()
    {
        using var context = _db.YeniContext();
        var tekKopya = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 1));
        var ikiKopya = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 2));
        var okuyan = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var bekleyen = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = TestVeri.OduncService(context, zaman: _zaman);
        var odunc1 = (await servis.OduncVerAsync(tekKopya.Id, okuyan.Id, 0)).Odunc!;
        var odunc2 = (await servis.OduncVerAsync(ikiKopya.Id, okuyan.Id, 0)).Odunc!;
        foreach (var kitapId in new[] { tekKopya.Id, ikiKopya.Id })
            await TestVeri.EkleAsync(context, new Rezervasyon
            {
                KitapId = kitapId, UyeId = bekleyen.Id, RezervasyonTarihi = _zaman.Simdi,
                GecerlilikTarihi = _zaman.Simdi.AddDays(7), Durumu = RezervasyonDurumu.Bekliyor
            });

        var reddedilen = await servis.UzatAsync(odunc1.Id);
        var izinVerilen = await servis.UzatAsync(odunc2.Id);

        Assert.False(reddedilen.Basarili);
        Assert.Contains("sırada bekleyen", reddedilen.Mesaj);
        Assert.True(izinVerilen.Basarili, izinVerilen.Mesaj);
    }
}
