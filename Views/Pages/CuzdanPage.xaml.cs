using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public class FinansKaydi
    {
        public string Tarih { get; set; } = "";
        public string Islem { get; set; } = "";
        public string Tutar { get; set; } = "";
        public string Durum { get; set; } = "";
    }

    public partial class CuzdanPage : Page
    {
        private decimal _bakiye = 150.00m;
        private decimal _borc = 15.00m;
        private List<FinansKaydi> _islemler = new();

        public CuzdanPage()
        {
            InitializeComponent();
            _islemler.Add(new FinansKaydi { Tarih = DateTime.Now.AddDays(-5).ToString("dd.MM.yyyy HH:mm"), Islem = "Kredi Kartı ile Bakiye Yükleme", Tutar = "+ ₺100,00", Durum = "Başarılı" });
            _islemler.Add(new FinansKaydi { Tarih = DateTime.Now.AddDays(-10).ToString("dd.MM.yyyy HH:mm"), Islem = "Gecikme Cezası Tahsilatı ('Clean Code')", Tutar = "- ₺8,00", Durum = "Ödendi" });
            _islemler.Add(new FinansKaydi { Tarih = DateTime.Now.AddDays(-20).ToString("dd.MM.yyyy HH:mm"), Islem = "Fotokopi / Çıktı Hizmeti", Tutar = "- ₺12,00", Durum = "Ödendi" });

            Guncelle();
        }

        private void Guncelle()
        {
            txtKartBakiyeGoster.Text = $"Bakiye: ₺{_bakiye:F2}";
            txtToplamBorc.Text = $"₺{_borc:F2}";
            btnCezaOde.Content = $"Cüzdandan Öde (₺{_borc:F2})";
            btnCezaOde.IsEnabled = (_borc > 0);

            dgFinans.ItemsSource = null;
            dgFinans.ItemsSource = _islemler;
        }

        private void BtnYukle20_Click(object sender, RoutedEventArgs e) => BakiyeEkle(20);
        private void BtnYukle50_Click(object sender, RoutedEventArgs e) => BakiyeEkle(50);
        private void BtnYukle100_Click(object sender, RoutedEventArgs e) => BakiyeEkle(100);

        private void BakiyeEkle(decimal miktar)
        {
            _bakiye += miktar;
            _islemler.Insert(0, new FinansKaydi
            {
                Tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                Islem = "Online Bakiye Yükleme",
                Tutar = $"+ ₺{miktar:F2}",
                Durum = "Başarılı"
            });
            MessageBox.Show($"Cüzdanınıza ₺{miktar:F2} başarıyla yüklendi! Yeni bakiye: ₺{_bakiye:F2}", "Bakiye Yüklendi", MessageBoxButton.OK, MessageBoxImage.Information);
            Guncelle();
        }

        private void BtnCezaOde_Click(object sender, RoutedEventArgs e)
        {
            if (_bakiye < _borc)
            {
                MessageBox.Show("Cüzdan bakiyeniz yetersiz! Lütfen önce bakiye yükleyin.", "Yetersiz Bakiye", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _bakiye -= _borc;
            _islemler.Insert(0, new FinansKaydi
            {
                Tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                Islem = "Gecikme Cezası Ödemesi",
                Tutar = $"- ₺{_borc:F2}",
                Durum = "Ödendi"
            });

            _borc = 0.00m;
            MessageBox.Show("Gecikme cezası cüzdan bakiyenizden başarıyla tahsil edildi. Borcunuz kalmamıştır!", "Ödeme Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            Guncelle();
        }
    }
}
