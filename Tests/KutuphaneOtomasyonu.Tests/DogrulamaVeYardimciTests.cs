using KutuphaneOtomasyonu.Services;
using Xunit;

namespace KutuphaneOtomasyonu.Tests;

public class DogrulamaVeYardimciTests
{
    /// <summary>9 haneli önekten kontrol basamakları doğru bir test kimlik numarası üretir.</summary>
    public static string GecerliTc(int onek)
    {
        var d = (100_000_000 + onek % 900_000_000).ToString().Select(c => c - '0').ToList();
        var onuncu = (((d[0] + d[2] + d[4] + d[6] + d[8]) * 7 - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        d.Add(onuncu);
        d.Add(d.Sum() % 10);
        return string.Concat(d);
    }

    [Theory]
    [InlineData("978-0-306-40615-7", true)]
    [InlineData("9780306406157", true)]
    [InlineData("0-306-40615-2", true)]
    [InlineData("080442957x", true)]
    [InlineData("9780306406158", false)]
    [InlineData("0306406153", false)]
    [InlineData("9780001000001978", false)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    public void Isbn_KontrolBasamagiDogrulanir(string isbn, bool beklenen) =>
        Assert.Equal(beklenen, Dogrulama.IsbnGecerliMi(isbn));

    [Fact]
    public void Isbn_NormallestirmeTireVeBosluklariKaldirir() =>
        Assert.Equal("080442957X", Dogrulama.IsbnNormallestir(" 0-8044-2957-x "));

    [Theory]
    [InlineData("10000000146", true)]
    [InlineData("10000000147", false)]
    [InlineData("01234567890", false)]
    [InlineData("1234567890", false)]
    [InlineData("1000000014a", false)]
    [InlineData(null, false)]
    public void TcKimlik_AlgoritmaDogrulanir(string? tc, bool beklenen) =>
        Assert.Equal(beklenen, Dogrulama.TcKimlikGecerliMi(tc));

    [Fact]
    public void TcKimlik_TestUreticisiGecerliNumaraUretir()
    {
        for (var i = 0; i < 200; i++)
            Assert.True(Dogrulama.TcKimlikGecerliMi(GecerliTc(i * 7919)));
    }

    [Theory]
    [InlineData("ad.soyad@ornek.edu.tr", true)]
    [InlineData("a@b.co", true)]
    [InlineData("eksik@alan", false)]
    [InlineData("bosluk var@ornek.com", false)]
    [InlineData("", false)]
    public void Email_BicimDogrulanir(string email, bool beklenen) =>
        Assert.Equal(beklenen, Dogrulama.EmailGecerliMi(email));

    [Theory]
    [InlineData("Düz metin", "Düz metin")]
    [InlineData("Virgül, içerir", "\"Virgül, içerir\"")]
    [InlineData("Tırnak \"var\"", "\"Tırnak \"\"var\"\"\"")]
    [InlineData("=HYPERLINK(\"x\")", "\"'=HYPERLINK(\"\"x\"\")\"")]
    [InlineData("+90", "'+90")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("satır\nsonu", "\"satır\nsonu\"")]
    public void Csv_AlanKacisiVeFormulKorumasi(string girdi, string beklenen) =>
        Assert.Equal(beklenen, CsvYazici.Alan(girdi));

    [Fact]
    public void Csv_SatirAlanlariVirgulleBirlestirir() =>
        Assert.Equal("1,\"a,b\",", CsvYazici.Satir(1, "a,b", null));

    [Fact]
    public void Sifre_HashDogrulanir_YanlisSifreReddedilir_TuzHerSeferindeFarklidir()
    {
        var ozet1 = SifreHasher.Hashle("Gizli-Parola1", 1_000);
        var ozet2 = SifreHasher.Hashle("Gizli-Parola1", 1_000);

        Assert.StartsWith("PBKDF2-SHA256$1000$", ozet1);
        Assert.NotEqual(ozet1, ozet2);
        Assert.True(SifreHasher.Dogrula("Gizli-Parola1", ozet1));
        Assert.False(SifreHasher.Dogrula("gizli-parola1", ozet1));
        Assert.False(SifreHasher.Dogrula("", ozet1));
        Assert.True(SifreHasher.YukseltmeGerekli(ozet1)); // iterasyon varsayılanın altında
    }

    [Fact]
    public void Sifre_EskiSabitTuzluOzetDogrulanirVeYukseltmeGerektirir()
    {
        var eski = SifreHasher.EskiOzet("admin123");

        Assert.True(SifreHasher.Dogrula("admin123", eski));
        Assert.False(SifreHasher.Dogrula("admin124", eski));
        Assert.True(SifreHasher.YukseltmeGerekli(eski));
    }

    [Theory]
    [InlineData("PBKDF2-SHA256$abc$AAAA$AAAA")]
    [InlineData("PBKDF2-SHA256$1000$***$AAAA")]
    [InlineData("PBKDF2-SHA256$1000")]
    public void Sifre_BozukOzetIstisnaFirlatmadanReddedilir(string bozuk) =>
        Assert.False(SifreHasher.Dogrula("x", bozuk));
}
