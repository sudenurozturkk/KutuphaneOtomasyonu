using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            var logPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup.log");
            Loaded += (s, e) => System.IO.File.AppendAllText(logPath, "[LoginWindow] Loaded event\n");
            Closing += (s, e) => System.IO.File.AppendAllText(logPath, $"[LoginWindow] Closing event (Cancel={e.Cancel})\n");
            Closed += (s, e) => System.IO.File.AppendAllText(logPath, "[LoginWindow] Closed event\n");

            // Sürükleme için
            MouseLeftButtonDown += (s, e) => DragMove();

            txtKullaniciAdi.Text = "admin";
            txtSifre.Password = "admin123";
        }

        private async void BtnGiris_Click(object sender, RoutedEventArgs e)
        {
            await GirisYap();
        }

        private async void TxtKullaniciAdi_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) txtSifre.Focus();
        }

        private async void TxtSifre_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) await GirisYap();
        }

        private async System.Threading.Tasks.Task GirisYap()
        {
            if (string.IsNullOrWhiteSpace(txtKullaniciAdi.Text) || string.IsNullOrWhiteSpace(txtSifre.Password))
            {
                GosterHata("Kullanıcı adı ve şifre gereklidir.");
                return;
            }

            btnGiris.IsEnabled = false;
            pbYukleme.Visibility = Visibility.Visible;
            pnlHata.Visibility = Visibility.Collapsed;

            try
            {
                using var context = new KutuphaneDbContext();
                var basarili = await AuthService.Instance.GirisAsync(
                    txtKullaniciAdi.Text.Trim(),
                    txtSifre.Password,
                    context);

                if (basarili)
                {
                    var logPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup.log");
                    System.IO.File.AppendAllText(logPath, "[Login] Basarili! MainWindow olusturuluyor...\n");

                    var mainWindow = new MainWindow();
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();

                    System.IO.File.AppendAllText(logPath, "[Login] MainWindow gosterildi, LoginWindow kapatiliyor...\n");
                    Close();
                }
                else
                {
                    GosterHata("Kullanıcı adı veya şifre hatalı.");
                }
            }
            catch (System.Exception ex)
            {
                var logPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup.log");
                System.IO.File.AppendAllText(logPath, $"[Login Hata Detayi] {ex}\n");
                GosterHata($"Hata: {ex.InnerException?.Message ?? ex.Message}");
            }
            finally
            {
                btnGiris.IsEnabled = true;
                pbYukleme.Visibility = Visibility.Collapsed;
            }
        }

        private void GosterHata(string mesaj)
        {
            txtHata.Text = mesaj;
            pnlHata.Visibility = Visibility.Visible;
        }

        private void FillAdmin_Click(object sender, MouseButtonEventArgs e)
        {
            txtKullaniciAdi.Text = "admin";
            txtSifre.Password = "admin123";
            pnlHata.Visibility = Visibility.Collapsed;
        }

        private void FillFatma_Click(object sender, MouseButtonEventArgs e)
        {
            txtKullaniciAdi.Text = "fatmak";
            txtSifre.Password = "123456";
            pnlHata.Visibility = Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
