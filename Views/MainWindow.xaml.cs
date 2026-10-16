using KutuphaneOtomasyonu.Services;
using KutuphaneOtomasyonu.Views.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace KutuphaneOtomasyonu.Views
{
    public partial class MainWindow : Window
    {
        private Button? _aktifNavBtn;
        private bool _isLoggingOut = false;
        private readonly DispatcherTimer _saatTimer;

        public static MainWindow? Instance { get; private set; }

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            Closed += (s, e) =>
            {
                if (!_isLoggingOut)
                    Application.Current.Shutdown();
            };

            // Kullanıcı bilgilerini göster
            var kullanici = AuthService.Instance.AktifKullanici;
            if (kullanici != null)
            {
                txtKullaniciAdi.Text = kullanici.TamAd;
                txtKullaniciRol.Text = kullanici.RolMetni;
                if (kullanici.Ad?.Length > 0)
                    txtAvatarHarf.Text = kullanici.Ad[0].ToString().ToUpper();
            }

            // Sistem ayarları ve bakım işlemleri yalnızca yöneticilere açıktır.
            if (!AuthService.Instance.YetkiVarMi(Models.KullaniciRolu.Admin))
                btnAyarlar.Visibility = Visibility.Collapsed;

            // Saat timer
            _saatTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _saatTimer.Tick += (s, e) =>
            {
                var now = DateTime.Now;
                txtSaatGoster.Text = now.ToString("HH:mm:ss");
                txtStatusSaat.Text = $"Sistem: Aktif • {now:dd.MM.yyyy}";
            };
            _saatTimer.Start();
            txtSaatGoster.Text = DateTime.Now.ToString("HH:mm:ss");

            // Dashboard'u aç
            NavigateTo("Dashboard");
            SetActiveNavButton(btnDashboard);
        }

        public void NavigateToNav(string tag)
        {
            NavigateTo(tag);
            var btn = tag switch
            {
                "Dashboard" => btnDashboard,
                "Kitaplar" => btnKitaplar,
                "Oduncler" => btnOduncler,
                "Uyeler" => btnUyeler,
                "SalonRezervasyon" => btnSalonRezervasyon,
                "Rezervasyonlar" => btnRezervasyonlar,
                "BarkodBanko" => btnBarkodBanko,
                "Cuzdan" => btnCuzdan,
                "EKitap" => btnEKitap,
                "Raporlar" => btnRaporlar,
                "Ayarlar" => btnAyarlar,
                "Akademik" => btnAkademik,
                "KampusTransfer" => btnKampusTransfer,
                "Mimari" => btnMimari,
                _ => null
            };
            if (btn != null) SetActiveNavButton(btn);
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var tag = btn.Tag?.ToString();
                NavigateTo(tag ?? "Dashboard");
                if (tag != "Cikis")
                    SetActiveNavButton(btn);
            }
        }

        private void BtnSifreDegistir_Click(object sender, RoutedEventArgs e)
        {
            new SifreDegistirDialog { Owner = this }.ShowDialog();
        }

        private void BtnDilDegistir_Click(object sender, RoutedEventArgs e)
        {
            var yeniDil = LocalizationService.Instance.AktifDil == "TR" ? "EN" : "TR";
            LocalizationService.Instance.DilDegistir(yeniDil);
            txtMevcutDilBayrak.Text = yeniDil == "EN" ? "🇬🇧 EN / TR" : "🇹🇷 TR / EN";

            MessageBox.Show(
                yeniDil == "EN"
                    ? "🌐 Interface language switched to English (Global University Consortium)."
                    : "🌐 Arayüz dili Türkçe olarak güncellendi.",
                "Language / Dil", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateTo(string sayfa)
        {
            string baslik;
            string iconKind;

            switch (sayfa)
            {
                case "Dashboard":
                    baslik = "Dashboard";
                    iconKind = "ViewDashboard";
                    MainFrame.Navigate(new DashboardPage());
                    break;
                case "Kitaplar":
                    baslik = "Kitap Kataloğu";
                    iconKind = "BookOpenPageVariant";
                    MainFrame.Navigate(new KitaplarPage());
                    break;
                case "Oduncler":
                    baslik = "Ödünç İşlemleri";
                    iconKind = "SwapHorizontal";
                    MainFrame.Navigate(new OdunclerPage());
                    break;
                case "Uyeler":
                    baslik = "Üye Yönetimi";
                    iconKind = "AccountGroup";
                    MainFrame.Navigate(new UyelerPage());
                    break;
                case "SalonRezervasyon":
                    baslik = "Salon Haritası & Rezervasyon";
                    iconKind = "DeskLamp";
                    MainFrame.Navigate(new SalonRezervasyonPage());
                    break;
                case "Rezervasyonlar":
                    baslik = "Rezervasyonlar";
                    iconKind = "CalendarClock";
                    MainFrame.Navigate(new RezervasyonlarPage());
                    break;
                case "BarkodBanko":
                    baslik = "Barkod Banko Terminali";
                    iconKind = "Barcode";
                    MainFrame.Navigate(new BarkodBankoPage());
                    break;
                case "Cuzdan":
                    baslik = "Dijital Cüzdan & Üye Kartı";
                    iconKind = "Wallet";
                    MainFrame.Navigate(new CuzdanPage());
                    break;
                case "EKitap":
                    baslik = "E-Kitap Okuyucu";
                    iconKind = "BookOpenBlankVariant";
                    MainFrame.Navigate(new EKitapPage());
                    break;
                case "Raporlar":
                    baslik = "Raporlar & Analitik";
                    iconKind = "ChartBar";
                    MainFrame.Navigate(new RaporlarPage());
                    break;
                case "Ayarlar" when !AuthService.Instance.YetkiVarMi(Models.KullaniciRolu.Admin):
                    MessageBox.Show("Sistem ayarları yalnızca sistem yöneticisi tarafından görüntülenebilir.", "Yetki",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                case "Ayarlar":
                    baslik = "Sistem Ayarları";
                    iconKind = "Cog";
                    MainFrame.Navigate(new AyarlarPage());
                    break;
                case "Akademik":
                    baslik = "Akademik Tez & Makale Havuzu (IRIS)";
                    iconKind = "School";
                    MainFrame.Navigate(new AkademikHubPage());
                    break;
                case "KampusTransfer":
                    baslik = "Kampüsler Arası Transfer (ILL)";
                    iconKind = "TruckDelivery";
                    MainFrame.Navigate(new KampusTransferPage());
                    break;
                case "Mimari":
                    baslik = "Canlı Mimari & UML Müfettişi";
                    iconKind = "ChartBubble";
                    MainFrame.Navigate(new MimariUmlPage());
                    break;
                case "Cikis":
                    _saatTimer.Stop();
                    AuthService.Instance.Cikis();
                    _isLoggingOut = true;
                    var loginWindow = new LoginWindow();
                    Application.Current.MainWindow = loginWindow;
                    loginWindow.Show();
                    Close();
                    return;
                default:
                    baslik = sayfa;
                    iconKind = "Circle";
                    break;
            }

            txtSayfaBasligi.Text = baslik;

            // İkon güncelle
            try
            {
                if (Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(iconKind, out var kind))
                    iconSayfa.Kind = kind;
            }
            catch { }
        }

        private void SetActiveNavButton(Button btn)
        {
            // Önceki aktif butonu sıfırla
            if (_aktifNavBtn != null)
            {
                _aktifNavBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)); // #94A3B8
                _aktifNavBtn.Background = Brushes.Transparent;
            }

            _aktifNavBtn = btn;
            // Aktif butonu vurgula - parlak beyaz ve koyu mavi kart zemin
            btn.Foreground = new SolidColorBrush(Colors.White);
            btn.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)); // #1E293B
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
