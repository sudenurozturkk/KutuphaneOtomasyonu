using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KutuphaneOtomasyonu.Models
{
    /// <summary>
    /// Sistem kullanıcısını (kütüphaneci/admin) temsil eden entity.
    /// </summary>
    public class Kullanici
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Ad { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Soyad { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string SifreHash { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Telefon { get; set; } = string.Empty;

        public KullaniciRolu Rol { get; set; } = KullaniciRolu.Kutuphaneci;

        public bool Aktif { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        public DateTime? SonGirisTarihi { get; set; }

        [MaxLength(500)]
        public string FotografYolu { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<Odunc> IslemYaptigiOduncler { get; set; } = new List<Odunc>();

        [NotMapped]
        public string TamAd => $"{Ad} {Soyad}";

        [NotMapped]
        public string RolMetni => Rol switch
        {
            KullaniciRolu.Admin => "👑 Sistem Yöneticisi",
            KullaniciRolu.Kutuphaneci => "📚 Kütüphaneci",
            KullaniciRolu.YardimciPersonel => "👤 Yardımcı Personel",
            _ => "Bilinmiyor"
        };
    }

    public enum KullaniciRolu
    {
        YardimciPersonel = 0,
        Kutuphaneci = 1,
        Admin = 2
    }

    /// <summary>
    /// Sistem ayarlarını temsil eden entity.
    /// Singleton pattern ile yönetilir.
    /// </summary>
    public class SistemAyarlari
    {
        [Key]
        public int Id { get; set; } = 1;

        [MaxLength(200)]
        public string KutuphaneAdi { get; set; } = "Üniversite Kütüphanesi";

        [MaxLength(500)]
        public string Adres { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Telefon { get; set; } = string.Empty;

        public int GunlukCezaTutari { get; set; } = 1; // TL

        public int MaxOduncSuresi { get; set; } = 14; // gün

        public int MaxOduncAdet { get; set; } = 3;

        public int MaxUzatmaSayisi { get; set; } = 2;

        public int RezervasyonGecerlilikSuresi { get; set; } = 3; // gün

        public bool OtomatikEmailBildirimi { get; set; } = true;

        public bool OtomatikSmsBildirimi { get; set; } = false;

        public int IadeHatirlatmaGunSayisi { get; set; } = 2; // iade tarihinden kaç gün önce hatırlat

        [MaxLength(200)]
        public string LogoYolu { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Tema { get; set; } = "Dark";
    }

    /// <summary>
    /// Sistem aktivite loglarını tutan entity.
    /// </summary>
    public class AktiviteLog
    {
        [Key]
        public int Id { get; set; }

        public int? KullaniciId { get; set; }

        [MaxLength(100)]
        public string Islem { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Detay { get; set; } = string.Empty;

        public DateTime Tarih { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string IPAdresi { get; set; } = string.Empty;

        public LogSeviyesi Seviye { get; set; } = LogSeviyesi.Bilgi;

        [ForeignKey("KullaniciId")]
        public virtual Kullanici? Kullanici { get; set; }
    }

    public enum LogSeviyesi
    {
        Bilgi = 0,
        Uyari = 1,
        Hata = 2,
        Kritik = 3
    }
}
