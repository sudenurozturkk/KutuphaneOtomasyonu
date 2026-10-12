using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class EKitapPage : Page
    {
        private int _sayfaNo = 1;
        private int _toplamSayfa = 4;
        private double _fontBoyutu = 15;
        private bool _acikTema = false;
        private int _seciliKitapIndex = 0;

        private readonly List<EKitapModel> _kitaplar = new()
        {
            new EKitapModel
            {
                Baslik = "Temiz Kod (Clean Code)",
                Yazar = "Robert C. Martin",
                ToplamSayfa = 4,
                Sayfalar = new[]
                {
                    new EKitapSayfasi
                    {
                        Baslik = "Bölüm 1: Anlamlı İsimlendirme ve Temiz Kod İlkeleri",
                        Icerik = "Temiz kod yazmak, programlamada en temel becerilerden biridir. Robert C. Martin bu kitapta kodun sadece çalışmasının yetmediğini, aynı zamanda okunabilir, anlaşılır ve bakımı kolay olması gerektiğini vurgular.\n\nİyi bir isim, kodun ne yaptığını açıkça ifade eder. Değişken, fonksiyon ve sınıf isimlerini seçerken şu ilkelere dikkat etmeliyiz:\n\n• İsimlerin amacı açıkça ortaya koyması\n• Yanlış bilgi vermekten kaçınılması\n• Anlamlı ayrımlar yapılması\n• Seslendirilemeyen isimlerden kaçınılması\n• Arama yapılabilen isimler kullanılması\n\nÖrnek olarak, 'd' gibi tek harfli değişken isimleri yerine 'elapsedTimeInDays' gibi açıklayıcı isimler tercih edilmelidir. Bu, kodun ilk okunuşta anlaşılmasını kolaylaştırır.\n\nFonksiyon isimleri için fiil veya fiil grupları kullanılmalıdır: saveRecord(), deleteUser(), getLastName() gibi. Sınıf isimleri ise isim veya isim grupları olmalıdır: Customer, WikiPage, Account, AddressParser gibi."
                    },
                    new EKitapSayfasi
                    {
                        Baslik = "Bölüm 2: Fonksiyonlar ve Tek Sorumluluk İlkesi",
                        Icerik = "Fonksiyonlar, yazılımın en temel yapı taşlarıdır. Temiz bir fonksiyonun özellikleri şunlardır:\n\n1. KÜÇÜK OLMALI: İdeal bir fonksiyon 5-10 satırdan fazla olmamalıdır. Çok büyük fonksiyonlar, birden fazla işi yapmaya çalışıyor demektir.\n\n2. BİR İŞ YAPMALI: Single Responsibility Principle (SRP) - Her fonksiyon yalnızca bir iş yapmalı, onu iyi yapmalı ve tek bir soyutlama seviyesinde çalışmalıdır.\n\n3. İSİM DOĞRU OLMALI: Fonksiyon ismi, fonksiyonun tam olarak ne yaptığını açıklamalıdır. İsim uzun olsa bile sorun değildir.\n\n4. PARAMETRE SAYISI AZ OLMALI: İdeal fonksiyon sıfır parametrelidir. Bir veya iki parametre kabul edilebilir. Üç parametre ciddi şekilde sorgulanmalıdır.\n\n5. YAN ETKİ OLMAMALI: Fonksiyon, sözleştiği dışında başka bir şey değiştirmemelidir.\n\nGöz açıklamaları kullanmak yerine kodun kendisi yeterince açıklayıcı olmalıdır. Eğer bir yoruma ihtiyaç duyuluyorsa, muhtemelen kod yeterince temiz değildir."
                    },
                    new EKitapSayfasi
                    {
                        Baslik = "Bölüm 3: Yorumlar, Formatlama ve Nesneler",
                        Icerik = "YORUMLAR HAKKINDA:\n\nEn iyi yorum, yazmak zorunda kalmadığınız yorumdur. Kod yeterince açık olduğunda yoruma gerek kalmaz. Bununla birlikte bazı durumlarda yorumlar değerlidir:\n\n• Lisans ve telif hakkı bilgileri\n• Açıklama gerektiren karmaşık algoritmaların niyeti\n• Beklenmedik davranışlar için uyarılar\n• TODO yorumları\n\nKAÇINILMALI:\n• Anlaşılmaz yorumlar\n• Gereksiz yorumlar (kodu tekrar eden)\n• Güncel olmayan yorumlar\n• Kod dışı bırakılmış kod blokları\n\nFORMATLAMA:\nKod formatı çok önemlidir. Gazete metaforunu düşünün: En önemli bilgiler üstte olmalı, detaylar aşağıda. Dikey boşluklar kavramları birbirinden ayırır. Yatay hizalama satırların uzunluğu hakkında fikir verir.\n\nNESNELER VE VERİ YAPILARI:\nNesneler davranışı gizler ve veriyi ortaya çıkarır. Veri yapıları veriyi ortaya çıkarır ve davranış olmaksızın çalışır."
                    },
                    new EKitapSayfasi
                    {
                        Baslik = "Bölüm 4: Test Güdümlü Geliştirme (TDD) ve Temiz Testler",
                        Icerik = "TEST GÜDÜMLÜ GELİŞTİRME (TDD):\n\nTDD üç temel kurala dayanır:\n1. Bir birim testi geçene kadar üretim kodu yazmazsınız.\n2. Hatalı üretim kodunu test etmek için yeterli olan birim testinden daha fazlasını yazmazsınız.\n3. Mevcut başarısız testi geçmek için gereken üretim kodundan daha fazlasını yazmazsınız.\n\nTEMİZ TESTLER:\n\nTestlerin üç önemli özelliği vardır:\n• OKUNABILIRLIK: Testler üretim kodundan daha okunabilir olmalıdır\n• GÜVENILIRLIK: Her test yalnızca bir kavramı test etmeli\n• HIZLILIK: Testler hızlı çalışmalı, yoksa sık çalıştırılmazlar\n\nAAA (Arrange-Act-Assert) Deseni:\n- Arrange: Test koşullarını hazırla\n- Act: Test edilen kodu çalıştır\n- Assert: Beklenen sonucu doğrula\n\nKapsama Oranı:\nYüksek test kapsamı (>80%) hedeflenmelidir. Ancak %100 kapsama hedefi yerine anlamlı ve kaliteli testler yazmak daha önemlidir. Entegrasyon testleri ve birim testleri birlikte kullanılmalıdır."
                    }
                }
            },
            new EKitapModel
            {
                Baslik = "Tasarım Desenleri (GoF)",
                Yazar = "Gang of Four",
                ToplamSayfa = 3,
                Sayfalar = new[]
                {
                    new EKitapSayfasi
                    {
                        Baslik = "Bölüm 1: Yaratıcı Desenler - Singleton ve Factory",
                        Icerik = "Tasarım desenleri, yazılım tasarımında sıkça karşılaşılan sorunlara yönelik tekrar kullanılabilir çözümlerdir. Gang of Four (GoF) kitabında 23 tasarım deseni tanımlanmıştır.\n\nSINGLETON DESENİ:\nBir sınıfın yalnızca bir örneğinin oluşturulmasını garantiler ve bu örneğe global erişim noktası sağlar.\n\nKullanım Alanları:\n• Veritabanı bağlantı havuzları\n• Günlük kaydı (logging) sistemleri\n• Yapılandırma yöneticileri\n\nFACTORY METHOD DESENİ:\nNesnelerin nasıl yaratılacağını alt sınıflara bırakır. Üst sınıf nesne yaratma arayüzünü tanımlar, alt sınıflar hangi sınıfın örnekleneceğine karar verir.\n\nFaydaları:\n• Kodun gevşek bağlı (loosely coupled) olmasını sağlar\n• Yeni türler eklemek kolaylaşır\n• Test edilebilirlik artar"
                    },
                    new EKitapSayfasi
                    {
                        Baslik = "Bölüm 2: Yapısal Desenler - Adapter ve Decorator",
                        Icerik = "ADAPTER DESENİ:\nBir sınıfın arayüzünü başka bir arayüze dönüştürür. Birbiriyle uyumsuz arayüzlere sahip sınıfların birlikte çalışmasını sağlar.\n\nÖrnek: Eski bir sistemin XML çıktısını, yeni sistemin JSON formatına uyarlamak için Adapter kullanılabilir.\n\nDECORATOR DESENİ:\nBir nesneye dinamik olarak yeni sorumluluklar ekler. Fonksiyonelliği genişletmek için alt sınıflamaya esnek bir alternatif sunar.\n\nKullanım Alanları:\n• Java I/O akışları (BufferedReader, FileReader)\n• Web istek/yanıt filtreleme\n• UI bileşenlerine dinamik özellik ekleme\n\nObserver Deseni:\nNesneler arasında bire-çok bağımlılık tanımlar, böylece bir nesne değiştiğinde ona bağımlı olan tüm nesneler otomatik olarak güncellenir."
                    },
                    new EKitapSayfasi
                    {
                        Baslik = "Bölüm 3: Davranışsal Desenler - Strategy ve Observer",
                        Icerik = "STRATEGY DESENİ:\nAlgoritmalar ailesini tanımlar, her birini kapsüller ve birbirinin yerine geçebilir kılar. Strateji, algoritmayı kullanan istemcilerden bağımsız olarak değiştirmeye olanak tanır.\n\nÖrnek: Bir ödeme sistemi, farklı ödeme stratejilerini (kredi kartı, PayPal, kripto para) Strategy deseni ile uygulayabilir.\n\nCOMMAND DESENİ:\nBir isteği nesne olarak kapsüller, böylece istemcileri farklı isteklerle parametrize etmenize olanak tanır. Geri alınabilir (undo) işlemler için idealdir.\n\nTEMPLATE METHOD DESENİ:\nBir işlemin iskeletini bir üst sınıf metodunda tanımlar, bazı adımları alt sınıflara bırakır. Alt sınıflar, işlemin yapısını değiştirmeden belirli adımları yeniden tanımlayabilir.\n\nBu 23 desenin tamamı, yazılım geliştirmede karşılaşılan tekrarlayan sorunlara kanıtlanmış çözümler sunmaktadır."
                    }
                }
            },
            new EKitapModel
            {
                Baslik = "Algoritmalar &amp; Veri Yapıları",
                Yazar = "Thomas Cormen",
                ToplamSayfa = 3,
                Sayfalar = new[]
                {
                    new EKitapSayfasi { Baslik = "Bölüm 1: Big-O Notasyonu ve Karmaşıklık Analizi", Icerik = "Algoritmaların verimliliğini ölçmek için Big-O notasyonu kullanılır. Bu notasyon, bir algoritmanın giriş boyutu arttıkça nasıl davrandığını ifade eder.\n\nYaygın Karmaşıklıklar:\n• O(1) - Sabit zaman: Dizi indexleme\n• O(log n) - Logaritmik: Binary search\n• O(n) - Doğrusal: Doğrusal arama\n• O(n log n) - Linearitmik: Merge sort\n• O(n²) - Karesel: Bubble sort\n• O(2^n) - Üstel: Fibonacci (naive)\n\nEn kötü durum, en iyi durum ve ortalama durum analizi yapılmalıdır. Genellikle en kötü durum analizi tercih edilir." },
                    new EKitapSayfasi { Baslik = "Bölüm 2: Sıralama Algoritmaları", Icerik = "Sıralama algoritmaları bilgisayar biliminin temel konularındandır.\n\nMERGE SORT: Böl ve yönet yaklaşımı. O(n log n) garantisi. Kararlı sıralama.\n\nQUICK SORT: Pivot seçimine dayalı bölümleme. Ortalama O(n log n), en kötü O(n²). Cache dostu.\n\nHEAP SORT: Max-heap yapısını kullanır. O(n log n) garantili, yerinde sıralama.\n\nTIM SORT: Python ve Java'da kullanılan hibrit algoritma. Gerçek dünya verilerinde çok verimli." },
                    new EKitapSayfasi { Baslik = "Bölüm 3: Graf Algoritmaları ve Dinamik Programlama", Icerik = "GRAF ALGORİTMALARI:\n\nBFS (Genişlik Öncelikli Arama): En kısa yolu bulur, queue kullanır.\n\nDijkstra Algoritması: Ağırlıklı graflarda en kısa yol. O((V+E) log V).\n\nDİNAMİK PROGRAMLAMA:\nAlt problemlerin çözümlerini saklayarak tekrar hesaplamayı önler.\n\nÖrnekler: Fibonacci, Knapsack, Longest Common Subsequence, Matrix Chain Multiplication." }
                }
            },
            new EKitapModel
            {
                Baslik = "Yapay Zeka: Modern Yaklaşım",
                Yazar = "Russell & Norvig",
                ToplamSayfa = 2,
                Sayfalar = new[]
                {
                    new EKitapSayfasi { Baslik = "Bölüm 1: Makine Öğrenmesi Temelleri", Icerik = "Makine öğrenmesi, bilgisayarların açık programlama olmadan öğrenmesini sağlayan yapay zeka alt dalıdır.\n\nDENETİMLİ ÖĞRENME:\nEtiketlenmiş veri ile model eğitilir. Sınıflandırma ve regresyon problemlerinde kullanılır.\n\nDENETİMSİZ ÖĞRENME:\nEtiketlenmemiş veriden örüntüler bulunur. Kümeleme ve boyut azaltma.\n\nPEKİŞTİRMELİ ÖĞRENME:\nAjan ödül/ceza mekanizmasıyla öğrenir. AlphaGo, oyun yapay zekaları." },
                    new EKitapSayfasi { Baslik = "Bölüm 2: Derin Öğrenme ve Nöral Ağlar", Icerik = "Derin öğrenme, çok katmanlı yapay sinir ağlarını kullanan makine öğrenmesi alt dalıdır.\n\nKATMANLI MİMARİ:\n• Giriş katmanı\n• Gizli katmanlar (çok sayıda)\n• Çıkış katmanı\n\nCNN (Konvolüsyonel Sinir Ağları): Görüntü tanıma için optimum.\n\nRNN/LSTM: Dizi verileri ve doğal dil işleme için.\n\nTransformer Mimarisi: BERT, GPT gibi büyük dil modelleri. Günümüzde en popüler yapay zeka mimarisi." }
                }
            }
        };

        private EKitapModel? _mevcutKitap;
        private EKitapSayfasi? _mevcutSayfa;

        public EKitapPage()
        {
            InitializeComponent();
            Loaded += EKitapPage_Loaded;
        }

        private void EKitapPage_Loaded(object sender, RoutedEventArgs e)
        {
            _mevcutKitap = _kitaplar[0];
            _sayfaNo = 1;
            _toplamSayfa = _mevcutKitap.ToplamSayfa;
            SayfaGuncelle();
        }

        private void CbKitapSecimi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || cbKitapSecimi == null) return;

            if (cbKitapSecimi.SelectedIndex >= 0 && cbKitapSecimi.SelectedIndex < _kitaplar.Count)
            {
                _seciliKitapIndex = cbKitapSecimi.SelectedIndex;
                _mevcutKitap = _kitaplar[_seciliKitapIndex];
                _sayfaNo = 1;
                _toplamSayfa = _mevcutKitap.ToplamSayfa;
                SayfaGuncelle();
            }
        }

        private void BtnFontKucult_Click(object sender, RoutedEventArgs e)
        {
            if (_fontBoyutu > 11)
            {
                _fontBoyutu -= 2;
                txtKitapIcerik.FontSize = _fontBoyutu;
                btnFontKucult.Content = $"A- ({_fontBoyutu:F0})";
            }
        }

        private void BtnFontBuyut_Click(object sender, RoutedEventArgs e)
        {
            if (_fontBoyutu < 24)
            {
                _fontBoyutu += 2;
                txtKitapIcerik.FontSize = _fontBoyutu;
                btnFontBuyut.Content = $"A+ ({_fontBoyutu:F0})";
            }
        }

        private void BtnTemaToggle_Click(object sender, RoutedEventArgs e)
        {
            _acikTema = !_acikTema;
            if (_acikTema)
            {
                bdKitapKagidi.Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xF7, 0xF0));
                txtKitapIcerik.Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));
                txtBolumBaslik.Foreground = new SolidColorBrush(Color.FromRgb(0x07, 0x07, 0x30));
                btnTemaToggle.Content = "🌙 Koyu Mod";
            }
            else
            {
                bdKitapKagidi.Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
                txtKitapIcerik.Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
                txtBolumBaslik.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                btnTemaToggle.Content = "☀️ Açık Mod";
            }
        }

        private void BtnOncekiSayfa_Click(object sender, RoutedEventArgs e)
        {
            if (_sayfaNo > 1)
            {
                _sayfaNo--;
                SayfaGuncelle();
            }
        }

        private void BtnSonrakiSayfa_Click(object sender, RoutedEventArgs e)
        {
            if (_sayfaNo < _toplamSayfa)
            {
                _sayfaNo++;
                SayfaGuncelle();
            }
        }

        private void SayfaGuncelle()
        {
            if (_mevcutKitap == null || txtBolumBaslik == null || txtKitapIcerik == null || txtKitapBaslikEtiket == null || txtSayfaNo == null) return;
            _mevcutSayfa = _mevcutKitap.Sayfalar[_sayfaNo - 1];
            txtBolumBaslik.Text = _mevcutSayfa.Baslik;
            txtKitapIcerik.Text = _mevcutSayfa.Icerik;
            txtKitapBaslikEtiket.Text = $"{_mevcutKitap.Baslik} • Sayfa {_sayfaNo} / {_toplamSayfa}";
            txtSayfaNo.Text = $"Sayfa {_sayfaNo} / {_toplamSayfa}";
            btnOncekiSayfa.IsEnabled = _sayfaNo > 1;
            btnSonrakiSayfa.IsEnabled = _sayfaNo < _toplamSayfa;

            // Okuma ilerleme çubuğu
            double oran = (double)_sayfaNo / _toplamSayfa;
            pbOkuma.Value = oran * 100;
            txtOkumaPct.Text = $"{(int)(oran * 100)}%";
        }
    }

    public class EKitapModel
    {
        public string Baslik { get; set; } = "";
        public string Yazar { get; set; } = "";
        public int ToplamSayfa { get; set; }
        public EKitapSayfasi[] Sayfalar { get; set; } = Array.Empty<EKitapSayfasi>();
    }

    public class EKitapSayfasi
    {
        public string Baslik { get; set; } = "";
        public string Icerik { get; set; } = "";
    }
}
