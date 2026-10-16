using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyonu.Views.Pages
{
    /// <summary>
    /// Sistem ayarları ve bakım işlemleri. Yalnızca yöneticiler erişebilir; kontroller
    /// <see cref="VeritabaniBakimService"/> içinde de tekrarlanır.
    /// </summary>
    public partial class AyarlarPage : Page
    {
        public AyarlarPage()
        {
            InitializeComponent();
            Loaded += AyarlarPage_Loaded;
        }

        private static Kullanici? IslemYapan => AuthService.Instance.AktifKullanici;

        private async void AyarlarPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtSonGuncelleme.Text = DateTime.Now.ToString("dd.MM.yyyy");

            if (!VeritabaniBakimService.YetkiliMi(IslemYapan))
            {
                IsEnabled = false;
                MessageBox.Show("Sistem ayarları yalnızca sistem yöneticisi tarafından görüntülenebilir.", "Yetki",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                var ayarlar = await context.SistemAyarlari.AsNoTracking().FirstOrDefaultAsync() ?? new SistemAyarlari();

                txtKutuphaneAdi.Text = ayarlar.KutuphaneAdi;
                txtEmail.Text = ayarlar.Email;
                txtTelefon.Text = ayarlar.Telefon;
                txtAdres.Text = ayarlar.Adres;
                txtMaxSure.Text = ayarlar.MaxOduncSuresi.ToString();
                txtMaxAdet.Text = ayarlar.MaxOduncAdet.ToString();
                txtGunlukCeza.Text = ayarlar.GunlukCezaTutari.ToString();
                txtMaxUzatma.Text = ayarlar.MaxUzatmaSayisi.ToString();

                // Uzatma, ödünç süresi kadar yapılır; ayrı bir ayar olarak saklanmaz.
                txtUzatmaSuresi.Text = ayarlar.MaxOduncSuresi.ToString();
                txtUzatmaSuresi.IsReadOnly = true;
                txtUzatmaSuresi.ToolTip = "Süre uzatma, 'Maksimum Ödünç Süresi' kadar yapılır.";
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Ayarlar yukleme", ex);
                MessageBox.Show($"Ayarlar yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (!SayiOku(txtMaxSure, "Maksimum ödünç süresi", out var sure) ||
                !SayiOku(txtMaxAdet, "Maksimum ödünç adedi", out var adet) ||
                !SayiOku(txtGunlukCeza, "Günlük ceza", out var ceza) ||
                !SayiOku(txtMaxUzatma, "Maksimum uzatma", out var uzatma))
                return;

            try
            {
                using var context = new KutuphaneDbContext();
                var sonuc = await new VeritabaniBakimService(context).AyarlariKaydetAsync(new SistemAyarlari
                {
                    KutuphaneAdi = txtKutuphaneAdi.Text,
                    Email = txtEmail.Text,
                    Telefon = txtTelefon.Text,
                    Adres = txtAdres.Text,
                    MaxOduncSuresi = sure,
                    MaxOduncAdet = adet,
                    GunlukCezaTutari = ceza,
                    MaxUzatmaSayisi = uzatma
                }, IslemYapan);

                if (sonuc.BasariliMi)
                {
                    txtUzatmaSuresi.Text = sure.ToString();
                    MessageBox.Show("✅ Sistem ayarları başarıyla kaydedildi.", "Başarılı",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(sonuc.Mesaj, "Doğrulama", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Ayarlar kaydetme", ex);
                MessageBox.Show($"Ayarlar kaydedilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnVeritabaniYedek_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var yedekAdi = $"kutuphane_yedek_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var hedef = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), yedekAdi);

                using var context = new KutuphaneDbContext();
                var sonuc = await new VeritabaniBakimService(context).YedekAlAsync(hedef, IslemYapan);

                if (sonuc.BasariliMi)
                    MessageBox.Show($"✅ Veritabanı yedeği masaüstüne alındı:\n{yedekAdi}", "Yedek Alındı",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(sonuc.Mesaj, "Yedek Alınamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Veritabani yedekleme", ex);
                MessageBox.Show($"Yedek alınırken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDataExport_Click(object sender, RoutedEventArgs e)
        {
            if (!VeritabaniBakimService.YetkiliMi(IslemYapan))
            {
                MessageBox.Show("Toplu veri dışa aktarımı yalnızca sistem yöneticisi tarafından yapılabilir.", "Yetki",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var onay = MessageBox.Show(
                "Bu rapor üyelerin ad ve e-posta bilgilerini içerir (kişisel veri). Dosyayı yalnızca yetkili kişilerle paylaşın.\n\nDevam edilsin mi?",
                "Kişisel Veri Uyarısı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (onay != MessageBoxResult.Yes) return;

            try
            {
                using var context = new KutuphaneDbContext();
                var kitaplar = await context.Kitaplar.AsNoTracking().ToListAsync();
                var uyeler = await context.Uyeler.AsNoTracking().ToListAsync();
                var oduncler = await context.Oduncler.AsNoTracking().Include(o => o.Kitap).Include(o => o.Uye).ToListAsync();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== KÜTÜPHANEOTOMASYONU VERİ RAPORU ===");
                sb.AppendLine($"Dışa Aktarım Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine($"--- KİTAPLAR ({kitaplar.Count} adet) ---");
                foreach (var k in kitaplar)
                    sb.AppendLine($"  [{k.Id}] {k.Baslik} | {k.Yazar} | {k.Kategori} | Stok: {k.MevcutAdet}/{k.ToplamAdet}");

                sb.AppendLine();
                sb.AppendLine($"--- ÜYELER ({uyeler.Count} adet) ---");
                foreach (var u in uyeler)
                    sb.AppendLine($"  [{u.Id}] {u.TamAd} | {u.Email} | {u.Durumu}");

                sb.AppendLine();
                sb.AppendLine($"--- ÖDÜNÇ KAYITLARI ({oduncler.Count} adet) ---");
                foreach (var o in oduncler)
                    sb.AppendLine($"  [{o.Id}] {o.Kitap?.Baslik} -> {o.Uye?.TamAd} | {o.OduncTarihi:dd.MM.yyyy} - {o.Durumu}");

                var dosya = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"kutuphane_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(dosya, sb.ToString(), System.Text.Encoding.UTF8);

                await DenetimService.KaydetAsync(context, DenetimService.Islemler.VeriDisaAktarildi,
                    $"Kitap {kitaplar.Count}, uye {uyeler.Count}, odunc {oduncler.Count}", IslemYapan?.Id);

                MessageBox.Show($"✅ Veri raporu masaüstüne aktarıldı:\n{Path.GetFileName(dosya)}", "Başarılı",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Veri disa aktarma", ex);
                MessageBox.Show($"Dışa aktarım hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnVeritabaniSifirla_Click(object sender, RoutedEventArgs e)
        {
            var onay = MessageBox.Show(
                "⚠️ UYARI: Bu işlem tüm kitap, üye, ödünç, rezervasyon, ceza ve transfer kayıtlarını silecek ve demo verilerini yeniden yükleyecek!\n\nKullanıcı hesapları, sistem ayarları ve denetim kayıtları korunur.\n\nDevam etmek istiyor musunuz?",
                "Veritabanı Sıfırlama",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (onay != MessageBoxResult.Yes) return;

            try
            {
                using var context = new KutuphaneDbContext();
                var sonuc = await new VeritabaniBakimService(context).DemoVerisineSifirlaAsync(IslemYapan);

                if (sonuc.BasariliMi)
                    MessageBox.Show("✅ Veritabanı başarıyla demo verileri ile sıfırlandı.", "Tamamlandı",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(sonuc.Mesaj, "Sıfırlanamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Veritabani sifirlama", ex);
                MessageBox.Show($"Sıfırlama hatası: {ex.Message}\n\nHiçbir veri değiştirilmedi.", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool SayiOku(TextBox kutu, string alanAdi, out int deger)
        {
            if (int.TryParse(kutu.Text.Trim(), out deger))
                return true;

            MessageBox.Show($"{alanAdi} sayısal olmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
}
