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

            try
            {
                using var context = new KutuphaneDbContext();
                var service = new KitapService(context);

                if (_mevcutKitap == null)
                {
                    var yeni = new Kitap
                    {
                        Baslik = txtBaslik.Text.Trim(),
                        Yazar = txtYazar.Text.Trim(),
                        ISBN = txtISBN.Text.Trim(),
                        Kategori = txtKategori.Text.Trim(),
                        Yayinevi = txtYayinevi.Text.Trim(),
                        RafKonumu = txtRafKonumu.Text.Trim(),
                        Aciklama = txtAciklama.Text.Trim(),
                        Dil = "Türkçe"
                    };

                    if (int.TryParse(txtYayinYili.Text, out int yy)) yeni.YayinYili = yy;
                    if (int.TryParse(txtSayfaSayisi.Text, out int ss)) yeni.SayfaSayisi = ss;
                    if (int.TryParse(txtToplamAdet.Text, out int ta))
                    {
                        yeni.ToplamAdet = ta;
                        yeni.MevcutAdet = ta;
                    }

                    await service.AddAsync(yeni);
                }
                else
                {
                    var guncel = await service.GetByIdAsync(_mevcutKitap.Id);
                    if (guncel != null)
                    {
                        guncel.Baslik = txtBaslik.Text.Trim();
                        guncel.Yazar = txtYazar.Text.Trim();
                        guncel.ISBN = txtISBN.Text.Trim();
                        guncel.Kategori = txtKategori.Text.Trim();
                        guncel.Yayinevi = txtYayinevi.Text.Trim();
                        guncel.RafKonumu = txtRafKonumu.Text.Trim();
                        guncel.Aciklama = txtAciklama.Text.Trim();

                        if (int.TryParse(txtYayinYili.Text, out int yy)) guncel.YayinYili = yy;
                        if (int.TryParse(txtSayfaSayisi.Text, out int ss)) guncel.SayfaSayisi = ss;
                        if (int.TryParse(txtToplamAdet.Text, out int ta))
                        {
                            var fark = ta - guncel.ToplamAdet;
                            guncel.ToplamAdet = ta;
                            guncel.MevcutAdet = Math.Max(0, guncel.MevcutAdet + fark);
                        }

                        await service.UpdateAsync(guncel);
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt işlemi başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
