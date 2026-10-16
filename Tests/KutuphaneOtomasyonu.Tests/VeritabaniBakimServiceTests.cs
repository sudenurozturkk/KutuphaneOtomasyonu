using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class VeritabaniBakimServiceTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.Dosya();
    private readonly string _yedekYolu = Path.Combine(Path.GetTempPath(), $"kutuphane_yedek_test_{Guid.NewGuid():N}.db");

    private static readonly Kullanici Admin = new() { Id = 1, Rol = KullaniciRolu.Admin, Aktif = true };

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_yedekYolu); } catch (IOException) { }
    }

    [Theory]
    [InlineData(KullaniciRolu.Kutuphaneci)]
    [InlineData(KullaniciRolu.YardimciPersonel)]
    public async Task YoneticiOlmayanBakimIslemleriniYapamaz(KullaniciRolu rol)
    {
        var kullanici = new Kullanici { Id = 2, Rol = rol, Aktif = true };
        using var context = _db.YeniContext();
        var servis = new VeritabaniBakimService(context);

        Assert.False((await servis.YedekAlAsync(_yedekYolu, kullanici)).BasariliMi);
        Assert.False((await servis.DemoVerisineSifirlaAsync(kullanici)).BasariliMi);
        Assert.False((await servis.AyarlariKaydetAsync(new SistemAyarlari(), kullanici)).BasariliMi);
        Assert.False((await servis.YedekAlAsync(_yedekYolu, null)).BasariliMi);
        Assert.False(File.Exists(_yedekYolu));
    }

    [Fact]
    public async Task PasifYoneticiYetkisizSayilir()
    {
        using var context = _db.YeniContext();
        var pasif = new Kullanici { Id = 1, Rol = KullaniciRolu.Admin, Aktif = false };

        Assert.False((await new VeritabaniBakimService(context).YedekAlAsync(_yedekYolu, pasif)).BasariliMi);
    }

    [Fact]
    public async Task YedekAl_WalModundakiSonVerileriDeIcerenOkunabilirKopyaUretir()
    {
        using (var context = _db.YeniContext())
        {
            context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            await TestVeri.EkleAsync(context, new Kullanici { Id = 1, Ad = "A", Soyad = "B", KullaniciAdi = "admin", SifreHash = "x", Rol = KullaniciRolu.Admin });
            await TestVeri.EkleAsync(context, TestVeri.Kitap(adet: 4));
            await TestVeri.EkleAsync(context, TestVeri.Uye());
        }

        using (var context = _db.YeniContext())
        {
            var sonuc = await new VeritabaniBakimService(context).YedekAlAsync(_yedekYolu, Admin);
            Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
        }

        var yedekSecenek = new DbContextOptionsBuilder<KutuphaneDbContext>()
            .UseSqlite($"Data Source={_yedekYolu};Pooling=False").Options;
        using (var yedek = new KutuphaneDbContext(yedekSecenek))
        {
            Assert.Equal(4, (await yedek.Kitaplar.SingleAsync()).ToplamAdet);
            Assert.Equal(1, await yedek.Uyeler.CountAsync());
        }
        SqliteConnection.ClearAllPools();

        using var dogrulama = _db.YeniContext();
        Assert.Single(await dogrulama.AktiviteLoglar.Where(l => l.Islem == DenetimService.Islemler.VeritabaniYedeklendi).ToListAsync());
    }

    [Fact]
    public async Task Veritabani_TransferKaydiOlanKitabinDogrudanSilinmesiniEngeller()
    {
        // Eski sıfırlama kodu Kitaplar'ı MateryalTransferler'den önce siliyordu; bu kısıt yüzünden başarısız oluyordu.
        using var context = _db.YeniContext();
        var kitap = await TestVeri.EkleAsync(context, TestVeri.Kitap());
        var uye = await TestVeri.EkleAsync(context, TestVeri.Uye());
        await TestVeri.EkleAsync(context, new MateryalTransfer { KitapId = kitap.Id, TalepEdenUyeId = uye.Id });

        await Assert.ThrowsAnyAsync<Exception>(() => context.Kitaplar.Where(k => k.Id == kitap.Id).ExecuteDeleteAsync());
    }

    [Fact]
    public async Task DemoVerisineSifirla_TransferVarkenCalisir_HesaplariVeDenetimKayitlariniKorur()
    {
        int kullaniciSayisi;
        using (var context = _db.YeniContext())
        {
            DatabaseSeeder.Seed(context);
            var kitap = await context.Kitaplar.FirstAsync();
            var uye = await context.Uyeler.FirstAsync(u => u.Durumu == UyeDurumu.Aktif && u.ToplamCeza == 0);
            await TestVeri.OduncService(context).OduncVerAsync(kitap.Id, uye.Id, 0);
            context.Kitaplar.Add(new Kitap { Baslik = "Sıfırlamada silinecek", Yazar = "Y", ToplamAdet = 1, MevcutAdet = 1 });
            await context.SaveChangesAsync();
            Assert.True(await context.MateryalTransferler.AnyAsync());
            kullaniciSayisi = await context.Kullanicilar.CountAsync();
        }

        using (var context = _db.YeniContext())
        {
            var sonuc = await new VeritabaniBakimService(context).DemoVerisineSifirlaAsync(Admin);
            Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
        }

        using var dogrulama = _db.YeniContext();
        Assert.Equal(30, await dogrulama.Kitaplar.CountAsync());
        Assert.False(await dogrulama.Kitaplar.AnyAsync(k => k.Baslik == "Sıfırlamada silinecek"));
        Assert.Equal(50, await dogrulama.Uyeler.CountAsync());
        Assert.Equal(kullaniciSayisi, await dogrulama.Kullanicilar.CountAsync());
        Assert.True(await dogrulama.AktiviteLoglar.AnyAsync(l => l.Islem == DenetimService.Islemler.OduncVerildi));
        Assert.True(await dogrulama.AktiviteLoglar.AnyAsync(l => l.Islem == DenetimService.Islemler.VeritabaniSifirlandi));

        // Yeniden yüklenen demo verisinde stok değişmezi korunur.
        var ihlal = await dogrulama.Kitaplar
            .Where(k => k.ToplamAdet - k.MevcutAdet != dogrulama.Oduncler.Count(o => o.KitapId == k.Id && o.Durumu == OduncDurumu.Aktif))
            .CountAsync();
        Assert.Equal(0, ihlal);
    }

    [Theory]
    [InlineData(0, 5, 2, 2)]
    [InlineData(14, 0, 2, 2)]
    [InlineData(14, 5, -1, 2)]
    [InlineData(14, 5, 2, 21)]
    public async Task AyarlariKaydet_AralikDisiDegerleriReddeder(int sure, int adet, int ceza, int uzatma)
    {
        using var context = _db.YeniContext();
        var sonuc = await new VeritabaniBakimService(context).AyarlariKaydetAsync(new SistemAyarlari
        {
            KutuphaneAdi = "Test", MaxOduncSuresi = sure, MaxOduncAdet = adet, GunlukCezaTutari = ceza, MaxUzatmaSayisi = uzatma
        }, Admin);

        Assert.False(sonuc.BasariliMi);
        Assert.False(await context.SistemAyarlari.AnyAsync());
    }

    [Fact]
    public async Task AyarlariKaydet_GecerliDegerleriKaydederVeDenetler()
    {
        using var context = _db.YeniContext();
        await TestVeri.EkleAsync(context, new Kullanici { Id = 1, Ad = "A", Soyad = "B", KullaniciAdi = "admin", SifreHash = "x", Rol = KullaniciRolu.Admin });

        var sonuc = await new VeritabaniBakimService(context).AyarlariKaydetAsync(new SistemAyarlari
        {
            KutuphaneAdi = " Merkez ", MaxOduncSuresi = 21, MaxOduncAdet = 4, GunlukCezaTutari = 3, MaxUzatmaSayisi = 1
        }, Admin);

        Assert.True(sonuc.BasariliMi, sonuc.Mesaj);
        var ayar = await context.SistemAyarlari.AsNoTracking().SingleAsync();
        Assert.Equal("Merkez", ayar.KutuphaneAdi);
        Assert.Equal(21, ayar.MaxOduncSuresi);
        Assert.Equal(1, ayar.MaxUzatmaSayisi);
        Assert.Single(await context.AktiviteLoglar.Where(l => l.Islem == DenetimService.Islemler.AyarlarGuncellendi).ToListAsync());
    }
}
