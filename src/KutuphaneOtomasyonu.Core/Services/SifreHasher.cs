using System;
using System.Security.Cryptography;
using System.Text;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Parola özetleme: PBKDF2-HMAC-SHA256, kullanıcı başına rastgele tuz.
    /// Biçim: <c>PBKDF2-SHA256$iterasyon$tuzBase64$ozetBase64</c>.
    /// Eski sürümlerin sabit tuzlu SHA-256 özetleri doğrulanabilir ve
    /// <see cref="YukseltmeGerekli"/> ile yeniden özetlenmek üzere işaretlenir.
    /// </summary>
    public static class SifreHasher
    {
        private const string Onek = "PBKDF2-SHA256";
        private const int VarsayilanIterasyon = 600_000; // OWASP Password Storage Cheat Sheet önerisi
        private const int TuzUzunlugu = 16;
        private const int OzetUzunlugu = 32;
        private const string EskiSabitTuz = "KutuphaneOtomasyonu_Salt_2024";

        public static string Hashle(string sifre) => Hashle(sifre, VarsayilanIterasyon);

        internal static string Hashle(string sifre, int iterasyon)
        {
            ArgumentNullException.ThrowIfNull(sifre);
            var tuz = RandomNumberGenerator.GetBytes(TuzUzunlugu);
            var ozet = Rfc2898DeriveBytes.Pbkdf2(sifre, tuz, iterasyon, HashAlgorithmName.SHA256, OzetUzunlugu);
            return $"{Onek}${iterasyon}${Convert.ToBase64String(tuz)}${Convert.ToBase64String(ozet)}";
        }

        public static bool Dogrula(string sifre, string kayitliOzet)
        {
            if (string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(kayitliOzet))
                return false;

            if (!kayitliOzet.StartsWith(Onek + "$", StringComparison.Ordinal))
                return EskiOzetiDogrula(sifre, kayitliOzet);

            var parcalar = kayitliOzet.Split('$');
            if (parcalar.Length != 4 || !int.TryParse(parcalar[1], out var iterasyon) || iterasyon <= 0)
                return false;

            try
            {
                var tuz = Convert.FromBase64String(parcalar[2]);
                var beklenen = Convert.FromBase64String(parcalar[3]);
                var gercek = Rfc2898DeriveBytes.Pbkdf2(sifre, tuz, iterasyon, HashAlgorithmName.SHA256, beklenen.Length);
                return CryptographicOperations.FixedTimeEquals(gercek, beklenen);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>Kayıtlı özet güncel algoritma ve iterasyon sayısıyla üretilmemişse true döner.</summary>
        public static bool YukseltmeGerekli(string kayitliOzet)
        {
            var parcalar = kayitliOzet.Split('$');
            return parcalar.Length != 4 ||
                   parcalar[0] != Onek ||
                   !int.TryParse(parcalar[1], out var iterasyon) ||
                   iterasyon < VarsayilanIterasyon;
        }

        /// <summary>Kullanıcı bulunamadığında yanıt süresini eşitlemek için sahte doğrulama yapar.</summary>
        internal static void SahteDogrulama(string sifre) =>
            Rfc2898DeriveBytes.Pbkdf2(sifre ?? string.Empty, new byte[TuzUzunlugu], VarsayilanIterasyon, HashAlgorithmName.SHA256, OzetUzunlugu);

        internal static string EskiOzet(string sifre) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(sifre + EskiSabitTuz)));

        private static bool EskiOzetiDogrula(string sifre, string kayitliOzet)
        {
            var beklenen = Encoding.ASCII.GetBytes(kayitliOzet);
            var gercek = Encoding.ASCII.GetBytes(EskiOzet(sifre));
            return CryptographicOperations.FixedTimeEquals(gercek, beklenen);
        }
    }
}
