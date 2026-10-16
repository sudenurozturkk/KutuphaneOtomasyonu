using System;
using System.Collections.Generic;
using KutuphaneOtomasyonu.Core;

namespace KutuphaneOtomasyonu.Patterns
{
    #region 1. STRATEGY PATTERN (CEZA VE SÜRE HESAPLAMA STRATEJİSİ)
    public interface ICezaStratejisi
    {
        decimal CezaHesapla(int gecikmeGunu);
    }

    public class OgrenciCezaStratejisi : ICezaStratejisi
    {
        public decimal CezaHesapla(int gecikmeGunu)
        {
            if (gecikmeGunu <= 0) return 0;
            return gecikmeGunu * 5.0m; // 5 TL / gün
        }
    }

    public class AkademisyenCezaStratejisi : ICezaStratejisi
    {
        public decimal CezaHesapla(int gecikmeGunu)
        {
            if (gecikmeGunu <= 0) return 0;
            return gecikmeGunu * 2.0m; // 2 TL / gün
        }
    }

    public class PersonelCezaStratejisi : ICezaStratejisi
    {
        public decimal CezaHesapla(int gecikmeGunu) => 0.0m; // Muaf
    }

    public class CezaContext
    {
        private ICezaStratejisi _strateji;

        public CezaContext(ICezaStratejisi strateji)
        {
            _strateji = strateji;
        }

        public void StratejiDegistir(ICezaStratejisi yeniStrateji)
        {
            _strateji = yeniStrateji;
        }

        public decimal HesabiCalistir(int gecikmeGunu)
        {
            return _strateji.CezaHesapla(gecikmeGunu);
        }
    }
    #endregion

    #region 2. OBSERVER PATTERN (REZERVASYON VE STOK TAKİPÇİSİ)
    public interface IKitapObserver
    {
        void Guncelle(string olay, Materyal materyal);
    }

    public class KullaniciObserver : IKitapObserver
    {
        public Kullanici Kullanici { get; }
        public List<string> Bildirimler { get; } = new List<string>();

        public KullaniciObserver(Kullanici kullanici)
        {
            Kullanici = kullanici;
        }

        public void Guncelle(string olay, Materyal materyal)
        {
            string mesaj = $"[Bildirim] Sn. {Kullanici.AdSoyad}, beklediğiniz '{materyal.Baslik}' kitabı iade edildi! Sizin için 48 saat ayrıldı.";
            Bildirimler.Add(mesaj);
        }
    }

    public class KitapSubject
    {
        private readonly Dictionary<int, List<IKitapObserver>> _gozlemciler = new Dictionary<int, List<IKitapObserver>>();

        public void AboneOl(int materyalId, IKitapObserver gozlemci)
        {
            if (!_gozlemciler.ContainsKey(materyalId))
                _gozlemciler[materyalId] = new List<IKitapObserver>();

            _gozlemciler[materyalId].Add(gozlemci);
        }

        public void Bildir(int materyalId, Materyal materyal)
        {
            if (_gozlemciler.ContainsKey(materyalId))
            {
                foreach (var obs in _gozlemciler[materyalId])
                {
                    obs.Guncelle("KITAP_MUSAIT", materyal);
                }
            }
        }
    }
    #endregion

    #region 3. FACTORY METHOD PATTERN (MATERYAL FABRİKASI)
    public static class MateryalFabrikasi
    {
        public static Materyal MateryalUret(
            string tur, 
            int id, 
            string isbn, 
            string baslik, 
            string yazar, 
            string yayinevi, 
            int yil, 
            string kategori, 
            int stok, 
            string ozet = "",
            int sayfaSayisi = 200,
            string kapakTipi = "Karton Kapak",
            RafKonumu konum = null,
            double dosyaBoyutu = 10.0,
            string format = "PDF",
            int sayiNo = 1,
            string periyot = "Aylık",
            string danisman = "",
            string universite = "",
            string derece = "Doktora")
        {
            switch (tur.ToUpper())
            {
                case "KITAP":
                case "BOOK":
                    return new Kitap(id, isbn, baslik, yazar, yayinevi, yil, kategori, stok, sayfaSayisi, kapakTipi, konum ?? new RafKonumu(), ozet);

                case "EKITAP":
                case "EBOOK":
                    return new EKitap(id, isbn, baslik, yazar, yayinevi, yil, kategori, dosyaBoyutu, format, ozet);

                case "DERGI":
                case "JOURNAL":
                    return new Dergi(id, isbn, baslik, yayinevi, yil, kategori, sayiNo, periyot, stok, ozet);

                case "TEZ":
                case "THESIS":
                    return new Tez(id, baslik, yazar, danisman, universite, kategori, derece, yil, ozet);

                default:
                    throw new ArgumentException($"Geçersiz Materyal Türü: {tur}");
            }
        }
    }
    #endregion

    #region 4. SINGLETON PATTERN (MERKEZİ VERİ VE DURUM DEPOSU)
    public class KutuphaneVeriDeposu
    {
        private static KutuphaneVeriDeposu _ornek;
        private static readonly object _kilit = new object();

        public List<Kullanici> Kullanicilar { get; } = new List<Kullanici>();
        public List<Materyal> Materyaller { get; } = new List<Materyal>();
        public List<OduncIslemi> Islemler { get; } = new List<OduncIslemi>();
        public List<Rezervasyon> Rezervasyonlar { get; } = new List<Rezervasyon>();
        public List<CezaKaydi> Cezalar { get; } = new List<CezaKaydi>();
        public KitapSubject BildirimMotoru { get; } = new KitapSubject();

