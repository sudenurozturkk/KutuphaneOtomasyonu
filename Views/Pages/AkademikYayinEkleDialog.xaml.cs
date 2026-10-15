using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class AkademikYayinEkleDialog : Window
    {
        public AkademikYayinEkleDialog()
        {
            InitializeComponent();
            cbTur.ItemsSource = Enum.GetValues<YayinTuru>();
            cbKampus.ItemsSource = Enum.GetValues<KampusLokasyonu>();
            cbTur.SelectedItem = YayinTuru.YuksekLisansTezi;
            cbKampus.SelectedItem = KampusLokasyonu.MerkezKutuphane;
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
                cbTur.SelectedItem is not YayinTuru tur)
            {
                MessageBox.Show("Başlık, yazar ve yayın türü zorunludur.", "Eksik Bilgi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                context.AkademikYayinlar.Add(new AkademikYayin
                {
                    Baslik = txtBaslik.Text.Trim(),
                    Yazar = txtYazar.Text.Trim(),
                    Danisman = string.IsNullOrWhiteSpace(txtDanisman.Text) ? null : txtDanisman.Text.Trim(),
                    Tur = tur,
                    DOI = string.IsNullOrWhiteSpace(txtDoi.Text) ? null : txtDoi.Text.Trim(),
                    Ozet = txtOzet.Text.Trim(),
                    MevcutKampus = cbKampus.SelectedItem is KampusLokasyonu kampus
                        ? kampus : KampusLokasyonu.MerkezKutuphane,
                    Yil = DateTime.Now.Year
                });
                await context.SaveChangesAsync();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yayın kaydedilemedi: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
