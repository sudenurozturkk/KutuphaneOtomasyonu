using System;

namespace KutuphaneOtomasyonu.Core
{
    /// <summary>
    /// Abstract Materyal Sınıfı (Kitap, E-Kitap, Dergi, Tez)
    /// </summary>
    public abstract class Materyal
    {
        public int Id { get; set; }
        public string Isbn { get; set; }
        public string Baslik { get; set; }
        public string Yazar { get; set; }
        public string Yayinevi { get; set; }
        public int YayinYili { get; set; }
        public string Kategori { get; set; }
        public int ToplamStok { get; set; }
        public int MevcutStok { get; set; }
        public int OduncSayisi { get; set; } = 0;
        public string Ozet { get; set; }

        protected Materyal(int id, string isbn, string baslik, string yazar, string yayinevi, int yayinYili, string kategori, int toplamStok, string ozet = "")
        {
            Id = id;
            Isbn = isbn;
            Baslik = baslik;
            Yazar = yazar;
            Yayinevi = yayinevi;
            YayinYili = yayinYili;
            Kategori = kategori;
            ToplamStok = toplamStok;
            MevcutStok = toplamStok;
            Ozet = ozet;
        }

        public virtual bool MevcutMu() => MevcutStok > 0;

        public virtual bool StokDusur()
        {
            if (MevcutStok > 0)
            {
                MevcutStok--;
                OduncSayisi++;
                return true;
            }
            return false;
        }

        public virtual bool StokArttir()
        {
            if (MevcutStok < ToplamStok)
            {
                MevcutStok++;
                return true;
            }
            return false;
        }

        public abstract string GetMateryalTuru();
        public override string ToString() => $"{Baslik} ({GetMateryalTuru()}) - Stok: {MevcutStok}/{ToplamStok}";
    }

    public class Kitap : Materyal
    {
        public int SayfaSayisi { get; set; }
        public string KapakTipi { get; set; }
        public RafKonumu Konum { get; set; } // Kompozisyon ilişkisi

        public Kitap(int id, string isbn, string baslik, string yazar, string yayinevi, int yayinYili, string kategori, int toplamStok, int sayfaSayisi, string kapakTipi, RafKonumu konum, string ozet = "")
            : base(id, isbn, baslik, yazar, yayinevi, yayinYili, kategori, toplamStok, ozet)
        {
            SayfaSayisi = sayfaSayisi;
            KapakTipi = kapakTipi;
            Konum = konum ?? new RafKonumu();
        }

        public override string GetMateryalTuru() => "Basılı Kitap";
    }

    public class EKitap : Materyal
    {
        public double DosyaBoyutuMB { get; set; }
        public string Format { get; set; } // PDF, EPUB

        public EKitap(int id, string isbn, string baslik, string yazar, string yayinevi, int yayinYili, string kategori, double dosyaBoyutuMB, string format = "PDF", string ozet = "")
            : base(id, isbn, baslik, yazar, yayinevi, yayinYili, kategori, 9999, ozet)
        {
            DosyaBoyutuMB = dosyaBoyutuMB;
            Format = format;
        }

        public override bool MevcutMu() => true; // E-Kitap fiziksel tükenmez
        public override string GetMateryalTuru() => "E-Kitap (Dijital)";
    }

    public class Dergi : Materyal
    {
        public string Issn { get; set; }
        public int SayiNo { get; set; }
        public string Periyot { get; set; } // Aylık, 3 Aylık

        public Dergi(int id, string issn, string baslik, string yayinevi, int yayinYili, string kategori, int sayiNo, string periyot, int toplamStok, string ozet = "")
            : base(id, issn, baslik, "Editör Heyeti", yayinevi, yayinYili, kategori, toplamStok, ozet)
        {
            Issn = issn;
            SayiNo = sayiNo;
            Periyot = periyot;
        }

        public override string GetMateryalTuru() => "Süreli Yayın / Dergi";
    }

    public class Tez : Materyal
    {
        public string Danisman { get; set; }
        public string Universite { get; set; }
        public string AnabilimDali { get; set; }
        public string Derece { get; set; } // Yüksek Lisans, Doktora

        public Tez(int id, string baslik, string yazar, string danisman, string universite, string anabilimDali, string derece, int yayinYili, string ozet = "")
            : base(id, $"THS-{id}", baslik, yazar, universite, yayinYili, "Akademik Tez", 1, ozet)
        {
            Danisman = danisman;
            Universite = universite;
            AnabilimDali = anabilimDali;
            Derece = derece;
        }

        public override string GetMateryalTuru() => "Akademik Tez";
    }
}
