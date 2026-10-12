using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KutuphaneOtomasyonu.Models
{
    /// <summary>
    /// Kütüphane üyesini temsil eden entity sınıfı.
    /// Öğrenci ve Personel üyeliği desteklenmektedir.
    /// </summary>
    public class Uye
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Ad { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Soyad { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Telefon { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Adres { get; set; } = string.Empty;

        [Required, MaxLength(11)]
        public string TCKimlikNo { get; set; } = string.Empty;

        [MaxLength(50)]
        public string OgrenciNo { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Bolum { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Fakulte { get; set; } = string.Empty;

        public UyeTipi UyeTipi { get; set; } = UyeTipi.Ogrenci;

        public UyeDurumu Durumu { get; set; } = UyeDurumu.Aktif;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        public DateTime? UyelikBitisTarihi { get; set; }

        public DateTime? SonGirisTarihi { get; set; }

        [MaxLength(500)]
        public string FotografYolu { get; set; } = string.Empty;

        public int MaxOduncAdet { get; set; } = 3;

        public int MaxOduncSuresi { get; set; } = 14; // gün

        public decimal ToplamCeza { get; set; } = 0;

        public decimal OdenenCeza { get; set; } = 0;

        [MaxLength(1000)]
        public string Notlar { get; set; } = string.Empty;

        // Navigation Properties
        public virtual ICollection<Odunc> Oduncler { get; set; } = new List<Odunc>();
        public virtual ICollection<Rezervasyon> Rezervasyonlar { get; set; } = new List<Rezervasyon>();
        public virtual ICollection<KitapYorum> Yorumlar { get; set; } = new List<KitapYorum>();
        public virtual ICollection<Favori> Favoriler { get; set; } = new List<Favori>();
        public virtual ICollection<Bildirim> Bildirimler { get; set; } = new List<Bildirim>();

        // Hesaplanan özellikler
        [NotMapped]
        public string TamAd => $"{Ad} {Soyad}";

        [NotMapped]
        public decimal KalanCeza => ToplamCeza - OdenenCeza;

        [NotMapped]
        public bool CezaliMi => KalanCeza > 0;

        [NotMapped]
        public string UyeTipiMetni => UyeTipi switch
        {
            UyeTipi.Ogrenci => "Öğrenci",
            UyeTipi.Akademisyen => "Akademisyen",
            UyeTipi.Personel => "Personel",
            UyeTipi.Misafir => "Misafir",
            _ => "Bilinmiyor"
        };
    }

    public enum UyeTipi
    {
        Ogrenci = 0,
        Akademisyen = 1,
        Personel = 2,
        Misafir = 3
    }

    public enum UyeDurumu
    {
        Aktif = 0,
        Pasif = 1,
        Askida = 2,
        Yasakli = 3
    }
}
