using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
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

    /// <summary>
    /// Üye cüzdanı: bakiye ve hareketler <see cref="CuzdanService"/> üzerinden kalıcı olarak tutulur.
    /// </summary>
    public partial class CuzdanPage : Page
    {
        public CuzdanPage()
        {
            InitializeComponent();
            Loaded += CuzdanPage_Loaded;
            ButonlariAyarla(false, 0);
        }

        private Uye? SeciliUye => cbUye.SelectedItem as Uye;

        private async void CuzdanPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new KutuphaneDbContext();
                cbUye.ItemsSource = await context.Uyeler.AsNoTracking()
                    .Where(u => u.AnonimlestirmeTarihi == null)
                    .OrderBy(u => u.Ad).ThenBy(u => u.Soyad)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Cuzdan uye listesi", ex);
                MessageBox.Show($"Üyeler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CbUye_SelectionChanged(object sender, SelectionChangedEventArgs e) => await GuncelleAsync();

        private async Task GuncelleAsync()
        {
            if (SeciliUye is not Uye secili)
            {
                ButonlariAyarla(false, 0);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                var uye = await context.Uyeler.AsNoTracking().SingleAsync(u => u.Id == secili.Id);
                var servis = new CuzdanService(context);
                var bakiye = await servis.BakiyeAsync(uye.Id);
                var hareketler = await servis.HareketlerAsync(uye.Id);
                var cezalar = await context.CezaKayitlari.AsNoTracking()
                    .Where(c => c.UyeId == uye.Id && !c.Odendi)
                    .OrderBy(c => c.OlusmaTarihi)
                    .ToListAsync();

                txtKartUyeAdi.Text = uye.TamAd;
                txtKartUyeTip.Text = $"{uye.Bolum} • {uye.UyeTipiMetni}".Trim(' ', '•');
                txtKartOgrenciNo.Text = string.IsNullOrEmpty(uye.OgrenciNo) ? $"Üye No: {uye.Id}" : $"Öğrenci No: {uye.OgrenciNo}";
                txtKartBakiyeGoster.Text = $"Bakiye: ₺{bakiye:N2}";
                txtToplamBorc.Text = $"₺{uye.KalanCeza:N2}";
                txtBorcDetay.Text = uye.KalanCeza <= 0
                    ? "Bekleyen borç yok."
                    : cezalar.Count > 0
                        ? string.Join("\n", cezalar.Take(3).Select(c => $"• {c.Aciklama} (₺{c.Tutar:N2})"))
                        : "Önceki kayıtlardan devreden borç.";
                btnCezaOde.Content = uye.KalanCeza > 0 ? $"Cüzdandan Öde (₺{uye.KalanCeza:N2})" : "Borç Yok";

                dgFinans.ItemsSource = hareketler.Select(h => new FinansKaydi
                {
                    Tarih = h.Tarih.ToString("dd.MM.yyyy HH:mm"),
                    Islem = h.Aciklama,
                    Tutar = h.Tutar >= 0 ? $"+ ₺{h.Tutar:N2}" : $"- ₺{-h.Tutar:N2}",
                    Durum = h.Tip == CuzdanHareketTipi.Yukleme ? "Yüklendi" : "Ödendi"
                }).ToList();

                ButonlariAyarla(true, uye.KalanCeza);
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Cuzdan guncelleme", ex);
                MessageBox.Show($"Cüzdan bilgileri yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButonlariAyarla(bool uyeSecili, decimal borc)
        {
            btnCezaOde.IsEnabled = uyeSecili && borc > 0;
            if (!uyeSecili) dgFinans.ItemsSource = null;
        }

        private async void BtnYukle20_Click(object sender, RoutedEventArgs e) => await BakiyeEkle(20);
        private async void BtnYukle50_Click(object sender, RoutedEventArgs e) => await BakiyeEkle(50);
        private async void BtnYukle100_Click(object sender, RoutedEventArgs e) => await BakiyeEkle(100);

        private async Task BakiyeEkle(decimal miktar)
        {
            if (SeciliUye is not Uye uye)
            {
                MessageBox.Show("Önce bir üye seçin.", "Üye Seçilmedi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var onay = MessageBox.Show($"{uye.TamAd} adına bankoda ₺{miktar:N2} tahsil edildi ve cüzdana yüklenecek. Onaylıyor musunuz?",
                "Bakiye Yükleme", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes) return;

            try
            {
                using var context = new KutuphaneDbContext();
                var sonuc = await new CuzdanService(context).YukleAsync(uye.Id, miktar, AuthService.Instance.AktifKullanici?.Id);
                if (!sonuc.BasariliMi)
                {
                    MessageBox.Show(sonuc.Mesaj, "Yüklenemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                MessageBox.Show($"₺{miktar:N2} yüklendi. Yeni bakiye: ₺{sonuc.YeniBakiye:N2}", "Bakiye Yüklendi",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await GuncelleAsync();
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Cuzdan yukleme", ex);
                MessageBox.Show($"Yükleme başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnCezaOde_Click(object sender, RoutedEventArgs e)
        {
            if (SeciliUye is not Uye uye) return;

            try
            {
                using var context = new KutuphaneDbContext();
                var sonuc = await new CuzdanService(context).CezaOdeAsync(uye.Id, AuthService.Instance.AktifKullanici?.Id);
                if (!sonuc.BasariliMi)
                {
                    MessageBox.Show(sonuc.Mesaj, "Ödeme Yapılamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                MessageBox.Show($"Ceza borcu cüzdandan tahsil edildi. Kalan bakiye: ₺{sonuc.YeniBakiye:N2}", "Ödeme Başarılı",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await GuncelleAsync();
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("Cuzdan ceza odeme", ex);
                MessageBox.Show($"Ödeme başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
