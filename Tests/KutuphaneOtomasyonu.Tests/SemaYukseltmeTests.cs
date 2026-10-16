using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

/// <summary>
/// Migration öncesi (EnsureCreated ile oluşturulmuş, <c>__EFMigrationsHistory</c> içermeyen) veritabanlarının
/// veri kaybı olmadan migration düzenine geçtiğini doğrular.
/// </summary>
public sealed class SemaYukseltmeTests : IDisposable
{
    private readonly TestVeritabani _db = TestVeritabani.Dosya(semaHazirla: false);

    public void Dispose() => _db.Dispose();

    /// <summary>Taban şemayı kurar, ardından migration geçmişini ve sonradan eklenen korumaları siler.</summary>
    private static void EskiKurulumOlustur(KutuphaneDbContext context, params string[] silinecekTablolar)
    {
        context.GetService<IMigrator>().Migrate(VeritabaniSemasi.TabanMigrasyonId);
        context.Database.ExecuteSqlRaw($"""
            DROP TABLE "__EFMigrationsHistory";
            DROP TRIGGER IF EXISTS "{KutuphaneDbContext.StokTetikleyiciEkleme}";
            DROP TRIGGER IF EXISTS "{KutuphaneDbContext.StokTetikleyiciGuncelleme}";
            DROP INDEX IF EXISTS "IX_Oduncler_UyeId_KitapId";
            """);
        foreach (var tablo in silinecekTablolar)
            context.Database.ExecuteSqlRaw($"DROP TABLE \"{tablo}\";");
    }

