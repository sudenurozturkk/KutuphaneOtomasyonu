using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KutuphaneOtomasyonu.Api.Tests;

public sealed class WebApiTests : IAsyncLifetime
{
    private readonly TestUygulamasi _uygulama = new();

    public Task InitializeAsync() => _uygulama.HazirlaAsync();

    public Task DisposeAsync()
    {
        _uygulama.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage yanit) =>
        JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

    private Task<HttpClient> KutuphaneciAsync() =>
        _uygulama.GirisYapmisIstemciAsync("fatma", TestUygulamasi.KutuphaneciSifresi);

    // ---------- Katalog ve erişim ----------

    [Fact]
    public async Task Katalog_OturumsuzAranir_NushaDurumlariGorunur()
    {
        var istemci = _uygulama.IstemciOlustur();

        var arama = await istemci.GetAsync("/api/katalog?arama=temiz");
        Assert.Equal(HttpStatusCode.OK, arama.StatusCode);
        var sonuc = await JsonAsync(arama);
        Assert.Equal(1, sonuc.GetProperty("toplam").GetInt32());
        var kitap = sonuc.GetProperty("kayitlar")[0];
        Assert.Equal("Temiz Kod", kitap.GetProperty("baslik").GetString());
        Assert.Equal(2, kitap.GetProperty("mevcutAdet").GetInt32());

        var detay = await JsonAsync(await istemci.GetAsync($"/api/katalog/{_uygulama.KitapId}"));
        var nushalar = detay.GetProperty("nushalar");
        Assert.Equal(2, nushalar.GetArrayLength());
        Assert.All(nushalar.EnumerateArray(), n => Assert.Equal("Rafta", n.GetProperty("durum").GetString()));

        Assert.Equal(HttpStatusCode.NotFound, (await istemci.GetAsync("/api/katalog/99999")).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/oduncler")]
    [InlineData("GET", "/api/uyeler")]
    [InlineData("GET", "/api/rezervasyonlar")]
    [InlineData("GET", "/api/pano")]
    [InlineData("POST", "/api/kitaplar")]
    [InlineData("POST", "/api/oduncler/iade-barkod")]
    public async Task PersonelUclari_OturumOlmadan401Doner(string yontem, string yol)
    {
        var istemci = _uygulama.IstemciOlustur();
        var istek = new HttpRequestMessage(new HttpMethod(yontem), yol);
        if (yontem == "POST") istek.Content = JsonContent.Create(new { });

        var yanit = await istemci.SendAsync(istek);

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    // ---------- Oturum ----------

    [Fact]
    public async Task Giris_CerezGuvenliHttpOnlyVeSameSiteStrict()
    {
        var istemci = _uygulama.IstemciOlustur();
        var yanit = await istemci.PostAsJsonAsync("/api/oturum/giris",
            new { kullaniciAdi = "fatma", sifre = TestUygulamasi.KutuphaneciSifresi });

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var cerez = Assert.Single(yanit.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cerez, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cerez, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cerez, StringComparison.OrdinalIgnoreCase);

        var ben = await JsonAsync(await istemci.GetAsync("/api/oturum/ben"));
        Assert.Equal("Kutuphaneci", ben.GetProperty("rol").GetString());
        Assert.False(ben.GetProperty("sifreDegistirmeli").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await istemci.PostAsync("/api/oturum/cikis", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await istemci.GetAsync("/api/oduncler")).StatusCode);
    }

    [Fact]
    public async Task Giris_YanlisSifre401_ArtArdaHatalardaHesapKilitlenir()
    {
        var istemci = _uygulama.IstemciOlustur();

        for (var i = 0; i < KimlikDogrulamaService.MaksimumHataliDeneme; i++)
        {
            var hatali = await istemci.PostAsJsonAsync("/api/oturum/giris", new { kullaniciAdi = "fatma", sifre = "yanlis" });
            Assert.Equal(HttpStatusCode.Unauthorized, hatali.StatusCode);
        }

        var dogruAmaKilitli = await istemci.PostAsJsonAsync("/api/oturum/giris",
            new { kullaniciAdi = "fatma", sifre = TestUygulamasi.KutuphaneciSifresi });
        Assert.Equal(HttpStatusCode.Unauthorized, dogruAmaKilitli.StatusCode);
        Assert.Contains("saniye", (await JsonAsync(dogruAmaKilitli)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task PasiflestirilenPersonelinOturumuHemenDuser()
    {
        var istemci = await KutuphaneciAsync();
        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/api/oduncler")).StatusCode);

        using (var db = _uygulama.YeniContext())
        {
            var kullanici = await db.Kullanicilar.SingleAsync(k => k.Id == _uygulama.KutuphaneciId);
            kullanici.Aktif = false;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await istemci.GetAsync("/api/oduncler")).StatusCode);
    }

    // ---------- Ödünç akışı ----------

    [Fact]
    public async Task OduncVer_BarkodVeOgrenciNoIle_IadeBarkodla_TamTur()
    {
        var istemci = await KutuphaneciAsync();
        string barkod;
        using (var db = _uygulama.YeniContext())
            barkod = (await db.KitapNushalari.OrderBy(n => n.Barkod).FirstAsync(n => n.KitapId == _uygulama.KitapId)).Barkod;

        var verme = await istemci.PostAsJsonAsync("/api/oduncler",
            new { nushaBarkodu = barkod.ToLowerInvariant(), uye = _uygulama.UyeOgrenciNo });
        Assert.Equal(HttpStatusCode.Created, verme.StatusCode);
        var odunc = await JsonAsync(verme);
        Assert.Equal(barkod, odunc.GetProperty("nushaBarkod").GetString());
        Assert.Equal("Ayşe Yılmaz", odunc.GetProperty("uyeAdSoyad").GetString());
        Assert.Equal("Aktif", odunc.GetProperty("durumu").GetString());

        var aktifler = await JsonAsync(await istemci.GetAsync("/api/oduncler"));
        Assert.Equal(1, aktifler.GetArrayLength());

        var ikinciKez = await istemci.PostAsJsonAsync("/api/oduncler", new { nushaBarkodu = barkod, uyeId = _uygulama.UyeId });
        Assert.Equal(HttpStatusCode.BadRequest, ikinciKez.StatusCode);

        var iade = await istemci.PostAsJsonAsync("/api/oduncler/iade-barkod", new { barkod });
        Assert.Equal(HttpStatusCode.OK, iade.StatusCode);
        Assert.Equal("Iade", (await JsonAsync(iade)).GetProperty("odunc").GetProperty("durumu").GetString());

        using var dogrulama = _uygulama.YeniContext();
        var nusha = await dogrulama.KitapNushalari.AsNoTracking().SingleAsync(n => n.Barkod == barkod);
        Assert.Equal(NushaDurumu.Rafta, nusha.Durum);
        var kitap = await dogrulama.Kitaplar.AsNoTracking().SingleAsync(k => k.Id == _uygulama.KitapId);
        Assert.Equal(2, kitap.MevcutAdet);
        var loglar = await dogrulama.AktiviteLoglar.AsNoTracking()
            .Where(l => l.Islem == DenetimService.Islemler.OduncVerildi || l.Islem == DenetimService.Islemler.IadeAlindi)
            .ToListAsync();
        Assert.Equal(2, loglar.Count);
        Assert.All(loglar, l => Assert.Equal(_uygulama.KutuphaneciId, l.KullaniciId));
    }

    [Fact]
    public async Task OduncVer_HataliIstekler_AnlasilirMesajlaReddedilir()
    {
        var istemci = await KutuphaneciAsync();

        var bilinmeyenBarkod = await istemci.PostAsJsonAsync("/api/oduncler", new { nushaBarkodu = "YOK123", uyeId = _uygulama.UyeId });
        Assert.Equal(HttpStatusCode.BadRequest, bilinmeyenBarkod.StatusCode);
        Assert.Contains("nüsha", (await JsonAsync(bilinmeyenBarkod)).GetProperty("detail").GetString());

        var uyeYok = await istemci.PostAsJsonAsync("/api/oduncler", new { kitapId = _uygulama.KitapId });
        Assert.Equal(HttpStatusCode.BadRequest, uyeYok.StatusCode);

        var bilinmeyenOdunc = await istemci.PostAsJsonAsync("/api/oduncler/99999/iade", new { durum = "Iyi" });
        Assert.Equal(HttpStatusCode.NotFound, bilinmeyenOdunc.StatusCode);
    }

    [Fact]
    public async Task EsZamanliIstekler_SonNushayiYalnizcaBirKezVerir()
    {
        // Aynı kitabın iki nüshası, iki farklı personel oturumundan aynı anda dört farklı üyeye verilmek isteniyor.
        using (var db = _uygulama.YeniContext())
        {
            for (var i = 0; i < 3; i++)
                db.Uyeler.Add(new Uye
                {
                    Ad = $"Uye{i}", Soyad = "Test", Email = $"uye{i}@ornek.edu.tr", OgrenciNo = $"20269900{i:D2}",
                    TCKimlikNo = "", PasaportNo = $"P0000{i}X", MaxOduncAdet = 3, MaxOduncSuresi = 14,
                    UyelikBitisTarihi = DateTime.Now.AddYears(1)
                });
            await db.SaveChangesAsync();
        }

        List<int> uyeIdleri;
        using (var db = _uygulama.YeniContext())
            uyeIdleri = await db.Uyeler.Select(u => u.Id).ToListAsync();

        var istemci1 = await KutuphaneciAsync();
        var istemci2 = await _uygulama.GirisYapmisIstemciAsync("ali", TestUygulamasi.YardimciSifresi);
        var gorevler = uyeIdleri.Select((uyeId, i) => (i % 2 == 0 ? istemci1 : istemci2)
            .PostAsJsonAsync("/api/oduncler", new { kitapId = _uygulama.KitapId, uyeId })).ToList();
        var yanitlar = await Task.WhenAll(gorevler);

        Assert.Equal(2, yanitlar.Count(y => y.StatusCode == HttpStatusCode.Created));
        Assert.Equal(uyeIdleri.Count - 2, yanitlar.Count(y => y.StatusCode == HttpStatusCode.BadRequest));
        using var dogrulama = _uygulama.YeniContext();
        var kitap = await dogrulama.Kitaplar.AsNoTracking().SingleAsync(k => k.Id == _uygulama.KitapId);
        Assert.Equal(0, kitap.MevcutAdet);
        Assert.Equal(2, await dogrulama.Oduncler.CountAsync(o => o.Durumu == OduncDurumu.Aktif));
    }

    // ---------- Roller ve kişisel veri ----------

    [Fact]
    public async Task YardimciPersonel_KimlikMaskeliGorur_KitapVeUyeEkleyemez()
    {
        var yardimci = await _uygulama.GirisYapmisIstemciAsync("ali", TestUygulamasi.YardimciSifresi);

        var detay = await JsonAsync(await yardimci.GetAsync($"/api/uyeler/{_uygulama.UyeId}"));
        Assert.True(detay.GetProperty("kisiselVeriMaskeli").GetBoolean());
        Assert.Equal(new string('•', 7) + "0078", detay.GetProperty("tcKimlikNo").GetString());
        Assert.DoesNotContain("05551234567", detay.GetProperty("telefon").GetString());

        Assert.Equal(HttpStatusCode.Forbidden,
            (await yardimci.PostAsJsonAsync("/api/kitaplar", new { baslik = "X", toplamAdet = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await yardimci.PostAsJsonAsync("/api/uyeler", new { ad = "X" })).StatusCode);

        var kutuphaneci = await KutuphaneciAsync();
        var tam = await JsonAsync(await kutuphaneci.GetAsync($"/api/uyeler/{_uygulama.UyeId}"));
        Assert.Equal(_uygulama.UyeTc, tam.GetProperty("tcKimlikNo").GetString());
    }

    [Fact]
    public async Task Kutuphaneci_KitapEkler_GecersizIsbnReddedilir()
    {
        var istemci = await KutuphaneciAsync();

        var gecersiz = await istemci.PostAsJsonAsync("/api/kitaplar", new
        {
            baslik = "Deneme", yazar = "Yazar", isbn = "9780132350885", kategori = "Yazılım", toplamAdet = 1, yayinYili = 2020
        });
        Assert.Equal(HttpStatusCode.BadRequest, gecersiz.StatusCode);

        var gecerli = await istemci.PostAsJsonAsync("/api/kitaplar", new
        {
            baslik = "Tasarım Desenleri", yazar = "Gamma", isbn = "9780201633610", kategori = "Yazılım",
            toplamAdet = 3, yayinYili = 1994, rafKonumu = "B-02"
        });
        Assert.Equal(HttpStatusCode.Created, gecerli.StatusCode);
        var id = (await JsonAsync(gecerli)).GetProperty("id").GetInt32();

        var detay = await JsonAsync(await istemci.GetAsync($"/api/katalog/{id}"));
        Assert.Equal(3, detay.GetProperty("nushalar").GetArrayLength());
    }

    [Fact]
    public async Task Rezervasyon_OlusturulurVeIptalEdilir()
    {
        var istemci = await KutuphaneciAsync();

        var olustur = await istemci.PostAsJsonAsync("/api/rezervasyonlar", new { kitapId = _uygulama.KitapId, uyeId = _uygulama.UyeId });
        Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);
        var id = (await JsonAsync(olustur)).GetProperty("id").GetInt32();

        var iptal = await istemci.PostAsync($"/api/rezervasyonlar/{id}/iptal", null);
        Assert.Equal(HttpStatusCode.OK, iptal.StatusCode);
        Assert.Equal("Iptal", (await JsonAsync(iptal)).GetProperty("durumu").GetString());
    }

    // ---------- Arayüz ve güvenlik başlıkları ----------

    [Fact]
    public async Task WebArayuzu_SunulurVeGuvenlikBasliklariEklenir()
    {
        var istemci = _uygulama.IstemciOlustur();

        var sayfa = await istemci.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, sayfa.StatusCode);
        Assert.Contains("<title>", await sayfa.Content.ReadAsStringAsync());
        Assert.Contains("default-src 'self'", sayfa.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", sayfa.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", sayfa.Headers.GetValues("X-Frame-Options").Single());
    }
}

public sealed class ZorunluSifreDegisimiTests : IAsyncLifetime
{
    private readonly TestUygulamasi _uygulama = new(sifreDegisimiZorunlu: true);

    public async Task InitializeAsync()
    {
        await _uygulama.HazirlaAsync();
        using var db = _uygulama.YeniContext();
        var kullanici = await db.Kullanicilar.SingleAsync(k => k.KullaniciAdi == "fatma");
        kullanici.SifreHash = SifreHasher.Hashle("admin123");
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _uygulama.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task VarsayilanSifreyleGiris_SifreDegisenekadarIslemYapamaz()
    {
        var istemci = _uygulama.IstemciOlustur();
        var giris = await istemci.PostAsJsonAsync("/api/oturum/giris", new { kullaniciAdi = "fatma", sifre = "admin123" });
        Assert.Equal(HttpStatusCode.OK, giris.StatusCode);
        Assert.True(JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement
            .GetProperty("sifreDegistirmeli").GetBoolean());

        Assert.Equal(HttpStatusCode.Forbidden, (await istemci.GetAsync("/api/oduncler")).StatusCode);

        var degistir = await istemci.PostAsJsonAsync("/api/oturum/sifre",
            new { mevcutSifre = "admin123", yeniSifre = "Yeni-Guclu-Sifre-1" });
        Assert.Equal(HttpStatusCode.OK, degistir.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/api/oduncler")).StatusCode);
    }
}
