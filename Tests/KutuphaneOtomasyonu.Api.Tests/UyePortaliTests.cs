using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Api.Tests;

/// <summary>Öğrenci portalı: üye girişi, kendi kayıtlarına erişim ve personel tarafından hesap açılması.</summary>
public sealed class UyePortaliTests : IAsyncLifetime
{
    private const string UyeSifresi = "Ogrenci-Sifre-1";
    private readonly TestUygulamasi _uygulama = new();

    public async Task InitializeAsync()
    {
        await _uygulama.HazirlaAsync();
        using var db = _uygulama.YeniContext();
        var uye = await db.Uyeler.SingleAsync(u => u.Id == _uygulama.UyeId);
        uye.PortalSifreHash = SifreHasher.Hashle(UyeSifresi);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _uygulama.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage yanit) =>
        JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

    private async Task<HttpClient> UyeGirisiAsync(string giris, string sifre)
    {
        var istemci = _uygulama.IstemciOlustur();
        var yanit = await istemci.PostAsJsonAsync("/api/uye-oturum/giris", new { giris, sifre });
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        return istemci;
    }

    [Fact]
    public async Task UyeGirisi_CerezAyriVeGuvenli_YanlisSifre401()
    {
        var istemci = _uygulama.IstemciOlustur();
        var hatali = await istemci.PostAsJsonAsync("/api/uye-oturum/giris", new { giris = _uygulama.UyeOgrenciNo, sifre = "yanlis" });
        Assert.Equal(HttpStatusCode.Unauthorized, hatali.StatusCode);

        var yanit = await istemci.PostAsJsonAsync("/api/uye-oturum/giris", new { giris = _uygulama.UyeOgrenciNo, sifre = UyeSifresi });
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var cerez = Assert.Single(yanit.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("kutuphane.uye.oturum=", cerez);
        Assert.Contains("httponly", cerez, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cerez, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cerez, StringComparison.OrdinalIgnoreCase);

        var ben = await JsonAsync(await istemci.GetAsync("/api/uye-oturum/ben"));
        Assert.Equal("Ayşe Yılmaz", ben.GetProperty("adSoyad").GetString());
    }

    [Theory]
    [InlineData("/api/oduncler")]
    [InlineData("/api/uyeler")]
    [InlineData("/api/pano")]
    [InlineData("/api/rezervasyonlar")]
    public async Task UyeOturumu_PersonelUclarinaErisemez(string yol)
    {
        var uye = await UyeGirisiAsync(_uygulama.UyeOgrenciNo, UyeSifresi);
        Assert.Equal(HttpStatusCode.Unauthorized, (await uye.GetAsync(yol)).StatusCode);
    }

    [Fact]
    public async Task PersonelOturumu_HesabimUclarinaErisemez()
    {
        var personel = await _uygulama.GirisYapmisIstemciAsync("fatma", TestUygulamasi.KutuphaneciSifresi);
        Assert.Equal(HttpStatusCode.Unauthorized, (await personel.GetAsync("/api/hesabim")).StatusCode);
    }

    [Fact]
    public async Task Hesabim_KendiOduncleriniGorur_Uzatir_BaskasininkiniGoremez()
    {
        // Personel iki farklı üyeye ödünç verir.
        int digerUyeId;
        using (var db = _uygulama.YeniContext())
        {
            var diger = new Uye
            {
                Ad = "Başka", Soyad = "Üye", Email = "baska@ornek.edu.tr", OgrenciNo = "2026000999", PasaportNo = "P1234567",
                MaxOduncAdet = 3, MaxOduncSuresi = 14, UyelikBitisTarihi = DateTime.Now.AddYears(1)
            };
            db.Uyeler.Add(diger);
            await db.SaveChangesAsync();
            digerUyeId = diger.Id;
        }
        var personel = await _uygulama.GirisYapmisIstemciAsync("fatma", TestUygulamasi.KutuphaneciSifresi);
        var kendi = await JsonAsync(await personel.PostAsJsonAsync("/api/oduncler", new { kitapId = _uygulama.KitapId, uyeId = _uygulama.UyeId }));
        var baskasi = await JsonAsync(await personel.PostAsJsonAsync("/api/oduncler", new { kitapId = _uygulama.KitapId, uyeId = digerUyeId }));

        var uye = await UyeGirisiAsync(_uygulama.UyeOgrenciNo, UyeSifresi);
        var ozet = await JsonAsync(await uye.GetAsync("/api/hesabim"));
        Assert.Equal(1, ozet.GetProperty("aktifOduncSayisi").GetInt32());
        Assert.False(ozet.TryGetProperty("tcKimlikNo", out _)); // portal özeti kimlik numarası taşımaz

        var oduncler = await JsonAsync(await uye.GetAsync("/api/hesabim/oduncler"));
        Assert.Equal(1, oduncler.GetArrayLength());
        Assert.Equal(kendi.GetProperty("id").GetInt32(), oduncler[0].GetProperty("id").GetInt32());

        var uzat = await uye.PostAsync($"/api/hesabim/oduncler/{kendi.GetProperty("id").GetInt32()}/uzat", null);
        Assert.Equal(HttpStatusCode.OK, uzat.StatusCode);
        Assert.Equal(1, (await JsonAsync(uzat)).GetProperty("odunc").GetProperty("uzatmaSayisi").GetInt32());

        var baskasininki = await uye.PostAsync($"/api/hesabim/oduncler/{baskasi.GetProperty("id").GetInt32()}/uzat", null);
        Assert.Equal(HttpStatusCode.NotFound, baskasininki.StatusCode);
        using var dogrulama = _uygulama.YeniContext();
        Assert.Equal(0, (await dogrulama.Oduncler.SingleAsync(o => o.Id == baskasi.GetProperty("id").GetInt32())).UzatmaSayisi);
    }

    [Fact]
    public async Task Hesabim_RezerveEder_IptalEder_BaskasininRezervasyonunuIptalEdemez()
    {
        var uye = await UyeGirisiAsync(_uygulama.UyeOgrenciNo, UyeSifresi);

        var olustur = await uye.PostAsJsonAsync("/api/hesabim/rezervasyonlar", new { kitapId = _uygulama.KitapId });
        Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);
        var id = (await JsonAsync(olustur)).GetProperty("id").GetInt32();
        Assert.Equal(_uygulama.UyeId, (await JsonAsync(olustur)).GetProperty("uyeId").GetInt32());

        Assert.Equal(1, (await JsonAsync(await uye.GetAsync("/api/hesabim/rezervasyonlar"))).GetArrayLength());

        // Başka üyenin rezervasyonu
        int digerRezervasyon;
        using (var db = _uygulama.YeniContext())
        {
            var diger = new Uye { Ad = "X", Soyad = "Y", Email = "x@ornek.edu.tr", PasaportNo = "P7654321", MaxOduncAdet = 3, MaxOduncSuresi = 14 };
            db.Uyeler.Add(diger);
            await db.SaveChangesAsync();
            var r = new Rezervasyon { KitapId = _uygulama.KitapId, UyeId = diger.Id, GecerlilikTarihi = DateTime.Now.AddDays(7), SiraNumarasi = 2 };
            db.Rezervasyonlar.Add(r);
            await db.SaveChangesAsync();
            digerRezervasyon = r.Id;
        }
        Assert.Equal(HttpStatusCode.NotFound, (await uye.PostAsync($"/api/hesabim/rezervasyonlar/{digerRezervasyon}/iptal", null)).StatusCode);

        var iptal = await uye.PostAsync($"/api/hesabim/rezervasyonlar/{id}/iptal", null);
        Assert.Equal(HttpStatusCode.OK, iptal.StatusCode);
        Assert.Equal("Iptal", (await JsonAsync(iptal)).GetProperty("durumu").GetString());
    }

