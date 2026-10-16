using KutuphaneOtomasyonu.Models;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Kişisel verilerin rol bazlı gösterimi (KVKK veri minimizasyonu).
    /// Kütüphaneci ve yönetici tam veriyi görür; yardımcı personel kimlik, telefon ve adres
    /// bilgilerini yalnızca son karakterleriyle görür.
    /// </summary>
    public static class KisiselVeri
    {
        public static bool TamGorebilirMi(Kullanici? kullanici) => TamGorebilirMi(kullanici?.Rol);

        public static bool TamGorebilirMi(KullaniciRolu? rol) => rol >= KullaniciRolu.Kutuphaneci;

        public static string Maskele(string? deger, int gorunenSon = 4)
        {
            if (string.IsNullOrEmpty(deger)) return string.Empty;
            if (deger.Length <= gorunenSon) return new string('•', deger.Length);
            return new string('•', deger.Length - gorunenSon) + deger[^gorunenSon..];
        }

        public static string Goster(string? deger, Kullanici? kullanici, int gorunenSon = 4) =>
            Goster(deger, kullanici?.Rol, gorunenSon);

        public static string Goster(string? deger, KullaniciRolu? rol, int gorunenSon = 4) =>
            TamGorebilirMi(rol) ? deger ?? string.Empty : Maskele(deger, gorunenSon);
    }
}
