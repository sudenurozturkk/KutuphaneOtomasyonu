using KutuphaneOtomasyonu.Data;
using System;
using System.IO;
using System.Windows;

namespace KutuphaneOtomasyonu
{
    public partial class App : Application
    {
        private static string LogPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                File.AppendAllText(LogPath, $"[AppDomain Crash] {args.ExceptionObject}\n");
            };

            DispatcherUnhandledException += (s, args) =>
            {
                File.AppendAllText(LogPath, $"[Dispatcher Crash] {args.Exception}\n");
                args.Handled = false;
            };

            File.AppendAllText(LogPath, "[App Ctor] Baslatildi\n");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            File.AppendAllText(LogPath, "[OnStartup] Basladi\n");

            try
            {
                base.OnStartup(e);

                File.AppendAllText(LogPath, "[OnStartup] Veritabani seed ediliyor...\n");
                using var context = new KutuphaneDbContext();
                DatabaseSeeder.Seed(context);
                File.AppendAllText(LogPath, "[OnStartup] Seed tamamlandi\n");

                if (e.Args.Length > 0 && e.Args[0] == "--test")
                {
                    var testResult = SmokeTester.RunSmokeTestAsync().GetAwaiter().GetResult();
                    var testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_result.txt");
                    File.WriteAllText(testFile, testResult);
                    Environment.Exit(0);
                    return;
                }

                File.AppendAllText(LogPath, "[OnStartup] LoginWindow aciliyor...\n");
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var login = new Views.LoginWindow();
                login.Show();
                File.AppendAllText(LogPath, "[OnStartup] LoginWindow gosterildi\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(LogPath, $"[OnStartup Hata] {ex}\n");
                MessageBox.Show(ex.ToString(), "Başlangıç Hatası");
            }
        }
    }
}
