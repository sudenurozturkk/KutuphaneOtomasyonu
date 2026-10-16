using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Bildirim servisi - Observer Pattern implementasyonu.
    /// </summary>
    public class BildirimService
    {
        private readonly KutuphaneDbContext _context;

        // Observer pattern için event
        public event Action<Bildirim>? YeniBildirim;

        public BildirimService(KutuphaneDbContext context)
        {
            _context = context;
        }

        public async Task GonderAsync(int uyeId, string baslik, string mesaj, BildirimTipi tipi)
        {
            var bildirim = Ekle(uyeId, baslik, mesaj, tipi);
            await _context.SaveChangesAsync();

            YeniBildirim?.Invoke(bildirim);
        }

        /// <summary>
        /// Bildirimi kaydetmeden bağlama ekler; çağıranın SaveChanges/transaction'ı ile birlikte kalıcı olur.
        /// </summary>
        public Bildirim Ekle(int uyeId, string baslik, string mesaj, BildirimTipi tipi)
        {
            var bildirim = new Bildirim
            {
                UyeId = uyeId,
                Baslik = baslik,
                Mesaj = mesaj,
                Tipi = tipi,
                OlusturmaTarihi = DateTime.Now
            };

            _context.Bildirimler.Add(bildirim);
            return bildirim;
        }

        public async Task<List<Bildirim>> GetOkunmamisAsync(int uyeId)
        {
            return await _context.Bildirimler
                .Where(b => b.UyeId == uyeId && !b.Okundu)
                .OrderByDescending(b => b.OlusturmaTarihi)
                .ToListAsync();
        }

        public async Task<int> GetOkunmamisSayiAsync(int uyeId)
        {
            return await _context.Bildirimler
                .CountAsync(b => b.UyeId == uyeId && !b.Okundu);
        }

        public async Task OkunduIsaretle(int bildirimId)
        {
            var bildirim = await _context.Bildirimler.FindAsync(bildirimId);
            if (bildirim != null)
            {
                bildirim.Okundu = true;
                bildirim.OkunmaTarihi = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>Yaklaşan iade tarihlerini kontrol eder ve bildirim gönderir.</summary>
        public async Task GecikmeKontrolEtAsync(int hatirlatmaGunSayisi = 2)
        {
            var yaklasanIadeler = await _context.Oduncler
                .Include(o => o.Kitap)
                .Where(o => o.Durumu == OduncDurumu.Aktif &&
                            o.PlanlananIadeTarihi.Date == DateTime.Today.AddDays(hatirlatmaGunSayisi))
                .ToListAsync();

            foreach (var odunc in yaklasanIadeler)
            {
                await GonderAsync(odunc.UyeId,
                    "İade Tarihi Yaklaşıyor",
                    $"'{odunc.Kitap?.Baslik}' kitabının iade tarihi {odunc.PlanlananIadeTarihi:dd.MM.yyyy}. Lütfen zamanında iade ediniz.",
                    BildirimTipi.Uyari);
            }
        }
    }

    /// <summary>
    /// Üye yönetimi servis sınıfı.
    /// </summary>
    public class UyeService
    {
        private readonly KutuphaneDbContext _context;

        public UyeService(KutuphaneDbContext context)
        {
            _context = context;
        }

        public async Task<List<Uye>> GetAllAsync()
        {
            return await _context.Uyeler
                .OrderBy(u => u.Ad)
                .ToListAsync();
        }

        public async Task<List<Uye>> SearchAsync(string? arama = null, UyeTipi? uyeTipi = null, UyeDurumu? durum = null)
        {
            var query = _context.Uyeler.AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                var aramaLower = arama.ToLower();
                query = query.Where(u =>
                    u.Ad.ToLower().Contains(aramaLower) ||
                    u.Soyad.ToLower().Contains(aramaLower) ||
                    u.Email.ToLower().Contains(aramaLower) ||
                    u.OgrenciNo.Contains(aramaLower) ||
                    u.TCKimlikNo.Contains(aramaLower));
            }

            if (uyeTipi.HasValue)
                query = query.Where(u => u.UyeTipi == uyeTipi.Value);

            if (durum.HasValue)
                query = query.Where(u => u.Durumu == durum.Value);

            return await query.OrderBy(u => u.Ad).ThenBy(u => u.Soyad).ToListAsync();
        }

        public async Task<Uye?> GetByIdAsync(int id)
        {
            return await _context.Uyeler
                .Include(u => u.Oduncler).ThenInclude(o => o.Kitap)
                .Include(u => u.Rezervasyonlar).ThenInclude(r => r.Kitap)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Üye tipine göre varsayılan ödünç kotası (demo verisindeki politika ile aynı):
        /// öğrenci ve misafir 3 kitap / 14 gün, akademisyen ve personel 5 kitap / 30 gün.
        /// </summary>
        public static (int MaxAdet, int MaxSure) VarsayilanKota(UyeTipi tip) => tip switch
        {
            UyeTipi.Akademisyen or UyeTipi.Personel => (5, 30),
            _ => (3, 14)
        };

        public async Task<UyeSonuc> EkleAsync(Uye uye, int? kullaniciId)
        {
            Normallestir(uye);
            var hata = AlanlariDogrula(uye, tcDegisti: true, emailDegisti: true) ?? await CakismaAsync(uye, null);
            if (hata != null) return UyeSonuc.Hata(hata);

            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            (uye.MaxOduncAdet, uye.MaxOduncSuresi) = VarsayilanKota(uye.UyeTipi);
            uye.KayitTarihi = DateTime.Now;
            _context.Uyeler.Add(uye);
            await _context.SaveChangesAsync();

            await DenetimService.KaydetAsync(_context, DenetimService.Islemler.UyeEklendi, $"Uye #{uye.Id}", kullaniciId);
            if (islem != null) await islem.CommitAsync();
            return UyeSonuc.Basarili(uye);
        }

        /// <summary>
        /// KVKK kapsamında silme talebi: üyenin kimlik ve iletişim verilerini kalıcı olarak siler, bildirimlerini
        /// kaldırır ve hesabı pasifleştirir. Ödünç/ceza geçmişi istatistik ve muhasebe için anonim olarak kalır.
        /// Aktif ödüncü, ödenmemiş borcu olan üyeler anonimleştirilemez. Yalnızca yönetici yapabilir.
        /// </summary>
        public async Task<UyeSonuc> AnonimlestirAsync(int uyeId, Kullanici? islemYapan)
        {
            if (islemYapan == null || !islemYapan.Aktif || islemYapan.Rol < KullaniciRolu.Admin)
                return UyeSonuc.Hata("Bu işlem yalnızca sistem yöneticisi tarafından yapılabilir.");

            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var uye = await _context.Uyeler.FindAsync(uyeId);
            if (uye == null) return UyeSonuc.Hata("Üye bulunamadı.");
            if (uye.AnonimMi) return UyeSonuc.Hata("Üye zaten anonimleştirilmiş.");
            if (await _context.Oduncler.AnyAsync(o => o.UyeId == uyeId && o.Durumu == OduncDurumu.Aktif))
                return UyeSonuc.Hata("Üyenin aktif ödüncü var; önce iadeleri alın.");
            if (uye.KalanCeza > 0)
                return UyeSonuc.Hata($"Üyenin {uye.KalanCeza:0.00} tutarında ödenmemiş borcu var.");

            uye.Ad = "Anonim";
            uye.Soyad = $"Üye {uye.Id}";
            uye.Email = $"anonim-{uye.Id}@anonim.invalid";
            uye.TCKimlikNo = string.Empty;
            uye.PasaportNo = string.Empty;
            uye.Telefon = string.Empty;
            uye.Adres = string.Empty;
            uye.OgrenciNo = string.Empty;
            uye.FotografYolu = string.Empty;
            uye.Notlar = string.Empty;
            uye.Durumu = UyeDurumu.Pasif;
            uye.AnonimlestirmeTarihi = DateTime.Now;

            await _context.Bildirimler.Where(b => b.UyeId == uyeId).ExecuteDeleteAsync();
            var rezervasyonlar = await _context.Rezervasyonlar
                .Where(r => r.UyeId == uyeId &&
                            (r.Durumu == RezervasyonDurumu.Bekliyor || r.Durumu == RezervasyonDurumu.Onaylandi))
                .ToListAsync();
            foreach (var rezervasyon in rezervasyonlar)
                rezervasyon.Durumu = RezervasyonDurumu.Iptal;

            DenetimService.Ekle(_context, DenetimService.Islemler.UyeAnonimlestirildi, $"Uye #{uye.Id}", islemYapan.Id, LogSeviyesi.Uyari);
            await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return UyeSonuc.Basarili(uye);
        }

        /// <summary>İletişim ve kimlik bilgilerini günceller; üye tipi değişirse kota yeni tipin varsayılanına çekilir.</summary>
        public async Task<UyeSonuc> GuncelleAsync(Uye degisiklik, int? kullaniciId)
        {
            var uye = await _context.Uyeler.FindAsync(degisiklik.Id);
            if (uye == null) return UyeSonuc.Hata("Üye bulunamadı.");
            if (uye.AnonimMi) return UyeSonuc.Hata("Anonimleştirilmiş üye düzenlenemez.");

            Normallestir(degisiklik);
            var tcDegisti = degisiklik.TCKimlikNo != uye.TCKimlikNo;
            var emailDegisti = !string.Equals(degisiklik.Email, uye.Email, StringComparison.OrdinalIgnoreCase);
            var hata = AlanlariDogrula(degisiklik, tcDegisti, emailDegisti) ?? await CakismaAsync(degisiklik, uye.Id);
            if (hata != null) return UyeSonuc.Hata(hata);

            if (degisiklik.UyeTipi != uye.UyeTipi)
                (uye.MaxOduncAdet, uye.MaxOduncSuresi) = VarsayilanKota(degisiklik.UyeTipi);

            uye.Ad = degisiklik.Ad;
            uye.Soyad = degisiklik.Soyad;
            uye.TCKimlikNo = degisiklik.TCKimlikNo;
            uye.PasaportNo = degisiklik.PasaportNo;
            uye.Email = degisiklik.Email;
            uye.Telefon = degisiklik.Telefon;
            uye.UyeTipi = degisiklik.UyeTipi;
            uye.OgrenciNo = degisiklik.OgrenciNo;
            uye.Bolum = degisiklik.Bolum;
            uye.Fakulte = degisiklik.Fakulte;

            DenetimService.Ekle(_context, DenetimService.Islemler.UyeGuncellendi, $"Uye #{uye.Id}", kullaniciId);
            await _context.SaveChangesAsync();
            return UyeSonuc.Basarili(uye);
        }

        private static void Normallestir(Uye uye)
        {
            uye.Ad = uye.Ad?.Trim() ?? string.Empty;
            uye.Soyad = uye.Soyad?.Trim() ?? string.Empty;
            uye.TCKimlikNo = uye.TCKimlikNo?.Trim() ?? string.Empty;
            uye.PasaportNo = (uye.PasaportNo?.Trim() ?? string.Empty).ToUpperInvariant();
            uye.Email = uye.Email?.Trim() ?? string.Empty;
            uye.Telefon = uye.Telefon?.Trim() ?? string.Empty;
            uye.OgrenciNo = uye.OgrenciNo?.Trim() ?? string.Empty;
            uye.Bolum = uye.Bolum?.Trim() ?? string.Empty;
            uye.Fakulte = uye.Fakulte?.Trim() ?? string.Empty;
        }

        // Kimlik/e-posta biçimi yalnızca yeni veya değişen değerlerde zorunlu tutulur;
        // eski kayıtlar (ör. demo verisi) düzenlenebilir kalır.
        private static string? AlanlariDogrula(Uye uye, bool tcDegisti, bool emailDegisti)
        {
            if (uye.Ad.Length == 0 || uye.Soyad.Length == 0) return "Ad ve soyad zorunludur.";
            if (uye.TCKimlikNo.Length == 0 && uye.PasaportNo.Length == 0)
                return "TC Kimlik No / Yabancı Kimlik No veya Pasaport No girilmelidir.";
            if (tcDegisti && uye.TCKimlikNo.Length > 0 && !Dogrulama.TcKimlikGecerliMi(uye.TCKimlikNo))
                return "TC Kimlik No geçersiz (11 hane ve kontrol basamakları doğru olmalı).";
            if (uye.PasaportNo.Length > 0 && !Dogrulama.PasaportGecerliMi(uye.PasaportNo))
                return "Pasaport No 6–20 karakter olmalı ve yalnızca harf ile rakam içermelidir.";
            if (uye.Email.Length == 0) return "E-posta zorunludur.";
            if (emailDegisti && !Dogrulama.EmailGecerliMi(uye.Email)) return "E-posta adresi geçersiz.";
            return null;
        }

        private async Task<string?> CakismaAsync(Uye uye, int? haricId)
        {
            var emailLower = uye.Email.ToLower();
            if (await _context.Uyeler.AnyAsync(u => u.Email.ToLower() == emailLower && (haricId == null || u.Id != haricId)))
                return "Bu e-posta adresiyle kayıtlı başka bir üye var.";
            if (uye.TCKimlikNo.Length > 0 &&
                await _context.Uyeler.AnyAsync(u => u.TCKimlikNo == uye.TCKimlikNo && (haricId == null || u.Id != haricId)))
                return "Bu TC Kimlik No ile kayıtlı başka bir üye var.";
            if (uye.PasaportNo.Length > 0 &&
                await _context.Uyeler.AnyAsync(u => u.PasaportNo == uye.PasaportNo && (haricId == null || u.Id != haricId)))
                return "Bu Pasaport No ile kayıtlı başka bir üye var.";
            return null;
        }

        public async Task<bool> EmailMevcutMu(string email, int? haricId = null)
        {
            return await _context.Uyeler
                .AnyAsync(u => u.Email == email && (haricId == null || u.Id != haricId));
        }

        public async Task<UyeIstatistik> GetIstatistikAsync()
        {
            return new UyeIstatistik
            {
                ToplamUye = await _context.Uyeler.CountAsync(),
                AktifUye = await _context.Uyeler.CountAsync(u => u.Durumu == UyeDurumu.Aktif),
                OgrenciSayisi = await _context.Uyeler.CountAsync(u => u.UyeTipi == UyeTipi.Ogrenci),
                AkademisyenSayisi = await _context.Uyeler.CountAsync(u => u.UyeTipi == UyeTipi.Akademisyen),
                PersonelSayisi = await _context.Uyeler.CountAsync(u => u.UyeTipi == UyeTipi.Personel),
                CezaliUyeSayisi = await _context.Uyeler.CountAsync(u => u.ToplamCeza > u.OdenenCeza),
                BugunKayitlar = await _context.Uyeler.CountAsync(u => u.KayitTarihi.Date == DateTime.Today)
            };
        }

        public async Task<UyeSonuc> CezaOdeAsync(int uyeId, decimal tutar, int? kullaniciId = null)
        {
            var uye = await _context.Uyeler.FindAsync(uyeId);
            if (uye == null) return UyeSonuc.Hata("Üye bulunamadı.");
            if (tutar <= 0) return UyeSonuc.Hata("Ödeme tutarı sıfırdan büyük olmalıdır.");
            if (tutar > uye.KalanCeza) return UyeSonuc.Hata($"Ödeme tutarı kalan borcu ({uye.KalanCeza:0.00}) aşamaz.");

            uye.OdenenCeza += tutar;
            DenetimService.Ekle(_context, DenetimService.Islemler.CezaOdendi, $"Uye #{uyeId}, tutar {tutar.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}", kullaniciId);

            // Ceza kayıtlarını da güncelle
            var odenmeyenCezalar = await _context.CezaKayitlari
                .Where(c => c.UyeId == uyeId && !c.Odendi)
                .OrderBy(c => c.OlusmaTarihi)
                .ToListAsync();

            // En eski cezadan başlayarak yalnızca tamamen karşılanan kayıtlar kapatılır.
            decimal kalanOdeme = tutar;
            foreach (var ceza in odenmeyenCezalar)
            {
                if (ceza.Tutar > kalanOdeme) break;
                ceza.Odendi = true;
                ceza.OdemeTarihi = DateTime.Now;
                kalanOdeme -= ceza.Tutar;
            }

            await _context.SaveChangesAsync();
            return UyeSonuc.Basarili(uye);
        }

        /// <summary>
        /// Üyelerin ödenmemiş toplam borcu. Ödünç uygunluğu bu değere (<see cref="Uye.KalanCeza"/>) göre belirlenir.
        /// </summary>
        public async Task<(decimal Tutar, int UyeSayisi)> GetBekleyenCezaAsync()
        {
            var borclar = await _context.Uyeler
                .Where(u => u.ToplamCeza > u.OdenenCeza)
                .Select(u => new { u.ToplamCeza, u.OdenenCeza })
                .ToListAsync();
            return (borclar.Sum(b => b.ToplamCeza - b.OdenenCeza), borclar.Count);
        }
    }

    public sealed class UyeSonuc
    {
        public bool BasariliMi { get; private init; }
        public string Mesaj { get; private init; } = string.Empty;
        public Uye? Uye { get; private init; }

        public static UyeSonuc Basarili(Uye uye) => new() { BasariliMi = true, Uye = uye };
        public static UyeSonuc Hata(string mesaj) => new() { BasariliMi = false, Mesaj = mesaj };
    }

    public class UyeIstatistik
    {
        public int ToplamUye { get; set; }
        public int AktifUye { get; set; }
        public int OgrenciSayisi { get; set; }
        public int AkademisyenSayisi { get; set; }
        public int PersonelSayisi { get; set; }
        public int CezaliUyeSayisi { get; set; }
        public int BugunKayitlar { get; set; }
    }

    /// <summary>
    /// Kimlik doğrulama servisi - Singleton Pattern.
    /// </summary>
    public class AuthService
    {
        /// <summary>Art arda bu kadar hatalı denemeden sonra hesap geçici olarak kilitlenir.</summary>
        public const int MaksimumHataliDeneme = 5;
        public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(1);

        private static AuthService? _instance;
        private static readonly object _lock = new();

        public static AuthService Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new AuthService(TimeProvider.System);
                }
            }
        }

        private readonly TimeProvider _zaman;
        private readonly Dictionary<string, (int HataSayisi, DateTimeOffset? KilitBitis)> _denemeler =
            new(StringComparer.OrdinalIgnoreCase);

        public Kullanici? AktifKullanici { get; private set; }
        public bool GirisYapilmis => AktifKullanici != null;

        // Giriş yapıldığında tetiklenir
        public event Action<Kullanici>? GirisYapildi;
        public event Action? CikisYapildi;

        internal AuthService(TimeProvider zaman)
        {
            _zaman = zaman;
        }

        public async Task<bool> GirisAsync(string kullaniciAdi, string sifre, KutuphaneDbContext context) =>
            (await GirisDetayliAsync(kullaniciAdi, sifre, context)).Basarili;

        public async Task<GirisSonuc> GirisDetayliAsync(string kullaniciAdi, string sifre, KutuphaneDbContext context)
        {
            kullaniciAdi = kullaniciAdi?.Trim() ?? string.Empty;
            var simdi = _zaman.GetUtcNow();

            lock (_denemeler)
            {
                if (_denemeler.TryGetValue(kullaniciAdi, out var durum) && durum.KilitBitis > simdi)
                {
                    var kalan = (int)Math.Ceiling((durum.KilitBitis.Value - simdi).TotalSeconds);
                    return GirisSonuc.Hata($"Çok sayıda hatalı deneme. {kalan} saniye sonra tekrar deneyin.");
                }
            }

            var kullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciAdi == kullaniciAdi && k.Aktif);

            // PBKDF2 kasıtlı olarak yavaştır; arayüz iş parçacığını bloklamamak için arka planda çalışır.
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
                return GirisSonuc.Hata("Kullanıcı adı veya şifre hatalı.");
            }

            lock (_denemeler)
                _denemeler.Remove(kullaniciAdi);

            if (SifreHasher.YukseltmeGerekli(kullanici!.SifreHash))
                kullanici.SifreHash = await Task.Run(() => SifreHasher.Hashle(sifre));

            kullanici.SonGirisTarihi = DateTime.Now;
            DenetimService.Ekle(context, DenetimService.Islemler.GirisBasarili, $"Kullanici #{kullanici.Id}", kullanici.Id);
            await context.SaveChangesAsync();

            AktifKullanici = kullanici;
            GirisYapildi?.Invoke(kullanici);
            return GirisSonuc.BasariliSonuc(SifreDegistirilmeli(sifre));
        }

        /// <summary>Demo kurulumla gelen veya politikadan kısa parolalar giriş sonrası değiştirilmelidir.</summary>
        private static readonly string[] BilinenVarsayilanSifreler = { "admin123", "123456" };

        public static bool SifreDegistirilmeli(string sifre) =>
            sifre.Length < MinimumSifreUzunlugu || BilinenVarsayilanSifreler.Contains(sifre);

        public const int MinimumSifreUzunlugu = 8;

        /// <summary>Oturumdaki kullanıcının parolasını, mevcut parolayı doğruladıktan sonra değiştirir.</summary>
        public async Task<GirisSonuc> SifreDegistirAsync(KutuphaneDbContext context, string mevcutSifre, string yeniSifre)
        {
            if (AktifKullanici == null) return GirisSonuc.Hata("Oturum açık değil.");

            var kullanici = await context.Kullanicilar.FirstOrDefaultAsync(k => k.Id == AktifKullanici.Id && k.Aktif);
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

        public void Cikis()
        {
            AktifKullanici = null;
            CikisYapildi?.Invoke();
        }

        public bool YetkiVarMi(KullaniciRolu minRol)
        {
            return AktifKullanici != null && AktifKullanici.Rol >= minRol;
        }

        /// <summary>Yetki yoksa <see cref="UnauthorizedAccessException"/> fırlatır (servis sınırında savunma).</summary>
        public void YetkiGerektir(KullaniciRolu minRol)
        {
            if (!YetkiVarMi(minRol))
                throw new UnauthorizedAccessException("Bu işlem için yetkiniz bulunmuyor.");
        }
    }

    public sealed class GirisSonuc
    {
        public bool Basarili { get; private init; }
        public string Mesaj { get; private init; } = string.Empty;

        /// <summary>Giriş başarılı ama parola varsayılan veya politikaya aykırı; değiştirilmesi istenmelidir.</summary>
        public bool SifreDegistirmeGerekli { get; private init; }

        public static GirisSonuc BasariliSonuc(bool sifreDegistirmeGerekli = false) =>
            new() { Basarili = true, SifreDegistirmeGerekli = sifreDegistirmeGerekli };
        public static GirisSonuc Hata(string mesaj) => new() { Basarili = false, Mesaj = mesaj };
    }
}
