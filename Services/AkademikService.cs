using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Services
{
    public class AkademikService
    {
        private readonly KutuphaneDbContext _context;

        public AkademikService(KutuphaneDbContext context)
        {
            _context = context;
        }

        public async Task<List<AkademikYayin>> GetYayinlarAsync(string? arama = null, YayinTuru? tur = null, KampusLokasyonu? kampus = null)
        {
            var query = _context.AkademikYayinlar.AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                var lower = arama.ToLower();
                query = query.Where(y =>
                    y.Baslik.ToLower().Contains(lower) ||
                    y.Yazar.ToLower().Contains(lower) ||
                    (y.Danisman != null && y.Danisman.ToLower().Contains(lower)) ||
                    y.AnahtarKelimeler.ToLower().Contains(lower) ||
                    (y.DOI != null && y.DOI.ToLower().Contains(lower)));
            }

            if (tur.HasValue)
                query = query.Where(y => y.Tur == tur.Value);

            if (kampus.HasValue)
                query = query.Where(y => y.MevcutKampus == kampus.Value);

            return await query.OrderByDescending(y => y.Yil).ThenByDescending(y => y.AlintiSayisi).ToListAsync();
        }

        public async Task<AkademikYayin?> GetYayinByIdAsync(int id)
        {
            return await _context.AkademikYayinlar.FindAsync(id);
        }

        public async Task IndirmeArtirAsync(int id)
        {
            var yayin = await _context.AkademikYayinlar.FindAsync(id);
            if (yayin != null)
            {
                yayin.IndirmeSayisi++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AlintiArtirAsync(int id)
        {
            var yayin = await _context.AkademikYayinlar.FindAsync(id);
            if (yayin != null)
            {
                yayin.AlintiSayisi++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<MateryalTransfer>> GetTransferlerAsync()
        {
            return await _context.MateryalTransferler
                .Include(t => t.Kitap)
                .Include(t => t.TalepEdenUye)
                .OrderByDescending(t => t.TalepTarihi)
                .ToListAsync();
        }

        public async Task<MateryalTransfer> TransferTalepEtAsync(int kitapId, int uyeId, KampusLokasyonu kaynak, KampusLokasyonu hedef)
        {
            var transfer = new MateryalTransfer
            {
                KitapId = kitapId,
                TalepEdenUyeId = uyeId,
                KaynakKampus = kaynak,
                HedefKampus = hedef,
                TalepTarihi = DateTime.Now,
                TahminiVarisTarihi = DateTime.Now.AddHours(24),
                Durum = TransferDurumu.TalepEdildi,
                KuryeTakipKodu = $"ILL-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
            };

            _context.MateryalTransferler.Add(transfer);
            await _context.SaveChangesAsync();
            return transfer;
        }
    }
}
