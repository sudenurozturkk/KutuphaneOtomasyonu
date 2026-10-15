using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class RezervasyonlarPage : Page
    {
        private List<Rezervasyon> _tumRezervasyonlar = new();

        public RezervasyonlarPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await VeriYukle();
        }

        private async Task VeriYukle()
        {
            try
            {
                using var context = new KutuphaneDbContext();
                _tumRezervasyonlar = await context.Rezervasyonlar
                    .Include(r => r.Kitap)
                    .Include(r => r.Uye)
                    .OrderByDescending(r => r.RezervasyonTarihi)
                    .ToListAsync();

                Filtrele();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyonlar yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filtrele()
        {
            if (!IsLoaded || dgRezervasyonlar == null || cbFiltreDurum == null)
                return;

            var arama = txtArama?.Text?.Trim().ToLower() ?? "";
            var durumSecim = (cbFiltreDurum.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filtered = _tumRezervasyonlar.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                filtered = filtered.Where(r =>
                    (r.Kitap?.Baslik?.ToLower().Contains(arama) ?? false) ||
                    (r.Uye?.TamAd?.ToLower().Contains(arama) ?? false) ||
                    r.Id.ToString().Contains(arama));
            }

            if (durumSecim != null && durumSecim != "Tüm Durumlar")
            {
                filtered = filtered.Where(r => r.Durumu.ToString().Equals(durumSecim, StringComparison.OrdinalIgnoreCase));
            }

            dgRezervasyonlar.ItemsSource = filtered.ToList();
            GuncelleButonlar();
        }

        private void TxtArama_TextChanged(object sender, TextChangedEventArgs e) => Filtrele();
        private void CbFiltreDurum_SelectionChanged(object sender, SelectionChangedEventArgs e) => Filtrele();
        private async void BtnYenile_Click(object sender, RoutedEventArgs e) => await VeriYukle();

        private void DgRezervasyonlar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GuncelleButonlar();
        }

        private void GuncelleButonlar()
        {
            if (dgRezervasyonlar.SelectedItem is Rezervasyon rez && DonusturulebilirMi(rez))
            {
                btnOdunceDonustur.IsEnabled = true;
                btnIptalEt.IsEnabled = true;
            }
            else
            {
                btnOdunceDonustur.IsEnabled = false;
                btnIptalEt.IsEnabled = false;
            }
        }

        private static bool DonusturulebilirMi(Rezervasyon rezervasyon) =>
            (rezervasyon.Durumu == RezervasyonDurumu.Bekliyor ||
             rezervasyon.Durumu == RezervasyonDurumu.Onaylandi) &&
            rezervasyon.GecerlilikTarihi >= DateTime.Now;

        private async void BtnYeniRezervasyon_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RezervasyonEkleDialog
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true)
                await VeriYukle();
        }

        private async void BtnOdunceDonustur_Click(object sender, RoutedEventArgs e)
        {
            if (dgRezervasyonlar.SelectedItem is not Rezervasyon rez)
            {
                MessageBox.Show("Önce bekleyen bir rezervasyon seçin.", "Seçim Gerekli",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                var dbRez = await context.Rezervasyonlar
                    .Include(r => r.Kitap)
                    .Include(r => r.Uye)
                    .FirstOrDefaultAsync(r => r.Id == rez.Id);

                if (dbRez == null || !DonusturulebilirMi(dbRez))
                {
                    MessageBox.Show("Bu rezervasyon artık geçerli değil veya başka bir işlem tarafından kullanıldı.",
                        "İşlem Yapılamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await VeriYukle();
                    return;
                }

                var bildirimService = new BildirimService(context);
                var ayarlar = await context.SistemAyarlari.FindAsync(1) ?? new SistemAyarlari();
                var oduncService = new OduncService(context, bildirimService, ayarlar);

                var sonuc = await oduncService.OduncVerAsync(dbRez.KitapId, dbRez.UyeId, AuthService.Instance.AktifKullanici?.Id ?? 1);
                if (sonuc.Basarili)
                {
                    // Rezervasyonu tamamlandı yap
                    dbRez.Durumu = RezervasyonDurumu.Tamamlandi;
                    await context.SaveChangesAsync();

                    MessageBox.Show($"Rezervasyon başarıyla ödünce dönüştürüldü!\nKitap: {dbRez.Kitap?.Baslik}\nÜye: {dbRez.Uye?.TamAd}", "İşlem Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    await VeriYukle();
                }
                else
                {
                    MessageBox.Show($"Ödünç verilemedi: {sonuc.Mesaj}", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnIptalEt_Click(object sender, RoutedEventArgs e)
        {
            if (dgRezervasyonlar.SelectedItem is not Rezervasyon rez)
            {
                MessageBox.Show("Önce iptal edilecek bekleyen rezervasyonu seçin.", "Seçim Gerekli",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var onay = MessageBox.Show($"'{rez.Kitap?.Baslik}' kitabı için olan rezervasyon sırasını iptal etmek istiyor musunuz?", "İptal Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes) return;

            try
            {
                using var context = new KutuphaneDbContext();
                var dbRez = await context.Rezervasyonlar.FindAsync(rez.Id);
                if (dbRez != null)
                {
                    dbRez.Durumu = RezervasyonDurumu.Iptal;
                    await context.SaveChangesAsync();
                }

                MessageBox.Show("Rezervasyon iptal edildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                await VeriYukle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İptal edilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
