using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
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
                cbKitaplar.ItemsSource = await context.Kitaplar.Where(k => k.Aktif).OrderBy(k => k.Baslik).ToListAsync();
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
                var servis = (await ServisKapsami.OlusturAsync(context)).Rezervasyon;
                var sonuc = await servis.OlusturAsync(kitapId, uyeId, txtNotlar.Text,
                    AuthService.Instance.AktifKullanici?.Id);

                if (!sonuc.BasariliMi)
                {
                    MessageBox.Show(sonuc.Mesaj, "Rezervasyon Oluşturulamadı",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

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
