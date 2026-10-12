using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class BarkodBankoPage : Page
    {
        public BarkodBankoPage()
        {
            InitializeComponent();
            LogTerminal("SYSTEM: Hızlı Barkod Bankosu başlatıldı.");
            LogTerminal("READY: Barkod veya ISBN okutulmaya hazır.");
        }

        private void LogTerminal(string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            txtTerminalLog.AppendText($"[{time}] {message}\n");
            txtTerminalLog.CaretIndex = txtTerminalLog.Text.Length;
            txtTerminalLog.ScrollToEnd();
        }

        private void TxtBarkod_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) txtUyeNo.Focus();
        }

        private void TxtUyeNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnHizliOdunc_Click(sender, e);
        }

        private async void BtnHizliOdunc_Click(object sender, RoutedEventArgs e)
        {
            var barkod = txtBarkod.Text.Trim();
            var uyeNo = txtUyeNo.Text.Trim();

            if (string.IsNullOrWhiteSpace(barkod) || string.IsNullOrWhiteSpace(uyeNo))
            {
                LogTerminal("HATA: Kitap barkodu ve üye bilgisi girilmelidir!");
                MessageBox.Show("Lütfen hem kitap barkodunu hem de üye bilgisini girin.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();

                // Kitap bul (ISBN, Id veya Başlık)
                var kitap = await context.Kitaplar.FirstOrDefaultAsync(k =>
                    k.ISBN == barkod || k.Id.ToString() == barkod || k.Baslik.Contains(barkod));

                if (kitap == null)
                {
                    LogTerminal($"HATA: '{barkod}' barkodlu kitap sistemde bulunamadı!");
                    return;
                }

                // Üye bul (TC, OgrenciNo, Id veya Ad)
                var uye = await context.Uyeler.FirstOrDefaultAsync(u =>
                    u.TCKimlikNo == uyeNo || u.OgrenciNo == uyeNo || u.Id.ToString() == uyeNo || u.Ad.Contains(uyeNo));

                if (uye == null)
                {
                    LogTerminal($"HATA: '{uyeNo}' numaralı üye sistemde bulunamadı!");
                    return;
                }

                var bildirimService = new BildirimService(context);
                var ayarlar = await context.SistemAyarlari.FindAsync(1) ?? new SistemAyarlari();
                var oduncService = new OduncService(context, bildirimService, ayarlar);

                var sonuc = await oduncService.OduncVerAsync(kitap.Id, uye.Id, AuthService.Instance.AktifKullanici?.Id ?? 1);

                if (sonuc.Basarili)
                {
                    LogTerminal($"BAŞARILI: '{kitap.Baslik}' kitabı -> '{uye.TamAd}' adlı üyeye ödünç verildi.");
                    LogTerminal($"FİŞ: İade Tarihi: {sonuc.Odunc?.PlanlananIadeTarihi:dd.MM.yyyy} | Kalan Stok: {kitap.MevcutAdet}");
                    txtBarkod.Clear();
                    txtBarkod.Focus();
                }
                else
                {
                    LogTerminal($"REDDEDİLDİ: {sonuc.Mesaj}");
                }
            }
            catch (Exception ex)
            {
                LogTerminal($"SİSTEM HATASI: {ex.Message}");
            }
        }

        private async void BtnHizliIade_Click(object sender, RoutedEventArgs e)
        {
            var barkod = txtBarkod.Text.Trim();
            if (string.IsNullOrWhiteSpace(barkod))
            {
                LogTerminal("HATA: İade alınacak kitabın barkodunu girin!");
                return;
            }

            try
            {
                using var context = new KutuphaneDbContext();
                var kitap = await context.Kitaplar.FirstOrDefaultAsync(k =>
                    k.ISBN == barkod || k.Id.ToString() == barkod || k.Baslik.Contains(barkod));

                if (kitap == null)
                {
                    LogTerminal($"HATA: '{barkod}' barkodlu kitap bulunamadı!");
                    return;
                }

                var aktifOdunc = await context.Oduncler
                    .Include(o => o.Uye)
                    .FirstOrDefaultAsync(o => o.KitapId == kitap.Id && o.Durumu == OduncDurumu.Aktif);

                if (aktifOdunc == null)
                {
                    LogTerminal($"BİLGİ: '{kitap.Baslik}' kitabı için aktif bir ödünç kaydı yok (zaten kütüphanede).");
                    return;
                }

                var bildirimService = new BildirimService(context);
                var ayarlar = await context.SistemAyarlari.FindAsync(1) ?? new SistemAyarlari();
                var oduncService = new OduncService(context, bildirimService, ayarlar);

                var sonuc = await oduncService.IadeAlAsync(aktifOdunc.Id, KitapDurumu.Iyi);

                if (sonuc.Basarili)
                {
                    LogTerminal($"İADE BAŞARILI: '{kitap.Baslik}' kitabı '{aktifOdunc.Uye.TamAd}' adlı üyeden teslim alındı.");
                    LogTerminal($"RAF KONUMU: Raf {kitap.RafKonumu} | Mevcut Stok: {kitap.MevcutAdet}");
                    txtBarkod.Clear();
                    txtBarkod.Focus();
                }
                else
                {
                    LogTerminal($"İADE HATASI: {sonuc.Mesaj}");
                }
            }
            catch (Exception ex)
            {
                LogTerminal($"SİSTEM HATASI: {ex.Message}");
            }
        }

        private void QuickFill_CleanCode(object sender, RoutedEventArgs e) => txtBarkod.Text = "Clean Code";
        private void QuickFill_Algo(object sender, RoutedEventArgs e) => txtBarkod.Text = "Algoritmalar";
        private void QuickFill_Member(object sender, RoutedEventArgs e) => txtUyeNo.Text = "Ahmet";
        private void BtnTemizleLog_Click(object sender, RoutedEventArgs e) => txtTerminalLog.Clear();
    }
}
