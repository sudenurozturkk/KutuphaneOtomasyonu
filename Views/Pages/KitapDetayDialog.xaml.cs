using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class KitapDetayDialog : Window
    {
        private int _kitapId;

        public KitapDetayDialog(int kitapId)
        {
            InitializeComponent();
            _kitapId = kitapId;
            Loaded += KitapDetayDialog_Loaded;
        }

        private async void KitapDetayDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var service = new KitapService(context);
                var k = await service.GetByIdAsync(_kitapId);
                if (k != null)
                {
                    txtBaslik.Text = k.Baslik;
                    txtYazar.Text = k.Yazar;
                    txtISBN.Text = $"ISBN: {k.ISBN}";
                    txtKategori.Text = $"Kategori: {k.Kategori} - {k.AltKategori}";
                    txtYayinevi.Text = $"Yayınevi: {k.Yayinevi} ({k.YayinYili})";
                    txtRaf.Text = $"Raf Konumu: {k.RafKonumu}";
                    txtStok.Text = $"Mevcut Stok: {k.MevcutAdet} / Toplam: {k.ToplamAdet}";
                    if (!string.IsNullOrWhiteSpace(k.Aciklama))
                        txtAciklama.Text = k.Aciklama;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Detaylar yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
