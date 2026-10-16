using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class KitapEkleDialog : Window
    {
        private Kitap? _mevcutKitap;

        public KitapEkleDialog() : this(null) { }

        public KitapEkleDialog(Kitap? kitap)
        {
            InitializeComponent();
            _mevcutKitap = kitap;

            if (_mevcutKitap != null)
            {
                txtDialogBaslik.Text = "✏️ Kitap Düzenle";
                txtBaslik.Text = _mevcutKitap.Baslik;
                txtYazar.Text = _mevcutKitap.Yazar;
                txtISBN.Text = _mevcutKitap.ISBN;
                txtYayinYili.Text = _mevcutKitap.YayinYili.ToString();
                txtKategori.Text = _mevcutKitap.Kategori;
                txtYayinevi.Text = _mevcutKitap.Yayinevi;
                txtSayfaSayisi.Text = _mevcutKitap.SayfaSayisi.ToString();
                txtToplamAdet.Text = _mevcutKitap.ToplamAdet.ToString();
                txtRafKonumu.Text = _mevcutKitap.RafKonumu;
                txtAciklama.Text = _mevcutKitap.Aciklama;
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
            if (string.IsNullOrWhiteSpace(txtBaslik.Text) ||
                string.IsNullOrWhiteSpace(txtYazar.Text) ||
                string.IsNullOrWhiteSpace(txtISBN.Text))
            {
                MessageBox.Show("Başlık, Yazar ve ISBN alanları zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!SayiOku(txtYayinYili.Text, "Yayın yılı", _mevcutKitap?.YayinYili ?? 0, out var yayinYili) ||
                !SayiOku(txtSayfaSayisi.Text, "Sayfa sayısı", _mevcutKitap?.SayfaSayisi ?? 0, out var sayfaSayisi) ||
                !SayiOku(txtToplamAdet.Text, "Toplam adet", _mevcutKitap?.ToplamAdet ?? 1, out var toplamAdet))
                return;

            try
            {
                using var context = new KutuphaneDbContext();
                var service = new KitapService(context);
                var kullaniciId = AuthService.Instance.AktifKullanici?.Id;

                var bilgi = new Kitap
                {
                    Id = _mevcutKitap?.Id ?? 0,
                    Baslik = txtBaslik.Text.Trim(),
                    Yazar = txtYazar.Text.Trim(),
                    ISBN = txtISBN.Text.Trim(),
                    Kategori = txtKategori.Text.Trim(),
                    Yayinevi = txtYayinevi.Text.Trim(),
                    RafKonumu = txtRafKonumu.Text.Trim(),
                    Aciklama = txtAciklama.Text.Trim(),
                    YayinYili = yayinYili,
                    SayfaSayisi = sayfaSayisi,
                    ToplamAdet = toplamAdet,
                    Dil = "Türkçe"
                };

                var sonuc = _mevcutKitap == null
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
                UygulamaLog.Hata("KitapEkleDialog kayit", ex);
                MessageBox.Show($"Kayıt işlemi başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Boş alan için varsayılanı kullanır; sayı olmayan girişte uyarır.</summary>
        private static bool SayiOku(string metin, string alanAdi, int varsayilan, out int deger)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                deger = varsayilan;
                return true;
            }

            if (int.TryParse(metin.Trim(), out deger))
                return true;

            MessageBox.Show($"{alanAdi} sayısal olmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
}
