using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class OduncVerDialog : Window
    {
        public OduncVerDialog()
        {
            InitializeComponent();
            Loaded += OduncVerDialog_Loaded;
        }

        private async void OduncVerDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var kitaplar = await context.Kitaplar
                    .Where(k => k.MevcutAdet > 0)
                    .OrderBy(k => k.Baslik)
                    .ToListAsync();
                cbKitaplar.ItemsSource = kitaplar;

                var uyeler = await context.Uyeler
                    .Where(u => u.Durumu == UyeDurumu.Aktif)
                    .OrderBy(u => u.Ad)
                    .ToListAsync();
                cbUyeler.ItemsSource = uyeler;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnKapat_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (cbKitaplar.SelectedValue is not int kitapId || cbUyeler.SelectedValue is not int uyeId)
            {
                MessageBox.Show("Lütfen bir kitap ve bir üye seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                var bildirimService = new BildirimService(context);
                var ayarlar = await context.SistemAyarlari.FindAsync(1) ?? new SistemAyarlari();
                var service = new OduncService(context, bildirimService, ayarlar);

                var kutuphaneciId = AuthService.Instance.AktifKullanici?.Id ?? 1;
                var sonuc = await service.OduncVerAsync(kitapId, uyeId, kutuphaneciId);

                if (sonuc.Basarili)
                {
                    MessageBox.Show(sonuc.Mesaj, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(sonuc.Mesaj, "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İşlem sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
