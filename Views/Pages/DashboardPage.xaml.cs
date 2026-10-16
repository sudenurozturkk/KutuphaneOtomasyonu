using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await VeriYukle();
        }

        private async System.Threading.Tasks.Task VeriYukle()
        {
            try
            {
                txtTarih.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy | HH:mm", new CultureInfo("tr-TR"));

                var kullanici = AuthService.Instance.AktifKullanici;
                if (kullanici != null)
                    txtKarsilama.Text = $"Merhaba, {kullanici.Ad} 👋";

                using var context = new KutuphaneDbContext();

                var kitapService = new KitapService(context);
                var oduncService = (await ServisKapsami.OlusturAsync(context)).Odunc;

                // İstatistikler
                var kitapIstat = await kitapService.GetIstatistikAsync();
                var oduncIstat = await oduncService.GetIstatistikAsync();

                txtToplamKitap.Text = kitapIstat.ToplamKitap.ToString("N0");
                txtKitapAlt.Text = $"{kitapIstat.MevcutKopya} mevcut • {kitapIstat.OdunctekiKopya} ödünçte";

                txtAktifOdunc.Text = oduncIstat.AktifOduncSayisi.ToString("N0");
                txtOduncAlt.Text = $"Bugün {oduncIstat.BugunOduncSayisi} yeni işlem";

                txtGecikmis.Text = oduncIstat.GecikmisSayisi.ToString("N0");
                txtGecikmisAlt.Text = oduncIstat.GecikmisSayisi > 0
                    ? $"⚠️ {oduncIstat.GecikmisSayisi} acil işlem"
                    : "✅ Her şey yolunda";

                var uyeService = new UyeService(context);
                var uyeIstat = await uyeService.GetIstatistikAsync();
                txtToplamUye.Text = uyeIstat.AktifUye.ToString("N0");
                txtUyeAlt.Text = $"{uyeIstat.OgrenciSayisi} öğrenci • {uyeIstat.AkademisyenSayisi} akademisyen";

                // Finansal özet: ödünç uygunluğunu belirleyen üye borçları üzerinden hesaplanır.
                var (bekleyenCeza, borcluUyeSayisi) = await uyeService.GetBekleyenCezaAsync();
                txtBekilenCeza.Text = $"₺{bekleyenCeza:F2}";
                txtCezaAlt.Text = bekleyenCeza > 0
                    ? $"{borcluUyeSayisi} üyenin bekleyen ödemesi var"
                    : "Tüm cezalar ödenmiş ✅";

                // Salon: veritabanındaki bugünkü aktif rezervasyonlar
                var (salonRezervasyon, rezerveMasa) = await new SalonRezervasyonService(context).GunlukOzetAsync(DateTime.Today);
                var bosMasa = SalonRezervasyonService.ToplamMasaSayisi - rezerveMasa;
                txtSalonBos.Text = bosMasa.ToString();
                txtSalonToplam.Text = $"/{SalonRezervasyonService.ToplamMasaSayisi}";
                pbSalon.Value = 100.0 * rezerveMasa / SalonRezervasyonService.ToplamMasaSayisi;
                txtSalonAlt.Text = $"Bugün {salonRezervasyon} aktif masa rezervasyonu var";

                // Gecikmiş ödünçler
                var gecikmisList = await oduncService.GetGecikmisTeslerAsync();
                if (gecikmisList.Count > 0)
                {
                    dgGecikmisOduncler.ItemsSource = gecikmisList;
                    dgGecikmisOduncler.Visibility = Visibility.Visible;
                    txtGecikmisBos.Visibility = Visibility.Collapsed;
                }
                else
                {
                    dgGecikmisOduncler.Visibility = Visibility.Collapsed;
                    txtGecikmisBos.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Dashboard yukleme", ex);
                MessageBox.Show($"Veriler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnYenile_Click(object sender, RoutedEventArgs e) => await VeriYukle();

        private void BtnHizliKitapEkle_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new KitapEkleDialog();
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                _ = VeriYukle();
        }

        private void BtnHizliOduncVer_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.NavigateToNav("Oduncler");
        }

        private void BtnHizliIadeAl_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.NavigateToNav("Oduncler");
        }

        private void BtnHizliUyeEkle_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new UyeEkleDialog();
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                _ = VeriYukle();
        }
    }
}
