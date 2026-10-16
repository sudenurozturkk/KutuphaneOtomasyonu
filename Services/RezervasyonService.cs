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
    /// Kitap rezervasyonu iş kuralları: oluşturma, iptal, ödünce dönüştürme ve süre dolumu.
    /// Kuyruk kuralları için bkz. <see cref="RezervasyonKuyrugu"/>.
    /// </summary>
    public class RezervasyonService
    {
        /// <summary>Yeni rezervasyonun sırada bekleyebileceği gün sayısı (önceki arayüz davranışı).</summary>
        public const int BeklemeSuresiGun = 7;

        private readonly KutuphaneDbContext _context;
        private readonly BildirimService _bildirimService;
        private readonly SistemAyarlari _ayarlar;
        private readonly TimeProvider _zaman;

        public RezervasyonService(KutuphaneDbContext context, BildirimService bildirimService, SistemAyarlari ayarlar,
            TimeProvider? zaman = null)
        {
            _context = context;
            _bildirimService = bildirimService;
            _ayarlar = ayarlar;
            _zaman = zaman ?? TimeProvider.System;
        }

        private DateTime Simdi => _zaman.GetLocalNow().DateTime;

        public static bool OdunceDonusturulebilirMi(Rezervasyon rezervasyon, DateTime simdi) =>
            RezervasyonKuyrugu.AktifMi(rezervasyon, simdi);

        public async Task<List<Rezervasyon>> GetTumuAsync() =>
            await _context.Rezervasyonlar
                .AsNoTracking()
                .Include(r => r.Kitap)
                .Include(r => r.Uye)
                .OrderByDescending(r => r.RezervasyonTarihi)
                .ToListAsync();

        public async Task<RezervasyonSonuc> OlusturAsync(int kitapId, int uyeId, string? notlar, int? kullaniciId)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var simdi = Simdi;

            var kitap = await _context.Kitaplar.FindAsync(kitapId);
            var uye = await _context.Uyeler.FindAsync(uyeId);
            if (kitap == null || !kitap.Aktif) return RezervasyonSonuc.Hata("Kitap bulunamadı veya arşivlenmiş.");
            if (uye == null || uye.Durumu != UyeDurumu.Aktif) return RezervasyonSonuc.Hata("Üye bulunamadı veya aktif değil.");

            var mevcutRezervasyonlar = await _context.Rezervasyonlar
                .Where(r => r.KitapId == kitapId &&
                            (r.Durumu == RezervasyonDurumu.Bekliyor || r.Durumu == RezervasyonDurumu.Onaylandi))
                .ToListAsync();
            var aktifler = mevcutRezervasyonlar.Where(r => RezervasyonKuyrugu.AktifMi(r, simdi)).ToList();

            if (aktifler.Any(r => r.UyeId == uyeId))
                return RezervasyonSonuc.Hata("Bu üyenin aynı kitap için zaten aktif bir rezervasyonu var.");

            if (await _context.Oduncler.AnyAsync(o => o.KitapId == kitapId && o.UyeId == uyeId && o.Durumu == OduncDurumu.Aktif))
                return RezervasyonSonuc.Hata("Üye bu kitabı zaten ödünç almış.");

            var rezervasyon = new Rezervasyon
            {
                KitapId = kitapId,
                UyeId = uyeId,
                RezervasyonTarihi = simdi,
                GecerlilikTarihi = simdi.AddDays(BeklemeSuresiGun),
                Durumu = RezervasyonDurumu.Bekliyor,
                SiraNumarasi = aktifler.Count(r => r.Durumu == RezervasyonDurumu.Bekliyor) + 1,
                Notlar = notlar?.Trim() ?? string.Empty
            };
            _context.Rezervasyonlar.Add(rezervasyon);
            DenetimService.Ekle(_context, DenetimService.Islemler.RezervasyonOlusturuldu,
                $"Kitap #{kitapId}, Uye #{uyeId}", kullaniciId);

            await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return RezervasyonSonuc.Basarili(rezervasyon);
        }

        /// <summary>
        /// Rezervasyonu iptal eder. İptal edilen rezervasyon için bir kopya ayrılmışsa kopya sıradakine geçer.
        /// </summary>
        public async Task<RezervasyonSonuc> IptalEtAsync(int rezervasyonId, int? kullaniciId)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var simdi = Simdi;

            var rezervasyon = await _context.Rezervasyonlar
                .Include(r => r.Kitap)
                .FirstOrDefaultAsync(r => r.Id == rezervasyonId);
            if (rezervasyon == null) return RezervasyonSonuc.Hata("Rezervasyon bulunamadı.");
            if (rezervasyon.Durumu != RezervasyonDurumu.Bekliyor && rezervasyon.Durumu != RezervasyonDurumu.Onaylandi)
                return RezervasyonSonuc.Hata("Yalnızca bekleyen veya onaylanmış rezervasyonlar iptal edilebilir.");

            var kopyaAyrilmisti = rezervasyon.Durumu == RezervasyonDurumu.Onaylandi && rezervasyon.GecerlilikTarihi >= simdi;
            rezervasyon.Durumu = RezervasyonDurumu.Iptal;

            if (kopyaAyrilmisti && rezervasyon.Kitap != null)
                await RezervasyonKuyrugu.SiradakineAyirAsync(_context, _bildirimService, _ayarlar,
                    rezervasyon.KitapId, rezervasyon.Kitap.Baslik, simdi);

            DenetimService.Ekle(_context, DenetimService.Islemler.RezervasyonIptal,
                $"Rezervasyon #{rezervasyon.Id}", kullaniciId);

            await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return RezervasyonSonuc.Basarili(rezervasyon);
        }

        /// <summary>Rezervasyonu tek transaction içinde ödünce dönüştürür.</summary>
        public async Task<OduncSonuc> OdunceDonusturAsync(int rezervasyonId, int kutuphaneciId)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);

            var rezervasyon = await _context.Rezervasyonlar.FirstOrDefaultAsync(r => r.Id == rezervasyonId);
            if (rezervasyon == null || !OdunceDonusturulebilirMi(rezervasyon, Simdi))
                return OduncSonuc.Hata("Bu rezervasyon artık geçerli değil veya başka bir işlem tarafından kullanıldı.");

            var oduncService = new OduncService(_context, _bildirimService, _ayarlar, _zaman);
            var sonuc = await oduncService.OduncVerAsync(rezervasyon.KitapId, rezervasyon.UyeId, kutuphaneciId);
            if (!sonuc.Basarili)
                return sonuc;

            DenetimService.Ekle(_context, DenetimService.Islemler.RezervasyonOdunceDonustu,
                $"Rezervasyon #{rezervasyonId}", kutuphaneciId > 0 ? kutuphaneciId : null);
            await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return sonuc;
        }

        /// <summary>
        /// Süresi dolan rezervasyonları <c>Suresi_Doldu</c> yapar; süresi dolan ayrılmış kopyaları sıradakine geçirir.
        /// </summary>
        /// <returns>Süresi dolan rezervasyon sayısı.</returns>
        public async Task<int> SuresiDolanlariIsleAsync()
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var simdi = Simdi;

            var dolanlar = await _context.Rezervasyonlar
                .Include(r => r.Kitap)
                .Where(r => (r.Durumu == RezervasyonDurumu.Bekliyor || r.Durumu == RezervasyonDurumu.Onaylandi) &&
                            r.GecerlilikTarihi < simdi)
                .ToListAsync();

            foreach (var rezervasyon in dolanlar)
            {
                var ayrilmisti = rezervasyon.Durumu == RezervasyonDurumu.Onaylandi;
                rezervasyon.Durumu = RezervasyonDurumu.Suresi_Doldu;
                if (ayrilmisti && rezervasyon.Kitap != null)
                    await RezervasyonKuyrugu.SiradakineAyirAsync(_context, _bildirimService, _ayarlar,
                        rezervasyon.KitapId, rezervasyon.Kitap.Baslik, simdi);
            }

            if (dolanlar.Count > 0)
                await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return dolanlar.Count;
        }
    }

    public sealed class RezervasyonSonuc
    {
        public bool BasariliMi { get; private init; }
        public string Mesaj { get; private init; } = string.Empty;
        public Rezervasyon? Rezervasyon { get; private init; }

        public static RezervasyonSonuc Basarili(Rezervasyon rezervasyon) => new() { BasariliMi = true, Rezervasyon = rezervasyon };
        public static RezervasyonSonuc Hata(string mesaj) => new() { BasariliMi = false, Mesaj = mesaj };
    }
}
