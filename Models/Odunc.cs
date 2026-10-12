using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KutuphaneOtomasyonu.Models
{
    /// <summary>
    /// Kitap ödünç alma işlemini temsil eden entity sınıfı.
    /// Ödünç alma, iade ve gecikme bilgilerini içerir.
    /// </summary>
    public class Odunc
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KitapId { get; set; }

        [Required]
        public int UyeId { get; set; }

        public int? KutuphanecId { get; set; }

        public DateTime OduncTarihi { get; set; } = DateTime.Now;

        public DateTime PlanlananIadeTarihi { get; set; }

        public DateTime? GercekIadeTarihi { get; set; }

        public OduncDurumu Durumu { get; set; } = OduncDurumu.Aktif;

        public int UzatmaSayisi { get; set; } = 0;

        public int MaxUzatmaSayisi { get; set; } = 2;

        public decimal CezaTutari { get; set; } = 0;

        public bool CezaOdendi { get; set; } = false;

        [MaxLength(1000)]
        public string Notlar { get; set; } = string.Empty;

        [MaxLength(500)]
        public string IadeNotlari { get; set; } = string.Empty;

        public KitapDurumu IadedekiKitapDurumu { get; set; } = KitapDurumu.Iyi;

        // Navigation Properties
        [ForeignKey("KitapId")]
        public virtual Kitap? Kitap { get; set; }

        [ForeignKey("UyeId")]
        public virtual Uye? Uye { get; set; }

        [ForeignKey("KutuphanecId")]
        public virtual Kullanici? Kutuphaneci { get; set; }

        // Hesaplanan özellikler
        [NotMapped]
        public bool GecikmisMi => GercekIadeTarihi == null && DateTime.Now > PlanlananIadeTarihi;

        [NotMapped]
        public int GecikmeGunSayisi => GecikmisMi ? (int)(DateTime.Now - PlanlananIadeTarihi).TotalDays : 0;

        [NotMapped]
        public bool UzatilabilirMi => UzatmaSayisi < MaxUzatmaSayisi && !GecikmisMi && Durumu == OduncDurumu.Aktif;

        [NotMapped]
        public string DurumMetni => Durumu switch
        {
            OduncDurumu.Aktif => GecikmisMi ? $"⚠️ {GecikmeGunSayisi} Gün Gecikmiş" : "✅ Aktif",
            OduncDurumu.Iade => "✓ İade Edildi",
            OduncDurumu.Kayip => "❌ Kayıp",
            OduncDurumu.Hasarli => "⚠️ Hasarlı",
            _ => "Bilinmiyor"
        };
    }

    public enum OduncDurumu
    {
        Aktif = 0,
        Iade = 1,
        Kayip = 2,
        Hasarli = 3
    }

    public enum KitapDurumu
    {
        Mukemmel = 0,
        Iyi = 1,
        Orta = 2,
        Hasarli = 3,
        Kayip = 4
    }
}
