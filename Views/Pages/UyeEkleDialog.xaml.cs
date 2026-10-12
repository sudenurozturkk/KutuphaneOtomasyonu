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
                string.IsNullOrWhiteSpace(txtTC.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Ad, Soyad, TC Kimlik No ve E-Posta alanları zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                var service = new UyeService(context);

                var tip = cbUyeTipi.SelectedItem is UyeTipi ut ? ut : UyeTipi.Ogrenci;

                if (_mevcutUye == null)
                {
                    var yeni = new Uye
                    {
                        Ad = txtAd.Text.Trim(),
                        Soyad = txtSoyad.Text.Trim(),
                        TCKimlikNo = txtTC.Text.Trim(),
                        Email = txtEmail.Text.Trim(),
                        Telefon = txtTelefon.Text.Trim(),
                        UyeTipi = tip,
                        OgrenciNo = txtOgrenciNo.Text.Trim(),
                        Bolum = txtBolum.Text.Trim(),
                        Fakulte = txtFakulte.Text.Trim(),
                        Durumu = UyeDurumu.Aktif
                    };

                    await service.AddAsync(yeni);
                }
                else
                {
                    var guncel = await service.GetByIdAsync(_mevcutUye.Id);
                    if (guncel != null)
                    {
                        guncel.Ad = txtAd.Text.Trim();
                        guncel.Soyad = txtSoyad.Text.Trim();
                        guncel.TCKimlikNo = txtTC.Text.Trim();
                        guncel.Email = txtEmail.Text.Trim();
                        guncel.Telefon = txtTelefon.Text.Trim();
                        guncel.UyeTipi = tip;
                        guncel.OgrenciNo = txtOgrenciNo.Text.Trim();
                        guncel.Bolum = txtBolum.Text.Trim();
                        guncel.Fakulte = txtFakulte.Text.Trim();

                        await service.UpdateAsync(guncel);
                    }
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
