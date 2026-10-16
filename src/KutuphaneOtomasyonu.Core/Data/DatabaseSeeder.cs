using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KutuphaneOtomasyonu.Data
{
    /// <summary>
    /// Veritabanını başlangıç verilerle dolduran seeder sınıfı.
    /// Şema işlemleri <see cref="VeritabaniSemasi"/> içindedir.
    /// </summary>
    public static class DatabaseSeeder
    {
        private static readonly Random _random = new Random(42);

        /// <summary>Demo öğrenci portalı hesabı (README'de belgelenmiştir).</summary>
        public const string DemoOgrenciNo = "2026000001";
        public const string DemoOgrenciEposta = "ogrenci@demo.edu.tr";
        public const string DemoOgrenciSifresi = "ogrenci123";

        /// <summary>Şemayı hazırlar (migration'lar) ve veritabanı boşsa demo verisini yükler.</summary>
        public static void Seed(KutuphaneDbContext context)
        {
            VeritabaniSemasi.Hazirla(context);
            DemoVerisiYukle(context);
        }

        /// <summary>
        /// Eksik demo verisini yükler; şemaya dokunmaz. Her tablo yalnızca boşsa doldurulur,
        /// bu yüzden açık bir transaction içinde güvenle çağrılabilir.
        /// </summary>
        public static void DemoVerisiYukle(KutuphaneDbContext context)
        {
            SeedSistemAyarlari(context);
            SeedKullanicilar(context);
            SeedKitaplar(context);
            SeedUyeler(context);
            SeedOduncler(context);
            SeedRezervasyonlar(context);
            SeedAkademikYayinlar(context);
            SeedMateryalTransferler(context);
        }

        /// <summary>12 haneli gövdeye ISBN-13 kontrol basamağını ekler.</summary>
        internal static string Isbn13(long govde)
        {
            var ilk12 = govde.ToString("D12");
            var toplam = 0;
            for (var i = 0; i < 12; i++)
                toplam += (ilk12[i] - '0') * (i % 2 == 0 ? 1 : 3);
            return ilk12 + (char)('0' + (10 - toplam % 10) % 10);
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
                    SifreHash = SifreHasher.Hashle("admin123"),
                    Email = "admin@kutuphane.edu.tr",
                    Rol = KullaniciRolu.Admin,
                    Telefon = "0532 111 0001"
                },
                new Kullanici
                {
                    Ad = "Fatma", Soyad = "Kaya",
                    KullaniciAdi = "fatmak",
                    SifreHash = SifreHasher.Hashle("123456"),
                    Email = "fatma@kutuphane.edu.tr",
                    Rol = KullaniciRolu.Kutuphaneci,
                    Telefon = "0532 111 0002"
                },
                new Kullanici
                {
                    Ad = "Mehmet", Soyad = "Demir",
                    KullaniciAdi = "mehmetd",
                    SifreHash = SifreHasher.Hashle("123456"),
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
            long isbnGovdesi = 978000100000L; // ilk 12 hane; 13. hane kontrol basamağıdır

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
                    ISBN = Isbn13(isbnGovdesi++),
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

            // Her kopya için barkodlu nüsha kaydı (barkod kitap numarasını içerdiğinden kayıttan sonra).
            foreach (var kitap in kitaplar)
            {
                for (var sira = 1; sira <= kitap.ToplamAdet; sira++)
                {
                    context.KitapNushalari.Add(new KitapNushasi
                    {
                        KitapId = kitap.Id,
                        Barkod = NushaIslemleri.BarkodUret(kitap.Id, sira),
                        RafKonumu = kitap.RafKonumu,
                        EklenmeTarihi = kitap.EklenmeTarihi
                    });
                }
            }
            context.SaveChanges();
        }

        private static void SeedUyeler(KutuphaneDbContext context)
        {
            if (context.Uyeler.Any()) return;

            var adlar = new[] { "Ali", "Ayşe", "Mehmet", "Fatma", "Can", "Zeynep", "Emre", "Selin", "Burak", "Elif", "Kerem", "Merve", "Ozan", "Deniz", "Tolga", "Gizem" };
            var soyadlar = new[] { "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Arslan", "Doğan", "Aydın", "Erdoğan", "Özdemir", "Aslan", "Çetin", "Koç", "Kurt", "Güven" };
            var bolumler = new[] { "Bilgisayar Mühendisliği", "Elektrik-Elektronik Mühendisliği", "Makine Mühendisliği", "Endüstri Mühendisliği", "Matematik", "Fizik", "Kimya", "Biyoloji", "İşletme", "Hukuk" };

            // Öğrenci portalını denemek için bilinen bilgilerle demo öğrenci (gerçek kurulumda kullanılmamalıdır).
            var uyeler = new List<Uye>
            {
                new()
                {
                    Ad = "Demo",
                    Soyad = "Öğrenci",
                    Email = DemoOgrenciEposta,
                    TCKimlikNo = "10000000078",
                    OgrenciNo = DemoOgrenciNo,
                    Bolum = "Bilgisayar Mühendisliği",
                    Fakulte = "Mühendislik Fakültesi",
                    UyeTipi = UyeTipi.Ogrenci,
                    Durumu = UyeDurumu.Aktif,
                    KayitTarihi = DateTime.Now.AddDays(-90),
                    MaxOduncAdet = 3,
                    MaxOduncSuresi = 14,
                    PortalSifreHash = SifreHasher.Hashle(DemoOgrenciSifresi),
                },
            };
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
            var raftakiNushalar = context.KitapNushalari
                .Where(n => n.Durum == NushaDurumu.Rafta)
                .OrderBy(n => n.Id)
                .ToList()
                .GroupBy(n => n.KitapId)
                .ToDictionary(g => g.Key, g => new Queue<KitapNushasi>(g));
            var uyeler = context.Uyeler.Where(u => u.Durumu == UyeDurumu.Aktif).ToList();
            var kutuphaneciId = context.Kullanicilar.OrderBy(k => k.Id).Select(k => (int?)k.Id).FirstOrDefault();
            var maxUzatma = context.SistemAyarlari.Select(a => a.MaxUzatmaSayisi).FirstOrDefault();

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
                    KutuphanecId = kutuphaneciId,
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
                // Aynı üyeye aynı kitabın ikinci aktif ödüncü verilmez (benzersiz indeks).
                if (oduncler.Any(o => o.Durumu == OduncDurumu.Aktif && o.KitapId == kitap.Id && o.UyeId == uye.Id)) continue;
                if (!raftakiNushalar.TryGetValue(kitap.Id, out var kuyruk) || kuyruk.Count == 0) continue;
                var nusha = kuyruk.Dequeue();
                var oduncTarihi = DateTime.Now.AddDays(-_random.Next(1, 20));

                nusha.Durum = NushaDurumu.Oduncte;
                nusha.DurumTarihi = oduncTarihi;
                oduncler.Add(new Odunc
                {
                    KitapId = kitap.Id,
                    UyeId = uye.Id,
                    KutuphanecId = kutuphaneciId,
                    OduncTarihi = oduncTarihi,
                    PlanlananIadeTarihi = oduncTarihi.AddDays(14),
                    Durumu = OduncDurumu.Aktif,
                    MaxUzatmaSayisi = maxUzatma,
                    Nusha = nusha
                });

                kitap.MevcutAdet--;
            }

            DemoOgrenciOdunclerEkle(oduncler, kitaplar, raftakiNushalar, uyeler, kutuphaneciId, maxUzatma);

            context.Oduncler.AddRange(oduncler);
            context.SaveChanges();
        }

        /// <summary>
        /// Öğrenci portalında gösterilecek veri olsun diye demo öğrenciye iki aktif ve iki geçmiş ödünç verir.
        /// Rastgele sayı üretecini kullanmaz; böylece diğer demo verisi değişmez.
        /// </summary>
        private static void DemoOgrenciOdunclerEkle(List<Odunc> oduncler, List<Kitap> kitaplar,
            Dictionary<int, Queue<KitapNushasi>> raftakiNushalar, List<Uye> uyeler, int? kutuphaneciId, int maxUzatma)
        {
            var ogrenci = uyeler.FirstOrDefault(u => u.OgrenciNo == DemoOgrenciNo);
            if (ogrenci == null) return;

            var bugun = DateTime.Today.AddHours(10);
            var aktifGunler = new[] { -11, -3 }; // biri 3 gün içinde iade edilecek, biri yeni alınmış
            var verilen = 0;
            foreach (var kitap in kitaplar.OrderBy(k => k.Id))
            {
                if (verilen == aktifGunler.Length) break;
                if (kitap.MevcutAdet <= 0 || !raftakiNushalar.TryGetValue(kitap.Id, out var kuyruk) || kuyruk.Count == 0) continue;
                if (oduncler.Any(o => o.Durumu == OduncDurumu.Aktif && o.KitapId == kitap.Id && o.UyeId == ogrenci.Id)) continue;

                var nusha = kuyruk.Dequeue();
                var oduncTarihi = bugun.AddDays(aktifGunler[verilen++]);
                nusha.Durum = NushaDurumu.Oduncte;
                nusha.DurumTarihi = oduncTarihi;
                oduncler.Add(new Odunc
                {
                    KitapId = kitap.Id, UyeId = ogrenci.Id, KutuphanecId = kutuphaneciId, Nusha = nusha,
                    OduncTarihi = oduncTarihi, PlanlananIadeTarihi = oduncTarihi.AddDays(ogrenci.MaxOduncSuresi),
                    Durumu = OduncDurumu.Aktif, MaxUzatmaSayisi = maxUzatma,
                });
                kitap.MevcutAdet--;
            }

            foreach (var (kitap, gunOnce) in kitaplar.OrderByDescending(k => k.Id).Take(2).Zip(new[] { 120, 45 }))
            {
                var oduncTarihi = bugun.AddDays(-gunOnce);
                oduncler.Add(new Odunc
                {
                    KitapId = kitap.Id, UyeId = ogrenci.Id, KutuphanecId = kutuphaneciId,
                    OduncTarihi = oduncTarihi, PlanlananIadeTarihi = oduncTarihi.AddDays(ogrenci.MaxOduncSuresi),
                    GercekIadeTarihi = oduncTarihi.AddDays(10), Durumu = OduncDurumu.Iade,
                });
            }
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

        private static void SeedAkademikYayinlar(KutuphaneDbContext context)
        {
            if (context.AkademikYayinlar.Any()) return;

            var yayinlar = new List<AkademikYayin>
            {
                new AkademikYayin
                {
                    Baslik = "Distributed Consensus in Cloud-Native Autonomous Agent Architectures",
                    Yazar = "Dr. Elena Rostova",
                    Danisman = "Prof. Dr. Michael Vance",
                    Tur = YayinTuru.DoktoraTezi,
                    Universite = "MIT & Global Research Consortium",
                    EnstituFakulte = "Computer Science and AI Laboratory (CSAIL)",
                    AnabilimDali = "Distributed Systems",
                    Yil = 2024,
                    DOI = "10.1145/3544548.3581234",
                    AnahtarKelimeler = "Raft, Paxos, Microservices, Edge Computing, LLM Agents",
                    Ozet = "This doctoral dissertation investigates multi-leader decentralized consensus protocols optimized for low-latency agentic swarms in hybrid edge-cloud topologies.",
                    MevcutKampus = KampusLokasyonu.MuhendislikKutuphanesi,
                    AlintiSayisi = 48,
                    IndirmeSayisi = 1240,
                    Dil = "English",
                    DosyaBoyutu = "8.4 MB (PDF)"
                },
                new AkademikYayin
                {
                    Baslik = "Büyük Dil Modellerinde Hallüsinasyon Azaltımı için Semantik Graph RAG Mimarileri",
                    Yazar = "Yük. Müh. Burak Candan",
                    Danisman = "Prof. Dr. Ayşe Şener",
                    Tur = YayinTuru.YuksekLisansTezi,
                    Universite = "İstanbul Teknik Üniversitesi",
                    EnstituFakulte = "Bilişim Enstitüsü",
                    AnabilimDali = "Yapay Zeka ve Veri Mühendisliği",
                    Yil = 2025,
                    DOI = "10.1016/j.knosys.2025.109822",
                    AnahtarKelimeler = "Graph RAG, Bilgi Çizgeleri, Hallüsinasyon, Vektör Veritabanı, Doğal Dil İşleme",
                    Ozet = "Bu tez, kurumsal kütüphane ve akademik arşivlerde karmaşık sorgulamalar esnasında dil modellerinin ürettiği uydurma bilgileri en aza indiren bilgi grafiği tabanlı yeni bir hibrit RAG mimarisi önermektedir.",
                    MevcutKampus = KampusLokasyonu.MerkezKutuphane,
                    AlintiSayisi = 26,
                    IndirmeSayisi = 890,
                    Dil = "Türkçe",
                    DosyaBoyutu = "6.1 MB (PDF)"
                },
                new AkademikYayin
                {
                    Baslik = "Quantum Annealing for NP-Hard Combinatorial Library Logistics",
                    Yazar = "Sarah Jenkins, Robert H. Chen",
                    Danisman = "Prof. Kenneth Clarke",
                    Tur = YayinTuru.HakemliMakale,
                    Universite = "Oxford University Press",
                    EnstituFakulte = "Department of Physics & Computation",
                    AnabilimDali = "Quantum Informatics",
                    Yil = 2024,
                    DOI = "10.1103/PhysRevLett.132.080401",
                    AnahtarKelimeler = "Quantum Computing, D-Wave, Traveling Salesman, Inter-library Loan Routing",
                    Ozet = "Published in IEEE Transactions on Quantum Engineering. Addresses campus-wide autonomous physical book transit optimization using Adiabatic Quantum Computing.",
                    MevcutKampus = KampusLokasyonu.MerkezKutuphane,
                    AlintiSayisi = 112,
                    IndirmeSayisi = 3450,
                    Dil = "English",
                    DosyaBoyutu = "3.2 MB (PDF)"
                },
                new AkademikYayin
                {
                    Baslik = "CRISPR-Cas9 Hedefli Genom Düzenlemesinde Biyo-Informatik Algoritmalar",
                    Yazar = "Dr. Deniz Aksoy",
                    Danisman = "Prof. Dr. Hakan Erdem",
                    Tur = YayinTuru.DoktoraTezi,
                    Universite = "Hacettepe Üniversitesi",
                    EnstituFakulte = "Sağlık Bilimleri Enstitüsü",
                    AnabilimDali = "Tıbbi Biyoloji ve Genetik",
                    Yil = 2023,
                    DOI = "10.1038/s41587-023-01890-w",
                    AnahtarKelimeler = "CRISPR, Genom Düzenleme, Derin Öğrenme, Off-target Tahmini",
                    Ozet = "Tıp fakültesi merkez laboratuvarlarında hedef dışı mutasyonları %99.4 doğrulukla önceden tespit eden konvolüsyonel derin öğrenme mimarisinin klinik validasyonu.",
                    MevcutKampus = KampusLokasyonu.TipSaglikKutuphanesi,
                    AlintiSayisi = 94,
                    IndirmeSayisi = 2100,
                    Dil = "Türkçe",
                    DosyaBoyutu = "14.2 MB (PDF)"
                },
                new AkademikYayin
                {
                    Baslik = "Hukuki Yapay Zeka Sistemlerinde Telif Hakları ve Veri Madenciliği İstisnaları",
                    Yazar = "Av. Zeynep Bilgehan",
                    Danisman = "Prof. Dr. Mehmet Fevzi",
                    Tur = YayinTuru.YuksekLisansTezi,
                    Universite = "Ankara Üniversitesi",
                    EnstituFakulte = "Sosyal Bilimler Enstitüsü",
                    AnabilimDali = "Bilişim ve Fikri Mülkiyet Hukuku",
                    Yil = 2025,
                    DOI = "10.5555/jur.2025.4412",
                    AnahtarKelimeler = "Fikri Mülkiyet, Telif, Generative AI, Veri Madenciliği, Dijital Kütüphaneler",
                    Ozet = "Avrupa Birliği Yapay Zeka Yasası (EU AI Act) ve Türk Hukuku kapsamında akademik kütüphanelerin yapay zeka eğitim veri setlerine erişim serbestisi ve lisans modelleri.",
                    MevcutKampus = KampusLokasyonu.SosyalBilimlerKutuphanesi,
                    AlintiSayisi = 18,
                    IndirmeSayisi = 640,
                    Dil = "Türkçe",
                    DosyaBoyutu = "4.8 MB (PDF)"
                }
            };

            context.AkademikYayinlar.AddRange(yayinlar);
            context.SaveChanges();
        }

        private static void SeedMateryalTransferler(KutuphaneDbContext context)
        {
            if (context.MateryalTransferler.Any()) return;

            var uye = context.Uyeler.FirstOrDefault();
            var kitap = context.Kitaplar.FirstOrDefault();
            if (uye == null || kitap == null) return;

            context.MateryalTransferler.Add(new MateryalTransfer
            {
                KitapId = kitap.Id,
                TalepEdenUyeId = uye.Id,
                KaynakKampus = KampusLokasyonu.MuhendislikKutuphanesi,
                HedefKampus = KampusLokasyonu.MerkezKutuphane,
                TalepTarihi = DateTime.Now.AddHours(-6),
                TahminiVarisTarihi = DateTime.Now.AddHours(18),
                Durum = TransferDurumu.Kuryede,
                KuryeTakipKodu = "ILL-2026-TR-8842"
            });

            context.SaveChanges();
        }
    }
}
