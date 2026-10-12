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
    /// Kitap yönetimi servis sınıfı.
    /// CRUD + iş mantığı içerir.
    /// </summary>
    public class KitapService
    {
        private readonly KutuphaneDbContext _context;

        public KitapService(KutuphaneDbContext context)
        {
            _context = context;
        }

        /// <summary>Tüm aktif kitapları getirir.</summary>
        public async Task<List<Kitap>> GetAllAsync()
        {
            return await _context.Kitaplar
                .Where(k => k.Aktif)
                .OrderBy(k => k.Baslik)
                .ToListAsync();
        }

        /// <summary>Arama kriterlerine göre kitap getirir.</summary>
        public async Task<List<Kitap>> SearchAsync(string? arama = null, string? kategori = null, string? dil = null, bool? mevcutMu = null)
        {
            var query = _context.Kitaplar.Where(k => k.Aktif).AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                var aramaLower = arama.ToLower();
                query = query.Where(k =>
                    k.Baslik.ToLower().Contains(aramaLower) ||
                    k.Yazar.ToLower().Contains(aramaLower) ||
                    k.ISBN.Contains(aramaLower) ||
                    k.Kategori.ToLower().Contains(aramaLower) ||
                    k.Yayinevi.ToLower().Contains(aramaLower));
            }

            if (!string.IsNullOrWhiteSpace(kategori))
                query = query.Where(k => k.Kategori == kategori);

            if (!string.IsNullOrWhiteSpace(dil))
                query = query.Where(k => k.Dil == dil);

            if (mevcutMu.HasValue)
                query = mevcutMu.Value ? query.Where(k => k.MevcutAdet > 0) : query.Where(k => k.MevcutAdet == 0);

            return await query.OrderByDescending(k => k.Puan).ToListAsync();
        }

        /// <summary>ID ile kitap getirir (ilişkili verilerle birlikte).</summary>
        public async Task<Kitap?> GetByIdAsync(int id)
        {
            return await _context.Kitaplar
                .Include(k => k.Yorumlar).ThenInclude(y => y.Uye)
                .Include(k => k.Oduncler)
                .FirstOrDefaultAsync(k => k.Id == id);
        }

        /// <summary>Yeni kitap ekler.</summary>
        public async Task<Kitap> AddAsync(Kitap kitap)
        {
            kitap.EklenmeTarihi = DateTime.Now;
            _context.Kitaplar.Add(kitap);
            await _context.SaveChangesAsync();
            return kitap;
        }

        /// <summary>Kitabı günceller.</summary>
        public async Task UpdateAsync(Kitap kitap)
        {
            kitap.GuncellenmeTarihi = DateTime.Now;
            _context.Kitaplar.Update(kitap);
            await _context.SaveChangesAsync();
        }

        /// <summary>Kitabı soft-delete ile siler (Aktif=false).</summary>
        public async Task DeleteAsync(int id)
        {
            var kitap = await _context.Kitaplar.FindAsync(id);
            if (kitap != null)
            {
                kitap.Aktif = false;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>Kategorileri döndürür.</summary>
        public async Task<List<string>> GetKategorilerAsync()
        {
            return await _context.Kitaplar
                .Where(k => k.Aktif)
                .Select(k => k.Kategori)
                .Distinct()
                .OrderBy(k => k)
                .ToListAsync();
        }

        /// <summary>En çok ödünç alınan kitapları döndürür.</summary>
        public async Task<List<Kitap>> GetPopulerKitaplarAsync(int adet = 10)
        {
            var populer = await _context.Oduncler
                .GroupBy(o => o.KitapId)
                .Select(g => new { KitapId = g.Key, Sayi = g.Count() })
                .OrderByDescending(x => x.Sayi)
                .Take(adet)
                .ToListAsync();

            var ids = populer.Select(p => p.KitapId).ToList();
            return await _context.Kitaplar
                .Where(k => ids.Contains(k.Id) && k.Aktif)
                .ToListAsync();
        }

        /// <summary>Son eklenen kitapları döndürür.</summary>
        public async Task<List<Kitap>> GetYeniEklenenlerAsync(int adet = 10)
        {
            return await _context.Kitaplar
                .Where(k => k.Aktif)
                .OrderByDescending(k => k.EklenmeTarihi)
                .Take(adet)
                .ToListAsync();
        }

        /// <summary>ISBN kontrolü yapar.</summary>
        public async Task<bool> IsbnMevcutMu(string isbn, int? haricId = null)
        {
            return await _context.Kitaplar
                .AnyAsync(k => k.ISBN == isbn && k.Aktif && (haricId == null || k.Id != haricId));
        }

        /// <summary>İstatistik verileri döndürür.</summary>
        public async Task<KitapIstatistik> GetIstatistikAsync()
        {
            var kitaplar = await _context.Kitaplar.Where(k => k.Aktif).ToListAsync();
            return new KitapIstatistik
            {
                ToplamKitap = kitaplar.Count,
                ToplamKopyaSayisi = kitaplar.Sum(k => k.ToplamAdet),
                MevcutKopya = kitaplar.Sum(k => k.MevcutAdet),
                OdunctekiKopya = kitaplar.Sum(k => k.ToplamAdet - k.MevcutAdet),
                KategoriSayisi = kitaplar.Select(k => k.Kategori).Distinct().Count(),
                OrtalamaPuan = kitaplar.Where(k => k.PuanSayisi > 0).Average(k => (double?)k.Puan) ?? 0
            };
        }
    }

    public class KitapIstatistik
    {
        public int ToplamKitap { get; set; }
        public int ToplamKopyaSayisi { get; set; }
        public int MevcutKopya { get; set; }
        public int OdunctekiKopya { get; set; }
        public int KategoriSayisi { get; set; }
        public double OrtalamaPuan { get; set; }
    }
}
