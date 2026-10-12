using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KutuphaneOtomasyonu.Models
{
    /// <summary>
    /// Kütüphanedeki bir kitabı temsil eden entity sınıfı.
    /// </summary>
    public class Kitap
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Baslik { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Yazar { get; set; } = string.Empty;

        [MaxLength(20)]
        public string ISBN { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Yayinevi { get; set; } = string.Empty;

        public int YayinYili { get; set; }

        [MaxLength(100)]
        public string Kategori { get; set; } = string.Empty;

        [MaxLength(100)]
        public string AltKategori { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Dil { get; set; } = "Türkçe";

        public int SayfaSayisi { get; set; }

        public int ToplamAdet { get; set; } = 1;

        public int MevcutAdet { get; set; } = 1;

        [MaxLength(2000)]
        public string Aciklama { get; set; } = string.Empty;

        [MaxLength(500)]
        public string KapakResimYolu { get; set; } = string.Empty;

        [MaxLength(50)]
        public string RafKonumu { get; set; } = string.Empty;

        public decimal SatinAlmaFiyati { get; set; }

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

        public DateTime? GuncellenmeTarihi { get; set; }

        public bool Aktif { get; set; } = true;

        public double Puan { get; set; } = 0.0;

        public int PuanSayisi { get; set; } = 0;

        public int GoruntulemeSayisi { get; set; } = 0;

        // Navigation Properties
        public virtual ICollection<Odunc> Oduncler { get; set; } = new List<Odunc>();
        public virtual ICollection<Rezervasyon> Rezervasyonlar { get; set; } = new List<Rezervasyon>();
        public virtual ICollection<KitapYorum> Yorumlar { get; set; } = new List<KitapYorum>();
        public virtual ICollection<Favori> Favoriler { get; set; } = new List<Favori>();

        // Hesaplanan özellikler
        [NotMapped]
        public bool MevcutMu => MevcutAdet > 0;

        [NotMapped]
        public string PuanGoster => PuanSayisi > 0 ? $"{Puan:F1}/5.0 ({PuanSayisi} değerlendirme)" : "Henüz değerlendirilmemiş";

        [NotMapped]
        public string DurumMetni => MevcutAdet > 0 ? $"{MevcutAdet}/{ToplamAdet} Mevcut" : "Tüm Kopyalar Ödünçte";
    }
}
