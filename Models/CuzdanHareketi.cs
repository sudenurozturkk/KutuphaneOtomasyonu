using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KutuphaneOtomasyonu.Models
{
    /// <summary>
    /// Üye cüzdanı hareket defteri. Bakiye ayrı bir alanda tutulmaz; hareketlerin toplamıdır
    /// (yükleme pozitif, harcama negatif). Hareketler değiştirilmez veya silinmez.
    /// </summary>
    public class CuzdanHareketi
    {
        [Key]
        public int Id { get; set; }

        public int UyeId { get; set; }

        public decimal Tutar { get; set; }

        public CuzdanHareketTipi Tip { get; set; }

        [MaxLength(300)]
        public string Aciklama { get; set; } = string.Empty;

        public DateTime Tarih { get; set; } = DateTime.Now;

        /// <summary>İşlemi yapan personel.</summary>
        public int? KullaniciId { get; set; }

        [ForeignKey(nameof(UyeId))]
        public virtual Uye? Uye { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        public virtual Kullanici? Kullanici { get; set; }
    }

    public enum CuzdanHareketTipi
    {
        /// <summary>Bankoda alınan nakit/kart ile bakiye yükleme.</summary>
        Yukleme = 0,
        /// <summary>Bakiyeden ceza borcu ödemesi.</summary>
        CezaOdeme = 1
    }
}
