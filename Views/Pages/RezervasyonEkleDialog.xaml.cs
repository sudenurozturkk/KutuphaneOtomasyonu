using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class RezervasyonEkleDialog : Window
    {
        public RezervasyonEkleDialog()
        {
            InitializeComponent();
            Loaded += Dialog_Loaded;
        }

        private async void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                cbKitaplar.ItemsSource = await context.Kitaplar.OrderBy(k => k.Baslik).ToListAsync();
                cbUyeler.ItemsSource = await context.Uyeler
                    .Where(u => u.Durumu == UyeDurumu.Aktif)
                    .OrderBy(u => u.Ad).ToListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyon verileri yüklenemedi: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (cbKitaplar.SelectedValue is not int kitapId || cbUyeler.SelectedValue is not int uyeId)
            {
                MessageBox.Show("Lütfen kitap ve üye seçin.", "Eksik Bilgi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                var ayniRezervasyon = await context.Rezervasyonlar.AnyAsync(r =>
                    r.KitapId == kitapId &&
                    r.UyeId == uyeId &&
                    (r.Durumu == RezervasyonDurumu.Bekliyor || r.Durumu == RezervasyonDurumu.Onaylandi) &&
                    r.GecerlilikTarihi >= DateTime.Now);

                if (ayniRezervasyon)
                {
                    MessageBox.Show("Bu üyenin aynı kitap için zaten aktif bir rezervasyonu var.",
                        "Rezervasyon Zaten Var", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var sira = await context.Rezervasyonlar.CountAsync(r =>
                    r.KitapId == kitapId && r.Durumu == RezervasyonDurumu.Bekliyor) + 1;
                context.Rezervasyonlar.Add(new Rezervasyon
                {
                    KitapId = kitapId,
                    UyeId = uyeId,
                    RezervasyonTarihi = DateTime.Now,
                    GecerlilikTarihi = DateTime.Now.AddDays(7),
                    Durumu = RezervasyonDurumu.Bekliyor,
                    SiraNumarasi = sira,
                    Notlar = txtNotlar.Text.Trim()
                });
                await context.SaveChangesAsync();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyon oluşturulamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
