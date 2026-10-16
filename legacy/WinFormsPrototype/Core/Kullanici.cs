using System;

namespace KutuphaneOtomasyonu.Core
{
    /// <summary>
    /// Abstract Kullanici Sınıfı (OOP Kalıtım & Polimorfizm Temeli)
    /// </summary>
    public abstract class Kullanici
    {
        public int Id { get; set; }
        public string TcNo { get; set; }
        public string AdSoyad { get; set; }
        public string Eposta { get; set; }
        public string Telefon { get; set; }
        public string Rol { get; protected set; }
        public string Barkod { get; set; }
        public decimal Bakiye { get; set; } = 0.0m;
        public decimal CezaBorcu { get; set; } = 0.0m;
        public int ToplamOduncSayisi { get; set; } = 0;

        protected Kullanici(int id, string tcNo, string adSoyad, string eposta, string telefon, string rol)
        {
            Id = id;
            TcNo = tcNo;
            AdSoyad = adSoyad;
            Eposta = eposta;
            Telefon = telefon;
            Rol = rol;
            Barkod = $"USR-{id}-{new Random().Next(1000, 9999)}";
        }

        // Polimorfik Metotlar
        public abstract int GetMaksimumOduncSayisi();
        public abstract int GetOduncSuresiGun();
        public abstract decimal GetGunlukCezaKatsayisi();

        public virtual bool OduncAlabilirMi()
        {
            return CezaBorcu < 50.0m; // 50 TL üzeri borcu olan ödünç alamaz
        }

        public void BakiyeYukle(decimal tutar)
        {
            if (tutar > 0)
                Bakiye += tutar;
        }

        public bool BorcOde(decimal tutar)
        {
            if (tutar > 0 && Bakiye >= tutar)
            {
                Bakiye -= tutar;
                CezaBorcu = Math.Max(0, CezaBorcu - tutar);
                return true;
            }
            return false;
        }

        public override string ToString() => $"{AdSoyad} ({Rol})";
    }

    public class Ogrenci : Kullanici
    {
        public string OgrenciNo { get; set; }
        public string Bolum { get; set; }
        public int Sinif { get; set; }
        public string EgitimSeviyesi { get; set; } // Lisans, Yüksek Lisans, Doktora

        public Ogrenci(int id, string tcNo, string adSoyad, string eposta, string telefon, string ogrenciNo, string bolum, int sinif, string egitimSeviyesi = "Lisans")
            : base(id, tcNo, adSoyad, eposta, telefon, "Ogrenci")
        {
            OgrenciNo = ogrenciNo;
            Bolum = bolum;
            Sinif = sinif;
            EgitimSeviyesi = egitimSeviyesi;
        }

        public override int GetMaksimumOduncSayisi()
        {
            if (EgitimSeviyesi == "Doktora") return 8;
            if (EgitimSeviyesi == "Yüksek Lisans") return 6;
            return 4; // Lisans
        }

        public override int GetOduncSuresiGun()
        {
            if (EgitimSeviyesi == "Doktora") return 28;
            if (EgitimSeviyesi == "Yüksek Lisans") return 21;
            return 15;
        }

        public override decimal GetGunlukCezaKatsayisi() => 5.0m; // Öğrenci için 5 TL / gün
    }

    public class Akademisyen : Kullanici
    {
        public string Unvan { get; set; } // Prof. Dr., Doç. Dr., Dr. Öğr. Üyesi
        public string Fakulte { get; set; }
        public string OdaNo { get; set; }

        public Akademisyen(int id, string tcNo, string adSoyad, string eposta, string telefon, string unvan, string fakulte, string odaNo)
            : base(id, tcNo, adSoyad, eposta, telefon, "Akademisyen")
        {
            Unvan = unvan;
            Fakulte = fakulte;
            OdaNo = odaNo;
        }

        public override int GetMaksimumOduncSayisi() => 15;
        public override int GetOduncSuresiGun() => 45;
        public override decimal GetGunlukCezaKatsayisi() => 2.0m; // Akademisyen için 2 TL / gün
    }

    public class Kutuphaneci : Kullanici
    {
        public string PersonelNo { get; set; }
        public string Vardiya { get; set; }

        public Kutuphaneci(int id, string tcNo, string adSoyad, string eposta, string telefon, string personelNo, string vardiya = "Gündüz")
            : base(id, tcNo, adSoyad, eposta, telefon, "Kutuphaneci")
        {
            PersonelNo = personelNo;
            Vardiya = vardiya;
        }

        public override int GetMaksimumOduncSayisi() => 10;
        public override int GetOduncSuresiGun() => 30;
        public override decimal GetGunlukCezaKatsayisi() => 0.0m; // Personel muaf
    }
}
