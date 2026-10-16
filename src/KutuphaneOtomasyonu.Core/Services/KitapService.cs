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
                .Include(k => k.Nushalar)
                .FirstOrDefaultAsync(k => k.Id == id);
        }

        /// <summary>Doğrulayıp yeni kitap ekler. ISBN tire/boşluk olmadan saklanır.</summary>
        public async Task<KitapSonuc> EkleAsync(Kitap kitap, int? kullaniciId)
        {
            kitap.ISBN = Dogrulama.IsbnNormallestir(kitap.ISBN);
            var hata = AlanlariDogrula(kitap, isbnDegisti: true) ?? await IsbnCakismasiAsync(kitap.ISBN, null);
            if (hata != null) return KitapSonuc.Hata(hata);

            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            kitap.MevcutAdet = kitap.ToplamAdet;
            kitap.EklenmeTarihi = DateTime.Now;
            _context.Kitaplar.Add(kitap);
            await _context.SaveChangesAsync();

            // Nüsha barkodları kitap numarasını içerdiği için kitap kaydedildikten sonra üretilir.
            await NushaIslemleri.YeniEkleAsync(_context, kitap, kitap.ToplamAdet, kitap.EklenmeTarihi);
            await DenetimService.KaydetAsync(_context, DenetimService.Islemler.KitapEklendi,
                $"Kitap #{kitap.Id}, {kitap.ToplamAdet} nusha", kullaniciId);
            if (islem != null) await islem.CommitAsync();
            return KitapSonuc.Basarili(kitap);
        }

        /// <summary>
        /// Kitap bilgilerini ve toplam kopya sayısını günceller. Toplam adet ödünçteki kopya sayısının
        /// altına indirilemez; mevcut adet <c>ToplamAdet - aktif ödünç</c> olarak yeniden hesaplanır.
        /// </summary>
        public async Task<KitapSonuc> GuncelleAsync(Kitap degisiklik, int? kullaniciId)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);

            var kitap = await _context.Kitaplar.FindAsync(degisiklik.Id);
            if (kitap == null) return KitapSonuc.Hata("Kitap bulunamadı.");

            var yeniIsbn = Dogrulama.IsbnNormallestir(degisiklik.ISBN);
            var isbnDegisti = yeniIsbn != Dogrulama.IsbnNormallestir(kitap.ISBN);
            degisiklik.ISBN = isbnDegisti ? yeniIsbn : kitap.ISBN;

            var hata = AlanlariDogrula(degisiklik, isbnDegisti) ??
                       (isbnDegisti ? await IsbnCakismasiAsync(yeniIsbn, kitap.Id) : null);
            if (hata != null) return KitapSonuc.Hata(hata);

            var simdi = DateTime.Now;
            await NushaIslemleri.TamamlaAsync(_context, kitap, simdi);

            var oduncte = await _context.Oduncler.CountAsync(o => o.KitapId == kitap.Id && o.Durumu == OduncDurumu.Aktif);
            if (degisiklik.ToplamAdet < oduncte)
                return KitapSonuc.Hata($"Toplam adet, ödünçteki kopya sayısının ({oduncte}) altına indirilemez.");

            var eskiRaf = kitap.RafKonumu;
            var yeniRaf = degisiklik.RafKonumu.Trim();
            if (eskiRaf != yeniRaf)
            {
                // Kitabın raf konumuyla aynı yerde duran nüshalar birlikte taşınır.
                var tasinacaklar = await _context.KitapNushalari
                    .Where(n => n.KitapId == kitap.Id && n.RafKonumu == eskiRaf)
                    .ToListAsync();
                foreach (var nusha in tasinacaklar)
                    nusha.RafKonumu = yeniRaf;
                kitap.RafKonumu = yeniRaf;
            }

            var fark = degisiklik.ToplamAdet - kitap.ToplamAdet;
            if (fark > 0)
            {
                await NushaIslemleri.YeniEkleAsync(_context, kitap, fark, simdi);
            }
            else if (fark < 0)
            {
                // Toplam azaltılırsa en son eklenen raftaki nüshalar envanterden çıkarılır (kayıtları korunur).
                var cikarilacaklar = await _context.KitapNushalari
                    .Where(n => n.KitapId == kitap.Id && n.Durum == NushaDurumu.Rafta)
                    .OrderByDescending(n => n.Id)
                    .Take(-fark)
                    .ToListAsync();
                if (cikarilacaklar.Count < -fark)
                    return KitapSonuc.Hata("Envanterden çıkarılacak kadar rafta nüsha yok.");
                foreach (var nusha in cikarilacaklar)
                {
                    nusha.Durum = NushaDurumu.Cikarildi;
                    nusha.DurumTarihi = simdi;
                }
            }

            kitap.Baslik = degisiklik.Baslik.Trim();
            kitap.Yazar = degisiklik.Yazar.Trim();
            kitap.ISBN = degisiklik.ISBN;
            kitap.Kategori = degisiklik.Kategori.Trim();
            kitap.Yayinevi = degisiklik.Yayinevi.Trim();
            kitap.RafKonumu = degisiklik.RafKonumu.Trim();
            kitap.Aciklama = degisiklik.Aciklama.Trim();
            kitap.YayinYili = degisiklik.YayinYili;
            kitap.SayfaSayisi = degisiklik.SayfaSayisi;
            kitap.ToplamAdet = degisiklik.ToplamAdet;
            kitap.MevcutAdet = degisiklik.ToplamAdet - oduncte;
            kitap.GuncellenmeTarihi = simdi;

            DenetimService.Ekle(_context, DenetimService.Islemler.KitapGuncellendi,
                $"Kitap #{kitap.Id}, toplam {kitap.ToplamAdet}", kullaniciId);
            await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return KitapSonuc.Basarili(kitap);
        }

        /// <summary>
        /// Kitabı arşivler (Aktif=false). Ödünçte kopyası olan kitap arşivlenemez; bekleyen rezervasyonlar iptal edilir.
        /// Geçmiş ödünç kayıtları korunur.
        /// </summary>
        public async Task<KitapSonuc> DeleteAsync(int id, int? kullaniciId = null)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);

            var kitap = await _context.Kitaplar.FindAsync(id);
            if (kitap == null) return KitapSonuc.Hata("Kitap bulunamadı.");

            var oduncte = await _context.Oduncler.CountAsync(o => o.KitapId == id && o.Durumu == OduncDurumu.Aktif);
            if (oduncte > 0)
                return KitapSonuc.Hata($"Kitabın {oduncte} kopyası ödünçte. Arşivlemeden önce iadeleri alın.");

            kitap.Aktif = false;
            var rezervasyonlar = await _context.Rezervasyonlar
                .Where(r => r.KitapId == id &&
                            (r.Durumu == RezervasyonDurumu.Bekliyor || r.Durumu == RezervasyonDurumu.Onaylandi))
                .ToListAsync();
            foreach (var rezervasyon in rezervasyonlar)
                rezervasyon.Durumu = RezervasyonDurumu.Iptal;

            DenetimService.Ekle(_context, DenetimService.Islemler.KitapArsivlendi,
                $"Kitap #{id}, iptal edilen rezervasyon {rezervasyonlar.Count}", kullaniciId);
            await _context.SaveChangesAsync();
            if (islem != null) await islem.CommitAsync();
            return KitapSonuc.Basarili(kitap);
        }

        private static string? AlanlariDogrula(Kitap kitap, bool isbnDegisti)
        {
            if (string.IsNullOrWhiteSpace(kitap.Baslik)) return "Başlık zorunludur.";
            if (string.IsNullOrWhiteSpace(kitap.Yazar)) return "Yazar zorunludur.";
            if (isbnDegisti && kitap.ISBN.Length > 0 && !Dogrulama.IsbnGecerliMi(kitap.ISBN))
                return "ISBN geçersiz. 10 veya 13 haneli, kontrol basamağı doğru bir ISBN girin.";
            if (kitap.YayinYili != 0 && (kitap.YayinYili < 1000 || kitap.YayinYili > DateTime.Now.Year + 1))
                return "Yayın yılı geçersiz.";
            if (kitap.SayfaSayisi < 0) return "Sayfa sayısı negatif olamaz.";
            if (kitap.ToplamAdet < 0) return "Toplam adet negatif olamaz.";
            return null;
        }

        private async Task<string?> IsbnCakismasiAsync(string isbn, int? haricId)
        {
            if (isbn.Length == 0) return null;
            var mevcut = await _context.Kitaplar
                .AsNoTracking()
                .Where(k => k.ISBN == isbn && (haricId == null || k.Id != haricId))
                .Select(k => new { k.Id, k.Aktif })
                .FirstOrDefaultAsync();
            if (mevcut == null) return null;
            return mevcut.Aktif
                ? $"Bu ISBN zaten kayıtlı (Kitap #{mevcut.Id})."
                : $"Bu ISBN arşivlenmiş bir kayıtta kullanılıyor (Kitap #{mevcut.Id}).";
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
                .ThenBy(x => x.KitapId)
                .Take(adet)
                .ToListAsync();

            var ids = populer.Select(p => p.KitapId).ToList();
            var kitaplar = await _context.Kitaplar
                .Where(k => ids.Contains(k.Id) && k.Aktif)
                .ToDictionaryAsync(k => k.Id);

            // IN sorgusu sırayı korumaz; ödünç sayısı sırasını geri uygula.
            return populer
                .Where(p => kitaplar.ContainsKey(p.KitapId))
                .Select(p => kitaplar[p.KitapId])
                .ToList();
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

    public sealed class KitapSonuc
    {
        public bool BasariliMi { get; private init; }
        public string Mesaj { get; private init; } = string.Empty;
        public Kitap? Kitap { get; private init; }

        public static KitapSonuc Basarili(Kitap kitap) => new() { BasariliMi = true, Kitap = kitap };
        public static KitapSonuc Hata(string mesaj) => new() { BasariliMi = false, Mesaj = mesaj };
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
