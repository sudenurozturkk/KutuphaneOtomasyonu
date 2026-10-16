using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class OdunclerPage : Page
    {
        private List<Odunc> _tumOduncler = new();

        public OdunclerPage()
        {
            InitializeComponent();
            Loaded += OdunclerPage_Loaded;
            tabOdunc.SelectionChanged += TabOdunc_SelectionChanged;
        }

        private async void OdunclerPage_Loaded(object sender, RoutedEventArgs e)
        {
            await VeriYukle();
        }

        private async System.Threading.Tasks.Task VeriYukle()
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var service = (await ServisKapsami.OlusturAsync(context)).Odunc;

                switch (tabOdunc.SelectedIndex)
                {
                    case 0: // Aktif
                        _tumOduncler = await service.GetAktifOdunclerAsync();
                        break;
                    case 1: // Gecikmiş
                        _tumOduncler = await service.GetGecikmisTeslerAsync();
                        break;
                    case 2: // Tüm geçmiş
                        _tumOduncler = await context.Oduncler
                            .Include(o => o.Kitap).Include(o => o.Uye)
                            .OrderByDescending(o => o.OduncTarihi)
                            .Take(500).ToListAsync();
                        break;
                }

                Filtrele();

                var istat = await service.GetIstatistikAsync();
                txtDurumBilgi.Text = $"Aktif: {istat.AktifOduncSayisi} | Gecikmiş: {istat.GecikmisSayisi} | Bugün İade: {istat.BugunIadeSayisi} | Toplam Ceza: ₺{istat.ToplamCeza:N2}";
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Oduncler yukleme", ex);
                MessageBox.Show($"Veriler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filtrele()
        {
            var arama = txtArama.Text?.Trim().ToLower() ?? "";
            var filtered = _tumOduncler.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(arama))
                filtered = filtered.Where(o =>
                    (o.Kitap?.Baslik.ToLower().Contains(arama) ?? false) ||
                    (o.Uye?.TamAd.ToLower().Contains(arama) ?? false) ||
                    o.Id.ToString().Contains(arama));

            dgOduncler.ItemsSource = filtered.ToList();
        }

        private void TxtArama_TextChanged(object sender, TextChangedEventArgs e) => Filtrele();

        private async void TabOdunc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) await VeriYukle();
        }

        private void BtnOduncVer_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OduncVerDialog();
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                _ = VeriYukle();
        }

        private void BtnIadeAl_Click(object sender, RoutedEventArgs e)
        {
            if (dgOduncler.SelectedItem is Odunc odunc && odunc.Durumu == OduncDurumu.Aktif)
            {
                IadeIslemi(odunc.Id);
            }
            else
            {
                MessageBox.Show("Lütfen aktif bir ödünç kaydı seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnIade_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
                IadeIslemi(id);
        }

        private void IadeIslemi(int oduncId)
        {
            var dlg = new IadeDialog(oduncId);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                _ = VeriYukle();
        }

        private async void BtnUzat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is int id)
                {
                    var sonuc = MessageBox.Show("Ödünç süresini uzatmak istiyor musunuz?",
                        "Süre Uzat", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (sonuc != MessageBoxResult.Yes) return;

                    using var context = new KutuphaneDbContext();
                    var service = (await ServisKapsami.OlusturAsync(context)).Odunc;

                    var uzatSonuc = await service.UzatAsync(id, AuthService.Instance.AktifKullanici?.Id);
                    MessageBox.Show(uzatSonuc.Mesaj, uzatSonuc.Basarili ? "Başarılı" : "Hata",
                        MessageBoxButton.OK, uzatSonuc.Basarili ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (uzatSonuc.Basarili) await VeriYukle();
                }
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Odunc uzatma", ex);
                MessageBox.Show($"Süre uzatılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
