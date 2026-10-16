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
                var kitaplar = await context.Kitaplar.AsNoTracking().Where(k => k.Aktif).OrderBy(k => k.Baslik).ToListAsync();
                var uyeler = await context.Uyeler.AsNoTracking().Where(u => u.Durumu == UyeDurumu.Aktif)
                    .OrderBy(u => u.Ad).ThenBy(u => u.Soyad).ToListAsync();

                if (kitaplar.Count == 0 || uyeler.Count == 0)
                {
                    MessageBox.Show("Transfer başlatabilmek için sistemde aktif kitap ve üye bulunmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var kampusler = Enum.GetValues<KampusLokasyonu>();
                var secim = new SecimDialog("Yeni ILL Transferi", "Transfer edilecek kitabı, talep eden üyeyi ve güzergahı seçin.", "Transferi Başlat",
                    new SecimAlani("Kitap *", kitaplar, nameof(Kitap.Baslik)),
                    new SecimAlani("Talep Eden Üye *", uyeler, nameof(Uye.TamAd)),
                    new SecimAlani("Kaynak Kampüs *", kampusler, string.Empty),
                    new SecimAlani("Hedef Kampüs *", kampusler, string.Empty))
                { Owner = Window.GetWindow(this) };
                if (secim.ShowDialog() != true)
                    return;

                var kitap = (Kitap)secim.Secilenler[0];
                var uye = (Uye)secim.Secilenler[1];
                var kaynak = (KampusLokasyonu)secim.Secilenler[2];
                var hedef = (KampusLokasyonu)secim.Secilenler[3];
                if (kaynak == hedef)
                {
                    MessageBox.Show("Kaynak ve hedef kampüs aynı olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var service = new AkademikService(context);
                var yeni = await service.TransferTalepEtAsync(kitap.Id, uye.Id, kaynak, hedef);

                MessageBox.Show($"🚚 Yeni Kampüsler Arası Kurye Transfer Talebi Oluşturuldu!\n\nKitap: {kitap.Baslik}\nTakip Kodu: {yeni.KuryeTakipKodu}\nGüzergah: {kaynak} ➔ {hedef}",
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
