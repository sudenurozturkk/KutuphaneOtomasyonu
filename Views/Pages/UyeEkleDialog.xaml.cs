using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class UyeEkleDialog : Window
    {
        private Uye? _mevcutUye;

        public UyeEkleDialog() : this(null) { }

        public UyeEkleDialog(Uye? uye)
        {
            InitializeComponent();
            _mevcutUye = uye;
            cbUyeTipi.ItemsSource = Enum.GetValues(typeof(UyeTipi));
            cbUyeTipi.SelectedItem = UyeTipi.Ogrenci;

            if (_mevcutUye != null)
            {
                txtDialogBaslik.Text = "✏️ Üye Düzenle";
                txtAd.Text = _mevcutUye.Ad;
                txtSoyad.Text = _mevcutUye.Soyad;
                txtTC.Text = _mevcutUye.TCKimlikNo;
                txtPasaport.Text = _mevcutUye.PasaportNo;
                txtEmail.Text = _mevcutUye.Email;
                txtTelefon.Text = _mevcutUye.Telefon;
                cbUyeTipi.SelectedItem = _mevcutUye.UyeTipi;
                txtOgrenciNo.Text = _mevcutUye.OgrenciNo;
                txtBolum.Text = _mevcutUye.Bolum;
                txtFakulte.Text = _mevcutUye.Fakulte;
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
            if (string.IsNullOrWhiteSpace(txtAd.Text) ||
                string.IsNullOrWhiteSpace(txtSoyad.Text) ||
                (string.IsNullOrWhiteSpace(txtTC.Text) && string.IsNullOrWhiteSpace(txtPasaport.Text)) ||
                string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Ad, Soyad, E-Posta ve kimlik/pasaport numarasından biri zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                var service = new UyeService(context);

                var tip = cbUyeTipi.SelectedItem is UyeTipi ut ? ut : UyeTipi.Ogrenci;
                var bilgi = new Uye
                {
                    Id = _mevcutUye?.Id ?? 0,
                    Ad = txtAd.Text,
                    Soyad = txtSoyad.Text,
                    TCKimlikNo = txtTC.Text,
                    PasaportNo = txtPasaport.Text,
                    Email = txtEmail.Text,
                    Telefon = txtTelefon.Text,
                    UyeTipi = tip,
                    OgrenciNo = txtOgrenciNo.Text,
                    Bolum = txtBolum.Text,
                    Fakulte = txtFakulte.Text,
                    Durumu = UyeDurumu.Aktif
                };

                var kullaniciId = AuthService.Instance.AktifKullanici?.Id;
                var sonuc = _mevcutUye == null
                    ? await service.EkleAsync(bilgi, kullaniciId)
                    : await service.GuncelleAsync(bilgi, kullaniciId);

                if (!sonuc.BasariliMi)
                {
                    MessageBox.Show(sonuc.Mesaj, "Doğrulama", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
