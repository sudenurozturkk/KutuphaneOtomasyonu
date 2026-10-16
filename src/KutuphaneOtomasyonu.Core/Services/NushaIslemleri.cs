using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Nüsha (fiziksel kopya) kayıtları için ortak işlemler. <see cref="YeniEkleAsync"/> değişiklikleri yalnızca
    /// bağlama ekler; <see cref="TamamlaAsync"/> onarımı hemen kaydeder. Her iki durumda da çağıranın açık bir
    /// transaction'ı olmalı ve sayaçları (<see cref="Kitap.ToplamAdet"/>, <see cref="Kitap.MevcutAdet"/>) tutarlı tutmalıdır.
    /// </summary>
    internal static class NushaIslemleri
    {
        /// <summary>Barkod biçimi: <c>KB</c> + 6 hane kitap no + 4 hane sıra (ör. KB0000120003). Sıra numaraları yeniden kullanılmaz.</summary>
        public static string BarkodUret(int kitapId, int sira) => $"KB{kitapId:D6}{sira:D4}";

        public static string BarkodNormallestir(string? barkod) => (barkod ?? string.Empty).Trim().ToUpperInvariant();

        private static async Task<int> SonrakiSiraAsync(KutuphaneDbContext context, int kitapId)
        {
            var kayitli = await context.KitapNushalari.CountAsync(n => n.KitapId == kitapId);
            var eklenen = context.ChangeTracker.Entries<KitapNushasi>()
                .Count(e => e.State == EntityState.Added && e.Entity.KitapId == kitapId);
            return kayitli + eklenen + 1;
        }

        /// <summary>Kitaba <paramref name="adet"/> yeni rafta nüsha ekler (sayaçları değiştirmez).</summary>
        public static async Task YeniEkleAsync(KutuphaneDbContext context, Kitap kitap, int adet, DateTime simdi)
        {
            var sira = await SonrakiSiraAsync(context, kitap.Id);
            for (var i = 0; i < adet; i++)
            {
                context.KitapNushalari.Add(new KitapNushasi
                {
                    KitapId = kitap.Id,
                    Barkod = BarkodUret(kitap.Id, sira + i),
                    Durum = NushaDurumu.Rafta,
                    RafKonumu = kitap.RafKonumu,
                    EklenmeTarihi = simdi
                });
            }
        }

        /// <summary>
        /// Nüsha kaydı hiç olmayan bir kitap için sayaçlardan nüsha üretir (migration ile aynı kural):
        /// aktif ödünçler nüshalarla eşleştirilir, <c>MevcutAdet</c> kadarı rafta sayılır, kalanlar dolaşım dışıdır.
        /// </summary>
        public static async Task TamamlaAsync(KutuphaneDbContext context, Kitap kitap, DateTime simdi)
        {
            if (kitap.Id == 0 || kitap.ToplamAdet <= 0) return;
            if (context.ChangeTracker.Entries<KitapNushasi>().Any(e => e.Entity.KitapId == kitap.Id)) return;
            if (await context.KitapNushalari.AnyAsync(n => n.KitapId == kitap.Id)) return;

            var aktifler = await context.Oduncler
                .Where(o => o.KitapId == kitap.Id && o.Durumu == OduncDurumu.Aktif && o.NushaId == null)
                .OrderBy(o => o.Id)
                .ToListAsync();

            var nushalar = Enumerable.Range(1, kitap.ToplamAdet)
                .Select(i => new KitapNushasi
                {
                    KitapId = kitap.Id,
                    Barkod = BarkodUret(kitap.Id, i),
                    RafKonumu = kitap.RafKonumu,
                    EklenmeTarihi = simdi
                })
                .ToList();

            var oduncte = Math.Min(aktifler.Count, nushalar.Count);
            for (var i = 0; i < oduncte; i++)
            {
                nushalar[i].Durum = NushaDurumu.Oduncte;
                aktifler[i].Nusha = nushalar[i];
            }

            var rafta = Math.Min(Math.Max(kitap.MevcutAdet, 0), nushalar.Count - oduncte);
            for (var i = oduncte + rafta; i < nushalar.Count; i++)
            {
                nushalar[i].Durum = NushaDurumu.Hasarli;
                nushalar[i].DurumTarihi = simdi;
            }

            context.KitapNushalari.AddRange(nushalar);
            kitap.ToplamAdet = oduncte + rafta;
            kitap.MevcutAdet = rafta;

            // Hemen kaydedilir ki sonraki sorgular nüshaları görsün; çağıranlar transaction içindedir.
            await context.SaveChangesAsync();
            UygulamaLog.Yaz($"[Nusha] Kitap #{kitap.Id} icin {nushalar.Count} nusha kaydi uretildi.");
        }
    }
}
