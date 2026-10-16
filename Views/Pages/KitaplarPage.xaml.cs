using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class KitaplarPage : Page
    {
        private List<Kitap> _tumKitaplar = new();
        private KitapService? _kitapService;
        private bool _isInitialized;

        public KitaplarPage()
        {
            InitializeComponent();
            _isInitialized = true;
            Loaded += KitaplarPage_Loaded;
        }

        private async void KitaplarPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kategori listesini doldur
                using var context = new KutuphaneDbContext();
                _kitapService = new KitapService(context);
                var kategoriler = await _kitapService.GetKategorilerAsync();

                cbKategori.Items.Clear();
                cbKategori.Items.Add(new ComboBoxItem { Content = "Tüm Kategoriler" });
                foreach (var kat in kategoriler)
                    cbKategori.Items.Add(new ComboBoxItem { Content = kat });
                cbKategori.SelectedIndex = 0;
                cbDil.SelectedIndex = 0;
                cbDurum.SelectedIndex = 0;

                await VeriYukle();
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Kitaplar kategori yukleme", ex);
                MessageBox.Show($"Veriler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task VeriYukle()
        {
            try
            {
                pbKitaplar.Visibility = Visibility.Visible;
                using var context = new KutuphaneDbContext();
                var service = new KitapService(context);
                _tumKitaplar = await service.GetAllAsync();
                Filtrele();
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Kitaplar yukleme", ex);
                MessageBox.Show($"Veriler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                pbKitaplar.Visibility = Visibility.Collapsed;
            }
        }

        private void Filtrele()
        {
            if (!_isInitialized || !IsLoaded || dgKitaplar == null || txtKitapSayisi == null ||
                txtArama == null || cbKategori == null || cbDil == null || cbDurum == null)
                return;

            var arama = txtArama?.Text?.Trim().ToLower() ?? "";
            var kategori = (cbKategori.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var dil = (cbDil.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var durum = (cbDurum.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filtered = _tumKitaplar.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(arama))
                filtered = filtered.Where(k =>
                    k.Baslik.ToLower().Contains(arama) ||
                    k.Yazar.ToLower().Contains(arama) ||
                    k.ISBN.Contains(arama));

            if (kategori != null && kategori != "Tüm Kategoriler")
                filtered = filtered.Where(k => k.Kategori == kategori);

            if (dil != null && dil != "Tümü")
                filtered = filtered.Where(k => k.Dil == dil);

            if (durum == "Mevcut") filtered = filtered.Where(k => k.MevcutAdet > 0);
            else if (durum == "Tükendi") filtered = filtered.Where(k => k.MevcutAdet == 0);

            var result = filtered.ToList();
            dgKitaplar.ItemsSource = result;
            txtKitapSayisi.Text = $"{result.Count} kitap";
        }

        private void TxtArama_TextChanged(object sender, TextChangedEventArgs e) => Filtrele();
        private void CbFiltre_Changed(object sender, SelectionChangedEventArgs e) => Filtrele();

        private void BtnTemizle_Click(object sender, RoutedEventArgs e)
        {
            txtArama.Text = "";
            cbKategori.SelectedIndex = 0;
            cbDil.SelectedIndex = 0;
            cbDurum.SelectedIndex = 0;
        }

        private void BtnKitapEkle_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new KitapEkleDialog();
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                _ = VeriYukle();
        }

        private void DgKitaplar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgKitaplar.SelectedItem is Kitap k)
                txtSecilenKitap.Text = $"✓ Seçili: {k.Baslik} | {k.Yazar}";
        }

        private void DgKitaplar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgKitaplar.SelectedItem is Kitap k)
                AcDetay(k.Id);
        }

        private void BtnKitapDetay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
                AcDetay(id);
        }

        private void AcDetay(int id)
        {
            var dlg = new KitapDetayDialog(id);
            dlg.Owner = Window.GetWindow(this);
            dlg.ShowDialog();
        }

        private void BtnKitapDuzenle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var kitap = _tumKitaplar.FirstOrDefault(k => k.Id == id);
                if (kitap == null) return;
                var dlg = new KitapEkleDialog(kitap);
                dlg.Owner = Window.GetWindow(this);
                if (dlg.ShowDialog() == true)
                    _ = VeriYukle();
            }
        }

        private async void BtnKitapSil_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var kitap = _tumKitaplar.FirstOrDefault(k => k.Id == id);
                if (kitap == null) return;

                var sonuc = MessageBox.Show(
                    $"'{kitap.Baslik}' kitabını silmek istediğinizden emin misiniz?",
                    "Kitap Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (sonuc == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new KutuphaneDbContext();
                        var service = new KitapService(context);
                        var arsiv = await service.DeleteAsync(id, AuthService.Instance.AktifKullanici?.Id);
                        if (!arsiv.BasariliMi)
                            MessageBox.Show(arsiv.Mesaj, "Kitap Silinemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        await VeriYukle();
                    }
                    catch (Exception ex)
                    {
                        UygulamaLog.Hata("Kitap arsivleme", ex);
                        MessageBox.Show($"Kitap silinemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(CsvYazici.Satir("ID", "Başlık", "Yazar", "ISBN", "Kategori", "Yıl", "Mevcut/Toplam", "Puan"));
                foreach (var k in _tumKitaplar)
                    sb.AppendLine(CsvYazici.Satir(k.Id, k.Baslik, k.Yazar, k.ISBN, k.Kategori, k.YayinYili,
                        $"{k.MevcutAdet}/{k.ToplamAdet}", k.Puan.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)));

                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "kitaplar.csv");
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                MessageBox.Show($"Dışa aktarıldı: {path}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Kitap CSV disa aktarma", ex);
                MessageBox.Show($"Dışa aktarılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
