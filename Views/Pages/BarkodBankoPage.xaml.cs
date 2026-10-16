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
                var arama = new BankoAramaService(context);

                // Önce nüsha barkodu (kesin kopya), sonra ISBN / Id / tekil başlık
                var nusha = await arama.NushaBulAsync(barkod);
                Kitap kitap;
                if (nusha?.Kitap != null)
                {
                    kitap = nusha.Kitap;
                }
                else
                {
                    var kitapArama = await arama.KitapBulAsync(barkod);
                    if (kitapArama.Kayit is not Kitap bulunan)
                    {
                        LogTerminal($"HATA: {kitapArama.Mesaj}");
                        return;
                    }
                    kitap = bulunan;
                }

                // Üye bul (TC, öğrenci no, Id; ad yalnızca tek eşleşmede)
                var uyeArama = await arama.UyeBulAsync(uyeNo);
                if (uyeArama.Kayit is not Uye uye)
                {
                    LogTerminal($"HATA: {uyeArama.Mesaj}");
                    return;
                }

                var oduncService = (await ServisKapsami.OlusturAsync(context)).Odunc;

                var sonuc = await oduncService.OduncVerAsync(kitap.Id, uye.Id, AuthService.Instance.AktifKullanici?.Id ?? 0, nusha?.Barkod);

                if (sonuc.Basarili)
                {
                    LogTerminal($"BAŞARILI: '{kitap.Baslik}' kitabı -> '{uye.TamAd}' adlı üyeye ödünç verildi.");
                    LogTerminal($"FİŞ: Nüsha {sonuc.Odunc?.Nusha?.Barkod} | İade Tarihi: {sonuc.Odunc?.PlanlananIadeTarihi:dd.MM.yyyy} | Kalan Stok: {kitap.MevcutAdet}");
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
                var arama = new BankoAramaService(context);

                // Nüsha barkodu ödüncü kesin belirler; ISBN/başlık ile birden çok kopya ödünçteyse üye bilgisi gerekir.
                var nusha = await arama.NushaBulAsync(barkod);
                Kitap kitap;
                AramaSonuc<Odunc> oduncArama;
                if (nusha?.Kitap != null)
                {
                    kitap = nusha.Kitap;
                    oduncArama = await arama.NushaninAktifOduncuAsync(nusha.Id);
                }
                else
                {
                    var kitapArama = await arama.KitapBulAsync(barkod);
                    if (kitapArama.Kayit is not Kitap bulunan)
                    {
                        LogTerminal($"HATA: {kitapArama.Mesaj}");
                        return;
                    }
                    kitap = bulunan;
                    oduncArama = await arama.AktifOduncBulAsync(kitap.Id, txtUyeNo.Text);
                }

                if (oduncArama.Kayit is not Odunc aktifOdunc)
                {
                    LogTerminal($"BİLGİ: '{kitap.Baslik}': {oduncArama.Mesaj}");
                    return;
                }

                var oduncService = (await ServisKapsami.OlusturAsync(context)).Odunc;

                var sonuc = await oduncService.IadeAlAsync(aktifOdunc.Id, KitapDurumu.Iyi);

                if (sonuc.Basarili)
                {
                    LogTerminal($"İADE BAŞARILI: '{kitap.Baslik}' kitabı '{aktifOdunc.Uye?.TamAd ?? "bilinmeyen üye"}' adlı üyeden teslim alındı.");
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
