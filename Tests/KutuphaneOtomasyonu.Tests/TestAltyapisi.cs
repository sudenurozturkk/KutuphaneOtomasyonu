using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace KutuphaneOtomasyonu.Tests;

/// <summary>
/// Uygulamanın gerçek şema hazırlığını (tetikleyiciler ve indeksler dahil) kullanan, demo verisi
/// içermeyen izole SQLite veritabanı. Varsayılan olarak bellek içi; eşzamanlılık testleri için dosya tabanlı.
/// </summary>
public sealed class TestVeritabani : IDisposable
{
    private readonly SqliteConnection? _baglanti;
    private readonly string? _dosyaYolu;

    public DbContextOptions<KutuphaneDbContext> Secenekler { get; }

    private TestVeritabani(SqliteConnection? baglanti, string? dosyaYolu, DbContextOptions<KutuphaneDbContext> secenekler,
        bool semaHazirla = true)
    {
        _baglanti = baglanti;
        _dosyaYolu = dosyaYolu;
        Secenekler = secenekler;

        if (!semaHazirla) return;
        using var context = YeniContext();
        VeritabaniSemasi.Hazirla(context);
    }

    public static TestVeritabani BellekIci()
    {
        var baglanti = new SqliteConnection("Data Source=:memory:");
        baglanti.Open();
        return new TestVeritabani(baglanti, null,
            new DbContextOptionsBuilder<KutuphaneDbContext>().UseSqlite(baglanti).Options);
    }

    /// <summary>Her context ayrı bağlantı açar; çoklu süreç/eşzamanlılık davranışını gerçekçi şekilde taklit eder.</summary>
    public static TestVeritabani Dosya(bool semaHazirla = true)
    {
        var yol = Path.Combine(Path.GetTempPath(), $"kutuphane_test_{Guid.NewGuid():N}.db");
        return new TestVeritabani(null, yol,
            new DbContextOptionsBuilder<KutuphaneDbContext>().UseSqlite($"Data Source={yol};Pooling=False").Options,
            semaHazirla);
    }

    public string DosyaYolu => _dosyaYolu ?? throw new InvalidOperationException("Bellek içi veritabanının dosyası yok.");

    public KutuphaneDbContext YeniContext() => new(Secenekler);

    public void Dispose()
    {
        _baglanti?.Dispose();
        if (_dosyaYolu != null)
        {
            SqliteConnection.ClearAllPools();
            foreach (var dosya in new[] { _dosyaYolu, _dosyaYolu + "-wal", _dosyaYolu + "-shm" })
            {
                try { File.Delete(dosya); } catch (IOException) { }
            }
        }
    }
}

/// <summary>Testlerde zamanı elle ilerletmek için.</summary>
public sealed class ElleZaman : TimeProvider
{
    private DateTimeOffset _simdi;

    public ElleZaman(DateTime yerelSimdi)
    {
        _simdi = new DateTimeOffset(yerelSimdi, TimeZoneInfo.Local.GetUtcOffset(yerelSimdi));
    }

    public override DateTimeOffset GetUtcNow() => _simdi.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    public void Ilerlet(TimeSpan sure) => _simdi = _simdi.Add(sure);

    public DateTime Simdi => _simdi.DateTime;
}

/// <summary>Sahte kişisel veri içermeyen test kayıtları.</summary>
public static class TestVeri
{
    private static int _sayac;

    /// <summary>Rafta <paramref name="adet"/> nüshası olan test kitabı (nüsha barkodları: TST + sayaç + sıra).</summary>
    public static Kitap Kitap(int adet = 1, decimal fiyat = 100m, bool aktif = true)
    {
        var no = Interlocked.Increment(ref _sayac);
        return new Kitap
        {
            Baslik = $"Test Kitabı {no}",
            Yazar = "Test Yazar",
            ISBN = string.Empty,
            ToplamAdet = adet,
            MevcutAdet = adet,
            SatinAlmaFiyati = fiyat,
            Aktif = aktif,
            Nushalar = Enumerable.Range(1, adet)
                .Select(i => new KitapNushasi { Barkod = $"TST{no:D6}{i:D3}", Durum = NushaDurumu.Rafta })
                .ToList()
        };
    }

    public static Uye Uye(int maxAdet = 3, int maxSure = 14, UyeDurumu durum = UyeDurumu.Aktif)
    {
        var no = Interlocked.Increment(ref _sayac);
        return new Uye
        {
            Ad = "Test",
            Soyad = $"Üye{no}",
            Email = $"uye{no}@test.invalid",
            TCKimlikNo = $"T{no:D10}",
            MaxOduncAdet = maxAdet,
            MaxOduncSuresi = maxSure,
            Durumu = durum
        };
    }

    public static SistemAyarlari Ayarlar(int gunlukCeza = 2, int maxUzatma = 2, int oduncSuresi = 14, int rezervasyonGun = 3) => new()
    {
        GunlukCezaTutari = gunlukCeza,
        MaxUzatmaSayisi = maxUzatma,
        MaxOduncSuresi = oduncSuresi,
        RezervasyonGecerlilikSuresi = rezervasyonGun
    };

    public static async Task<T> EkleAsync<T>(KutuphaneDbContext context, T varlik) where T : class
    {
        context.Add(varlik);
        await context.SaveChangesAsync();
        return varlik;
    }

    public static OduncService OduncService(KutuphaneDbContext context, SistemAyarlari? ayarlar = null, TimeProvider? zaman = null) =>
        new(context, new BildirimService(context), ayarlar ?? Ayarlar(), zaman);
}
