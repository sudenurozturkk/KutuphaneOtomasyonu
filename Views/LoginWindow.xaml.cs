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

            Loaded += (s, e) => UygulamaLog.Yaz("[LoginWindow] Loaded event");
            Closed += (s, e) => UygulamaLog.Yaz("[LoginWindow] Closed event");

            // Sürükleme için
            MouseLeftButtonDown += (s, e) => DragMove();

#if DEBUG
            // Demo kimlik bilgileri yalnızca geliştirme derlemelerinde doldurulur.
            txtKullaniciAdi.Text = "admin";
            txtSifre.Password = "admin123";
#else
            pnlDemoHesaplar.Visibility = Visibility.Collapsed;
#endif
        }

        private async void BtnGiris_Click(object sender, RoutedEventArgs e)
        {
            await GirisYap();
        }

        private void TxtKullaniciAdi_KeyDown(object sender, KeyEventArgs e)
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
                var sonuc = await AuthService.Instance.GirisDetayliAsync(
                    txtKullaniciAdi.Text.Trim(),
                    txtSifre.Password,
                    context);

                if (sonuc.Basarili)
                {
#if !DEBUG
                    // Varsayılan/kısa parolalarla üretim kullanımına izin verilmez (Debug demo derlemesinde atlanır).
                    if (sonuc.SifreDegistirmeGerekli &&
                        new SifreDegistirDialog(zorunlu: true) { Owner = this }.ShowDialog() != true)
                    {
                        AuthService.Instance.Cikis();
                        GosterHata("Devam etmek için şifrenizi değiştirmeniz gerekir.");
                        return;
                    }
#endif
                    UygulamaLog.Yaz("[Login] Basarili, MainWindow aciliyor");

                    var mainWindow = new MainWindow();
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    Close();
                }
                else
                {
                    GosterHata(sonuc.Mesaj);
                }
            }
            catch (System.Exception ex)
            {
                UygulamaLog.Hata("Login", ex);
                GosterHata("Giriş sırasında beklenmeyen bir hata oluştu. Ayrıntılar günlük dosyasına yazıldı.");
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
#if DEBUG
            txtKullaniciAdi.Text = "admin";
            txtSifre.Password = "admin123";
            pnlHata.Visibility = Visibility.Collapsed;
#endif
        }

        private void FillFatma_Click(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            txtKullaniciAdi.Text = "fatmak";
            txtSifre.Password = "123456";
            pnlHata.Visibility = Visibility.Collapsed;
#endif
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
