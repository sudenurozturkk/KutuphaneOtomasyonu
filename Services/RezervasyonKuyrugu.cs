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
    /// Kitap rezervasyon kuyruğu kuralları.
    /// <list type="bullet">
    /// <item><c>Bekliyor</c>: sırada bekliyor; <c>GecerlilikTarihi</c> geçerse süresi dolar.</item>
    /// <item><c>Onaylandi</c>: bir kopya bu üye için ayrıldı; <c>GecerlilikTarihi</c> teslim alma son tarihidir.
    /// Ayrılmış kopyalar başka üyelere ödünç verilemez.</item>
    /// </list>
    /// Metotlar değişiklikleri yalnızca bağlama ekler; kaydetmek çağıranın sorumluluğundadır.
    /// </summary>
    internal static class RezervasyonKuyrugu
    {
        public static bool AktifMi(Rezervasyon r, DateTime simdi) =>
            (r.Durumu == RezervasyonDurumu.Bekliyor || r.Durumu == RezervasyonDurumu.Onaylandi) &&
            r.GecerlilikTarihi >= simdi;

        /// <summary>Kitap için şu an geçerli olan ayrılmış (Onaylandi) rezervasyonlar.</summary>
        public static Task<List<Rezervasyon>> AyrilmisKopyalarAsync(KutuphaneDbContext context, int kitapId, DateTime simdi) =>
            context.Rezervasyonlar
                .Where(r => r.KitapId == kitapId &&
                            r.Durumu == RezervasyonDurumu.Onaylandi &&
                            r.GecerlilikTarihi >= simdi)
                .ToListAsync();

        /// <summary>
        /// Rafa dönen bir kopyayı sıradaki geçerli rezervasyona ayırır. Süresi dolmuş bekleyen
        /// rezervasyonlar yol üzerinde <c>Suresi_Doldu</c> olarak işaretlenir.
        /// </summary>
        /// <returns>Kopya ayrılan rezervasyon; sırada kimse yoksa <c>null</c>.</returns>
        public static async Task<Rezervasyon?> SiradakineAyirAsync(
            KutuphaneDbContext context, BildirimService bildirimService, SistemAyarlari ayarlar,
            int kitapId, string kitapBasligi, DateTime simdi)
        {
            var bekleyenler = await context.Rezervasyonlar
                .Where(r => r.KitapId == kitapId && r.Durumu == RezervasyonDurumu.Bekliyor)
                .OrderBy(r => r.RezervasyonTarihi)
                .ThenBy(r => r.Id)
                .ToListAsync();

            foreach (var rezervasyon in bekleyenler)
            {
                // Aynı kaydetme işleminde daha önce ayrılmış/işaretlenmiş (henüz kaydedilmemiş) kayıtları atla.
                if (rezervasyon.Durumu != RezervasyonDurumu.Bekliyor)
                    continue;

                if (rezervasyon.GecerlilikTarihi < simdi)
                {
                    rezervasyon.Durumu = RezervasyonDurumu.Suresi_Doldu;
                    continue;
                }

                var gun = Math.Max(1, ayarlar.RezervasyonGecerlilikSuresi);
                rezervasyon.Durumu = RezervasyonDurumu.Onaylandi;
                rezervasyon.GecerlilikTarihi = simdi.AddDays(gun);

                bildirimService.Ekle(rezervasyon.UyeId,
                    "Rezerve Kitabınız Hazır!",
                    $"'{kitapBasligi}' kitabı sizin için ayrıldı. Lütfen {rezervasyon.GecerlilikTarihi:dd.MM.yyyy} tarihine kadar ({gun} gün içinde) teslim alınız.",
                    BildirimTipi.Rezervasyon);
                return rezervasyon;
            }

            return null;
        }
    }
}
