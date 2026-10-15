using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public sealed class SalonRezervasyonServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly KutuphaneDbContext _context;
    private readonly SalonRezervasyonService _service;

    public SalonRezervasyonServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<KutuphaneDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new KutuphaneDbContext(options);
        _context.Database.EnsureCreated();
        _service = new SalonRezervasyonService(_context);
    }

    [Fact]
    public async Task OlusturAsync_GecerliRezervasyonuAktifListeyeEkler()
    {
        var sonuc = await _service.OlusturAsync(
            "D-101", "Sessiz", "Test Kullanıcısı", "09:00 - 11:00",
            "2 saat", "Araştırma", new DateTime(2026, 10, 15));

        Assert.True(sonuc.BasariliMi);
        Assert.NotNull(sonuc.Rezervasyon);

        var aktif = await _service.GetAktifAsync(new DateTime(2026, 10, 15), "09:00 - 11:00");
        var rezervasyon = Assert.Single(aktif);
        Assert.Equal("D-101", rezervasyon.MasaKodu);
        Assert.Equal("Test Kullanıcısı", rezervasyon.Kullanici);
    }

    [Fact]
    public async Task OlusturAsync_AyniMasaVeSlotIcinYineleneniReddeder()
    {
        var tarih = new DateTime(2026, 10, 15);
        var ilk = await _service.OlusturAsync("D-101", "Sessiz", "Birinci", "09:00 - 11:00", "2 saat", "", tarih);
        var ikinci = await _service.OlusturAsync("D-101", "Sessiz", "İkinci", "09:00 - 11:00", "2 saat", "", tarih);

        Assert.True(ilk.BasariliMi);
        Assert.False(ikinci.BasariliMi);
        Assert.Contains("zaten rezerve", ikinci.Mesaj);
    }

    [Fact]
    public async Task OlusturAsync_FarkliGunVeyaSlotIcinRezervasyonaIzinVerir()
    {
        var tarih = new DateTime(2026, 10, 15);
        var ilk = await _service.OlusturAsync("D-101", "Sessiz", "Birinci", "09:00 - 11:00", "2 saat", "", tarih);
        var farkliSlot = await _service.OlusturAsync("D-101", "Sessiz", "İkinci", "11:00 - 13:00", "2 saat", "", tarih);
        var farkliGun = await _service.OlusturAsync("D-101", "Sessiz", "Üçüncü", "09:00 - 11:00", "2 saat", "", tarih.AddDays(1));

        Assert.True(ilk.BasariliMi);
        Assert.True(farkliSlot.BasariliMi);
        Assert.True(farkliGun.BasariliMi);
    }

    [Fact]
    public async Task OlusturAsync_ZorunluAlanEksikseHataDoner()
    {
        var sonuc = await _service.OlusturAsync("", "Sessiz", "", "09:00 - 11:00", "2 saat", "", DateTime.Today);

        Assert.False(sonuc.BasariliMi);
        Assert.Contains("zorunlu", sonuc.Mesaj);
    }

    [Fact]
    public async Task IptalEtAsync_AktifRezervasyonuIptalEderVeTekrariniReddeder()
    {
        var olusturma = await _service.OlusturAsync(
            "D-101", "Sessiz", "Test Kullanıcısı", "09:00 - 11:00",
            "2 saat", "", DateTime.Today);

        Assert.NotNull(olusturma.Rezervasyon);
        var iptal = await _service.IptalEtAsync(olusturma.Rezervasyon!.Id);
        var tekrarIptal = await _service.IptalEtAsync(olusturma.Rezervasyon.Id);

        Assert.True(iptal.BasariliMi);
        Assert.False(tekrarIptal.BasariliMi);
        Assert.Empty(await _service.GetAktifAsync(DateTime.Today, "09:00 - 11:00"));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
