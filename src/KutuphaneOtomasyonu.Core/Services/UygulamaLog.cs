using System;
using System.IO;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Tanılama günlüğü. Kullanıcıya ait veri klasörüne yazar ve hiçbir koşulda
    /// istisna fırlatmaz; günlük yazılamıyorsa uygulama çalışmaya devam eder.
    /// Parola, kimlik numarası gibi kişisel veriler buraya yazılmamalıdır.
    /// </summary>
    public static class UygulamaLog
    {
        private const long MaksimumBoyut = 1024 * 1024;
        private static readonly object _kilit = new();

        public static string LogYolu { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KutuphaneOtomasyonu", "logs", "app.log");

        public static void Yaz(string mesaj)
        {
            try
            {
                lock (_kilit)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogYolu)!);

                    var dosya = new FileInfo(LogYolu);
                    if (dosya.Exists && dosya.Length > MaksimumBoyut)
                        File.Move(LogYolu, LogYolu + ".1", overwrite: true);

                    File.AppendAllText(LogYolu, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {mesaj}{Environment.NewLine}");
                }
            }
            catch
            {
                // Günlük yazımı hiçbir zaman uygulamayı durdurmamalı.
            }
        }

        public static void Hata(string baglam, Exception ex) => Yaz($"[HATA] {baglam}: {ex}");
    }
}
