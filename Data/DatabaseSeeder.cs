using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KutuphaneOtomasyonu.Data
{
    /// <summary>
    /// Veritabanını başlangıç verilerle dolduran seeder sınıfı.
    /// Factory Method Pattern kullanır.
    /// </summary>
    public static class DatabaseSeeder
    {
        private static readonly Random _random = new Random(42);

        public static void Seed(KutuphaneDbContext context)
        {
            context.Database.EnsureCreated();

            // Sırayla seed et
            SeedSistemAyarlari(context);
            SeedKullanicilar(context);
            SeedKitaplar(context);
            SeedUyeler(context);
            SeedOduncler(context);
            SeedRezervasyonlar(context);
        }

        private static void SeedSistemAyarlari(KutuphaneDbContext context)
        {
            if (context.SistemAyarlari.Any()) return;

            context.SistemAyarlari.Add(new SistemAyarlari
            {
                Id = 1,
                KutuphaneAdi = "Teknoloji Üniversitesi Merkez Kütüphanesi",
                Adres = "Üniversite Caddesi No:1, Ankara",
                Email = "kutuphane@teknolojiü.edu.tr",
                Telefon = "0312 555 0000",
                GunlukCezaTutari = 2,
                MaxOduncSuresi = 14,
                MaxOduncAdet = 5,
                MaxUzatmaSayisi = 2,
                Tema = "Dark"
            });
            context.SaveChanges();
        }

        private static void SeedKullanicilar(KutuphaneDbContext context)
        {
            if (context.Kullanicilar.Any()) return;

            var kullanicilar = new List<Kullanici>
            {
                new Kullanici
                {
                    Ad = "Ahmet", Soyad = "Yıldız",
                    KullaniciAdi = "admin",
                    SifreHash = BCryptHash("admin123"),
                    Email = "admin@kutuphane.edu.tr",
                    Rol = KullaniciRolu.Admin,
                    Telefon = "0532 111 0001"
                },
                new Kullanici
                {
                    Ad = "Fatma", Soyad = "Kaya",
                    KullaniciAdi = "fatmak",
                    SifreHash = BCryptHash("123456"),
                    Email = "fatma@kutuphane.edu.tr",
                    Rol = KullaniciRolu.Kutuphaneci,
                    Telefon = "0532 111 0002"
                },
                new Kullanici
                {
                    Ad = "Mehmet", Soyad = "Demir",
                    KullaniciAdi = "mehmetd",
                    SifreHash = BCryptHash("123456"),
                    Email = "mehmet@kutuphane.edu.tr",
                    Rol = KullaniciRolu.Kutuphaneci,
                    Telefon = "0532 111 0003"
                }
            };

            context.Kullanicilar.AddRange(kullanicilar);
            context.SaveChanges();
        }

        private static void SeedKitaplar(KutuphaneDbContext context)
        {
            if (context.Kitaplar.Any()) return;

            var kategoriler = new[]
            {
                ("Bilgisayar Bilimleri", "Algoritmalar"),
                ("Bilgisayar Bilimleri", "Yazılım Mühendisliği"),
                ("Bilgisayar Bilimleri", "Yapay Zeka"),
                ("Matematik", "Lineer Cebir"),
                ("Matematik", "Diferansiyel Denklemler"),
                ("Fizik", "Kuantum Mekaniği"),
                ("Edebiyat", "Türk Edebiyatı"),
                ("Edebiyat", "Dünya Edebiyatı"),
                ("Tarih", "Osmanlı Tarihi"),
                ("Tarih", "Dünya Tarihi"),
                ("Felsefe", "Epistemoloji"),
                ("Psikoloji", "Bilişsel Psikoloji"),
                ("Ekonomi", "Makroekonomi"),
                ("Hukuk", "Medeni Hukuk"),
                ("Tıp", "Anatomi")
            };

            var kitaplar = new List<Kitap>();
            long isbn = 9780001000001L;

            var kitapVerileri = new[]
            {
                ("Algoritmalar ve Veri Yapıları", "Thomas H. Cormen", 2009, "Bilgisayar Bilimleri", "Algoritmalar", 1292, "MIT Press", 450.00m),
                ("Temiz Kod (Clean Code)", "Robert C. Martin", 2008, "Bilgisayar Bilimleri", "Yazılım Mühendisliği", 431, "Prentice Hall", 320.00m),
                ("Tasarım Desenleri (Design Patterns)", "Gang of Four", 1994, "Bilgisayar Bilimleri", "Yazılım Mühendisliği", 395, "Addison-Wesley", 380.00m),
                ("Yapay Zeka: Modern Bir Yaklaşım", "Stuart Russell", 2020, "Bilgisayar Bilimleri", "Yapay Zeka", 1132, "Pearson", 520.00m),
                ("Derin Öğrenme", "Ian Goodfellow", 2016, "Bilgisayar Bilimleri", "Yapay Zeka", 775, "MIT Press", 480.00m),
                ("Lineer Cebir ve Uygulamaları", "Gilbert Strang", 2016, "Matematik", "Lineer Cebir", 600, "Wellesley-Cambridge", 290.00m),
                ("Hesaplama Teorisi", "Michael Sipser", 2012, "Bilgisayar Bilimleri", "Algoritmalar", 504, "Cengage", 340.00m),
                ("İşletim Sistemleri", "Andrew Tanenbaum", 2014, "Bilgisayar Bilimleri", "Yazılım Mühendisliği", 1072, "Pearson", 420.00m),
                ("Bilgisayar Ağları", "James Kurose", 2016, "Bilgisayar Bilimleri", "Ağlar", 856, "Pearson", 390.00m),
                ("Veritabanı Yönetim Sistemleri", "Raghu Ramakrishnan", 2002, "Bilgisayar Bilimleri", "Veritabanı", 1132, "McGraw-Hill", 430.00m),
                ("Tutunamayanlar", "Oğuz Atay", 1972, "Edebiyat", "Türk Edebiyatı", 724, "İletişim", 85.00m),
                ("Şeker Portakalı", "José Mauro de Vasconcelos", 1968, "Edebiyat", "Dünya Edebiyatı", 192, "Can Yayınları", 65.00m),
                ("Suç ve Ceza", "Fyodor Dostoyevski", 1866, "Edebiyat", "Dünya Edebiyatı", 671, "İş Bankası Kültür", 90.00m),
                ("Savaş ve Barış", "Lev Tolstoy", 1869, "Edebiyat", "Dünya Edebiyatı", 1456, "Türkiye İş Bankası", 120.00m),
                ("Kürk Mantolu Madonna", "Sabahattin Ali", 1943, "Edebiyat", "Türk Edebiyatı", 168, "Yapı Kredi", 55.00m),
                ("İki Devlet", "Ziya Gökalp", 1923, "Tarih", "Türk Tarihi", 280, "Türk Tarih Kurumu", 75.00m),
                ("Osmanlı İmparatorluğu Tarihi", "Halil İnalcık", 1999, "Tarih", "Osmanlı Tarihi", 624, "Türkiye İş Bankası", 110.00m),
                ("Felsefeye Giriş", "Bertrand Russell", 1912, "Felsefe", "Genel Felsefe", 248, "Kabalcı", 80.00m),
                ("Psikoloji ve Yaşam", "Philip G. Zimbardo", 2011, "Psikoloji", "Genel Psikoloji", 864, "Pearson", 350.00m),
                ("Davranışsal Ekonomi", "Richard H. Thaler", 2015, "Ekonomi", "Davranış Ekonomisi", 368, "Penguin", 280.00m),
                ("C# ile Nesne Yönelimli Programlama", "Ahmet Yılmaz", 2023, "Bilgisayar Bilimleri", "Programlama", 580, "Seçkin", 195.00m),
                ("Python ile Makine Öğrenmesi", "Aurélien Géron", 2019, "Bilgisayar Bilimleri", "Yapay Zeka", 738, "O'Reilly", 410.00m),
                ("Web Geliştirme: HTML, CSS ve JavaScript", "Jon Duckett", 2014, "Bilgisayar Bilimleri", "Web Geliştirme", 604, "Wiley", 280.00m),
                ("Mikro İşlemciler ve Gömülü Sistemler", "Muhammad Ali Mazidi", 2016, "Elektrik-Elektronik", "Gömülü Sistemler", 912, "Pearson", 360.00m),
                ("Diferansiyel Denklemler", "William E. Boyce", 2017, "Matematik", "Diferansiyel Denklemler", 624, "Wiley", 320.00m),
                ("Olasılık ve İstatistik", "Sheldon M. Ross", 2018, "Matematik", "İstatistik", 840, "Elsevier", 340.00m),
                ("Yazılım Mimarisi: Desenlere Yönelik", "Frank Buschmann", 1996, "Bilgisayar Bilimleri", "Yazılım Mimarisi", 476, "Wiley", 380.00m),
                ("Refactoring", "Martin Fowler", 2018, "Bilgisayar Bilimleri", "Yazılım Mühendisliği", 448, "Addison-Wesley", 360.00m),
                ("The Pragmatic Programmer", "Andrew Hunt", 2019, "Bilgisayar Bilimleri", "Yazılım Mühendisliği", 352, "Addison-Wesley", 340.00m),
                ("Fizik İlkeleri", "David Halliday", 2013, "Fizik", "Genel Fizik", 1248, "Wiley", 450.00m),
            };

            var raflar = new[] { "A-01", "A-02", "B-01", "B-02", "C-01", "C-02", "D-01", "D-02" };

            foreach (var (baslik, yazar, yil, kat, altKat, sayfa, yayinevi, fiyat) in kitapVerileri)
            {
                var adet = _random.Next(1, 6);
                kitaplar.Add(new Kitap
                {
                    Baslik = baslik,
                    Yazar = yazar,
                    ISBN = $"978{isbn++:D10}",
                    YayinYili = yil,
                    Kategori = kat,
                    AltKategori = altKat,
                    SayfaSayisi = sayfa,
                    Yayinevi = yayinevi,
                    SatinAlmaFiyati = fiyat,
                    ToplamAdet = adet,
                    MevcutAdet = adet,
                    Dil = yil > 1950 && yayinevi.Contains("Wiley") || yayinevi.Contains("MIT") || yayinevi.Contains("O'Reilly") ? "İngilizce" : "Türkçe",
                    RafKonumu = raflar[_random.Next(raflar.Length)],
                    Puan = Math.Round(3.5 + _random.NextDouble() * 1.5, 1),
                    PuanSayisi = _random.Next(5, 150),
                    GoruntulemeSayisi = _random.Next(50, 2000),
                    EklenmeTarihi = DateTime.Now.AddDays(-_random.Next(100, 1000)),
                    Aciklama = $"{baslik} - {yazar} tarafından yazılmış, {kat} alanında kapsamlı bir kaynak eser."
                });
            }

            context.Kitaplar.AddRange(kitaplar);
            context.SaveChanges();
        }

        private static void SeedUyeler(KutuphaneDbContext context)
        {
            if (context.Uyeler.Any()) return;

            var adlar = new[] { "Ali", "Ayşe", "Mehmet", "Fatma", "Can", "Zeynep", "Emre", "Selin", "Burak", "Elif", "Kerem", "Merve", "Ozan", "Deniz", "Tolga", "Gizem" };
            var soyadlar = new[] { "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Arslan", "Doğan", "Aydın", "Erdoğan", "Özdemir", "Aslan", "Çetin", "Koç", "Kurt", "Güven" };
            var bolumler = new[] { "Bilgisayar Mühendisliği", "Elektrik-Elektronik Mühendisliği", "Makine Mühendisliği", "Endüstri Mühendisliği", "Matematik", "Fizik", "Kimya", "Biyoloji", "İşletme", "Hukuk" };

            var uyeler = new List<Uye>();
            for (int i = 0; i < 50; i++)
            {
                var ad = adlar[_random.Next(adlar.Length)];
                var soyad = soyadlar[_random.Next(soyadlar.Length)];
                var bolum = bolumler[_random.Next(bolumler.Length)];

                uyeler.Add(new Uye
                {
                    Ad = ad,
                    Soyad = soyad,
                    Email = $"{ad.ToLower()}.{soyad.ToLower()}{i}@ogrenci.edu.tr",
                    Telefon = $"05{_random.Next(30, 59)}{_random.Next(1000000, 9999999)}",
                    TCKimlikNo = $"{_random.Next(10000000, 99999999):D11}",
                    OgrenciNo = $"20{_random.Next(18, 26)}{_random.Next(100000, 999999)}",
                    Bolum = bolum,
                    Fakulte = bolum.Contains("Mühendislik") ? "Mühendislik Fakültesi" : "Fen-Edebiyat Fakültesi",
                    UyeTipi = i < 40 ? UyeTipi.Ogrenci : (i < 47 ? UyeTipi.Akademisyen : UyeTipi.Personel),
                    Durumu = i < 45 ? UyeDurumu.Aktif : (i < 48 ? UyeDurumu.Pasif : UyeDurumu.Askida),
                    KayitTarihi = DateTime.Now.AddDays(-_random.Next(30, 1500)),
                    MaxOduncAdet = i < 40 ? 3 : 5,
                    MaxOduncSuresi = i < 40 ? 14 : 30,
                    ToplamCeza = _random.Next(0, 5) == 0 ? _random.Next(5, 50) : 0
                });
            }

            context.Uyeler.AddRange(uyeler);
            context.SaveChanges();
        }

        private static void SeedOduncler(KutuphaneDbContext context)
        {
            if (context.Oduncler.Any()) return;

            var kitaplar = context.Kitaplar.ToList();
            var uyeler = context.Uyeler.Where(u => u.Durumu == UyeDurumu.Aktif).ToList();

            var oduncler = new List<Odunc>();

            // Geçmiş ödünçler (iade edilmiş)
            for (int i = 0; i < 100; i++)
            {
                var kitap = kitaplar[_random.Next(kitaplar.Count)];
                var uye = uyeler[_random.Next(uyeler.Count)];
                var oduncTarihi = DateTime.Now.AddDays(-_random.Next(60, 365));
                var planlanmis = oduncTarihi.AddDays(14);
                var gercekIade = planlanmis.AddDays(_random.Next(-5, 10));

                oduncler.Add(new Odunc
                {
                    KitapId = kitap.Id,
                    UyeId = uye.Id,
                    KutuphanecId = 1,
                    OduncTarihi = oduncTarihi,
                    PlanlananIadeTarihi = planlanmis,
                    GercekIadeTarihi = gercekIade,
                    Durumu = OduncDurumu.Iade,
                    CezaTutari = gercekIade > planlanmis ? (decimal)(gercekIade - planlanmis).TotalDays * 2 : 0
                });
            }

            // Aktif ödünçler
            for (int i = 0; i < 15; i++)
            {
                var kitap = kitaplar[_random.Next(kitaplar.Count)];
                if (kitap.MevcutAdet <= 0) continue;

                var uye = uyeler[_random.Next(uyeler.Count)];
                var oduncTarihi = DateTime.Now.AddDays(-_random.Next(1, 20));

                oduncler.Add(new Odunc
                {
                    KitapId = kitap.Id,
                    UyeId = uye.Id,
                    KutuphanecId = 1,
                    OduncTarihi = oduncTarihi,
                    PlanlananIadeTarihi = oduncTarihi.AddDays(14),
                    Durumu = OduncDurumu.Aktif
                });

                kitap.MevcutAdet--;
            }

            context.Oduncler.AddRange(oduncler);
            context.SaveChanges();
        }

        private static void SeedRezervasyonlar(KutuphaneDbContext context)
        {
            if (context.Rezervasyonlar.Any()) return;

            var kitaplar = context.Kitaplar.Where(k => k.MevcutAdet == 0).ToList();
            var uyeler = context.Uyeler.Where(u => u.Durumu == UyeDurumu.Aktif).ToList();

            if (!kitaplar.Any() || !uyeler.Any()) return;

            for (int i = 0; i < 5; i++)
            {
                if (i >= kitaplar.Count || i >= uyeler.Count) break;
                context.Rezervasyonlar.Add(new Rezervasyon
                {
                    KitapId = kitaplar[i].Id,
                    UyeId = uyeler[i].Id,
                    RezervasyonTarihi = DateTime.Now.AddDays(-_random.Next(1, 7)),
                    GecerlilikTarihi = DateTime.Now.AddDays(3),
                    Durumu = RezervasyonDurumu.Bekliyor,
                    SiraNumarasi = 1
                });
            }

            context.SaveChanges();
        }

        private static string BCryptHash(string password)
        {
            // Basit hash - production'da gerçek BCrypt kullanılmalı
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(password + "KutuphaneOtomasyonu_Salt_2024")
                )
            );
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCryptHash(password) == hash;
        }
    }
}
