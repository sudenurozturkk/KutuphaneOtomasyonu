using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Yönetici bakım işlemleri: yedekleme, demo verisine sıfırlama ve sistem ayarları.
    /// Tüm işlemler <see cref="KullaniciRolu.Admin"/> yetkisi gerektirir ve denetim kaydı oluşturur.
    /// </summary>
    public class VeritabaniBakimService
    {
        private readonly KutuphaneDbContext _context;

        public VeritabaniBakimService(KutuphaneDbContext context)
        {
            _context = context;
        }

        public static bool YetkiliMi(Kullanici? islemYapan) =>
            islemYapan != null && islemYapan.Aktif && islemYapan.Rol >= KullaniciRolu.Admin;

        /// <summary>
        /// SQLite çevrimiçi yedekleme API'si ile tutarlı bir kopya alır (WAL içeriği dahil).
        /// Ham dosya kopyası WAL modunda son işlemleri kaçırabileceği için kullanılmaz.
        /// </summary>
        public async Task<BakimSonuc> YedekAlAsync(string hedefYol, Kullanici? islemYapan)
        {
            if (!YetkiliMi(islemYapan)) return BakimSonuc.Yetkisiz();
            if (string.IsNullOrWhiteSpace(hedefYol)) return BakimSonuc.Hata("Hedef dosya yolu gerekli.");

            var tamYol = Path.GetFullPath(hedefYol);
            Directory.CreateDirectory(Path.GetDirectoryName(tamYol)!);

            var kaynak = (SqliteConnection)_context.Database.GetDbConnection();
            var acikti = kaynak.State == System.Data.ConnectionState.Open;
            if (!acikti) await kaynak.OpenAsync();
            try
            {
                using var hedef = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = tamYol,
                    Pooling = false
                }.ToString());
                await hedef.OpenAsync();
                kaynak.BackupDatabase(hedef);
            }
            finally
            {
                if (!acikti) await kaynak.CloseAsync();
            }

            await DenetimService.KaydetAsync(_context, DenetimService.Islemler.VeritabaniYedeklendi,
                $"Dosya {Path.GetFileName(tamYol)}", islemYapan!.Id);
            return BakimSonuc.Basarili(tamYol);
        }

        /// <summary>
        /// Katalog, üye ve dolaşım verilerini siler ve demo verisini yeniden yükler; tek transaction içinde yürür.
        /// Kullanıcı hesapları, sistem ayarları ve denetim kayıtları korunur.
        /// </summary>
        public async Task<BakimSonuc> DemoVerisineSifirlaAsync(Kullanici? islemYapan)
        {
            if (!YetkiliMi(islemYapan)) return BakimSonuc.Yetkisiz();

            await using var islem = await _context.Database.BeginTransactionAsync();

            // Yabancı anahtar sırası: önce bağımlı tablolar.
            await _context.CezaKayitlari.ExecuteDeleteAsync();
            await _context.Bildirimler.ExecuteDeleteAsync();
            await _context.Favoriler.ExecuteDeleteAsync();
            await _context.KitapYorumlari.ExecuteDeleteAsync();
            await _context.Rezervasyonlar.ExecuteDeleteAsync();
            await _context.MateryalTransferler.ExecuteDeleteAsync();
            await _context.Oduncler.ExecuteDeleteAsync();
            await _context.KitapNushalari.ExecuteDeleteAsync();
            await _context.Kitaplar.ExecuteDeleteAsync();
            await _context.CuzdanHareketleri.ExecuteDeleteAsync();
            await _context.Uyeler.ExecuteDeleteAsync();
            _context.ChangeTracker.Clear();

            DatabaseSeeder.DemoVerisiYukle(_context);

            DenetimService.Ekle(_context, DenetimService.Islemler.VeritabaniSifirlandi, "Demo verisi yeniden yuklendi",
                islemYapan!.Id, LogSeviyesi.Uyari);
            await _context.SaveChangesAsync();
            await islem.CommitAsync();
            return BakimSonuc.Basarili();
        }

        /// <summary>Sistem ayarlarını aralık kontrolüyle kaydeder.</summary>
        public async Task<BakimSonuc> AyarlariKaydetAsync(SistemAyarlari yeni, Kullanici? islemYapan)
        {
            if (!YetkiliMi(islemYapan)) return BakimSonuc.Yetkisiz();

            if (yeni.MaxOduncSuresi is < 1 or > 365) return BakimSonuc.Hata("Ödünç süresi 1–365 gün olmalıdır.");
            if (yeni.MaxOduncAdet is < 1 or > 100) return BakimSonuc.Hata("Ödünç adedi 1–100 olmalıdır.");
            if (yeni.GunlukCezaTutari is < 0 or > 10_000) return BakimSonuc.Hata("Günlük ceza 0–10.000 olmalıdır.");
            if (yeni.MaxUzatmaSayisi is < 0 or > 20) return BakimSonuc.Hata("Uzatma sayısı 0–20 olmalıdır.");
            if (string.IsNullOrWhiteSpace(yeni.KutuphaneAdi)) return BakimSonuc.Hata("Kütüphane adı zorunludur.");

            var ayar = await _context.SistemAyarlari.FirstOrDefaultAsync();
            if (ayar == null)
            {
                ayar = new SistemAyarlari();
                _context.SistemAyarlari.Add(ayar);
            }

            ayar.KutuphaneAdi = yeni.KutuphaneAdi.Trim();
            ayar.Email = yeni.Email?.Trim() ?? string.Empty;
            ayar.Telefon = yeni.Telefon?.Trim() ?? string.Empty;
            ayar.Adres = yeni.Adres?.Trim() ?? string.Empty;
            ayar.MaxOduncSuresi = yeni.MaxOduncSuresi;
            ayar.MaxOduncAdet = yeni.MaxOduncAdet;
            ayar.GunlukCezaTutari = yeni.GunlukCezaTutari;
            ayar.MaxUzatmaSayisi = yeni.MaxUzatmaSayisi;

            DenetimService.Ekle(_context, DenetimService.Islemler.AyarlarGuncellendi,
                $"Sure {ayar.MaxOduncSuresi}, adet {ayar.MaxOduncAdet}, ceza {ayar.GunlukCezaTutari}, uzatma {ayar.MaxUzatmaSayisi}",
                islemYapan!.Id);
            await _context.SaveChangesAsync();
            return BakimSonuc.Basarili();
        }
    }

    public sealed class BakimSonuc
    {
        public bool BasariliMi { get; private init; }
        public string Mesaj { get; private init; } = string.Empty;
        public string? DosyaYolu { get; private init; }

        public static BakimSonuc Basarili(string? dosyaYolu = null) => new() { BasariliMi = true, DosyaYolu = dosyaYolu };
        public static BakimSonuc Hata(string mesaj) => new() { BasariliMi = false, Mesaj = mesaj };
        public static BakimSonuc Yetkisiz() => Hata("Bu işlem yalnızca sistem yöneticisi tarafından yapılabilir.");
    }
}
