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
    /// <remarks>
    /// Korunan değişmezler (her yazma işlemi tek transaction içinde yürür):
    /// <list type="bullet">
    /// <item><c>ToplamAdet - MevcutAdet</c> = kitabın aktif ödünç sayısı.</item>
    /// <item>Bir üyenin aynı kitap için en fazla bir aktif ödüncü olur.</item>
    /// <item>Rezervasyon sırasındaki üyelere ayrılmış kopyalar başkasına verilmez.</item>
    /// </list>
    /// </remarks>
    public class OduncService
    {
        private readonly KutuphaneDbContext _context;
        private readonly BildirimService _bildirimService;
        private readonly SistemAyarlari _ayarlar;
        private readonly TimeProvider _zaman;

        public OduncService(KutuphaneDbContext context, BildirimService bildirimService, SistemAyarlari ayarlar,
            TimeProvider? zaman = null)
        {
            _context = context;
            _bildirimService = bildirimService;
            _ayarlar = ayarlar;
            _zaman = zaman ?? TimeProvider.System;
        }

        private DateTime Simdi => _zaman.GetLocalNow().DateTime;

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
            var simdi = Simdi;
            return await _context.Oduncler
                .Include(o => o.Kitap)
                .Include(o => o.Uye)
                .Where(o => o.Durumu == OduncDurumu.Aktif && o.PlanlananIadeTarihi < simdi)
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
        /// Kitap ödünç verme işlemi. Ödünç süresi üyenin kendi limitinden (<see cref="Uye.MaxOduncSuresi"/>) alınır.
        /// Üyenin bu kitap için aktif bir rezervasyonu varsa rezervasyon tamamlanmış sayılır.
        /// </summary>
        /// <param name="nushaBarkodu">Verilecek nüshanın barkodu; boşsa raftaki ilk nüsha seçilir.</param>
        public async Task<OduncSonuc> OduncVerAsync(int kitapId, int uyeId, int kutuphanecId, string? nushaBarkodu = null)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var simdi = Simdi;

            var kitap = await _context.Kitaplar.FindAsync(kitapId);
            var uye = await _context.Uyeler.FindAsync(uyeId);

            if (kitap == null) return OduncSonuc.Hata("Kitap bulunamadı.");
            await NushaIslemleri.TamamlaAsync(_context, kitap, simdi);
            if (!kitap.Aktif) return OduncSonuc.Hata("Kitap arşivlenmiş; ödünç verilemez.");
            if (uye == null) return OduncSonuc.Hata("Üye bulunamadı.");
            if (uye.Durumu != UyeDurumu.Aktif) return OduncSonuc.Hata("Üye aktif değil.");
            if (uye.UyelikBitisTarihi.HasValue && uye.UyelikBitisTarihi.Value < simdi)
                return OduncSonuc.Hata($"Üyelik süresi {uye.UyelikBitisTarihi.Value:dd.MM.yyyy} tarihinde dolmuş.");
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

            // Rezervasyon sırasındaki diğer üyelere ayrılmış kopyalar kullanılamaz.
            var ayrilmislar = await RezervasyonKuyrugu.AyrilmisKopyalarAsync(_context, kitapId, simdi);
            var digerUyelereAyrilan = ayrilmislar.Count(r => r.UyeId != uyeId);
            if (kitap.MevcutAdet - digerUyelereAyrilan <= 0)
                return OduncSonuc.Hata("Mevcut kopyalar rezervasyon sırasındaki üyeler için ayrılmış.");

            KitapNushasi? nusha;
            if (!string.IsNullOrWhiteSpace(nushaBarkodu))
            {
                var barkod = NushaIslemleri.BarkodNormallestir(nushaBarkodu);
                nusha = await _context.KitapNushalari.FirstOrDefaultAsync(n => n.Barkod == barkod);
                if (nusha == null || nusha.KitapId != kitapId)
                    return OduncSonuc.Hata("Nüsha barkodu bu kitaba ait değil.");
                if (nusha.Durum != NushaDurumu.Rafta)
                    return OduncSonuc.Hata($"Bu nüsha ödünç verilemez: {nusha.DurumMetni}.");
            }
            else
            {
                nusha = await _context.KitapNushalari
                    .Where(n => n.KitapId == kitapId && n.Durum == NushaDurumu.Rafta)
                    .OrderBy(n => n.Id)
                    .FirstOrDefaultAsync();
                if (nusha == null)
                    return OduncSonuc.Hata("Rafta ödünç verilebilecek nüsha bulunamadı (envanter kaydı tutarsız olabilir).");
            }

            nusha.Durum = NushaDurumu.Oduncte;
            nusha.DurumTarihi = simdi;

            var odunc = new Odunc
            {
                KitapId = kitapId,
                UyeId = uyeId,
                KutuphanecId = KullaniciIdVeyaNull(kutuphanecId),
                OduncTarihi = simdi,
                PlanlananIadeTarihi = simdi.AddDays(uye.MaxOduncSuresi),
                Durumu = OduncDurumu.Aktif,
                MaxUzatmaSayisi = _ayarlar.MaxUzatmaSayisi,
                Nusha = nusha
            };

            kitap.MevcutAdet--;
            _context.Oduncler.Add(odunc);

            var uyeninRezervasyonlari = await _context.Rezervasyonlar
                .Where(r => r.KitapId == kitapId && r.UyeId == uyeId &&
                            (r.Durumu == RezervasyonDurumu.Bekliyor || r.Durumu == RezervasyonDurumu.Onaylandi))
                .ToListAsync();
            foreach (var rezervasyon in uyeninRezervasyonlari)
                rezervasyon.Durumu = RezervasyonDurumu.Tamamlandi;

            _bildirimService.Ekle(uyeId,
                "Kitap Ödünç Alındı",
                $"'{kitap.Baslik}' kitabını {odunc.PlanlananIadeTarihi:dd.MM.yyyy} tarihine kadar iade etmeniz gerekmektedir.",
                BildirimTipi.Bilgi);

            DenetimService.Ekle(_context, DenetimService.Islemler.OduncVerildi,
                $"Kitap #{kitapId}, Nusha {nusha.Barkod}, Uye #{uyeId}", KullaniciIdVeyaNull(kutuphanecId));

            return await KaydetAsync(islem, () => OduncSonuc.BasariliSonuc(odunc));
        }

        /// <summary>
        /// Kitap iade işlemi.
        /// <list type="bullet">
        /// <item><c>Kayip</c> ve <c>Hasarli</c> iadelerde kopya dolaşımdan çıkarılır (<c>ToplamAdet</c> azalır).</item>
        /// <item>Diğer durumlarda kopya rafa döner ve rezervasyon sırasındaki ilk geçerli üyeye ayrılır.</item>
        /// </list>
        /// </summary>
        public async Task<OduncSonuc> IadeAlAsync(int oduncId, KitapDurumu kitapDurumu, string? notlar = null)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var simdi = Simdi;

            var odunc = await _context.Oduncler
                .Include(o => o.Kitap)
                .Include(o => o.Uye)
                .Include(o => o.Nusha)
                .FirstOrDefaultAsync(o => o.Id == oduncId);

            if (odunc == null) return OduncSonuc.Hata("Ödünç kaydı bulunamadı.");
            if (odunc.Durumu != OduncDurumu.Aktif) return OduncSonuc.Hata("Bu ödünç aktif değil.");
            if (odunc.Kitap == null || odunc.Uye == null) return OduncSonuc.Hata("Ödünç kaydının kitap veya üye bilgisi eksik.");

            var kitap = odunc.Kitap;
            var uye = odunc.Uye;
            await NushaIslemleri.TamamlaAsync(_context, kitap, simdi);

            if (odunc.Nusha != null)
            {
                odunc.Nusha.Durum = kitapDurumu switch
                {
                    KitapDurumu.Hasarli => NushaDurumu.Hasarli,
                    KitapDurumu.Kayip => NushaDurumu.Kayip,
                    _ => NushaDurumu.Rafta
                };
                odunc.Nusha.DurumTarihi = simdi;
            }

            odunc.GercekIadeTarihi = simdi;
            odunc.IadedekiKitapDurumu = kitapDurumu;
            odunc.Durumu = OduncDurumu.Iade;
            if (notlar != null) odunc.IadeNotlari = notlar;

            // Gecikme cezası hesapla (tam gün başına)
            decimal ceza = 0;
            if (simdi > odunc.PlanlananIadeTarihi)
            {
                var gecikmeGun = (int)(simdi - odunc.PlanlananIadeTarihi).TotalDays;
                var gecikmeCezasi = gecikmeGun * _ayarlar.GunlukCezaTutari;
                if (gecikmeCezasi > 0)
                    CezaEkle(odunc, CezaTipi.Gecikme, gecikmeCezasi,
                        $"{gecikmeGun} gün gecikme x {_ayarlar.GunlukCezaTutari}₺/gün = {gecikmeCezasi}₺");
                ceza += gecikmeCezasi;
            }

            if (kitapDurumu == KitapDurumu.Hasarli)
            {
                var hasarCeza = kitap.SatinAlmaFiyati * 0.5m;
                ceza += hasarCeza;
                odunc.Durumu = OduncDurumu.Hasarli;
                CezaEkle(odunc, CezaTipi.Hasar, hasarCeza, "Hasarlı iade - kitap fiyatının %50'si");

                // Hasarlı kopya incelemeye kadar dolaşımdan çıkarılır.
                kitap.ToplamAdet--;
            }
            else if (kitapDurumu == KitapDurumu.Kayip)
            {
                var kayipCeza = kitap.SatinAlmaFiyati;
                ceza += kayipCeza;
                odunc.Durumu = OduncDurumu.Kayip;
                CezaEkle(odunc, CezaTipi.Kayip, kayipCeza, "Kayıp iade - tam kitap fiyatı");

                // Kayıp kopya rafa dönmez.
                kitap.ToplamAdet--;
            }
            else
            {
                kitap.MevcutAdet++;
                await RezervasyonKuyrugu.SiradakineAyirAsync(_context, _bildirimService, _ayarlar, kitap.Id, kitap.Baslik, simdi);
            }

            DenetimService.Ekle(_context, DenetimService.Islemler.IadeAlindi,
                $"Odunc #{odunc.Id}, Kitap #{kitap.Id}, Uye #{uye.Id}, Durum {kitapDurumu}, Ceza {ceza.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}",
                AuthService.Instance.AktifKullanici?.Id);

            return await KaydetAsync(islem, () =>
                OduncSonuc.BasariliSonuc(odunc, ceza > 0 ? $"İade başarılı. Toplam ceza: {ceza:C2}" : "İade başarılı."));
        }

        /// <summary>
        /// Ödünç süresini sistem ayarındaki <see cref="SistemAyarlari.MaxOduncSuresi"/> kadar uzatır.
        /// Gecikmiş ödünçler, uzatma hakkı bitmiş ödünçler ve başka üyenin boş kopya beklediği kitaplar uzatılamaz.
        /// </summary>
        public async Task<OduncSonuc> UzatAsync(int oduncId)
        {
            await using var islem = await VeritabaniIslemi.BaslatAsync(_context);
            var simdi = Simdi;

            var odunc = await _context.Oduncler
                .Include(o => o.Uye)
                .FirstOrDefaultAsync(o => o.Id == oduncId);

            if (odunc == null) return OduncSonuc.Hata("Ödünç kaydı bulunamadı.");
            if (odunc.Durumu != OduncDurumu.Aktif) return OduncSonuc.Hata("Bu ödünç aktif değil.");
            if (simdi > odunc.PlanlananIadeTarihi) return OduncSonuc.Hata("Gecikmiş ödünç uzatılamaz.");
            if (odunc.UzatmaSayisi >= odunc.MaxUzatmaSayisi)
                return OduncSonuc.Hata($"Uzatma hakkı doldu ({odunc.UzatmaSayisi}/{odunc.MaxUzatmaSayisi}).");

            // Başka bir üye bu kitabı bekliyorsa ve ona verilebilecek boş kopya yoksa uzatma yapılmaz.
            var baskasiBekliyor = await _context.Rezervasyonlar.AnyAsync(r =>
                r.KitapId == odunc.KitapId && r.UyeId != odunc.UyeId &&
                r.Durumu == RezervasyonDurumu.Bekliyor && r.GecerlilikTarihi >= simdi);
            if (baskasiBekliyor)
            {
                var kitap = await _context.Kitaplar.FindAsync(odunc.KitapId);
                var ayrilan = (await RezervasyonKuyrugu.AyrilmisKopyalarAsync(_context, odunc.KitapId, simdi)).Count;
                if (kitap == null || kitap.MevcutAdet - ayrilan <= 0)
                    return OduncSonuc.Hata("Bu kitap için sırada bekleyen üye var; süre uzatılamaz. Lütfen kitabı iade edin.");
            }

            odunc.PlanlananIadeTarihi = odunc.PlanlananIadeTarihi.AddDays(_ayarlar.MaxOduncSuresi);
            odunc.UzatmaSayisi++;

            _bildirimService.Ekle(odunc.UyeId,
                "Ödünç Süresi Uzatıldı",
                $"Ödünç süreniz {odunc.PlanlananIadeTarihi:dd.MM.yyyy} tarihine uzatıldı.",
                BildirimTipi.Bilgi);

            DenetimService.Ekle(_context, DenetimService.Islemler.SureUzatildi,
                $"Odunc #{odunc.Id}, yeni tarih {odunc.PlanlananIadeTarihi:yyyy-MM-dd}",
                AuthService.Instance.AktifKullanici?.Id);

            return await KaydetAsync(islem, () => OduncSonuc.BasariliSonuc(odunc, "Süre uzatıldı."));
        }

        /// <summary>Dashboard istatistikleri.</summary>
        public async Task<OduncIstatistik> GetIstatistikAsync()
        {
            var bugun = Simdi.Date;
            var simdi = Simdi;
            return new OduncIstatistik
            {
                AktifOduncSayisi = await _context.Oduncler.CountAsync(o => o.Durumu == OduncDurumu.Aktif),
                GecikmisSayisi = await _context.Oduncler.CountAsync(o => o.Durumu == OduncDurumu.Aktif && o.PlanlananIadeTarihi < simdi),
                BugunIadeSayisi = await _context.Oduncler.CountAsync(o => o.GercekIadeTarihi.HasValue && o.GercekIadeTarihi.Value.Date == bugun),
                BugunOduncSayisi = await _context.Oduncler.CountAsync(o => o.OduncTarihi.Date == bugun),
                ToplamOduncSayisi = await _context.Oduncler.CountAsync(),
                ToplamCeza = await _context.CezaKayitlari.SumAsync(c => c.Tutar),
                OdenenCeza = await _context.CezaKayitlari.Where(c => c.Odendi).SumAsync(c => c.Tutar)
            };
        }

        private void CezaEkle(Odunc odunc, CezaTipi tip, decimal tutar, string aciklama)
        {
            odunc.CezaTutari += tutar;
            odunc.Uye!.ToplamCeza += tutar;
            _context.CezaKayitlari.Add(new CezaKaydi
            {
                UyeId = odunc.UyeId,
                OduncId = odunc.Id,
                Tutar = tutar,
                Tipi = tip,
                Aciklama = aciklama,
                OlusmaTarihi = Simdi
            });
        }

        private async Task<OduncSonuc> KaydetAsync(
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? islem, Func<OduncSonuc> basarili)
        {
            try
            {
                await _context.SaveChangesAsync();
                if (islem != null) await islem.CommitAsync();
                return basarili();
            }
            catch (DbUpdateException ex)
            {
                UygulamaLog.Hata("OduncService kayit", ex);
                _context.ChangeTracker.Clear();
                return OduncSonuc.Hata("İşlem veritabanı kuralları nedeniyle kaydedilemedi (eşzamanlı bir işlem olabilir). Listeyi yenileyip tekrar deneyin.");
            }
        }

        // Eski çağrılar oturum yokken 1 değerini gönderiyordu; var olmayan kullanıcıya FK yazılmasın.
        private int? KullaniciIdVeyaNull(int kutuphanecId) =>
            kutuphanecId > 0 ? kutuphanecId : null;
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
