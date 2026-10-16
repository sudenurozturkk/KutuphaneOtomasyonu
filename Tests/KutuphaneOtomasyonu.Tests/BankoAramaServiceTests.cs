using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class BankoAramaServiceTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task KitapBul_KesinTanimlayiciOncelikli_BaslikYalnizcaTekEslesmedeKabulEdilir()
    {
        using var context = _db.YeniContext();
        var temiz = await TestVeri.EkleAsync(context, new Kitap { Baslik = "Temiz Kod (Clean Code)", Yazar = "Y", ISBN = "9780306406157", ToplamAdet = 1, MevcutAdet = 1 });
        await TestVeri.EkleAsync(context, new Kitap { Baslik = "Algoritmalar I", Yazar = "Y", ToplamAdet = 1, MevcutAdet = 1 });
        await TestVeri.EkleAsync(context, new Kitap { Baslik = "Algoritmalar II", Yazar = "Y", ToplamAdet = 1, MevcutAdet = 1 });
        await TestVeri.EkleAsync(context, new Kitap { Baslik = "Arşivdeki Clean Code Notları", Yazar = "Y", ToplamAdet = 1, MevcutAdet = 1, Aktif = false });
        var arama = new BankoAramaService(context);

        Assert.Equal(temiz.Id, (await arama.KitapBulAsync("978-0-306-40615-7")).Kayit?.Id);
        Assert.Equal(temiz.Id, (await arama.KitapBulAsync(temiz.Id.ToString())).Kayit?.Id);
        Assert.Equal(temiz.Id, (await arama.KitapBulAsync("clean code")).Kayit?.Id);

        var belirsiz = await arama.KitapBulAsync("Algoritmalar");
        Assert.Null(belirsiz.Kayit);
        Assert.True(belirsiz.BelirsizMi);
        Assert.Contains("2 kitap", belirsiz.Mesaj);

        Assert.Null((await arama.KitapBulAsync("olmayan")).Kayit);
        Assert.Null((await arama.KitapBulAsync("  ")).Kayit);
    }

    [Fact]
    public async Task UyeBul_TcOgrenciNoVeIdKesin_AdBelirsizseReddedilir()
    {
        using var context = _db.YeniContext();
        var ayse1 = TestVeri.Uye();
        ayse1.Ad = "Ayşe";
        ayse1.OgrenciNo = "2024123456";
        await TestVeri.EkleAsync(context, ayse1);
        var ayse2 = TestVeri.Uye();
        ayse2.Ad = "Ayşe";
        await TestVeri.EkleAsync(context, ayse2);
        var arama = new BankoAramaService(context);

        Assert.Equal(ayse1.Id, (await arama.UyeBulAsync(ayse1.TCKimlikNo)).Kayit?.Id);
        Assert.Equal(ayse1.Id, (await arama.UyeBulAsync("2024123456")).Kayit?.Id);
        Assert.Equal(ayse2.Id, (await arama.UyeBulAsync(ayse2.Id.ToString())).Kayit?.Id);
        Assert.Equal(ayse2.Id, (await arama.UyeBulAsync(ayse2.TamAd.ToUpperInvariant())).Kayit?.Id);
        Assert.True((await arama.UyeBulAsync("ayşe")).BelirsizMi);
    }

    [Fact]
    public async Task AktifOduncBul_BirdenFazlaKopyaOduncteyseUyeBilgisiIster()
    {
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 3));
        var uye1 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var uye2 = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var baskasi = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var odunc = TestVeri.OduncService(context);
        var arama = new BankoAramaService(context);

        Assert.Null((await arama.AktifOduncBulAsync(kitap.Id, null)).Kayit);

        var ilk = (await odunc.OduncVerAsync(kitap.Id, uye1.Id, 0)).Odunc!;
        Assert.Equal(ilk.Id, (await arama.AktifOduncBulAsync(kitap.Id, null)).Kayit?.Id);

        var ikinci = (await odunc.OduncVerAsync(kitap.Id, uye2.Id, 0)).Odunc!;
        Assert.True((await arama.AktifOduncBulAsync(kitap.Id, "")).BelirsizMi);
        Assert.Equal(ikinci.Id, (await arama.AktifOduncBulAsync(kitap.Id, uye2.TCKimlikNo)).Kayit?.Id);
        Assert.Null((await arama.AktifOduncBulAsync(kitap.Id, baskasi.TCKimlikNo)).Kayit);
    }
}
