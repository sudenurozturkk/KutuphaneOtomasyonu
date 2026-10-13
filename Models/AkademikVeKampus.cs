using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KutuphaneOtomasyonu.Models
{
    public enum YayinTuru
    {
        DoktoraTezi = 0,
        YuksekLisansTezi = 1,
        HakemliMakale = 2,
        KonferansBildirisi = 3,
        ArastirmaRaporu = 4
    }

    public enum KampusLokasyonu
    {
        MerkezKutuphane = 0,
        MuhendislikKutuphanesi = 1,
        TipSaglikKutuphanesi = 2,
        SosyalBilimlerKutuphanesi = 3
    }

    public enum TransferDurumu
    {
        TalepEdildi = 0,
        Kuryede = 1,
        HedefKutuphanede = 2,
        TeslimEdildi = 3,
        Iptal = 4
    }

    /// <summary>
    /// Dünya üniversiteleri standardında akademik araştırma yayını (Tez, Makale, Bildiri).
    /// </summary>
    public class AkademikYayin
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Baslik { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Yazar { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Danisman { get; set; }

        [Required]
        public YayinTuru Tur { get; set; } = YayinTuru.YuksekLisansTezi;

        [MaxLength(150)]
        public string Universite { get; set; } = "İstanbul Teknik Üniversitesi & Global Consortium";

        [MaxLength(150)]
        public string EnstituFakulte { get; set; } = "Fen Bilimleri Enstitüsü";

        [MaxLength(100)]
        public string AnabilimDali { get; set; } = "Bilgisayar Mühendisliği";

        public int Yil { get; set; } = DateTime.Now.Year;

        [MaxLength(100)]
        public string? DOI { get; set; } // Örn: 10.1145/1234567.890

        [MaxLength(500)]
        public string AnahtarKelimeler { get; set; } = string.Empty;

        public string Ozet { get; set; } = string.Empty;

        public KampusLokasyonu MevcutKampus { get; set; } = KampusLokasyonu.MerkezKutuphane;

        public int AlintiSayisi { get; set; } = 0;

        public int IndirmeSayisi { get; set; } = 0;

        [MaxLength(50)]
        public string Dil { get; set; } = "İngilizce";

        public string DosyaBoyutu { get; set; } = "4.2 MB (PDF)";

        // Citation üreticileri (APA, MLA, Harvard, BibTeX)
        [NotMapped]
        public string ApaCitation => $"{Yazar} ({Yil}). {Baslik}. {Universite}, {EnstituFakulte}. {(string.IsNullOrWhiteSpace(DOI) ? "" : $"https://doi.org/{DOI}")}";

        [NotMapped]
        public string MlaCitation => $"{Yazar}. \"{Baslik}.\" {Universite}, {Yil}. {(string.IsNullOrWhiteSpace(DOI) ? "" : $"DOI: {DOI}")}";

        [NotMapped]
        public string BibTexCitation => $"@thesis{{{Yazar.Split(' ')[^1].ToLower()}{Yil},\n  author = {{{Yazar}}},\n  title = {{{Baslik}}},\n  year = {{{Yil}}},\n  school = {{{Universite}}},\n  doi = {{{DOI ?? "N/A"}}}\n}}";
    }

    /// <summary>
    /// Kütüphaneler ve Kampüsler Arası Ödünç Transferi (Inter-Library Loan - ILL).
    /// </summary>
    public class MateryalTransfer
    {
        [Key]
        public int Id { get; set; }

        public int? KitapId { get; set; }
        public int? AkademikYayinId { get; set; }

        public int TalepEdenUyeId { get; set; }

        public KampusLokasyonu KaynakKampus { get; set; }
        public KampusLokasyonu HedefKampus { get; set; }

        public DateTime TalepTarihi { get; set; } = DateTime.Now;
        public DateTime? TahminiVarisTarihi { get; set; } = DateTime.Now.AddHours(24);

        public TransferDurumu Durum { get; set; } = TransferDurumu.TalepEdildi;

        [MaxLength(100)]
        public string KuryeTakipKodu { get; set; } = $"ILL-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        // Navigation
        [ForeignKey("KitapId")]
        public virtual Kitap? Kitap { get; set; }

        [ForeignKey("TalepEdenUyeId")]
        public virtual Uye? TalepEdenUye { get; set; }
    }
}
