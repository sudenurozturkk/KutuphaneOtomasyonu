using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Barkod bankosu için kitap/üye/ödünç eşleştirme. Önce kesin tanımlayıcılar (ISBN, Id, TC, öğrenci no)
    /// denenir; serbest metin (başlık/ad) yalnızca tek bir kayıtla eşleşirse kabul edilir. Belirsiz girişler
    /// reddedilir, böylece yanlış kitap yanlış üyeye verilmez.
    /// </summary>
    public class BankoAramaService
    {
        private static readonly CultureInfo Turkce = CultureInfo.GetCultureInfo("tr-TR");
        private readonly KutuphaneDbContext _context;

        public BankoAramaService(KutuphaneDbContext context)
        {
            _context = context;
        }

        /// <summary>Nüsha barkoduyla kesin eşleşme (kitap bilgisiyle birlikte). Arşivlenmiş kitapların nüshaları da döner.</summary>
        public async Task<KitapNushasi?> NushaBulAsync(string? barkod)
        {
            var normal = NushaIslemleri.BarkodNormallestir(barkod);
            if (normal.Length == 0) return null;
            return await _context.KitapNushalari.Include(n => n.Kitap).FirstOrDefaultAsync(n => n.Barkod == normal);
        }

        /// <summary>Nüshanın aktif ödüncü; nüsha barkodu iade için tek başına yeterlidir.</summary>
        public async Task<AramaSonuc<Odunc>> NushaninAktifOduncuAsync(int nushaId)
        {
            var odunc = await _context.Oduncler
                .Include(o => o.Uye)
                .FirstOrDefaultAsync(o => o.NushaId == nushaId && o.Durumu == OduncDurumu.Aktif);
            return odunc != null
                ? AramaSonuc<Odunc>.Tek(odunc)
                : AramaSonuc<Odunc>.Bulunamadi("Bu nüsha ödünçte değil (zaten kütüphanede).");
        }

        public async Task<AramaSonuc<Kitap>> KitapBulAsync(string? giris)
        {
            var metin = giris?.Trim() ?? string.Empty;
            if (metin.Length == 0) return AramaSonuc<Kitap>.Bulunamadi("Kitap barkodu boş.");

            var isbn = Dogrulama.IsbnNormallestir(metin);
            var isbnEslesen = await _context.Kitaplar.FirstOrDefaultAsync(k => k.Aktif && k.ISBN == isbn);
            if (isbnEslesen != null) return AramaSonuc<Kitap>.Tek(isbnEslesen);

            if (int.TryParse(metin, out var id))
            {
                var idEslesen = await _context.Kitaplar.FirstOrDefaultAsync(k => k.Aktif && k.Id == id);
                if (idEslesen != null) return AramaSonuc<Kitap>.Tek(idEslesen);
            }

            var adaylar = (await _context.Kitaplar.Where(k => k.Aktif).ToListAsync())
                .Where(k => Iceriyor(k.Baslik, metin))
                .ToList();
            return Sec(adaylar, $"'{metin}' ile eşleşen kitap bulunamadı.", "kitap", k => $"#{k.Id} {k.Baslik}");
        }

        public async Task<AramaSonuc<Uye>> UyeBulAsync(string? giris)
        {
            var metin = giris?.Trim() ?? string.Empty;
            if (metin.Length == 0) return AramaSonuc<Uye>.Bulunamadi("Üye bilgisi boş.");

            var pasaport = metin.ToUpperInvariant();
            var kesin = await _context.Uyeler.FirstOrDefaultAsync(u => u.TCKimlikNo != "" && u.TCKimlikNo == metin)
                        ?? await _context.Uyeler.FirstOrDefaultAsync(u => u.PasaportNo != "" && u.PasaportNo == pasaport)
                        ?? await _context.Uyeler.FirstOrDefaultAsync(u => u.OgrenciNo != "" && u.OgrenciNo == metin);
            if (kesin != null) return AramaSonuc<Uye>.Tek(kesin);

            if (int.TryParse(metin, out var id))
            {
                var idEslesen = await _context.Uyeler.FindAsync(id);
                if (idEslesen != null) return AramaSonuc<Uye>.Tek(idEslesen);
            }

            var adaylar = (await _context.Uyeler.ToListAsync())
                .Where(u => Iceriyor(u.TamAd, metin))
                .ToList();
            return Sec(adaylar, $"'{metin}' ile eşleşen üye bulunamadı.", "üye", u => $"#{u.Id} {u.TamAd}");
        }

        /// <summary>
        /// Kitabın aktif ödüncünü bulur. Aynı kitaptan birden fazla kopya ödünçteyse üye bilgisi zorunludur.
        /// </summary>
        public async Task<AramaSonuc<Odunc>> AktifOduncBulAsync(int kitapId, string? uyeGiris)
        {
            var aktifler = await _context.Oduncler
                .Include(o => o.Uye)
                .Where(o => o.KitapId == kitapId && o.Durumu == OduncDurumu.Aktif)
                .ToListAsync();

            if (aktifler.Count == 0)
                return AramaSonuc<Odunc>.Bulunamadi("Bu kitap için aktif bir ödünç kaydı yok (zaten kütüphanede).");

            if (string.IsNullOrWhiteSpace(uyeGiris))
            {
                return aktifler.Count == 1
                    ? AramaSonuc<Odunc>.Tek(aktifler[0])
                    : AramaSonuc<Odunc>.Belirsiz($"Bu kitaptan {aktifler.Count} kopya ödünçte. İadeyi yapan üyeyi de girin.");
            }

            var uye = await UyeBulAsync(uyeGiris);
            if (uye.Kayit == null) return AramaSonuc<Odunc>.Bulunamadi(uye.Mesaj);

            var odunc = aktifler.FirstOrDefault(o => o.UyeId == uye.Kayit.Id);
            return odunc != null
                ? AramaSonuc<Odunc>.Tek(odunc)
                : AramaSonuc<Odunc>.Bulunamadi($"{uye.Kayit.TamAd} bu kitabı ödünç almamış.");
        }

        private static bool Iceriyor(string kaynak, string aranan) =>
            Turkce.CompareInfo.IndexOf(kaynak, aranan, CompareOptions.IgnoreCase) >= 0;

        private static AramaSonuc<T> Sec<T>(List<T> adaylar, string bulunamadi, string tur, Func<T, string> etiket) where T : class =>
            adaylar.Count switch
            {
                0 => AramaSonuc<T>.Bulunamadi(bulunamadi),
                1 => AramaSonuc<T>.Tek(adaylar[0]),
                _ => AramaSonuc<T>.Belirsiz(
                    $"Giriş {adaylar.Count} {tur} ile eşleşiyor ({string.Join(", ", adaylar.Take(3).Select(etiket))}{(adaylar.Count > 3 ? ", ..." : "")}). Barkod/numara girin.")
            };
    }

    public sealed class AramaSonuc<T> where T : class
    {
        public T? Kayit { get; private init; }
        public string Mesaj { get; private init; } = string.Empty;
        public bool BelirsizMi { get; private init; }

        public static AramaSonuc<T> Tek(T kayit) => new() { Kayit = kayit };
        public static AramaSonuc<T> Bulunamadi(string mesaj) => new() { Mesaj = mesaj };
        public static AramaSonuc<T> Belirsiz(string mesaj) => new() { Mesaj = mesaj, BelirsizMi = true };
    }
}
