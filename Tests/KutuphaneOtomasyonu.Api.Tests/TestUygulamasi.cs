using System.Net.Http.Json;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOtomasyonu.Api.Tests;

/// <summary>
/// Web API'yi geçici bir SQLite dosyasıyla, üretim ayarlarıyla (güvenli çerez, HSTS, zorunlu parola değişimi
/// isteğe bağlı) başlatır ve bilinen test verisini yükler.
/// </summary>
public sealed class TestUygulamasi : WebApplicationFactory<Program>
{
    public const string KutuphaneciSifresi = "Kutuphane-2026!";
    public const string YardimciSifresi = "Yardimci-2026!";

    private readonly string _veritabaniYolu =
        Path.Combine(Path.GetTempPath(), $"kutuphane_api_test_{Guid.NewGuid():N}.db");
    private readonly bool _sifreDegisimiZorunlu;

    public TestUygulamasi(bool sifreDegisimiZorunlu = false)
    {
        _sifreDegisimiZorunlu = sifreDegisimiZorunlu;
    }

    public int KutuphaneciId { get; private set; }
    public int YardimciId { get; private set; }
    public int UyeId { get; private set; }
    public int KitapId { get; private set; }
    public string UyeOgrenciNo => "2026000123";
    public string UyeTc => "10000000078";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Veritabani:Yol", _veritabaniYolu);
        builder.UseSetting("Veritabani:DemoVerisi", "false");
        builder.UseSetting("Guvenlik:VarsayilanSifreDegisimiZorunlu", _sifreDegisimiZorunlu ? "true" : "false");
    }

    /// <summary>Uygulamayı başlatır (şemayı oluşturur) ve test verisini ekler.</summary>
    public async Task HazirlaAsync()
    {
        using var kapsam = Services.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<KutuphaneDbContext>();

        var kutuphaneci = new Kullanici
        {
            Ad = "Fatma", Soyad = "Kaya", KullaniciAdi = "fatma", Rol = KullaniciRolu.Kutuphaneci,
            SifreHash = SifreHasher.Hashle(KutuphaneciSifresi)
        };
        var yardimci = new Kullanici
        {
            Ad = "Ali", Soyad = "Can", KullaniciAdi = "ali", Rol = KullaniciRolu.YardimciPersonel,
            SifreHash = SifreHasher.Hashle(YardimciSifresi)
        };
        db.Kullanicilar.AddRange(kutuphaneci, yardimci);
        db.SistemAyarlari.Add(new SistemAyarlari());

        var uye = new Uye
        {
            Ad = "Ayşe", Soyad = "Yılmaz", Email = "ayse@ornek.edu.tr", Telefon = "05551234567",
            TCKimlikNo = UyeTc, OgrenciNo = UyeOgrenciNo, Bolum = "Bilgisayar Mühendisliği",
            UyeTipi = UyeTipi.Ogrenci, MaxOduncAdet = 3, MaxOduncSuresi = 14,
            UyelikBitisTarihi = DateTime.Now.AddYears(1)
        };
        db.Uyeler.Add(uye);
        await db.SaveChangesAsync();

        // Kitap ve nüshaları servis üzerinden eklenir (barkodlar ve denetim kaydı gerçek akışla oluşur).
        using var servisler = await ServisKapsami.OlusturAsync(db);
        var sonuc = await servisler.Kitap.EkleAsync(new Kitap
        {
            Baslik = "Temiz Kod", Yazar = "Robert C. Martin", ISBN = "9780132350884", Kategori = "Yazılım",
            Yayinevi = "Prentice Hall", YayinYili = 2008, SayfaSayisi = 431, ToplamAdet = 2, RafKonumu = "B-01"
        }, kutuphaneci.Id);
        if (!sonuc.BasariliMi) throw new InvalidOperationException(sonuc.Mesaj);

        KutuphaneciId = kutuphaneci.Id;
        YardimciId = yardimci.Id;
        UyeId = uye.Id;
        KitapId = sonuc.Kitap!.Id;
    }

    public HttpClient IstemciOlustur() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    public async Task<HttpClient> GirisYapmisIstemciAsync(string kullaniciAdi, string sifre)
    {
        var istemci = IstemciOlustur();
        var yanit = await istemci.PostAsJsonAsync("/api/oturum/giris", new { kullaniciAdi, sifre });
        yanit.EnsureSuccessStatusCode();
        return istemci;
    }

    public KutuphaneDbContext YeniContext()
    {
        var kapsam = Services.CreateScope();
        return kapsam.ServiceProvider.GetRequiredService<KutuphaneDbContext>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        SqliteConnection.ClearAllPools();
        foreach (var ek in new[] { "", "-wal", "-shm" })
        {
            try { File.Delete(_veritabaniYolu + ek); } catch (IOException) { }
        }
    }
}
