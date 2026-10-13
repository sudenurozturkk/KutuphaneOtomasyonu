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
    public partial class KampusTransferPage : Page
    {
        public KampusTransferPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await VeriYukle();
        }

        private async Task VeriYukle()
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var service = new AkademikService(context);
                var list = await service.GetTransferlerAsync();
                dgTransferler.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Transfer kayıtları yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnYenile_Click(object sender, RoutedEventArgs e) => await VeriYukle();

        private async void BtnYeniTransfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var kitap = await context.Kitaplar.FirstOrDefaultAsync();
                var uye = await context.Uyeler.FirstOrDefaultAsync();

                if (kitap == null || uye == null)
                {
                    MessageBox.Show("Transfer başlatabilmek için sistemde kitap ve üye bulunmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var service = new AkademikService(context);
                var yeni = await service.TransferTalepEtAsync(kitap.Id, uye.Id, KampusLokasyonu.TipSaglikKutuphanesi, KampusLokasyonu.MerkezKutuphane);

                MessageBox.Show($"🚚 Yeni Kampüsler Arası Kurye Transfer Talebi Oluşturuldu!\n\nKitap: {kitap.Baslik}\nTakip Kodu: {yeni.KuryeTakipKodu}\nGüzergah: Tıp Kütüphanesi ➔ Merkez Kütüphane",
                    "ILL Transfer Başlatıldı", MessageBoxButton.OK, MessageBoxImage.Information);

                await VeriYukle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Transfer talebi oluşturulamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
