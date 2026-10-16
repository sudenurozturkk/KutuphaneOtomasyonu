using System.Linq;
using System.Text.RegularExpressions;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>Alan doğrulama kuralları (saf fonksiyonlar; veritabanına erişmez).</summary>
    public static partial class Dogrulama
    {
        /// <summary>Tire ve boşlukları kaldırır, ISBN-10 kontrol karakteri 'x'i büyük harfe çevirir.</summary>
        public static string IsbnNormallestir(string? isbn) =>
            new string((isbn ?? string.Empty).Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();

        /// <summary>Normalleştirilmiş ISBN-10 veya ISBN-13 kontrol basamağını doğrular.</summary>
        public static bool IsbnGecerliMi(string? isbn)
        {
            var s = IsbnNormallestir(isbn);

            if (s.Length == 13 && s.All(char.IsAsciiDigit))
            {
                var toplam = 0;
                for (var i = 0; i < 13; i++)
                    toplam += (s[i] - '0') * (i % 2 == 0 ? 1 : 3);
                return toplam % 10 == 0;
            }

            if (s.Length == 10 && s[..9].All(char.IsAsciiDigit) && (char.IsAsciiDigit(s[9]) || s[9] == 'X'))
            {
                var toplam = 0;
                for (var i = 0; i < 10; i++)
                {
                    var deger = s[i] == 'X' ? 10 : s[i] - '0';
                    toplam += deger * (10 - i);
                }
                return toplam % 11 == 0;
            }

            return false;
        }

        /// <summary>T.C. Kimlik Numarası (ve aynı algoritmayı kullanan Yabancı Kimlik Numarası) kontrolü.</summary>
        public static bool TcKimlikGecerliMi(string? tc)
        {
            if (tc == null || tc.Length != 11 || !tc.All(char.IsAsciiDigit) || tc[0] == '0')
                return false;

            var d = tc.Select(c => c - '0').ToArray();
            var tekler = d[0] + d[2] + d[4] + d[6] + d[8];
            var ciftler = d[1] + d[3] + d[5] + d[7];
            var onuncu = ((tekler * 7 - ciftler) % 10 + 10) % 10;
            if (onuncu != d[9])
                return false;

            return d.Take(10).Sum() % 10 == d[10];
        }

        /// <summary>Pasaport numarası: ülkeler arası biçim farkı nedeniyle yalnızca 6–20 harf/rakam kontrolü.</summary>
        public static bool PasaportGecerliMi(string? pasaport) =>
            pasaport != null && pasaport.Length is >= 6 and <= 20 && pasaport.All(char.IsAsciiLetterOrDigit);

        public static bool EmailGecerliMi(string? email) =>
            !string.IsNullOrWhiteSpace(email) && EmailDeseni().IsMatch(email.Trim());

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
        private static partial Regex EmailDeseni();
    }
}
