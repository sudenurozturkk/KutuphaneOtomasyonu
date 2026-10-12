using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class IadeDialog : Window
    {
        private int _oduncId;

        public IadeDialog(int oduncId)
        {
            InitializeComponent();
            _oduncId = oduncId;
            cbKitapDurumu.ItemsSource = Enum.GetValues(typeof(KitapDurumu));
            cbKitapDurumu.SelectedItem = KitapDurumu.Iyi;
            Loaded += IadeDialog_Loaded;
        }

        private async void IadeDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var odunc = await context.Oduncler
                    .Include(o => o.Kitap)
                    .Include(o => o.Uye)
                    .FirstOrDefaultAsync(o => o.Id == _oduncId);

                if (odunc != null)
                {
                    txtOduncBilgi.Text = $"Kitap: {odunc.Kitap.Baslik}\nÜye: {odunc.Uye.TamAd}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt bulunamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void BtnOnayla_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var bildirimService = new BildirimService(context);
                var ayarlar = await context.SistemAyarlari.FindAsync(1) ?? new SistemAyarlari();
                var service = new OduncService(context, bildirimService, ayarlar);

                var secilenDurum = cbKitapDurumu.SelectedItem is KitapDurumu kd ? kd : KitapDurumu.Iyi;
                var notlar = txtNotlar.Text.Trim();

                var sonuc = await service.IadeAlAsync(_oduncId, secilenDurum, notlar);

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
                MessageBox.Show($"İade işlemi sırasında hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
