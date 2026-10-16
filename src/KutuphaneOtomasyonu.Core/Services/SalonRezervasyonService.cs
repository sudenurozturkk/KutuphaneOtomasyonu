using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Services
{
    public class SalonRezervasyonService
    {
        /// <summary>Salon haritasındaki masa sayısı (Sessiz 8, Bireysel 8, Grup 4, Premium 4).</summary>
        public const int ToplamMasaSayisi = 24;

        private readonly KutuphaneDbContext _context;

        public SalonRezervasyonService(KutuphaneDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalonRezervasyonu>> GetAktifAsync(DateTime tarih, string zamanAraligi)
        {
            var gun = tarih.Date;
            return await _context.SalonRezervasyonlari
                .AsNoTracking()
                .Where(r => r.Tarih >= gun &&
                            r.Tarih < gun.AddDays(1) &&
                            r.ZamanAraligi == zamanAraligi &&
                            r.Durum == "Aktif")
                .OrderBy(r => r.OlusturulmaTarihi)
                .ToListAsync();
        }

        /// <summary>Gün içindeki aktif rezervasyon sayısı ve en az bir rezervasyonu olan masa sayısı.</summary>
        public async Task<(int RezervasyonSayisi, int RezerveMasaSayisi)> GunlukOzetAsync(DateTime tarih)
        {
            var gun = tarih.Date;
            var masalar = await _context.SalonRezervasyonlari
                .AsNoTracking()
                .Where(r => r.Tarih >= gun && r.Tarih < gun.AddDays(1) && r.Durum == "Aktif")
                .Select(r => r.MasaKodu)
                .ToListAsync();
            return (masalar.Count, masalar.Distinct().Count());
        }

        public async Task<SalonRezervasyonSonuc> OlusturAsync(
            string masaKodu,
            string bolge,
            string kullanici,
            string zamanAraligi,
            string sure,
            string amac,
            DateTime tarih)
        {
            if (string.IsNullOrWhiteSpace(masaKodu) ||
                string.IsNullOrWhiteSpace(kullanici) ||
                string.IsNullOrWhiteSpace(zamanAraligi))
            {
                return SalonRezervasyonSonuc.Hata("Masa, kullanıcı ve zaman aralığı zorunludur.");
            }

            var gun = tarih.Date;
            var mevcut = await _context.SalonRezervasyonlari.AnyAsync(r =>
                r.MasaKodu == masaKodu &&
                r.Tarih >= gun &&
                r.Tarih < gun.AddDays(1) &&
                r.ZamanAraligi == zamanAraligi &&
                r.Durum == "Aktif");

            if (mevcut)
                return SalonRezervasyonSonuc.Hata("Bu masa seçilen zaman diliminde zaten rezerve edilmiş.");

            var rezervasyon = new SalonRezervasyonu
            {
                MasaKodu = masaKodu.Trim(),
                Bolge = bolge.Trim(),
                Kullanici = kullanici.Trim(),
                ZamanAraligi = zamanAraligi.Trim(),
                Sure = string.IsNullOrWhiteSpace(sure) ? "2 saat" : sure.Trim(),
                Amac = amac?.Trim() ?? string.Empty,
                Tarih = gun,
                Durum = "Aktif"
            };

            _context.SalonRezervasyonlari.Add(rezervasyon);
            try
            {
                await _context.SaveChangesAsync();
                return SalonRezervasyonSonuc.Basarili(rezervasyon);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
            {
                _context.Entry(rezervasyon).State = EntityState.Detached;
                return SalonRezervasyonSonuc.Hata("Bu masa seçilen zaman diliminde başka bir işlem tarafından rezerve edildi.");
            }
        }

        public async Task<SalonRezervasyonSonuc> IptalEtAsync(int id)
        {
            var rezervasyon = await _context.SalonRezervasyonlari
                .SingleOrDefaultAsync(r => r.Id == id && r.Durum == "Aktif");

            if (rezervasyon == null)
                return SalonRezervasyonSonuc.Hata("Rezervasyon bulunamadı veya zaten iptal edilmiş.");

            rezervasyon.Durum = "Iptal";
            await _context.SaveChangesAsync();
            return SalonRezervasyonSonuc.Basarili(rezervasyon);
        }
    }

    public sealed class SalonRezervasyonSonuc
    {
        public bool BasariliMi { get; }
        public string Mesaj { get; }
        public SalonRezervasyonu? Rezervasyon { get; }

        private SalonRezervasyonSonuc(bool basariliMi, string mesaj, SalonRezervasyonu? rezervasyon)
        {
            BasariliMi = basariliMi;
            Mesaj = mesaj;
            Rezervasyon = rezervasyon;
        }

        public static SalonRezervasyonSonuc Basarili(SalonRezervasyonu rezervasyon) =>
            new(true, string.Empty, rezervasyon);

        public static SalonRezervasyonSonuc Hata(string mesaj) =>
            new(false, mesaj, null);
    }
}
