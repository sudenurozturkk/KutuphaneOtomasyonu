using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views
{
    public partial class SifreDegistirDialog : Window
    {
        public SifreDegistirDialog(bool zorunlu = false)
        {
            InitializeComponent();
            txtYeniEtiket.Text = $"Yeni şifre * (en az {AuthService.MinimumSifreUzunlugu} karakter)";
            if (zorunlu)
                GosterHata("Şifreniz varsayılan veya çok kısa. Devam etmeden önce yeni bir şifre belirleyin.");
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
            if (txtYeni.Password != txtYeniTekrar.Password)
            {
                GosterHata("Yeni şifreler eşleşmiyor.");
                return;
            }

            btnKaydet.IsEnabled = false;
            try
            {
                using var context = new KutuphaneDbContext();
                var sonuc = await AuthService.Instance.SifreDegistirAsync(context, txtMevcut.Password, txtYeni.Password);
                if (!sonuc.Basarili)
                {
                    GosterHata(sonuc.Mesaj);
                    return;
                }

                MessageBox.Show("Şifreniz değiştirildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Sifre degistirme", ex);
                GosterHata("Şifre değiştirilemedi. Ayrıntılar günlük dosyasına yazıldı.");
            }
            finally
            {
                btnKaydet.IsEnabled = true;
            }
        }

        private void GosterHata(string mesaj)
        {
            txtHata.Text = mesaj;
            txtHata.Visibility = Visibility.Visible;
        }
    }
}
