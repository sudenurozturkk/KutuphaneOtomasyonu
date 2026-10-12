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
    /// Ödünç işlemleri servis sınıfı.
    /// Ödünç alma, iade, uzatma ve gecikme hesaplama işlemlerini yönetir.
    /// Observer Pattern: Bildirim servisi ile entegre çalışır.
    /// </summary>
    public class OduncService
    {
        private readonly KutuphaneDbContext _context;
        private readonly BildirimService _bildirimService;
        private readonly SistemAyarlari _ayarlar;

        public OduncService(KutuphaneDbContext context, BildirimService bildirimService, SistemAyarlari ayarlar)
        {
            _context = context;
            _bildirimService = bildirimService;
            _ayarlar = ayarlar;
        }

        /// <summary>Aktif ödünçleri getirir.</summary>
        public async Task<List<Odunc>> GetAktifOdunclerAsync()
        {
            return await _context.Oduncler
                .Include(o => o.Kitap)
                .Include(o => o.Uye)
                .Where(o => o.Durumu == OduncDurumu.Aktif)
                .OrderBy(o => o.PlanlananIadeTarihi)
                .ToListAsync();
        }

        /// <summary>Gecikmiş ödünçleri getirir.</summary>
        public async Task<List<Odunc>> GetGecikmisTeslerAsync()
        {
            return await _context.Oduncler
                .Include(o => o.Kitap)
                .Include(o => o.Uye)
                .Where(o => o.Durumu == OduncDurumu.Aktif && o.PlanlananIadeTarihi < DateTime.Now)
                .OrderBy(o => o.PlanlananIadeTarihi)
                .ToListAsync();
        }

        /// <summary>Üyenin ödünçlerini getirir.</summary>
        public async Task<List<Odunc>> GetUyeOdunclerAsync(int uyeId)
        {
            return await _context.Oduncler
                .Include(o => o.Kitap)
                .Where(o => o.UyeId == uyeId)
                .OrderByDescending(o => o.OduncTarihi)
                .ToListAsync();
        }

        /// <summary>
        /// Kitap ödünç verme işlemi.
        /// Strategy Pattern: Üye tipine göre süre belirlenir.
        /// </summary>
        public async Task<OduncSonuc> OduncVerAsync(int kitapId, int uyeId, int kutuphanecId)
        {
            var kitap = await _context.Kitaplar.FindAsync(kitapId);
            var uye = await _context.Uyeler.FindAsync(uyeId);

            if (kitap == null) return OduncSonuc.Hata("Kitap bulunamadı.");
            if (uye == null) return OduncSonuc.Hata("Üye bulunamadı.");
            if (uye.Durumu != UyeDurumu.Aktif) return OduncSonuc.Hata("Üye aktif değil.");
            if (kitap.MevcutAdet <= 0) return OduncSonuc.Hata("Kitabın mevcut kopyası yok.");
            if (uye.CezaliMi) return OduncSonuc.Hata($"Üyenin {uye.KalanCeza:C2} tutarında ödenmemiş cezası var.");

            // Aktif ödünç sayısı kontrolü
            var aktifSayi = await _context.Oduncler
                .CountAsync(o => o.UyeId == uyeId && o.Durumu == OduncDurumu.Aktif);

            if (aktifSayi >= uye.MaxOduncAdet)
                return OduncSonuc.Hata($"Üye maksimum {uye.MaxOduncAdet} ödünç limitine ulaştı.");

            // Aynı kitabı zaten ödünçte mi?
            var varMi = await _context.Oduncler
                .AnyAsync(o => o.UyeId == uyeId && o.KitapId == kitapId && o.Durumu == OduncDurumu.Aktif);
            if (varMi) return OduncSonuc.Hata("Üye bu kitabı zaten ödünçte.");

            // İşlem
            var odunc = new Odunc
            {
                KitapId = kitapId,
                UyeId = uyeId,
                KutuphanecId = kutuphanecId,
                OduncTarihi = DateTime.Now,
                PlanlananIadeTarihi = DateTime.Now.AddDays(uye.MaxOduncSuresi),
                Durumu = OduncDurumu.Aktif
            };

            kitap.MevcutAdet--;

            _context.Oduncler.Add(odunc);
            await _context.SaveChangesAsync();

            // Bildirim gönder
            await _bildirimService.GonderAsync(uyeId,
                "Kitap Ödünç Alındı",
                $"'{kitap.Baslik}' kitabını {odunc.PlanlananIadeTarihi:dd.MM.yyyy} tarihine kadar iade etmeniz gerekmektedir.",
                BildirimTipi.Bilgi);

            return OduncSonuc.BasariliSonuc(odunc);
        }

        /// <summary>Kitap iade işlemi.</summary>
        public async Task<OduncSonuc> IadeAlAsync(int oduncId, KitapDurumu kitapDurumu, string? notlar = null)
        {
            var odunc = await _context.Oduncler
                .Include(o => o.Kitap)
                .Include(o => o.Uye)
                .FirstOrDefaultAsync(o => o.Id == oduncId);

            if (odunc == null) return OduncSonuc.Hata("Ödünç kaydı bulunamadı.");
            if (odunc.Durumu != OduncDurumu.Aktif) return OduncSonuc.Hata("Bu ödünç aktif değil.");

            odunc.GercekIadeTarihi = DateTime.Now;
            odunc.IadedekiKitapDurumu = kitapDurumu;
            odunc.Durumu = OduncDurumu.Iade;
            if (notlar != null) odunc.IadeNotlari = notlar;

            // Gecikme cezası hesapla
            decimal ceza = 0;
            if (DateTime.Now > odunc.PlanlananIadeTarihi)
            {
                var gecikmeGun = (int)(DateTime.Now - odunc.PlanlananIadeTarihi).TotalDays;
                ceza = gecikmeGun * _ayarlar.GunlukCezaTutari;
                odunc.CezaTutari = ceza;

                odunc.Uye!.ToplamCeza += ceza;

                // Ceza kaydı ekle
                _context.CezaKayitlari.Add(new CezaKaydi
                {
                    UyeId = odunc.UyeId,
                    OduncId = odunc.Id,
                    Tutar = ceza,
                    Tipi = CezaTipi.Gecikme,
                    Aciklama = $"{gecikmeGun} gün gecikme x {_ayarlar.GunlukCezaTutari}₺/gün = {ceza}₺"
                });
            }

            // Hasar/kayıp cezası
            if (kitapDurumu == KitapDurumu.Hasarli)
            {
                var hasarCeza = odunc.Kitap!.SatinAlmaFiyati * 0.5m;
                ceza += hasarCeza;
                odunc.CezaTutari += hasarCeza;
                odunc.Uye!.ToplamCeza += hasarCeza;
                odunc.Durumu = OduncDurumu.Hasarli;

                _context.CezaKayitlari.Add(new CezaKaydi
                {
                    UyeId = odunc.UyeId,
                    OduncId = odunc.Id,
                    Tutar = hasarCeza,
                    Tipi = CezaTipi.Hasar,
                    Aciklama = "Hasarlı iade - kitap fiyatının %50'si"
                });
            }
            else if (kitapDurumu == KitapDurumu.Kayip)
            {
                var kayipCeza = odunc.Kitap!.SatinAlmaFiyati;
                ceza += kayipCeza;
                odunc.CezaTutari += kayipCeza;
                odunc.Uye!.ToplamCeza += kayipCeza;
                odunc.Durumu = OduncDurumu.Kayip;

                _context.CezaKayitlari.Add(new CezaKaydi
                {
                    UyeId = odunc.UyeId,
                    OduncId = odunc.Id,
                    Tutar = kayipCeza,
                    Tipi = CezaTipi.Kayip,
                    Aciklama = "Kayıp iade - tam kitap fiyatı"
                });

                // Kayıp ise stok güncelleme yok
                odunc.Kitap!.ToplamAdet--;
            }
            else
            {
                odunc.Kitap!.MevcutAdet++;
            }

            // Rezervasyon varsa bildir
            var bekleyenRezervasyon = await _context.Rezervasyonlar
                .Include(r => r.Uye)
                .Where(r => r.KitapId == odunc.KitapId && r.Durumu == RezervasyonDurumu.Bekliyor)
                .OrderBy(r => r.RezervasyonTarihi)
                .FirstOrDefaultAsync();

            if (bekleyenRezervasyon != null && kitapDurumu != KitapDurumu.Kayip)
            {
                await _bildirimService.GonderAsync(bekleyenRezervasyon.UyeId,
                    "Rezerve Kitabınız Hazır!",
                    $"'{odunc.Kitap.Baslik}' kitabı artık mevcut. Lütfen 3 gün içinde teslim alınız.",
                    BildirimTipi.Rezervasyon);

                bekleyenRezervasyon.Durumu = RezervasyonDurumu.Onaylandi;
            }

            await _context.SaveChangesAsync();

            return OduncSonuc.BasariliSonuc(odunc, ceza > 0 ? $"İade başarılı. Toplam ceza: {ceza:C2}" : "İade başarılı.");
        }

        /// <summary>Ödünç süresini uzatır.</summary>
        public async Task<OduncSonuc> UzatAsync(int oduncId)
        {
            var odunc = await _context.Oduncler
                .Include(o => o.Uye)
                .FirstOrDefaultAsync(o => o.Id == oduncId);

            if (odunc == null) return OduncSonuc.Hata("Ödünç kaydı bulunamadı.");
            if (!odunc.UzatilabilirMi) return OduncSonuc.Hata("Bu ödünç uzatılamaz.");

            odunc.PlanlananIadeTarihi = odunc.PlanlananIadeTarihi.AddDays(_ayarlar.MaxOduncSuresi);
            odunc.UzatmaSayisi++;

            await _context.SaveChangesAsync();

            await _bildirimService.GonderAsync(odunc.UyeId,
                "Ödünç Süresi Uzatıldı",
                $"Ödünç süreniz {odunc.PlanlananIadeTarihi:dd.MM.yyyy} tarihine uzatıldı.",
                BildirimTipi.Bilgi);

            return OduncSonuc.BasariliSonuc(odunc, "Süre uzatıldı.");
        }

        /// <summary>Dashboard istatistikleri.</summary>
        public async Task<OduncIstatistik> GetIstatistikAsync()
        {
            var bugun = DateTime.Today;
            return new OduncIstatistik
            {
                AktifOduncSayisi = await _context.Oduncler.CountAsync(o => o.Durumu == OduncDurumu.Aktif),
                GecikmisSayisi = await _context.Oduncler.CountAsync(o => o.Durumu == OduncDurumu.Aktif && o.PlanlananIadeTarihi < DateTime.Now),
                BugunIadeSayisi = await _context.Oduncler.CountAsync(o => o.GercekIadeTarihi.HasValue && o.GercekIadeTarihi.Value.Date == bugun),
                BugunOduncSayisi = await _context.Oduncler.CountAsync(o => o.OduncTarihi.Date == bugun),
                ToplamOduncSayisi = await _context.Oduncler.CountAsync(),
                ToplamCeza = await _context.CezaKayitlari.SumAsync(c => c.Tutar),
                OdenenCeza = await _context.CezaKayitlari.Where(c => c.Odendi).SumAsync(c => c.Tutar)
            };
        }
    }

    public class OduncSonuc
    {
        public bool Basarili { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public Odunc? Odunc { get; set; }

        public static OduncSonuc BasariliSonuc(Odunc odunc, string mesaj = "İşlem başarılı.") =>
            new() { Basarili = true, Mesaj = mesaj, Odunc = odunc };

        public static OduncSonuc Hata(string mesaj) =>
            new() { Basarili = false, Mesaj = mesaj };
    }

    public class OduncIstatistik
    {
        public int AktifOduncSayisi { get; set; }
        public int GecikmisSayisi { get; set; }
        public int BugunIadeSayisi { get; set; }
        public int BugunOduncSayisi { get; set; }
        public int ToplamOduncSayisi { get; set; }
        public decimal ToplamCeza { get; set; }
        public decimal OdenenCeza { get; set; }
        public decimal OdenmeyenCeza => ToplamCeza - OdenenCeza;
    }
}
