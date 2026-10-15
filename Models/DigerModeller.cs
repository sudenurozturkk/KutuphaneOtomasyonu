using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KutuphaneOtomasyonu.Models
{
    public class SalonRezervasyonu
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string MasaKodu { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Bolge { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Kullanici { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string ZamanAraligi { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Sure { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Amac { get; set; } = string.Empty;

        public DateTime Tarih { get; set; } = DateTime.Today;

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Durum { get; set; } = "Aktif";
    }

    /// <summary>
    /// Kitap rezervasyonunu temsil eden entity.
    /// Bir üye mevcut olmayan kitap için rezervasyon yapabilir.
    /// </summary>
    public class Rezervasyon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KitapId { get; set; }

        [Required]
        public int UyeId { get; set; }

        public DateTime RezervasyonTarihi { get; set; } = DateTime.Now;

        public DateTime GecerlilikTarihi { get; set; }

        public RezervasyonDurumu Durumu { get; set; } = RezervasyonDurumu.Bekliyor;

        public int SiraNumarasi { get; set; } = 1;

        [MaxLength(500)]
        public string Notlar { get; set; } = string.Empty;

        // Navigation Properties
        [ForeignKey("KitapId")]
        public virtual Kitap? Kitap { get; set; }

        [ForeignKey("UyeId")]
        public virtual Uye? Uye { get; set; }

        [NotMapped]
        public bool GecerliMi => DateTime.Now <= GecerlilikTarihi && Durumu == RezervasyonDurumu.Bekliyor;
    }

    public enum RezervasyonDurumu
    {
        Bekliyor = 0,
        Onaylandi = 1,
        Iptal = 2,
        Tamamlandi = 3,
        Suresi_Doldu = 4
    }

    /// <summary>
    /// Kitap yorumu ve puanlamasını temsil eden entity.
    /// </summary>
    public class KitapYorum
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KitapId { get; set; }

        [Required]
        public int UyeId { get; set; }

        public int Puan { get; set; } // 1-5

        [MaxLength(2000)]
        public string Yorum { get; set; } = string.Empty;

        public DateTime YorumTarihi { get; set; } = DateTime.Now;

        public bool Onaylandi { get; set; } = true;

        [ForeignKey("KitapId")]
        public virtual Kitap? Kitap { get; set; }

        [ForeignKey("UyeId")]
        public virtual Uye? Uye { get; set; }
    }

    /// <summary>
    /// Üye favori kitaplarını temsil eden entity.
    /// </summary>
    public class Favori
    {
        [Key]
        public int Id { get; set; }

        public int UyeId { get; set; }

        public int KitapId { get; set; }

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

        [ForeignKey("UyeId")]
        public virtual Uye? Uye { get; set; }

        [ForeignKey("KitapId")]
        public virtual Kitap? Kitap { get; set; }
    }

    /// <summary>
    /// Sistem bildirimleri entity'si.
    /// </summary>
    public class Bildirim
    {
        [Key]
        public int Id { get; set; }

        public int? UyeId { get; set; }

        [Required, MaxLength(200)]
        public string Baslik { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Mesaj { get; set; } = string.Empty;

        public BildirimTipi Tipi { get; set; } = BildirimTipi.Bilgi;

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        public bool Okundu { get; set; } = false;

        public DateTime? OkunmaTarihi { get; set; }

        [ForeignKey("UyeId")]
        public virtual Uye? Uye { get; set; }
    }

    public enum BildirimTipi
    {
        Bilgi = 0,
        Uyari = 1,
        Hata = 2,
        Basari = 3,
        Gecikme = 4,
        Rezervasyon = 5
    }

    /// <summary>
    /// Ceza kayıtları entity'si.
    /// </summary>
    public class CezaKaydi
    {
        [Key]
        public int Id { get; set; }

        public int UyeId { get; set; }

        public int OduncId { get; set; }

        public decimal Tutar { get; set; }

        public CezaTipi Tipi { get; set; }

        [MaxLength(500)]
        public string Aciklama { get; set; } = string.Empty;

        public DateTime OlusmaTarihi { get; set; } = DateTime.Now;

        public bool Odendi { get; set; } = false;

        public DateTime? OdemeTarihi { get; set; }

        [ForeignKey("UyeId")]
        public virtual Uye? Uye { get; set; }

        [ForeignKey("OduncId")]
        public virtual Odunc? Odunc { get; set; }
    }

    public enum CezaTipi
    {
        Gecikme = 0,
        Hasar = 1,
        Kayip = 2
    }
}
