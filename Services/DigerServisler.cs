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
            var bildirim = new Bildirim
            {
                UyeId = uyeId,
                Baslik = baslik,
                Mesaj = mesaj,
                Tipi = tipi,
                OlusturmaTarihi = DateTime.Now
            };

            _context.Bildirimler.Add(bildirim);
            await _context.SaveChangesAsync();

            YeniBildirim?.Invoke(bildirim);
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

        public async Task<Uye> AddAsync(Uye uye)
        {
            uye.KayitTarihi = DateTime.Now;
            _context.Uyeler.Add(uye);
            await _context.SaveChangesAsync();
            return uye;
        }

        public async Task UpdateAsync(Uye uye)
        {
            _context.Uyeler.Update(uye);
            await _context.SaveChangesAsync();
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

        public async Task CezaOdeAsync(int uyeId, decimal tutar)
        {
            var uye = await _context.Uyeler.FindAsync(uyeId);
            if (uye == null) return;

            uye.OdenenCeza += tutar;

            // Ceza kayıtlarını da güncelle
            var odenmeyenCezalar = await _context.CezaKayitlari
                .Where(c => c.UyeId == uyeId && !c.Odendi)
                .OrderBy(c => c.OlusmaTarihi)
                .ToListAsync();

            decimal kalanOdeme = tutar;
            foreach (var ceza in odenmeyenCezalar)
            {
                if (kalanOdeme <= 0) break;
                if (ceza.Tutar <= kalanOdeme)
                {
                    ceza.Odendi = true;
                    ceza.OdemeTarihi = DateTime.Now;
                    kalanOdeme -= ceza.Tutar;
                }
            }

            await _context.SaveChangesAsync();
        }
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
        private static AuthService? _instance;
        private static readonly object _lock = new();

        public static AuthService Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new AuthService();
                }
            }
        }

        public Kullanici? AktifKullanici { get; private set; }
        public bool GirisYapilmis => AktifKullanici != null;

        // Giriş yapıldığında tetiklenir
        public event Action<Kullanici>? GirisYapildi;
        public event Action? CikisYapildi;

        private AuthService() { }

        public async Task<bool> GirisAsync(string kullaniciAdi, string sifre, KutuphaneDbContext context)
        {
            var hash = DatabaseSeeder.VerifyPassword(sifre, string.Empty); // Hash metodunu çağır
            var kullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciAdi == kullaniciAdi && k.Aktif);

            if (kullanici == null) return false;

            if (!DatabaseSeeder.VerifyPassword(sifre, kullanici.SifreHash)) return false;

            kullanici.SonGirisTarihi = DateTime.Now;
            await context.SaveChangesAsync();

            AktifKullanici = kullanici;
            GirisYapildi?.Invoke(kullanici);
            return true;
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
    }
}
