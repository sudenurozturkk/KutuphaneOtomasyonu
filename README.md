# 📚 Kütüphane Yönetim Sistemi (Enterprise Suite v3.0)

Modern, yüksek performanslı ve kurumsal standartlarda geliştirilmiş masaüstü kütüphane otomasyon sistemi. 
Bu proje, **Yazılım Tasarım Mimarisi** ilkeleri ve **Gang of Four (GoF)** tasarım desenleri temel alınarak geliştirilmiştir.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF%20%2B%20MaterialDesign-0078D7?style=for-the-badge)
![Database](https://img.shields.io/badge/Database-SQLite%20%2B%20EF%20Core-003B57?style=for-the-badge&logo=sqlite)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%26%204--Tier-10B981?style=for-the-badge)

---

## 🏛️ Mimari Yapı ve Tasarım Desenleri

Proje 4 katmanlı ayrık mimari (Separation of Concerns) üzerine inşa edilmiştir:

1. **Sunum Katmanı (Presentation):** WPF (XAML), MVVM deseni, Material Design koyu tema (Dark Mode).
2. **İş Mantığı Katmanı (Business/Services):** Kitap, Ödünç, Üye, Bildirim ve Ayar servisleri.
3. **Veri Erişim Katmanı (Data Access):** Entity Framework Core 9, Generic Repository & SQLite.
4. **Alan Modelleri (Domain Models):** Zengin entity sınıfları, enum tipleri ve validasyon kuralları.

### Uygulanan GoF Tasarım Desenleri
- **Singleton Deseni:** `AuthService.Instance` ile tekil oturum ve kullanıcı kimlik yönetimi.
- **Factory Method Deseni:** `KullaniciFactory` ile Öğrenci, Akademisyen ve Personel nesnelerinin dinamik yetkilendirilmesi.
- **Observer Deseni:** `BildirimService` ve `IOduncObserver` ile ödünç/gecikme durumlarında anlık bildirim fırlatma.
- **Strategy Deseni:** `ICezaHesaplamaStratejisi` ile üye tipine göre dinamik ceza/indirim tarifesi.
- **Repository / Unit of Work Deseni:** Veritabanı sorgularının servis katmanında soyutlanması.

---

## 🚀 Temel Özellikler & Modüller

Uygulama 12 ana modül ve 6 etkileşimli modal diyalog penceresinden oluşur:

| Modül | Açıklama |
|---|---|
| **📊 Genel Bakış (Dashboard)** | Canlı KPI metrikleri, geciken iadeler, salon durumu ve hızlı aksiyon butonları. |
| **📖 Kitap Kataloğu** | 30+ hazır kayıt, kategori/dil filtreleme, ISBN arama ve masaüstüne CSV aktarımı. |
| **🔄 Ödünç & İade** | Aktif, gecikmiş ve geçmiş işlemler; süre uzatma ve dinamik ceza tahsilatı. |
| **👥 Üye Yönetimi** | Öğrenci/Akademisyen profilleri, detay pencereleri, aktif/pasif durum yönetimi. |
| **🪑 2D Salon & Masa Haritası** | Sessiz, Bireysel ve Grup alanlarında 20 masa için interaktif rezervasyon ve boşaltma. |
| **📅 Rezervasyon Kuyruğu** | Tüketen kitaplar için üye sırası; sıradan doğrudan ödünce dönüştürme ve iptal. |
| **⚡ Barkod Banko Terminali** | Barkod/ISBN okutma simülasyonu, hızlı fiş çıktısı ve log akışı. |
| **💳 Akıllı Cüzdan & Kart** | Üye bakiye yükleme (₺20, ₺50, ₺100) ve cüzdandan tek tıkla ceza mahsubu. |
| **📱 E-Kitap Okuyucu** | Dahili temiz kod ve algoritma kitapları, sayfalama ve font ölçekleme. |
| **📈 Raporlar & Analitik** | Kütüphane metrikleri, dağılım grafikleri ve masaüstüne UTF-8 CSV ihracı. |
| **⚙️ Sistem Parametreleri** | Ceza tutarları, maksimum ödünç limitleri ve anlık veritabanı yedeği alma. |
| **🧩 Canlı Mimari & UML (LAB)** | 4 katman inceleme, 5 GoF deseni canlı testi ve bellek optimizasyonu. |

---

## 🛠️ Kurulum ve Çalıştırma

### Gereksinimler
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10/11 x64

### Projeyi Başlatma
```bash
# Projeyi klonlayın
git clone https://github.com/sudenurozturkk/KutuphaneOtomasyonu.git

# Proje dizinine gidin
cd KutuphaneOtomasyonu

# Bağımlılıkları derleyin ve çalıştırın
dotnet run
```

### Varsayılan Giriş Bilgileri
- **Yönetici Hesabı:** `admin` / `admin123`
- **Üye Hesabı:** `fatmak` / `123456`

---

## 🧪 Otomatik Testler (Smoke Test)
Tüm modülleri, diyalogları ve navigasyon rotalarını test etmek için:
```bash
dotnet run -- --test
```
Bu komut 12 sayfayı, 6 diyalog penceresini ve veritabanı bağlantılarını sırayla test eder ve `%100 [PASS]` raporu üretir.

---

## 📄 Lisans
Bu proje eğitim ve kurumsal yazılım mimarisi demonstrasyonu amacıyla hazırlanmıştır. MIT Lisansı altında açık kaynak olarak kullanılabilir.
