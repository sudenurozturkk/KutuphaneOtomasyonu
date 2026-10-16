using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class RaporlarPage : Page
    {
        public RaporlarPage()
        {
            InitializeComponent();
            Loaded += RaporlarPage_Loaded;
        }

        private async void RaporlarPage_Loaded(object sender, RoutedEventArgs e)
        {
            await VeriYukle();
        }

        private async void BtnYenile_Click(object sender, RoutedEventArgs e)
        {
            await VeriYukle();
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var masaustu = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var dosyaYolu = System.IO.Path.Combine(masaustu, $"Kutuphane_Analitik_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.csv");

                var sb = new StringBuilder();
                sb.AppendLine("KÜTÜPHANE OTOMASYONU - YÖNETİM VE ANALİTİK RAPORU");
                sb.AppendLine($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine("METRİK,DEĞER,AÇIKLAMA");
                sb.AppendLine($"Toplam Ödünç Hareketi,{txtToplamIslem.Text},Tüm zamanlar");
                sb.AppendLine($"Aktif Ödünçte Olan Kitap,{txtAktifUye.Text},Teslim bekleyen");
                sb.AppendLine($"Bu Ay Eklenen Kitap,{txtKoleksiyonBuyume.Text},Yeni edinimler");
                sb.AppendLine($"Tahsil Edilen Ceza Tutarı,{txtTahsilat.Text},Kasa girişi");
                sb.AppendLine("");
                sb.AppendLine("DETAYLI İSTATİSTİKLER");
                sb.AppendLine(txtKitapIstat.Text.Replace("\n", "\r\n"));
                sb.AppendLine(txtOduncIstat.Text.Replace("\n", "\r\n"));
                sb.AppendLine(txtUyeIstat.Text.Replace("\n", "\r\n"));

                System.IO.File.WriteAllText(dosyaYolu, sb.ToString(), Encoding.UTF8);

                MessageBox.Show($"✅ Analitik raporu başarıyla masaüstüne CSV formatında aktarıldı:\n{System.IO.Path.GetFileName(dosyaYolu)}",
                    "Rapor Dışa Aktarıldı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rapor kaydedilirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task VeriYukle()
        {
            try
            {
                using var context = new KutuphaneDbContext();
                var kitapService = new KitapService(context);
                var oduncService = (await ServisKapsami.OlusturAsync(context)).Odunc;

                var kIstat = await kitapService.GetIstatistikAsync();
                var oIstat = await oduncService.GetIstatistikAsync();
                var toplamUye = await context.Uyeler.CountAsync();
                var aktifUye = await context.Uyeler.CountAsync(u => u.Durumu == UyeDurumu.Aktif);
                var buAyEklenKitap = await context.Kitaplar
                    .CountAsync(k => k.EklenmeTarihi.Month == DateTime.Now.Month && k.EklenmeTarihi.Year == DateTime.Now.Year);

                // KPI Kartları
                txtToplamIslem.Text = oIstat.ToplamOduncSayisi.ToString();
                txtIslemAlt.Text = $"↑ Tüm zamanlar toplamı";

                txtAktifUye.Text = oIstat.AktifOduncSayisi.ToString();
                txtAktifUyeAlt.Text = $"Şu an kitap ödünçte olan";

                txtKoleksiyonBuyume.Text = buAyEklenKitap.ToString();
                txtKoleksiyonAlt.Text = $"Toplam koleksiyon: {kIstat.ToplamKitap} kitap";

                txtTahsilat.Text = $"₺{oIstat.OdenenCeza:F2}";
                txtTahsilatAlt.Text = $"Toplam ceza: ₺{oIstat.ToplamCeza:F2}";

                // Grafik bar yükseklikleri (normalizasyon: maks = 50 ödünç = 176px)
                double maksOdunc = Math.Max(oIstat.AktifOduncSayisi, 5);
                double oranEkim = Math.Min((double)oIstat.AktifOduncSayisi / 50.0 * 176.0, 176.0);
                barEkimOdunc.Height = Math.Max(oranEkim, 12);
                double iadeOranEkim = Math.Min((double)(oIstat.ToplamOduncSayisi - oIstat.AktifOduncSayisi) / 50.0 * 176.0, 176.0);
                barEkimIade.Height = Math.Max(iadeOranEkim, 8);

                // Detay İstatistikler
                var sbK = new StringBuilder();
                sbK.AppendLine($"📚 Toplam Kitap Çeşidi: {kIstat.ToplamKitap}");
                sbK.AppendLine($"📖 Toplam Nüsha / Adet: {kIstat.ToplamKopyaSayisi}");
                sbK.AppendLine($"✅ Mevcut Stok: {kIstat.MevcutKopya}");
                sbK.AppendLine($"🔄 Ödünçte Olan: {kIstat.OdunctekiKopya}");
                sbK.AppendLine($"🏷️ Kategori Sayısı: {kIstat.KategoriSayisi}");
                txtKitapIstat.Text = sbK.ToString();

                var sbO = new StringBuilder();
                sbO.AppendLine($"🔄 Aktif Ödünç: {oIstat.AktifOduncSayisi}");
                sbO.AppendLine($"⚠️ Gecikmiş İade: {oIstat.GecikmisSayisi}");
                sbO.AppendLine($"📊 Toplam Hareket: {oIstat.ToplamOduncSayisi}");
                sbO.AppendLine($"💸 Toplam Ceza: ₺{oIstat.ToplamCeza:F2}");
                sbO.AppendLine($"💰 Tahsil Edilen: ₺{oIstat.OdenenCeza:F2}");
                txtOduncIstat.Text = sbO.ToString();

                var sbU = new StringBuilder();
                sbU.AppendLine($"👥 Toplam Kayıtlı Üye: {toplamUye}");
                sbU.AppendLine($"✅ Aktif Üye: {aktifUye}");
                sbU.AppendLine($"🚫 Askıya Alınan: {toplamUye - aktifUye}");
                sbU.AppendLine($"📈 Bu Ay Yeni Üye: 0");
                sbU.AppendLine($"🎓 Aktiflik Oranı: {(toplamUye > 0 ? (aktifUye * 100 / toplamUye) : 0)}%");
                txtUyeIstat.Text = sbU.ToString();

                // Popüler Kitaplar
                var pop = await kitapService.GetPopulerKitaplarAsync(10);
                var siralanmis = pop.Select((k, i) => new PopulerKitapVM
                {
                    Sira = (i + 1).ToString(),
                    Baslik = k.Baslik,
                    Yazar = k.Yazar,
                    Kategori = k.Kategori,
                    GoruntulemeSayisi = k.GoruntulemeSayisi,
                    PuanGoster = k.PuanGoster
                }).ToList();
                dgPopuler.ItemsSource = siralanmis;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Raporlar yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class PopulerKitapVM
    {
        public string Sira { get; set; } = "";
        public string Baslik { get; set; } = "";
        public string Yazar { get; set; } = "";
        public string Kategori { get; set; } = "";
        public int GoruntulemeSayisi { get; set; }
        public string PuanGoster { get; set; } = "";
    }
}