    [Fact]
    public async Task PersonelGeciciSifreVerir_UyeDegistirmedenIslemYapamaz_SifirlamaEskiOturumuDusurur()
    {
        var personel = await _uygulama.GirisYapmisIstemciAsync("fatma", TestUygulamasi.KutuphaneciSifresi);
        var eskiOturum = await UyeGirisiAsync(_uygulama.UyeOgrenciNo, UyeSifresi);
        Assert.Equal(HttpStatusCode.OK, (await eskiOturum.GetAsync("/api/hesabim")).StatusCode);

        var yanit = await personel.PostAsync($"/api/uyeler/{_uygulama.UyeId}/portal-sifresi", null);
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal("no-store", yanit.Headers.CacheControl?.ToString());
        var gecici = (await JsonAsync(yanit)).GetProperty("geciciSifre").GetString()!;

        // Şifre sıfırlandı: önceki oturum geçersiz.
        Assert.Equal(HttpStatusCode.Unauthorized, (await eskiOturum.GetAsync("/api/hesabim")).StatusCode);

        var uye = _uygulama.IstemciOlustur();
        var giris = await JsonAsync(await uye.PostAsJsonAsync("/api/uye-oturum/giris", new { giris = "AYSE@ornek.edu.tr", sifre = gecici }));
        Assert.True(giris.GetProperty("sifreDegistirmeli").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await uye.GetAsync("/api/hesabim")).StatusCode);

        var degistir = await uye.PostAsJsonAsync("/api/uye-oturum/sifre", new { mevcutSifre = gecici, yeniSifre = "Benim-Yeni-Sifrem" });
        Assert.Equal(HttpStatusCode.OK, degistir.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await uye.GetAsync("/api/hesabim")).StatusCode);

        // Yardımcı personel hesap açamaz.
        var yardimci = await _uygulama.GirisYapmisIstemciAsync("ali", TestUygulamasi.YardimciSifresi);
        Assert.Equal(HttpStatusCode.Forbidden, (await yardimci.PostAsync($"/api/uyeler/{_uygulama.UyeId}/portal-sifresi", null)).StatusCode);
    }

    [Fact]
    public async Task YasaklananUyeninOturumuDuser()
    {
        var uye = await UyeGirisiAsync(_uygulama.UyeOgrenciNo, UyeSifresi);
        using (var db = _uygulama.YeniContext())
        {
            var kayit = await db.Uyeler.SingleAsync(u => u.Id == _uygulama.UyeId);
            kayit.Durumu = UyeDurumu.Yasakli;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await uye.GetAsync("/api/hesabim")).StatusCode);
    }
}
