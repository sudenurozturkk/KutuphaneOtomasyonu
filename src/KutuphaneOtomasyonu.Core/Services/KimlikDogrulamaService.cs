using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Personel kimlik doğrulaması: parola kontrolü, hatalı deneme kilidi, eski özet yükseltme ve parola değişimi.
    /// Oturum durumu tutmaz; bu yüzden hem masaüstü oturumu (<see cref="AuthService"/>) hem de çok kullanıcılı
    /// Web API tarafından paylaşılabilir. Kilit sayaçları örnek başına tutulur ve iş parçacığı güvenlidir.
    /// </summary>
    public sealed class KimlikDogrulamaService
    {
        /// <summary>Art arda bu kadar hatalı denemeden sonra hesap geçici olarak kilitlenir.</summary>
        public const int MaksimumHataliDeneme = GirisKilidi.MaksimumHataliDeneme;
        public static readonly TimeSpan KilitSuresi = GirisKilidi.KilitSuresi;
        public const int MinimumSifreUzunlugu = 8;

        /// <summary>Demo kurulumla gelen parolalar; giriş sonrası değiştirilmeli ve yeni parola olarak kabul edilmez.</summary>
        private static readonly string[] BilinenVarsayilanSifreler = { "admin123", "123456", "ogrenci123" };

        private readonly GirisKilidi _kilit;

        public KimlikDogrulamaService(TimeProvider zaman)
        {
            _kilit = new GirisKilidi(zaman);
        }

        public static bool SifreDegistirilmeli(string sifre) =>
            sifre.Length < MinimumSifreUzunlugu || BilinenVarsayilanSifreler.Contains(sifre);

        /// <summary>Yeni parola politikası (personel ve üye için ortak). Uygunsa <c>null</c> döner.</summary>
        public static string? YeniSifreHatasi(string mevcutSifre, string? yeniSifre, string hesapAdi)
        {
            if (string.IsNullOrEmpty(yeniSifre) || yeniSifre.Length < MinimumSifreUzunlugu)
                return $"Yeni şifre en az {MinimumSifreUzunlugu} karakter olmalıdır.";
            if (yeniSifre == mevcutSifre) return "Yeni şifre mevcut şifreyle aynı olamaz.";
            if (BilinenVarsayilanSifreler.Contains(yeniSifre)) return "Bu şifre demo şifresidir; başka bir şifre seçin.";
            if (!string.IsNullOrEmpty(hesapAdi) && string.Equals(yeniSifre, hesapAdi, StringComparison.OrdinalIgnoreCase))
                return "Şifre kullanıcı adıyla aynı olamaz.";
            return null;
        }

        /// <summary>
        /// Kullanıcı adı ve parolayı doğrular. Başarılıysa son giriş zamanını ve gerekiyorsa parola özetini günceller,
        /// denetim kaydı yazar ve kullanıcıyı döner.
        /// </summary>
        public async Task<(GirisSonuc Sonuc, Kullanici? Kullanici)> DogrulaAsync(
            string kullaniciAdi, string sifre, KutuphaneDbContext context)
        {
            kullaniciAdi = kullaniciAdi?.Trim() ?? string.Empty;
            sifre ??= string.Empty;

            var kilitMesaji = _kilit.KilitMesaji(kullaniciAdi);
            if (kilitMesaji != null) return (GirisSonuc.Hata(kilitMesaji), null);

            var kullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciAdi == kullaniciAdi && k.Aktif);

            // PBKDF2 kasıtlı olarak yavaştır; çağıran iş parçacığını (arayüz veya istek) bloklamamak için arka planda çalışır.
            if (!await SifreDogruMuAsync(sifre, kullanici?.SifreHash))
            {
                _kilit.HataKaydet(kullaniciAdi);

                // Bilinmeyen kullanıcı adları kaydedilmez: yanlış alana yazılmış bir parola olabilir.
                await DenetimService.KaydetAsync(context, DenetimService.Islemler.GirisBasarisiz,
                    kullanici != null ? $"Kullanici #{kullanici.Id}" : "Bilinmeyen kullanici adi",
                    kullanici?.Id, LogSeviyesi.Uyari);
                return (GirisSonuc.Hata("Kullanıcı adı veya şifre hatalı."), null);
            }

            _kilit.Temizle(kullaniciAdi);

            if (SifreHasher.YukseltmeGerekli(kullanici!.SifreHash))
                kullanici.SifreHash = await Task.Run(() => SifreHasher.Hashle(sifre));

            kullanici.SonGirisTarihi = DateTime.Now;
            DenetimService.Ekle(context, DenetimService.Islemler.GirisBasarili, $"Kullanici #{kullanici.Id}", kullanici.Id);
            await context.SaveChangesAsync();

            return (GirisSonuc.BasariliSonuc(SifreDegistirilmeli(sifre)), kullanici);
        }

        /// <summary>Kullanıcının parolasını, mevcut parolayı doğruladıktan sonra değiştirir.</summary>
        public async Task<GirisSonuc> SifreDegistirAsync(
            KutuphaneDbContext context, int kullaniciId, string mevcutSifre, string yeniSifre)
        {
            mevcutSifre ??= string.Empty;
            var kullanici = await context.Kullanicilar.FirstOrDefaultAsync(k => k.Id == kullaniciId && k.Aktif);
            if (kullanici == null) return GirisSonuc.Hata("Kullanıcı bulunamadı.");
            if (!await SifreDogruMuAsync(mevcutSifre, kullanici.SifreHash)) return GirisSonuc.Hata("Mevcut şifre hatalı.");

            var hata = YeniSifreHatasi(mevcutSifre, yeniSifre, kullanici.KullaniciAdi);
            if (hata != null) return GirisSonuc.Hata(hata);

            kullanici.SifreHash = await Task.Run(() => SifreHasher.Hashle(yeniSifre));
            DenetimService.Ekle(context, DenetimService.Islemler.SifreDegistirildi, $"Kullanici #{kullanici.Id}", kullanici.Id);
            await context.SaveChangesAsync();
            return GirisSonuc.BasariliSonuc();
        }

        /// <summary>Özet yoksa da aynı süreyi harcar; böylece yanıt süresinden hesabın varlığı anlaşılmaz.</summary>
        internal static async Task<bool> SifreDogruMuAsync(string sifre, string? ozet)
        {
            if (string.IsNullOrEmpty(ozet))
            {
                await Task.Run(() => SifreHasher.SahteDogrulama(sifre));
                return false;
            }
            return await Task.Run(() => SifreHasher.Dogrula(sifre, ozet));
        }
    }

    /// <summary>Hesap başına art arda hatalı giriş sayacı ve geçici kilit (iş parçacığı güvenli).</summary>
    public sealed class GirisKilidi
    {
        public const int MaksimumHataliDeneme = 5;
        public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(1);

        private readonly TimeProvider _zaman;
        private readonly Dictionary<string, (int HataSayisi, DateTimeOffset? KilitBitis)> _denemeler =
            new(StringComparer.OrdinalIgnoreCase);

        public GirisKilidi(TimeProvider zaman) => _zaman = zaman;

        /// <summary>Hesap kilitliyse kullanıcıya gösterilecek mesaj, değilse <c>null</c>.</summary>
        public string? KilitMesaji(string anahtar)
        {
            var simdi = _zaman.GetUtcNow();
            lock (_denemeler)
            {
                if (_denemeler.TryGetValue(anahtar, out var durum) && durum.KilitBitis > simdi)
                {
                    var kalan = (int)Math.Ceiling((durum.KilitBitis.Value - simdi).TotalSeconds);
                    return $"Çok sayıda hatalı deneme. {kalan} saniye sonra tekrar deneyin.";
                }
            }
            return null;
        }

        public void HataKaydet(string anahtar)
        {
            var simdi = _zaman.GetUtcNow();
            lock (_denemeler)
            {
                var onceki = _denemeler.TryGetValue(anahtar, out var d) ? d.HataSayisi : 0;
                var yeni = onceki + 1;
                _denemeler[anahtar] = yeni >= MaksimumHataliDeneme ? (0, simdi + KilitSuresi) : (yeni, null);
            }
        }

        public void Temizle(string anahtar)
        {
            lock (_denemeler)
                _denemeler.Remove(anahtar);
        }
    }
}
