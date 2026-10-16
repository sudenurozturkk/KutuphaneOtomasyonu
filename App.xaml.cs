using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Services;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace KutuphaneOtomasyonu
{
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                UygulamaLog.Yaz($"[AppDomain Crash] {args.ExceptionObject}");

            DispatcherUnhandledException += (s, args) =>
            {
                UygulamaLog.Hata("Dispatcher Crash", args.Exception);
                args.Handled = false;
            };

            UygulamaLog.Yaz("[App Ctor] Baslatildi");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            UygulamaLog.Yaz("[OnStartup] Basladi");

            try
            {
                base.OnStartup(e);

                if (e.Args.Contains("--test"))
                {
                    CalistirSmokeTest();
                    return;
                }

                UygulamaLog.Yaz("[OnStartup] Veritabani seed ediliyor...");
                using (var context = new KutuphaneDbContext())
                    DatabaseSeeder.Seed(context);
                UygulamaLog.Yaz("[OnStartup] Seed tamamlandi");

                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var login = new Views.LoginWindow();
                login.Show();
                UygulamaLog.Yaz("[OnStartup] LoginWindow gosterildi");
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("OnStartup", ex);
                MessageBox.Show(
                    $"Uygulama başlatılamadı: {ex.Message}\n\nAyrıntılar günlük dosyasına yazıldı:\n{UygulamaLog.LogYolu}",
                    "Başlangıç Hatası");
            }
        }

        /// <summary>
        /// Smoke testi kullanıcının gerçek veritabanına dokunmadan, geçici bir veritabanında çalıştırır.
        /// </summary>
        private static void CalistirSmokeTest()
        {
            var geciciDb = Path.Combine(Path.GetTempPath(), $"kutuphane_smoke_{Guid.NewGuid():N}.db");
            Environment.SetEnvironmentVariable(KutuphaneDbContext.VeritabaniYoluOrtamDegiskeni, geciciDb);

            var exitCode = 1;
            try
            {
                // UI iş parçacığı bloklanmaz: await devamları dispatcher üzerinde çalışabilsin diye mesaj döngüsü pompalanır.
                var gorev = SmokeTester.RunSmokeTestAsync();
                var cerceve = new System.Windows.Threading.DispatcherFrame();
                gorev.ContinueWith(_ => cerceve.Continue = false, TaskScheduler.Default);
                System.Windows.Threading.Dispatcher.PushFrame(cerceve);
                var testResult = gorev.GetAwaiter().GetResult();
                var testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_result.txt");
                File.WriteAllText(testFile, testResult);
                exitCode = testResult.Contains("[FAIL]") ? 1 : 0;
            }
            catch (Exception ex)
            {
                UygulamaLog.Hata("SmokeTest", ex);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                foreach (var dosya in new[] { geciciDb, geciciDb + "-wal", geciciDb + "-shm" })
                {
                    try { File.Delete(dosya); } catch { }
                }
            }

            Environment.Exit(exitCode);
        }
    }
}
