using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class UyeDetayDialog : Window
    {
        private readonly int _uyeId;

        public UyeDetayDialog(int uyeId)
        {
            InitializeComponent();
            _uyeId = uyeId;
            Loaded += UyeDetayDialog_Loaded;
        }

        private async void UyeDetayDialog_Loaded(object sender, RoutedEventArgs e) => await YukleAsync();

        private async System.Threading.Tasks.Task YukleAsync()
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var service = new UyeService(context);
                var u = await service.GetByIdAsync(_uyeId);
                if (u == null) return;

                var kullanici = AuthService.Instance.AktifKullanici;
                txtTamAd.Text = u.TamAd;
                txtUyeTipi.Text = $"Üye Tipi: {u.UyeTipiMetni}";
                txtTC.Text = $"TC / YKN: {Bos(KisiselVeri.Goster(u.TCKimlikNo, kullanici))}";
                txtPasaport.Text = $"Pasaport: {Bos(KisiselVeri.Goster(u.PasaportNo, kullanici))}";
                txtEmail.Text = $"E-Posta: {u.Email}";
                txtTelefon.Text = $"Telefon: {Bos(KisiselVeri.Goster(u.Telefon, kullanici))}";
                txtOgrenciNo.Text = $"Öğrenci/Sicil No: {Bos(u.OgrenciNo)}";
                txtBolum.Text = $"Bölüm: {u.Bolum} / {u.Fakulte}";
                txtDurum.Text = u.AnonimMi
                    ? $"Durum: Anonimleştirildi ({u.AnonimlestirmeTarihi:dd.MM.yyyy})"
                    : $"Durum: {u.Durumu} · Öğrenci portalı: " +
                      (!u.PortalHesabiVar ? "hesap yok" : u.PortalSifreGecici ? "geçici şifre verildi" : "aktif");

                _portalHesabiVar = u.PortalHesabiVar;
                btnPortalSifresi.Content = u.PortalHesabiVar ? "Portal Şifresini Sıfırla" : "Portal Hesabı Aç";
                btnPortalSifresi.Visibility = AuthService.Instance.YetkiVarMi(KullaniciRolu.Kutuphaneci) && !u.AnonimMi
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                txtMaskeBilgi.Visibility = KisiselVeri.TamGorebilirMi(kullanici) ? Visibility.Collapsed : Visibility.Visible;
                btnAnonimlestir.Visibility = AuthService.Instance.YetkiVarMi(KullaniciRolu.Admin) && !u.AnonimMi
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("UyeDetayDialog yukleme", ex);
                MessageBox.Show($"Üye bilgileri yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Bos(string deger) => string.IsNullOrEmpty(deger) ? "-" : deger;

        private bool _portalHesabiVar;

        /// <summary>Öğrenci portalı için tek seferlik geçici şifre üretir; üye ilk girişte kendi şifresini belirler.</summary>
        private async void BtnPortalSifresi_Click(object sender, RoutedEventArgs e)
        {
            if (_portalHesabiVar && MessageBox.Show(
                    "Üyenin mevcut portal şifresi geçersiz olacak ve açık oturumları kapanacak.\n\nDevam edilsin mi?",
                    "Portal Şifresini Sıfırla", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using var context = new KutuphaneDbContext();
                var (sonuc, geciciSifre) = await new UyeHesapService(TimeProvider.System)
                    .GeciciSifreOlusturAsync(context, _uyeId, AuthService.Instance.AktifKullanici?.Id);
                if (!sonuc.BasariliMi)
                {
                    MessageBox.Show(sonuc.Mesaj, "Portal hesabı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var giris = string.IsNullOrEmpty(sonuc.Uye!.OgrenciNo) ? sonuc.Uye.Email : sonuc.Uye.OgrenciNo;
                MessageBox.Show(
                    $"Giriş: {giris}\nGeçici şifre: {geciciSifre}\n\n" +
                    "Şifreyi üyeye iletin; bu pencere kapandıktan sonra tekrar gösterilmez. " +
                    "Üye ilk girişte kendi şifresini belirleyecektir. (Ctrl+C ile kopyalanabilir.)",
                    "Öğrenci Portalı", MessageBoxButton.OK, MessageBoxImage.Information);
                await YukleAsync();
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Portal sifresi olusturma", ex);
                MessageBox.Show($"İşlem başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAnonimlestir_Click(object sender, RoutedEventArgs e)
        {
            var onay = MessageBox.Show(
                "Bu üyenin ad, kimlik, iletişim bilgileri ve bildirimleri KALICI olarak silinecek; hesap pasifleştirilecek.\n" +
                "Ödünç geçmişi istatistik için anonim olarak kalır. Bu işlem geri alınamaz.\n\nDevam edilsin mi?",
                "KVKK Silme Talebi", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (onay != MessageBoxResult.Yes) return;

            try
            {
                using var context = new KutuphaneDbContext();
                var sonuc = await new UyeService(context).AnonimlestirAsync(_uyeId, AuthService.Instance.AktifKullanici);
                if (!sonuc.BasariliMi)
                {
                    MessageBox.Show(sonuc.Mesaj, "Anonimleştirilemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("Üyenin kişisel verileri silindi.", "Tamamlandı", MessageBoxButton.OK, MessageBoxImage.Information);
                await YukleAsync();
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Uye anonimlestirme", ex);
                MessageBox.Show($"İşlem başarısız: {ex.Message}\n\nHiçbir veri değiştirilmedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