        private KutuphaneVeriDeposu()
        {
            BaslangicVerileriniYukle();
        }

        public static KutuphaneVeriDeposu Ornek
        {
            get
            {
                if (_ornek == null)
                {
                    lock (_kilit)
                    {
                        if (_ornek == null)
                        {
                            _ornek = new KutuphaneVeriDeposu();
                        }
                    }
                }
                return _ornek;
            }
        }

        private void BaslangicVerileriniYukle()
        {
            // Kullanıcılar
            var o1 = new Ogrenci(1, "12345678901", "Ahmet Yılmaz", "ahmet@ogr.edu.tr", "0532 111 2233", "2024105021", "Bilgisayar Mühendisliği", 3, "Lisans");
            o1.Bakiye = 50.0m;
            var o2 = new Ogrenci(2, "98765432109", "Zeynep Kaya", "zeynep@ogr.edu.tr", "0544 222 3344", "2023108044", "Yazılım Mühendisliği", 4, "Yüksek Lisans");
            o2.Bakiye = 100.0m;
            var a1 = new Akademisyen(3, "11223344556", "Doç. Dr. Mehmet Demir", "mehmet.demir@edu.tr", "0505 444 5566", "Doç. Dr.", "Mühendislik Fakültesi", "B-204");
            a1.Bakiye = 200.0m;
            var k1 = new Kutuphaneci(4, "99887766554", "Ayşe Yurt", "ayse.yurt@lib.edu.tr", "0530 999 8877", "LIB-8041", "Gündüz");

            Kullanicilar.AddRange(new Kullanici[] { o1, o2, a1, k1 });

            // Materyaller (MateryalFabrikasi ile üretilir)
            var m1 = MateryalFabrikasi.MateryalUret("KITAP", 101, "978-0132350884", "Clean Code (Temiz Kod)", "Robert C. Martin", "Prentice Hall", 2008, "Yazılım Mühendisliği", 4, "Çevik yazılım ustalığı el kitabı.", 464, "Karton Kapak", new RafKonumu("Merkez Kütüphane", 2, "C-4", "12"));
            var m2 = MateryalFabrikasi.MateryalUret("KITAP", 102, "978-0201633610", "Design Patterns (Tasarım Desenleri)", "GoF", "Addison-Wesley", 1994, "Yazılım Mimarisi", 3, "Nesne yönelimli yazılım tasarımının efsanevi desenleri.", 395, "Ciltli", new RafKonumu("Merkez Kütüphane", 2, "C-4", "14"));
            var m3 = MateryalFabrikasi.MateryalUret("KITAP", 103, "978-0134757599", "Refactoring", "Martin Fowler", "Addison-Wesley", 2018, "Yazılım Mimarisi", 2, "Mevcut kodun tasarımını iyileştirme.", 448, "Karton Kapak", new RafKonumu("Merkez Kütüphane", 2, "C-5", "03"));
            var m4 = MateryalFabrikasi.MateryalUret("KITAP", 104, "978-0262033848", "Introduction to Algorithms", "CLRS", "MIT Press", 2009, "Algoritmalar", 5, "Dünya standardı algoritma referans kitabı.", 1312, "Ciltli", new RafKonumu("Merkez Kütüphane", 1, "A-2", "01"));
            var m5 = MateryalFabrikasi.MateryalUret("EKITAP", 201, "978-1491950357", "Building Microservices", "Sam Newman", "O'Reilly", 2021, "Bulut Mimarisi", 999, "Mikroservis sistemleri el kitabı.", dosyaBoyutu: 14.5, format: "PDF");
            var m6 = MateryalFabrikasi.MateryalUret("DERGI", 301, "0098-5589", "IEEE Trans. on Software Engineering", "", "IEEE", 2026, "Akademik Dergi", 2, "Yazılım mühendisliği hakemli dergisi.", sayiNo: 12, periyot: "Aylık");
            var m7 = MateryalFabrikasi.MateryalUret("TEZ", 401, "", "Dağıtık Mimarilerde Hata Toleransı", "Ali Kemal Çelik", "", 2025, "Akademik Tez", 1, "Doktora tezi.", danisman: "Prof. Dr. Ahmet Turan", universite: "İTÜ", derece: "Doktora");

            Materyaller.AddRange(new Materyal[] { m1, m2, m3, m4, m5, m6, m7 });

            // Örnek Başlangıç İşlemleri
            var islem1 = new OduncIslemi(1001, 1, 101, DateTime.Now.AddDays(-5), 15);
            var islem2 = new OduncIslemi(1002, 1, 104, DateTime.Now.AddDays(-20), 15); // Gecikmiş!
            islem2.Durum = "GECIKTI";

            Islemler.AddRange(new OduncIslemi[] { islem1, islem2 });
            m1.StokDusur();
            m4.StokDusur();

            // Örnek Ceza (5 gün gecikme: 5 * 5 = 25 TL)
            var ceza1 = new CezaKaydi(5001, 1002, 1, 25.0m, 5);
            Cezalar.Add(ceza1);
            o1.CezaBorcu = 25.0m;
        }
    }
    #endregion
}