    // Eski sürümler bu sütunlarla yazıyordu; güncel varlık modeli eski şemaya yazamaz, bu yüzden SQL kullanılır.
    private static void EskiKitapEkle(KutuphaneDbContext context, int id, int toplam, int mevcut) =>
        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Kitaplar" ("Id","Baslik","Yazar","ISBN","Yayinevi","YayinYili","Kategori","AltKategori","Dil","SayfaSayisi",
                "ToplamAdet","MevcutAdet","Aciklama","KapakResimYolu","RafKonumu","SatinAlmaFiyati","EklenmeTarihi","Aktif","Puan","PuanSayisi","GoruntulemeSayisi")
            VALUES ({0}, 'Eski Kitap', 'Yazar', '', '', 2000, '', '', 'Türkçe', 100, {1}, {2}, '', '', 'A-01', '50.0', '2025-01-01 00:00:00', 1, 0, 0, 0);
            """, id, toplam, mevcut);

    private static void EskiUyeEkle(KutuphaneDbContext context, int id) =>
        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Uyeler" ("Id","Ad","Soyad","Email","Telefon","Adres","TCKimlikNo","OgrenciNo","Bolum","Fakulte","UyeTipi","Durumu",
                "KayitTarihi","FotografYolu","MaxOduncAdet","MaxOduncSuresi","ToplamCeza","OdenenCeza","Notlar")
            VALUES ({0}, 'Eski', 'Üye', 'eski' || {0} || '@test.invalid', '', '', 'E' || {0}, '', '', '', 0, 0, '2025-01-01 00:00:00', '', 3, 14, '0.0', '0.0', '');
            """, id);

    private static void EskiOduncEkle(KutuphaneDbContext context, int kitapId, int uyeId, OduncDurumu durum) =>
        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Oduncler" ("KitapId","UyeId","OduncTarihi","PlanlananIadeTarihi","Durumu","UzatmaSayisi","MaxUzatmaSayisi",
                "CezaTutari","CezaOdendi","Notlar","IadeNotlari","IadedekiKitapDurumu")
            VALUES ({0}, {1}, '2025-01-01 00:00:00', '2025-01-15 00:00:00', {2}, 0, 2, '0.0', 0, '', '', 1);
            """, kitapId, uyeId, (int)durum);

    private static int NesneSayisi(KutuphaneDbContext context, string tur, string ad) =>
        context.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = '{tur}' AND name = '{ad}'").Single();

    [Fact]
    public async Task Hazirla_YeniVeritabanindaTumMigrationlariUygular()
    {
        using var context = _db.YeniContext();

        VeritabaniSemasi.Hazirla(context);

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Contains(VeritabaniSemasi.TabanMigrasyonId, await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(1, NesneSayisi(context, "trigger", KutuphaneDbContext.StokTetikleyiciGuncelleme));
    }

    [Fact]
    public async Task Hazirla_EskiKurulumuTabanlar_KorumalariEkler_VeriyiKorur_TekrarCalistirilabilir()
    {
        using (var context = _db.YeniContext())
        {
            EskiKurulumOlustur(context);
            EskiKitapEkle(context, 1, toplam: 2, mevcut: 1);
            EskiUyeEkle(context, 1);
            EskiOduncEkle(context, 1, 1, OduncDurumu.Aktif);
        }

        using (var context = _db.YeniContext())
        {
            VeritabaniSemasi.Hazirla(context);
            VeritabaniSemasi.Hazirla(context);

            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            Assert.Equal(1, NesneSayisi(context, "trigger", KutuphaneDbContext.StokTetikleyiciEkleme));
            Assert.Equal(1, NesneSayisi(context, "trigger", KutuphaneDbContext.StokTetikleyiciGuncelleme));
            Assert.Equal(1, NesneSayisi(context, "index", "IX_Oduncler_UyeId_KitapId"));
            Assert.Equal(1, await context.Oduncler.CountAsync());
            Assert.Equal(2, (await context.Kitaplar.SingleAsync()).ToplamAdet);
        }
    }

    [Fact]
    public async Task Hazirla_EskiSayaclardanNushaUretir_AktifOdunclerleEslestirir_SizanKopyayiDolasimDisiYapar()
    {
        using (var context = _db.YeniContext())
        {
            EskiKurulumOlustur(context);
            // Kitap 1: 3 kopya, 1 ödünçte, 2 rafta (tutarlı).
            EskiKitapEkle(context, 1, toplam: 3, mevcut: 2);
            // Kitap 2: eski hasarlı-iade hatası: 2 kopya, 1 rafta, ödünç yok -> 1 kopya ne rafta ne ödünçte.
            EskiKitapEkle(context, 2, toplam: 2, mevcut: 1);
            EskiUyeEkle(context, 1);
            EskiUyeEkle(context, 2);
            EskiOduncEkle(context, 1, 1, OduncDurumu.Aktif);
            EskiOduncEkle(context, 1, 2, OduncDurumu.Iade);
        }

        using (var context = _db.YeniContext())
        {
            VeritabaniSemasi.Hazirla(context);

            var nushalar1 = await context.KitapNushalari.Where(n => n.KitapId == 1).OrderBy(n => n.Id).ToListAsync();
            Assert.Equal(new[] { "KB0000010001", "KB0000010002", "KB0000010003" }, nushalar1.Select(n => n.Barkod));
            Assert.Equal(1, nushalar1.Count(n => n.Durum == NushaDurumu.Oduncte));
            Assert.Equal(2, nushalar1.Count(n => n.Durum == NushaDurumu.Rafta));
            var aktif = await context.Oduncler.SingleAsync(o => o.Durumu == OduncDurumu.Aktif);
            Assert.Equal(nushalar1.Single(n => n.Durum == NushaDurumu.Oduncte).Id, aktif.NushaId);
            Assert.Null((await context.Oduncler.SingleAsync(o => o.Durumu == OduncDurumu.Iade)).NushaId);

            var kitap1 = await context.Kitaplar.SingleAsync(k => k.Id == 1);
            Assert.Equal((3, 2), (kitap1.ToplamAdet, kitap1.MevcutAdet));

            var nushalar2 = await context.KitapNushalari.Where(n => n.KitapId == 2).ToListAsync();
            Assert.Equal(1, nushalar2.Count(n => n.Durum == NushaDurumu.Rafta));
            Assert.Equal(1, nushalar2.Count(n => n.Durum == NushaDurumu.Hasarli));
            var kitap2 = await context.Kitaplar.SingleAsync(k => k.Id == 2);
            Assert.Equal((1, 1), (kitap2.ToplamAdet, kitap2.MevcutAdet));

            // Migration sonrası dolaşım servisleri nüshalarla çalışır.
            var iade = await TestVeri.OduncService(context).IadeAlAsync(aktif.Id, KitapDurumu.Iyi);
            Assert.True(iade.Basarili, iade.Mesaj);
            Assert.Equal(3, await context.KitapNushalari.CountAsync(n => n.KitapId == 1 && n.Durum == NushaDurumu.Rafta));
        }
    }

    [Fact]
    public async Task Hazirla_EskiKurulumdaEksikTablolariOlusturur()
    {
        using (var context = _db.YeniContext())
            EskiKurulumOlustur(context, "SalonRezervasyonlari", "MateryalTransferler", "AkademikYayinlar");

        using (var context = _db.YeniContext())
        {
            VeritabaniSemasi.Hazirla(context);

            Assert.Equal(0, await context.AkademikYayinlar.CountAsync());
            Assert.Equal(0, await context.MateryalTransferler.CountAsync());
            Assert.Equal(0, await context.SalonRezervasyonlari.CountAsync());
        }
    }

    [Fact]
    public async Task Hazirla_YinelenenAktifOduncleriOnarir_SonrakiniKapatir_KarsiliksizKopyayiDolasimDisiYapar()
    {
        using (var context = _db.YeniContext())
        {
            EskiKurulumOlustur(context);
            EskiKitapEkle(context, 1, toplam: 3, mevcut: 1);
            EskiUyeEkle(context, 1);
            EskiOduncEkle(context, 1, 1, OduncDurumu.Aktif);
            EskiOduncEkle(context, 1, 1, OduncDurumu.Aktif);
        }

        using (var context = _db.YeniContext())
        {
            var hata = Record.Exception(() => VeritabaniSemasi.Hazirla(context));

            Assert.Null(hata);
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            Assert.Equal(1, NesneSayisi(context, "index", "IX_Oduncler_UyeId_KitapId"));

            var oduncler = await context.Oduncler.OrderBy(o => o.Id).ToListAsync();
            Assert.Equal(OduncDurumu.Aktif, oduncler[0].Durumu);
            Assert.Equal(OduncDurumu.Iade, oduncler[1].Durumu);
            Assert.Contains("Otomatik veri onarimi", oduncler[1].IadeNotlari);
            Assert.True(await context.AktiviteLoglar.AnyAsync(l => l.Islem == "VeriOnarimi"));

            // 3 kopya: 1 ödünçte, 1 rafta, kapatılan ödünce ait kopya karşılıksız -> dolaşım dışı.
            var kitap = await context.Kitaplar.SingleAsync();
            Assert.Equal((2, 1), (kitap.ToplamAdet, kitap.MevcutAdet));
            Assert.Equal(1, await context.KitapNushalari.CountAsync(n => n.Durum == NushaDurumu.Hasarli));
        }
    }
}
