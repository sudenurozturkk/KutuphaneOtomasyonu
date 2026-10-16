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
            var transferler = await _context.MateryalTransferler
                .Include(t => t.Kitap)
                .Include(t => t.TalepEdenUye)
                .OrderByDescending(t => t.TalepTarihi)
                .ToListAsync();

            var yayinIdleri = transferler.Where(t => t.AkademikYayinId.HasValue).Select(t => t.AkademikYayinId!.Value).Distinct().ToList();
            var yayinBasliklari = await _context.AkademikYayinlar
                .Where(y => yayinIdleri.Contains(y.Id))
                .ToDictionaryAsync(y => y.Id, y => y.Baslik);

            foreach (var transfer in transferler)
            {
                transfer.MateryalBasligi = transfer.Kitap?.Baslik
                    ?? (transfer.AkademikYayinId is int yid && yayinBasliklari.TryGetValue(yid, out var baslik) ? $"🎓 {baslik}" : "—");
            }

            return transferler;
        }

        /// <summary>Akademik yayının basılı kopyası için kampüsler arası transfer talebi oluşturur.</summary>
        public async Task<MateryalTransfer> YayinTransferTalepEtAsync(int yayinId, int uyeId, KampusLokasyonu kaynak, KampusLokasyonu hedef)
        {
            if (!await _context.AkademikYayinlar.AnyAsync(y => y.Id == yayinId))
                throw new InvalidOperationException("Akademik yayın bulunamadı.");
            if (!await _context.Uyeler.AnyAsync(u => u.Id == uyeId && u.Durumu == UyeDurumu.Aktif))
                throw new InvalidOperationException("Talep eden üye bulunamadı veya aktif değil.");
            if (kaynak == hedef)
                throw new InvalidOperationException("Kaynak ve hedef kampüs aynı olamaz.");

            var transfer = new MateryalTransfer
            {
                KitapId = null,
                AkademikYayinId = yayinId,
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

        public async Task<MateryalTransfer> TransferTalepEtAsync(int kitapId, int uyeId, KampusLokasyonu kaynak, KampusLokasyonu hedef)
        {
            if (!await _context.Kitaplar.AnyAsync(k => k.Id == kitapId && k.Aktif))
                throw new InvalidOperationException("Kitap bulunamadı veya arşivlenmiş.");
            if (!await _context.Uyeler.AnyAsync(u => u.Id == uyeId && u.Durumu == UyeDurumu.Aktif))
                throw new InvalidOperationException("Talep eden üye bulunamadı veya aktif değil.");
            if (kaynak == hedef)
                throw new InvalidOperationException("Kaynak ve hedef kampüs aynı olamaz.");

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
