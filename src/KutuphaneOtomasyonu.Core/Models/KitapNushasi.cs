using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KutuphaneOtomasyonu.Models
{
    /// <summary>
    /// Bir kitabın fiziksel nüshası (kopyası). Barkod nüsha düzeyindedir; ödünç kaydı hangi nüshanın
    /// verildiğini <see cref="Odunc.NushaId"/> ile tutar.
    /// <para><see cref="Kitap.ToplamAdet"/> = Rafta + Oduncte nüsha sayısı; <see cref="Kitap.MevcutAdet"/> = Rafta nüsha sayısı.</para>
    /// </summary>
    public class KitapNushasi
    {
        [Key]
        public int Id { get; set; }

        public int KitapId { get; set; }

        [Required, MaxLength(30)]
        public string Barkod { get; set; } = string.Empty;

        public NushaDurumu Durum { get; set; } = NushaDurumu.Rafta;

        [MaxLength(50)]
        public string RafKonumu { get; set; } = string.Empty;

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

        public DateTime? DurumTarihi { get; set; }

        [ForeignKey(nameof(KitapId))]
        public virtual Kitap? Kitap { get; set; }

        [NotMapped]
        public bool DolasimdaMi => Durum is NushaDurumu.Rafta or NushaDurumu.Oduncte;

        [NotMapped]
        public string DurumMetni => Durum switch
        {
            NushaDurumu.Rafta => "Rafta",
            NushaDurumu.Oduncte => "Ödünçte",
            NushaDurumu.Hasarli => "Hasarlı (dolaşım dışı)",
            NushaDurumu.Kayip => "Kayıp",
            NushaDurumu.Cikarildi => "Envanterden çıkarıldı",
            _ => "Bilinmiyor"
        };
    }

    public enum NushaDurumu
    {
        Rafta = 0,
        Oduncte = 1,
        Hasarli = 2,
        Kayip = 3,
        Cikarildi = 4
    }
}
