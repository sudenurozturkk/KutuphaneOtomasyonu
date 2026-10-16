using System;

namespace KutuphaneOtomasyonu.Core
{
    /// <summary>
    /// Kütüphane Çalışma Masası ve Grup Odası Rezervasyon Varlığı
    /// </summary>
    public class MasaRezervasyonu
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }
        public string MasaKodu { get; set; } // Örn: D-101, ROOM-A
        public string ZamanDilimi { get; set; } // "09:00 - 12:00", "13:00 - 16:00"
        public DateTime Tarih { get; set; } = DateTime.Today;
        public bool AktifMi { get; set; } = true;

        public MasaRezervasyonu(int id, int kullaniciId, string masaKodu, string zamanDilimi)
        {
            Id = id;
            KullaniciId = kullaniciId;
            MasaKodu = masaKodu;
            ZamanDilimi = zamanDilimi;
        }
    }
}
