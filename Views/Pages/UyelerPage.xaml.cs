using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class UyelerPage : Page
    {
        private List<Uye> _tumUyeler = new();

        public UyelerPage()
        {
            InitializeComponent();
            Loaded += UyelerPage_Loaded;
            cbTip.SelectedIndex = 0;
            cbDurum.SelectedIndex = 0;
        }

        private async void UyelerPage_Loaded(object sender, RoutedEventArgs e)
        {
            await VeriYukle();
        }

        private async System.Threading.Tasks.Task VeriYukle()
        {
            using var context = new KutuphaneDbContext();
            var service = new UyeService(context);
            _tumUyeler = await service.GetAllAsync();
            Filtrele();
            txtUyeSayisi.Text = $"{_tumUyeler.Count} üye";
        }

        private void Filtrele()
        {
            if (!IsLoaded || dgUyeler == null || txtUyeSayisi == null || cbTip == null || cbDurum == null)
                return;

            var arama = txtArama?.Text?.Trim().ToLower() ?? "";
            var tip = (cbTip.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var durum = (cbDurum.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filtered = _tumUyeler.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(arama))
                filtered = filtered.Where(u =>
                    u.TamAd.ToLower().Contains(arama) ||
                    u.Email.ToLower().Contains(arama) ||
                    u.OgrenciNo.Contains(arama));

            if (tip is not null and not "Tüm Tipler")
            {
                if (tip == "Öğrenci")    filtered = filtered.Where(u => u.UyeTipi == UyeTipi.Ogrenci);
                else if (tip == "Akademisyen") filtered = filtered.Where(u => u.UyeTipi == UyeTipi.Akademisyen);
                else if (tip == "Personel") filtered = filtered.Where(u => u.UyeTipi == UyeTipi.Personel);
            }

            if (durum is not null and not "Tüm Durumlar")
            {
                if (durum == "Aktif")   filtered = filtered.Where(u => u.Durumu == UyeDurumu.Aktif);
                else if (durum == "Pasif")  filtered = filtered.Where(u => u.Durumu == UyeDurumu.Pasif);
                else if (durum == "Askıda") filtered = filtered.Where(u => u.Durumu == UyeDurumu.Askida);
            }

            var result = filtered.ToList();
            dgUyeler.ItemsSource = result;
            txtUyeSayisi.Text = $"{result.Count} üye";
        }

        private void TxtArama_TextChanged(object sender, TextChangedEventArgs e) => Filtrele();
        private void CbFiltre_Changed(object sender, SelectionChangedEventArgs e) => Filtrele();

        private void BtnTemizle_Click(object sender, RoutedEventArgs e)
        {
            txtArama.Clear();
            cbTip.SelectedIndex = 0;
            cbDurum.SelectedIndex = 0;
        }

        private void BtnUyeEkle_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new UyeEkleDialog();
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true) _ = VeriYukle();
        }

        private void DgUyeler_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgUyeler.SelectedItem is Uye u) AcDetay(u.Id);
        }

        private void BtnUyeDetay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id) AcDetay(id);
        }

        private void AcDetay(int id)
        {
            var dlg = new UyeDetayDialog(id);
            dlg.Owner = Window.GetWindow(this);
            dlg.ShowDialog();
        }

        private void BtnUyeDuzenle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var uye = _tumUyeler.FirstOrDefault(u => u.Id == id);
                if (uye == null) return;
                var dlg = new UyeEkleDialog(uye);
                dlg.Owner = Window.GetWindow(this);
                if (dlg.ShowDialog() == true) _ = VeriYukle();
            }
        }
    }
}
