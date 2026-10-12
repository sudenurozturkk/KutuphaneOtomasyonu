using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class AyarlarPage : Page
    {
        private SistemAyarlari? _ayarlar;

        public AyarlarPage()
        {
            InitializeComponent();
            Loaded += AyarlarPage_Loaded;
        }

        private async void AyarlarPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtSonGuncelleme.Text = DateTime.Now.ToString("dd.MM.yyyy");

            try
            {
                using var context = new KutuphaneDbContext();
                _ayarlar = await context.SistemAyarlari.FirstOrDefaultAsync() ?? new SistemAyarlari();

                txtKutuphaneAdi.Text = _ayarlar.KutuphaneAdi;
                txtEmail.Text = _ayarlar.Email;
                txtTelefon.Text = _ayarlar.Telefon;
                txtAdres.Text = _ayarlar.Adres;
                txtMaxSure.Text = _ayarlar.MaxOduncSuresi.ToString();
                txtMaxAdet.Text = _ayarlar.MaxOduncAdet.ToString();
                txtGunlukCeza.Text = _ayarlar.GunlukCezaTutari.ToString();
                txtMaxUzatma.Text = _ayarlar.MaxUzatmaSayisi.ToString();
                if (txtUzatmaSuresi != null) txtUzatmaSuresi.Text = "7";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var ayar = await context.SistemAyarlari.FirstOrDefaultAsync();
                if (ayar == null)
                {
                    ayar = new SistemAyarlari();
                    context.SistemAyarlari.Add(ayar);
                }

                ayar.KutuphaneAdi = txtKutuphaneAdi.Text.Trim();
                ayar.Email = txtEmail.Text.Trim();
                ayar.Telefon = txtTelefon.Text.Trim();
                ayar.Adres = txtAdres.Text.Trim();

                if (int.TryParse(txtMaxSure.Text, out int ms)) ayar.MaxOduncSuresi = ms;
                if (int.TryParse(txtMaxAdet.Text, out int ma)) ayar.MaxOduncAdet = ma;
                if (int.TryParse(txtGunlukCeza.Text, out int gc)) ayar.GunlukCezaTutari = gc;
                if (int.TryParse(txtMaxUzatma.Text, out int mu)) ayar.MaxUzatmaSayisi = mu;

                await context.SaveChangesAsync();
                MessageBox.Show("✅ Sistem ayarları başarıyla kaydedildi.", "Başarılı",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnVeritabaniYedek_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var kaynak = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kutuphane.db");
                if (!File.Exists(kaynak))
                {
                    MessageBox.Show("Veritabanı dosyası bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var yedekAdi = $"kutuphane_yedek_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var hedef = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), yedekAdi);
                File.Copy(kaynak, hedef, true);

                MessageBox.Show($"✅ Veritabanı yedeği masaüstüne alındı:\n{yedekAdi}", "Yedek Alındı",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yedek alınırken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDataExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var kitaplar = await context.Kitaplar.ToListAsync();
                var uyeler = await context.Uyeler.ToListAsync();
                var oduncler = await context.Oduncler.Include(o => o.Kitap).Include(o => o.Uye).ToListAsync();

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

                MessageBox.Show($"✅ Veri raporu masaüstüne aktarıldı:\n{Path.GetFileName(dosya)}", "Başarılı",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dışa aktarım hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnVeritabaniSifirla_Click(object sender, RoutedEventArgs e)
        {
            var onay = MessageBox.Show(
                "⚠️ UYARI: Bu işlem mevcut tüm verileri silecek ve demo verileri yeniden yükleyecek!\n\nDevam etmek istiyor musunuz?",
                "Veritabanı Sıfırlama",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (onay == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = new KutuphaneDbContext();
                    context.Oduncler.RemoveRange(context.Oduncler);
                    context.Rezervasyonlar.RemoveRange(context.Rezervasyonlar);
                    context.KitapYorumlari.RemoveRange(context.KitapYorumlari);
                    context.Favoriler.RemoveRange(context.Favoriler);
                    context.Bildirimler.RemoveRange(context.Bildirimler);
                    context.Kitaplar.RemoveRange(context.Kitaplar);
                    context.Uyeler.RemoveRange(context.Uyeler);
                    await context.SaveChangesAsync();

                    Data.DatabaseSeeder.Seed(context);

                    MessageBox.Show("✅ Veritabanı başarıyla demo verileri ile sıfırlandı.", "Tamamlandı",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Sıfırlama hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
