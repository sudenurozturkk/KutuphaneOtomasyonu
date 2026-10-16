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
        public const int MaksimumHataliDeneme = 5;
        public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(1);
        public const int MinimumSifreUzunlugu = 8;

        /// <summary>Demo kurulumla gelen parolalar; giriş sonrası değiştirilmeli ve yeni parola olarak kabul edilmez.</summary>
        private static readonly string[] BilinenVarsayilanSifreler = { "admin123", "123456" };

        private readonly TimeProvider _zaman;
        private readonly Dictionary<string, (int HataSayisi, DateTimeOffset? KilitBitis)> _denemeler =
            new(StringComparer.OrdinalIgnoreCase);

        public KimlikDogrulamaService(TimeProvider zaman)
        {
            _zaman = zaman;
        }

        public static bool SifreDegistirilmeli(string sifre) =>
            sifre.Length < MinimumSifreUzunlugu || BilinenVarsayilanSifreler.Contains(sifre);

        /// <summary>
        /// Kullanıcı adı ve parolayı doğrular. Başarılıysa son giriş zamanını ve gerekiyorsa parola özetini günceller,
        /// denetim kaydı yazar ve kullanıcıyı döner.
        /// </summary>
        public async Task<(GirisSonuc Sonuc, Kullanici? Kullanici)> DogrulaAsync(
            string kullaniciAdi, string sifre, KutuphaneDbContext context)
        {
            kullaniciAdi = kullaniciAdi?.Trim() ?? string.Empty;
            sifre ??= string.Empty;
            var simdi = _zaman.GetUtcNow();

            lock (_denemeler)
            {
                if (_denemeler.TryGetValue(kullaniciAdi, out var durum) && durum.KilitBitis > simdi)
                {
                    var kalan = (int)Math.Ceiling((durum.KilitBitis.Value - simdi).TotalSeconds);
                    return (GirisSonuc.Hata($"Çok sayıda hatalı deneme. {kalan} saniye sonra tekrar deneyin."), null);
                }
            }

            var kullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciAdi == kullaniciAdi && k.Aktif);

            // PBKDF2 kasıtlı olarak yavaştır; çağıran iş parçacığını (arayüz veya istek) bloklamamak için arka planda çalışır.
            bool dogru;
            if (kullanici == null)
            {
                await Task.Run(() => SifreHasher.SahteDogrulama(sifre));
                dogru = false;
            }
            else
            {
                var ozet = kullanici.SifreHash;
                dogru = await Task.Run(() => SifreHasher.Dogrula(sifre, ozet));
            }

            if (!dogru)
            {
                lock (_denemeler)
                {
                    var onceki = _denemeler.TryGetValue(kullaniciAdi, out var d) ? d.HataSayisi : 0;
                    var yeni = onceki + 1;
                    _denemeler[kullaniciAdi] = yeni >= MaksimumHataliDeneme
                        ? (0, simdi + KilitSuresi)
                        : (yeni, null);
                }

                // Bilinmeyen kullanıcı adları kaydedilmez: yanlış alana yazılmış bir parola olabilir.
                await DenetimService.KaydetAsync(context, DenetimService.Islemler.GirisBasarisiz,
                    kullanici != null ? $"Kullanici #{kullanici.Id}" : "Bilinmeyen kullanici adi",
                    kullanici?.Id, LogSeviyesi.Uyari);
                return (GirisSonuc.Hata("Kullanıcı adı veya şifre hatalı."), null);
            }

            lock (_denemeler)
                _denemeler.Remove(kullaniciAdi);

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
            var mevcutOzet = kullanici.SifreHash;
            if (!await Task.Run(() => SifreHasher.Dogrula(mevcutSifre, mevcutOzet))) return GirisSonuc.Hata("Mevcut şifre hatalı.");
            if (string.IsNullOrEmpty(yeniSifre) || yeniSifre.Length < MinimumSifreUzunlugu)
                return GirisSonuc.Hata($"Yeni şifre en az {MinimumSifreUzunlugu} karakter olmalıdır.");
            if (yeniSifre == mevcutSifre) return GirisSonuc.Hata("Yeni şifre mevcut şifreyle aynı olamaz.");
            if (BilinenVarsayilanSifreler.Contains(yeniSifre)) return GirisSonuc.Hata("Bu şifre demo şifresidir; başka bir şifre seçin.");
            if (string.Equals(yeniSifre, kullanici.KullaniciAdi, StringComparison.OrdinalIgnoreCase))
                return GirisSonuc.Hata("Şifre kullanıcı adıyla aynı olamaz.");

            kullanici.SifreHash = await Task.Run(() => SifreHasher.Hashle(yeniSifre));
            DenetimService.Ekle(context, DenetimService.Islemler.SifreDegistirildi, $"Kullanici #{kullanici.Id}", kullanici.Id);
            await context.SaveChangesAsync();
            return GirisSonuc.BasariliSonuc();
        }
    }
}
