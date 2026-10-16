using System.Collections.Generic;
using System.Linq;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// RFC 4180 uyumlu CSV alan kaçışı ve hesap tablosu formül enjeksiyonuna karşı koruma
    /// (OWASP "CSV Injection"): <c>= + - @</c>, sekme veya satır başı ile başlayan alanların başına <c>'</c> eklenir.
    /// </summary>
    public static class CsvYazici
    {
        private static readonly char[] FormulBaslangiclari = { '=', '+', '-', '@', '\t', '\r' };

        public static string Alan(object? deger)
        {
            var metin = deger?.ToString() ?? string.Empty;

            if (metin.Length > 0 && FormulBaslangiclari.Contains(metin[0]))
                metin = "'" + metin;

            var tirnakGerekli = metin.IndexOfAny(new[] { ',', '"', '\n', '\r', ';' }) >= 0 || metin != metin.Trim();
            return tirnakGerekli ? "\"" + metin.Replace("\"", "\"\"") + "\"" : metin;
        }

        public static string Satir(IEnumerable<object?> alanlar) => string.Join(",", alanlar.Select(Alan));

        public static string Satir(params object?[] alanlar) => Satir((IEnumerable<object?>)alanlar);
    }
}
