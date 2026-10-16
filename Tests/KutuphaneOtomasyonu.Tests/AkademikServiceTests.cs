using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class AkademikServiceTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.BellekIci();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task YayinTransferi_YayiniKaydeder_KitapAtamaz_ListedeYayinBasligiGorunur()
    {
        using var context = _db.YeniContext();
        var yayin = await TestVeri.EkleAsync(context, new AkademikYayin { Baslik = "Örnek Tez", Yazar = "A. Yazar", MevcutKampus = KampusLokasyonu.TipSaglikKutuphanesi });
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = new AkademikService(context);

        var transfer = await servis.YayinTransferTalepEtAsync(yayin.Id, uye.Id, yayin.MevcutKampus, KampusLokasyonu.MerkezKutuphane);

        var kayit = await context.MateryalTransferler.AsNoTracking().SingleAsync(t => t.Id == transfer.Id);
        Assert.Null(kayit.KitapId);
        Assert.Equal(yayin.Id, kayit.AkademikYayinId);
        Assert.Equal(uye.Id, kayit.TalepEdenUyeId);
        Assert.Equal("🎓 Örnek Tez", Assert.Single(await servis.GetTransferlerAsync()).MateryalBasligi);
    }

    [Fact]
    public async Task KitapTransferi_ArsivKitabiPasifUyeyiVeAyniKampusuReddeder_GecerliTalebiKaydeder()
    {
        using var context = _db.YeniContext();
        var arsiv = await TestVeri.EkleAsync(context, TestVeri.Kitap(aktif: false));
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var pasif = await TestVeri.EkleAsync(context, TestVeri.Uye(durum: UyeDurumu.Askida));
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = new AkademikService(context);
        const KampusLokasyonu tip = KampusLokasyonu.TipSaglikKutuphanesi, merkez = KampusLokasyonu.MerkezKutuphane;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servis.TransferTalepEtAsync(arsiv.Id, uye.Id, tip, merkez));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servis.TransferTalepEtAsync(kitap.Id, pasif.Id, tip, merkez));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servis.TransferTalepEtAsync(kitap.Id, uye.Id, merkez, merkez));
        var transfer = await servis.TransferTalepEtAsync(kitap.Id, uye.Id, tip, merkez);

        Assert.Equal(kitap.Baslik, Assert.Single(await servis.GetTransferlerAsync()).MateryalBasligi);
        Assert.StartsWith("ILL-", transfer.KuryeTakipKodu);
    }

    [Fact]
    public async Task YayinTransferi_PasifUyeVeAyniKampusReddedilir()
    {
        using var context = _db.YeniContext();
        var yayin = await TestVeri.EkleAsync(context, new AkademikYayin { Baslik = "Tez", Yazar = "Y" });
        var pasif = await TestVeri.EkleAsync(context, TestVeri.Uye(durum: UyeDurumu.Pasif));
        var aktif = await TestVeri.EkleAsync(context, TestVeri.Uye());
        var servis = new AkademikService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servis.YayinTransferTalepEtAsync(yayin.Id, pasif.Id, KampusLokasyonu.TipSaglikKutuphanesi, KampusLokasyonu.MerkezKutuphane));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servis.YayinTransferTalepEtAsync(yayin.Id, aktif.Id, KampusLokasyonu.MerkezKutuphane, KampusLokasyonu.MerkezKutuphane));
        Assert.Empty(await context.MateryalTransferler.ToListAsync());
    }
}
