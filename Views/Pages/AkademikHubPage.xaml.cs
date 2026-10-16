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
    public partial class AkademikHubPage : Page
    {
        private List<AkademikYayin> _tumYayinlar = new();
        private AkademikYayin? _seciliYayin;

        public AkademikHubPage()
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
                _tumYayinlar = await service.GetYayinlarAsync();

                Filtrele();

                if (_tumYayinlar.Count > 0 && _seciliYayin == null)
                {
                    YayinSec(_tumYayinlar[0]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Akademik veriler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filtrele()
        {
            if (!IsLoaded || dgAkademik == null || txtSonucSayisi == null || cbYayinTuru == null || cbKampus == null)
                return;

            var arama = txtArama?.Text?.Trim().ToLower() ?? "";
            var turSecim = (cbYayinTuru.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var kampusSecim = (cbKampus.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filtered = _tumYayinlar.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                filtered = filtered.Where(y =>
                    y.Baslik.ToLower().Contains(arama) ||
                    y.Yazar.ToLower().Contains(arama) ||
                    (y.Danisman != null && y.Danisman.ToLower().Contains(arama)) ||
                    y.AnahtarKelimeler.ToLower().Contains(arama) ||
                    (y.DOI != null && y.DOI.ToLower().Contains(arama)));
            }

            if (turSecim != null && turSecim != "Tüm Türler")
            {
                filtered = filtered.Where(y => y.Tur.ToString().Contains(turSecim.Replace(" ", "")));
            }

            if (kampusSecim != null && kampusSecim != "Tüm Kampüsler")
            {
                filtered = filtered.Where(y => y.MevcutKampus.ToString().Contains(kampusSecim.Split(' ')[0]));
            }

            var list = filtered.ToList();
            dgAkademik.ItemsSource = list;
            txtSonucSayisi.Text = $"{list.Count} Akademik Eser";
        }

        private void YayinSec(AkademikYayin yayin)
        {
            _seciliYayin = yayin;
            txtSeciliBaslik.Text = yayin.Baslik;
            txtSeciliYazar.Text = $"{yayin.Yazar} • Danışman: {yayin.Danisman ?? "Belirtilmemiş"} ({yayin.Yil})";
            txtSeciliDOI.Text = $"DOI: {yayin.DOI ?? "Kayıtlı Değil"} | {yayin.DosyaBoyutu}";
            txtSeciliOzet.Text = string.IsNullOrWhiteSpace(yayin.Ozet) ? "Özet bulunmuyor." : yayin.Ozet;
        }

        private void DgAkademik_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAkademik.SelectedItem is AkademikYayin yayin)
            {
                YayinSec(yayin);
            }
        }

        private void TxtArama_TextChanged(object sender, TextChangedEventArgs e) => Filtrele();
        private void CbFiltre_Changed(object sender, SelectionChangedEventArgs e) => Filtrele();
        private async void BtnYenile_Click(object sender, RoutedEventArgs e) => await VeriYukle();

        private async void BtnKopyalaApa_Click(object sender, RoutedEventArgs e)
        {
            if (!YayinSeciliMi()) return;
            var yayin = _seciliYayin!;
            Clipboard.SetText(yayin.ApaCitation);
            await AlintiKaydet();
            MessageBox.Show($"✅ APA 7th Edition formatındaki akademik alıntı panoya kopyalandı:\n\n{yayin.ApaCitation}",
                "Citation Kopyalandı", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnKopyalaMla_Click(object sender, RoutedEventArgs e)
        {
            if (!YayinSeciliMi()) return;
            var yayin = _seciliYayin!;
            Clipboard.SetText(yayin.MlaCitation);
            await AlintiKaydet();
            MessageBox.Show($"✅ MLA 9th Edition formatındaki akademik alıntı panoya kopyalandı:\n\n{yayin.MlaCitation}",
                "Citation Kopyalandı", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnKopyalaBibTex_Click(object sender, RoutedEventArgs e)
        {
            if (!YayinSeciliMi()) return;
            var yayin = _seciliYayin!;
            Clipboard.SetText(yayin.BibTexCitation);
            await AlintiKaydet();
            MessageBox.Show($"✅ LaTeX için BibTeX formatı panoya kopyalandı:\n\n{yayin.BibTexCitation}",
                "BibTeX Kopyalandı", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task AlintiKaydet()
        {
            if (_seciliYayin == null) return;
            try
            {
                using var context = new KutuphaneDbContext();
                var service = new AkademikService(context);
                await service.AlintiArtirAsync(_seciliYayin.Id);
                _seciliYayin.AlintiSayisi++;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Atıf sayısı güncellenemedi: {ex.Message}", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnTamMetinIndir_Click(object sender, RoutedEventArgs e)
        {
            if (!YayinSeciliMi()) return;
            var yayin = _seciliYayin!;
            try
            {
                using var context = new KutuphaneDbContext();
                var service = new AkademikService(context);
                await service.IndirmeArtirAsync(yayin.Id);
                yayin.IndirmeSayisi++;

                MessageBox.Show($"📥 '{yayin.Baslik}' eseri açık erişim (Open Access) lisansıyla indirildi!\nBoyut: {yayin.DosyaBoyutu}",
                    "Tam Metin İndirildi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İndirme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnBaskaKampustenIste_Click(object sender, RoutedEventArgs e)
        {
            if (!YayinSeciliMi()) return;
            var yayin = _seciliYayin!;

            var onay = MessageBox.Show($"'{yayin.Baslik}' eserinin basılı kopyasını {yayin.MevcutKampus} şubesinden Merkez Kütüphane'ye kurye ile transfer etmek istiyor musunuz?",
                "Inter-Library Loan (ILL) Talebi", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (onay == MessageBoxResult.Yes)
            {
                if (yayin.MevcutKampus == KampusLokasyonu.MerkezKutuphane)
                {
                    MessageBox.Show("Bu eser zaten Merkez Kütüphane'de bulunuyor; transfer gerekmiyor.", "Bilgi",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    using var context = new KutuphaneDbContext();
                    var uyeler = await context.Uyeler.AsNoTracking()
                        .Where(u => u.Durumu == UyeDurumu.Aktif)
                        .OrderBy(u => u.Ad).ThenBy(u => u.Soyad)
                        .ToListAsync();

                    var secim = new SecimDialog("Talep Eden Üye", "Transfer talebi hangi üye adına oluşturulacak?", "Talep Oluştur",
                        new SecimAlani("Üye *", uyeler, nameof(Uye.TamAd)))
                    { Owner = Window.GetWindow(this) };
                    if (secim.ShowDialog() != true || secim.Secilenler[0] is not Uye uye)
                        return;

                    var service = new AkademikService(context);
                    var transfer = await service.YayinTransferTalepEtAsync(yayin.Id, uye.Id, yayin.MevcutKampus, KampusLokasyonu.MerkezKutuphane);

                    MessageBox.Show($"✅ ILL Kütüphaneler arası transfer talebi oluşturuldu!\nTakip Kodu: {transfer.KuryeTakipKodu}\nTahmini Varış: 24 Saat İçinde",
                        "Transfer Başlatıldı", MessageBoxButton.OK, MessageBoxImage.Information);

                    MainWindow.Instance?.NavigateToNav("KampusTransfer");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Transfer başlatılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnYeniYayin_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AkademikYayinEkleDialog
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true)
                await VeriYukle();
        }

        private bool YayinSeciliMi()
        {
            if (_seciliYayin != null)
                return true;

            MessageBox.Show("Önce listeden bir akademik eser seçin.", "Seçim Gerekli",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }
    }
}
