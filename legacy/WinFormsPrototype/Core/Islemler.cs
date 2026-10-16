using System;

namespace KutuphaneOtomasyonu.Core
{
    /// <summary>
    /// Ödünç Alma İşlemi (Çokluklar: Kullanici "1" -> "0..*" OduncIslemi, Materyal "1" -> "0..*" OduncIslemi)
    /// </summary>
    public class OduncIslemi
    {
        public int IslemId { get; set; }
        public int KullaniciId { get; set; }
        public int MateryalId { get; set; }
        public DateTime AlisTarihi { get; set; }
        public DateTime SonTeslimTarihi { get; set; }
        public DateTime? GerceklesenIadeTarihi { get; set; }
        public string Durum { get; set; } = "AKTIF"; // AKTIF, IADE_EDILDI, GECIKTI
        public int UzatmaSayisi { get; set; } = 0;
        public int MaksimumUzatma { get; set; } = 2;

        public OduncIslemi(int islemId, int kullaniciId, int materyalId, DateTime alisTarihi, int sureGun)
        {
            IslemId = islemId;
            KullaniciId = kullaniciId;
            MateryalId = materyalId;
            AlisTarihi = alisTarihi;
            SonTeslimTarihi = alisTarihi.AddDays(sureGun);
        }

        public bool GeciktiMi()
        {
            if (Durum == "IADE_EDILDI") return false;
            return DateTime.Now > SonTeslimTarihi;
        }

        public int GecikmeGunu()
        {
            DateTime karsilastir = GerceklesenIadeTarihi ?? DateTime.Now;
            if (karsilastir <= SonTeslimTarihi) return 0;
            return (int)Math.Ceiling((karsilastir - SonTeslimTarihi).TotalDays);
        }

        public bool SureUzat(int ekGun = 7)
        {
            if (UzatmaSayisi >= MaksimumUzatma)
                throw new InvalidOperationException($"Maksimum uzatma sınırına ({MaksimumUzatma} kez) ulaşıldı!");

            if (GeciktiMi())
                throw new InvalidOperationException("Gecikmiş kitapların süresi uzatılamaz!");

            SonTeslimTarihi = SonTeslimTarihi.AddDays(ekGun);
            UzatmaSayisi++;
            return true;
        }

        public void IadeEt()
        {
            GerceklesenIadeTarihi = DateTime.Now;
            Durum = "IADE_EDILDI";
        }
    }

    /// <summary>
    /// Rezervasyon Kaydı
    /// </summary>
    public class Rezervasyon
    {
        public int RezervasyonId { get; set; }
        public int KullaniciId { get; set; }
        public int MateryalId { get; set; }
        public DateTime TalepTarihi { get; set; }
        public string Durum { get; set; } = "BEKLEMEDE"; // BEKLEMEDE, BILDIRILDI, TAMAMLANDI, IPTAL

        public Rezervasyon(int id, int kullaniciId, int materyalId)
        {
            RezervasyonId = id;
            KullaniciId = kullaniciId;
            MateryalId = materyalId;
            TalepTarihi = DateTime.Now;
        }
    }

    /// <summary>
    /// Ceza Kaydı (Çokluk: OduncIslemi "1" -> "0..1" CezaKaydi)
    /// </summary>
    public class CezaKaydi
    {
        public int CezaId { get; set; }
        public int IslemId { get; set; }
        public int KullaniciId { get; set; }
        public decimal Tutar { get; set; }
        public int GecikmeGunu { get; set; }
        public bool OdendiMi { get; set; } = false;
        public DateTime OlusmaTarihi { get; set; } = DateTime.Now;
        public string MakbuzNo { get; set; }

        public CezaKaydi(int cezaId, int islemId, int kullaniciId, decimal tutar, int gecikmeGunu)
        {
            CezaId = cezaId;
            IslemId = islemId;
            KullaniciId = kullaniciId;
            Tutar = tutar;
            GecikmeGunu = gecikmeGunu;
            MakbuzNo = $"DEKONT-{cezaId}-{new Random().Next(10000, 99999)}";
        }

        public void Ode()
        {
            OdendiMi = true;
        }
    }
}
