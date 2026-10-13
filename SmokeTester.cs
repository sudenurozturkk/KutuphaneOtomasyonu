using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu
{
    public static class SmokeTester
    {
        public static async Task<string> RunSmokeTestAsync()
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine("=== BASLANGIC: GENISLETILMIS TUM MODULLER TESTI ===");

            try
            {
                using var context = new KutuphaneDbContext();
                DatabaseSeeder.Seed(context);
                results.AppendLine("[PASS] Veritabani baglantisi ve seed islemi basarili.");

                // 1. AuthService Test
                var loginOk = await AuthService.Instance.GirisAsync("admin", "admin123", context);
                if (!loginOk) throw new Exception("Admin girisi basarisiz!");
                results.AppendLine($"[PASS] Admin girisi basarili: {AuthService.Instance.AktifKullanici?.TamAd}");

                // 2. KitapService & Veriler
                var kitapService = new KitapService(context);
                var kitaplar = await kitapService.GetAllAsync();
                results.AppendLine($"[PASS] Kitap listeleme: {kitaplar.Count} kitap mevcut.");
                var kIstat = await kitapService.GetIstatistikAsync();
                results.AppendLine($"[PASS] Kitap istatistik: {kIstat.ToplamKitap} cesit, {kIstat.MevcutKopya} adet stokta.");

                // 3. UyeService & Veriler
                var uyeService = new UyeService(context);
                var uyeler = await uyeService.GetAllAsync();
                results.AppendLine($"[PASS] Uye listeleme: {uyeler.Count} uye bulundu.");

                // 4. OduncService & Veriler
                var bildirimService = new BildirimService(context);
                var ayarlar = await context.SistemAyarlari.FindAsync(1) ?? new SistemAyarlari();
                var oduncService = new OduncService(context, bildirimService, ayarlar);
                var oIstat = await oduncService.GetIstatistikAsync();
                results.AppendLine($"[PASS] Odunc istatistik: {oIstat.AktifOduncSayisi} aktif odunc.");

                // 5. 12 Sayfanın Bireysel Olarak Olusturulmasi
                results.AppendLine("--- 12 SAYFANIN TESTI ---");
                var p1 = new Views.Pages.DashboardPage();
                results.AppendLine("[PASS] 1. DashboardPage basariyla olusturuldu.");

                var p2 = new Views.Pages.KitaplarPage();
                results.AppendLine("[PASS] 2. KitaplarPage basariyla olusturuldu.");

                var p3 = new Views.Pages.OdunclerPage();
                results.AppendLine("[PASS] 3. OdunclerPage basariyla olusturuldu.");

                var p4 = new Views.Pages.UyelerPage();
                results.AppendLine("[PASS] 4. UyelerPage basariyla olusturuldu.");

                var p5 = new Views.Pages.SalonRezervasyonPage();
                results.AppendLine("[PASS] 5. SalonRezervasyonPage basariyla olusturuldu.");

                var p6 = new Views.Pages.RezervasyonlarPage();
                results.AppendLine("[PASS] 6. RezervasyonlarPage basariyla olusturuldu.");

                var p7 = new Views.Pages.BarkodBankoPage();
                results.AppendLine("[PASS] 7. BarkodBankoPage basariyla olusturuldu.");

                var p8 = new Views.Pages.CuzdanPage();
                results.AppendLine("[PASS] 8. CuzdanPage basariyla olusturuldu.");

                var p9 = new Views.Pages.EKitapPage();
                results.AppendLine("[PASS] 9. EKitapPage basariyla olusturuldu.");

                var p10 = new Views.Pages.RaporlarPage();
                results.AppendLine("[PASS] 10. RaporlarPage basariyla olusturuldu.");

                var p11 = new Views.Pages.AyarlarPage();
                results.AppendLine("[PASS] 11. AyarlarPage basariyla olusturuldu.");

                var p12 = new Views.Pages.MimariUmlPage();
                results.AppendLine("[PASS] 12. MimariUmlPage basariyla olusturuldu.");

                var p13 = new Views.Pages.AkademikHubPage();
                results.AppendLine("[PASS] 13. AkademikHubPage basariyla olusturuldu.");

                var p14 = new Views.Pages.KampusTransferPage();
                results.AppendLine("[PASS] 14. KampusTransferPage basariyla olusturuldu.");

                // 6. 6 Dialog Penceresinin Olusturulmasi
                results.AppendLine("--- 6 DIYALOG PENCERESININ TESTI ---");
                var kitap = kitaplar[0];
                var uye = uyeler[0];
                var d1 = new Views.Pages.KitapEkleDialog(kitap);
                results.AppendLine("[PASS] 1. KitapEkleDialog basariyla olusturuldu.");

                var d2 = new Views.Pages.UyeEkleDialog(uye);
                results.AppendLine("[PASS] 2. UyeEkleDialog basariyla olusturuldu.");

                var d3 = new Views.Pages.OduncVerDialog();
                results.AppendLine("[PASS] 3. OduncVerDialog basariyla olusturuldu.");

                var aktifOduncler = await oduncService.GetAktifOdunclerAsync();
                int ornekOduncId = aktifOduncler.Count > 0 ? aktifOduncler[0].Id : 1;
                var d4 = new Views.Pages.IadeDialog(ornekOduncId);
                results.AppendLine("[PASS] 4. IadeDialog basariyla olusturuldu.");

                var d5 = new Views.Pages.KitapDetayDialog(kitap.Id);
                results.AppendLine("[PASS] 5. KitapDetayDialog basariyla olusturuldu.");

                var d6 = new Views.Pages.UyeDetayDialog(uye.Id);
                results.AppendLine("[PASS] 6. UyeDetayDialog basariyla olusturuldu.");

                // 7. MainWindow ve 14 Navigasyonun Calismasi
                results.AppendLine("--- MAINWINDOW VE MENÜ NAVİGASYONLARI TESTI ---");
                var mainWindow = new Views.MainWindow();
                results.AppendLine("[PASS] MainWindow basariyla olusturuldu.");

                string[] menuTags = {
                    "Dashboard", "Kitaplar", "Oduncler", "Uyeler",
                    "SalonRezervasyon", "Rezervasyonlar", "BarkodBanko",
                    "Cuzdan", "EKitap", "Raporlar", "Ayarlar",
                    "Akademik", "KampusTransfer", "Mimari"
                };

                foreach (var tag in menuTags)
                {
                    mainWindow.NavigateToNav(tag);
                    results.AppendLine($"[PASS] Menu navigasyonu: '{tag}' basariyla gecis yapti.");
                }

                results.AppendLine("=== TEBRIKLER! 14 SAYFA + 6 DIYALOG + TUM NAVIGASYONLAR %100 BASARILI ===");
            }
            catch (Exception ex)
            {
                results.AppendLine($"[FAIL] Hata olustu: {ex}");
            }

            return results.ToString();
        }
    }
}
