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
    /// Üye cüzdanı: bankoda alınan ödemelerle bakiye yükleme ve bakiyeden ceza tahsilatı.
    /// Uygulama ödeme sağlayıcısına bağlanmaz; yükleme, personelin tahsil ettiği tutarın kaydıdır.
    /// </summary>
    public class CuzdanService
    {
        public const decimal TekYuklemeUstSiniri = 5_000m;

        private readonly KutuphaneDbContext _context;

        public CuzdanService(KutuphaneDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> BakiyeAsync(int uyeId)
        {
            // SQLite decimal toplamını sunucu tarafında desteklemediği için istemci tarafında toplanır.
            var tutarlar = await _context.CuzdanHareketleri.Where(h => h.UyeId == uyeId).Select(h => h.Tutar).ToListAsync();
            return tutarlar.Sum();
        }

        public async Task<List<CuzdanHareketi>> HareketlerAsync(int uyeId, int adet = 50) =>
            await _context.CuzdanHareketleri
                .AsNoTracking()
                .Where(h => h.UyeId == uyeId)
                .OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id)
                .Take(adet)
                .ToListAsync();

        public async Task<CuzdanSonuc> YukleAsync(int uyeId, decimal tutar, int? kullaniciId)
        {
            if (tutar <= 0) return CuzdanSonuc.Hata("Yükleme tutarı sıfırdan büyük olmalıdır.");
            if (tutar > TekYuklemeUstSiniri) return CuzdanSonuc.Hata($"Tek seferde en fazla ₺{TekYuklemeUstSiniri:N0} yüklenebilir.");

            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var uye = await _context.Uyeler.FindAsync(uyeId);
            if (uye == null || uye.AnonimMi) return CuzdanSonuc.Hata("Üye bulunamadı.");

            _context.CuzdanHareketleri.Add(new CuzdanHareketi
            {
                UyeId = uyeId,
                Tutar = tutar,
                Tip = CuzdanHareketTipi.Yukleme,
                Aciklama = "Bankoda bakiye yükleme",
                KullaniciId = kullaniciId
            });
            DenetimService.Ekle(_context, DenetimService.Islemler.CuzdanYukleme, $"Uye #{uyeId}, tutar {tutar.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}", kullaniciId);
            await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return CuzdanSonuc.Basarili(await BakiyeAsync(uyeId));
        }

        /// <summary>Üyenin kalan ceza borcunun tamamını cüzdan bakiyesinden tahsil eder.</summary>
        public async Task<CuzdanSonuc> CezaOdeAsync(int uyeId, int? kullaniciId)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var uye = await _context.Uyeler.FindAsync(uyeId);
            if (uye == null) return CuzdanSonuc.Hata("Üye bulunamadı.");

            var borc = uye.KalanCeza;
            if (borc <= 0) return CuzdanSonuc.Hata("Üyenin ödenecek borcu yok.");
            var bakiye = await BakiyeAsync(uyeId);
            if (bakiye < borc) return CuzdanSonuc.Hata($"Bakiye yetersiz (bakiye ₺{bakiye:N2}, borç ₺{borc:N2}).");

            var odeme = await new UyeService(_context).CezaOdeAsync(uyeId, borc, kullaniciId);
            if (!odeme.BasariliMi) return CuzdanSonuc.Hata(odeme.Mesaj);

            _context.CuzdanHareketleri.Add(new CuzdanHareketi
            {
                UyeId = uyeId,
                Tutar = -borc,
                Tip = CuzdanHareketTipi.CezaOdeme,
                Aciklama = "Ceza borcu ödemesi",
                KullaniciId = kullaniciId
            });
            await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return CuzdanSonuc.Basarili(bakiye - borc);
        }
    }

    public sealed class CuzdanSonuc
    {
        public bool BasariliMi { get; private init; }
        public string Mesaj { get; private init; } = string.Empty;
        public decimal YeniBakiye { get; private init; }

        public static CuzdanSonuc Basarili(decimal bakiye) => new() { BasariliMi = true, YeniBakiye = bakiye };
        public static CuzdanSonuc Hata(string mesaj) => new() { BasariliMi = false, Mesaj = mesaj };
    }
}
