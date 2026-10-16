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
                var servis = await ServisOlusturAsync(context);
                await servis.SuresiDolanlariIsleAsync();
                _tumRezervasyonlar = await servis.GetTumuAsync();

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
            RezervasyonService.OdunceDonusturulebilirMi(rezervasyon, DateTime.Now);

        private static async Task<RezervasyonService> ServisOlusturAsync(KutuphaneDbContext context) =>
            (await ServisKapsami.OlusturAsync(context)).Rezervasyon;

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
                var servis = await ServisOlusturAsync(context);
                var sonuc = await servis.OdunceDonusturAsync(rez.Id, AuthService.Instance.AktifKullanici?.Id ?? 0);
                if (sonuc.Basarili)
                {
                    MessageBox.Show($"Rezervasyon başarıyla ödünce dönüştürüldü!\nKitap: {rez.Kitap?.Baslik}\nÜye: {rez.Uye?.TamAd}", "İşlem Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Ödünç verilemedi: {sonuc.Mesaj}", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                await VeriYukle();
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
                var sonuc = await (await ServisOlusturAsync(context))
                    .IptalEtAsync(rez.Id, AuthService.Instance.AktifKullanici?.Id);

                MessageBox.Show(sonuc.BasariliMi ? "Rezervasyon iptal edildi." : sonuc.Mesaj, "Bilgi",
                    MessageBoxButton.OK, sonuc.BasariliMi ? MessageBoxImage.Information : MessageBoxImage.Warning);
                await VeriYukle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İptal edilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
