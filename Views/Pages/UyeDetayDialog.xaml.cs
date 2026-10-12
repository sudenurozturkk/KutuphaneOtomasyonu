using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class UyeDetayDialog : Window
    {
        private int _uyeId;

        public UyeDetayDialog(int uyeId)
        {
            InitializeComponent();
            _uyeId = uyeId;
            Loaded += UyeDetayDialog_Loaded;
        }

        private async void UyeDetayDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var service = new UyeService(context);
                var u = await service.GetByIdAsync(_uyeId);
                if (u != null)
                {
                    txtTamAd.Text = u.TamAd;
                    txtUyeTipi.Text = $"Üye Tipi: {u.UyeTipi}";
                    txtTC.Text = $"TC Kimlik: {u.TCKimlikNo}";
                    txtEmail.Text = $"E-Posta: {u.Email}";
                    txtTelefon.Text = $"Telefon: {u.Telefon}";
                    txtOgrenciNo.Text = $"Öğrenci/Sicil No: {u.OgrenciNo}";
                    txtBolum.Text = $"Bölüm: {u.Bolum} / {u.Fakulte}";
                    txtDurum.Text = $"Durum: {u.Durumu}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Üye bilgileri yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnKapat_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
