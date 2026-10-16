# UML Diyagramları

Bu klasördeki diyagramlar projenin gerçek kodundan (`Models/`, `Services/`, `Data/`, `Views/`) çıkarılmıştır.
Her diyagramın kaynağı bir Mermaid dosyasıdır (`.mmd`); `.png` ve `.svg` dosyaları bu kaynaktan üretilmiştir.

| # | Ödev maddesi | Dosya |
|---|---|---|
| 1 | En az dört sınıflı sınıf diyagramı, ilişkiler ve çokluklar | [1-sinif-diyagrami](1-sinif-diyagrami.png) |
| 2 | Bir öğrencinin ödünç aldığı kitaba ait nesne diyagramı | [2-nesne-diyagrami](2-nesne-diyagrami.png) |
| 3 | En az 3 mantıksal paket | [3-paket-diyagrami](3-paket-diyagrami.png) |
| 4 | Web API ve veritabanı içeren dağıtım diyagramı | [4-dagitim-diyagrami](4-dagitim-diyagrami.png) |

---

## 1. Sınıf Diyagramı

![Sınıf diyagramı](1-sinif-diyagrami.png)

Ödünç alma alanının 7 sınıfı (`Uye`, `Kitap`, `KitapNushasi`, `Odunc`, `Rezervasyon`, `CezaKaydi`, `Kullanici`),
ödünç işlemlerini yöneten `OduncService` ve 3 numaralandırma (enum) gösterilmiştir. `/` ile başlayan nitelikler
**türetilmiş** niteliklerdir (veritabanında tutulmaz, diğer alanlardan hesaplanır; örn. `KalanCeza = ToplamCeza - OdenenCeza`).

| İlişki | Tür | Çokluk | Anlamı |
|---|---|---|---|
| Kitap — KitapNushasi | Kompozisyon (◆) | 1 — 1..* | Her kitabın en az bir fiziksel nüshası vardır; nüsha kitabı olmadan var olamaz. |
| Uye — Odunc | İlişki | 1 — 0..* | Bir üye hiç ödünç almamış olabilir ya da birçok ödünç kaydı olabilir. |
| Kitap — Odunc | İlişki | 1 — 0..* | Bir kitabın ödünç geçmişinde birden çok kayıt bulunur. |
| KitapNushasi — Odunc | İlişki | 0..1 — 0..* | Ödünç belirli bir nüshaya bağlıdır (eski kayıtlarda boş olabilir); bir nüsha zaman içinde birçok kez ödünç verilir. |
| Kullanici — Odunc | İlişki | 0..1 — 0..* | İşlemi yapan kütüphaneci kaydedilir. |
| Uye — Rezervasyon, Kitap — Rezervasyon | İlişki | 1 — 0..* | Bir üye birden çok kitap için sıraya girebilir; bir kitabın birden çok bekleyeni olabilir. |
| Odunc — CezaKaydi | İlişki | 1 — 0..* | Gecikme, hasar veya kayıp bir ödünçte ceza doğurabilir. |
| Uye — CezaKaydi | İlişki | 1 — 0..* | Cezalar üyenin borcuna yazılır. |
| OduncService ⇢ Odunc / KitapNushasi | Bağımlılık | — | Servis ödünç kaydını oluşturur ve nüsha durumunu günceller. |

## 2. Nesne Diyagramı

![Nesne diyagramı](2-nesne-diyagrami.png)

Bilgisayar Mühendisliği öğrencisi **Ayşe Yılmaz**'ın, kütüphaneci **Fatma Kaya** aracılığıyla 10.10.2026'da
ödünç aldığı *Temiz Kod* kitabının 16.10.2026 tarihindeki anlık görüntüsüdür:

- `odunc128` aktiftir; iade tarihi 14 günlük öğrenci süresine göre 24.10.2026'dır ve henüz uzatılmamıştır.
- Kitabın iki nüshası vardır: `n1` (KB0000020001) Ayşe'dedir, `n2` raftadır; bu yüzden `MevcutAdet = 1`.
- Nesne değerleri sınıf diyagramındaki tiplerle ve ilişkilerle birebir uyumludur (örn. `odunc128.NushaId = n1.Id = 3`).

## 3. Paket Diyagramı

![Paket diyagramı](3-paket-diyagrami.png)

Kod, C# isim alanlarına (namespace) karşılık gelen 4 mantıksal pakete ve 2 alt pakete ayrılmıştır:

| Paket | Sorumluluk |
|---|---|
| `KutuphaneOtomasyonu.Views` (+ `Views.Pages`) | Sunum katmanı: WPF pencereleri, sayfalar ve diyaloglar |
| `KutuphaneOtomasyonu.Services` | İş mantığı: ödünç/iade kuralları, rezervasyon kuyruğu, yetkilendirme |
| `KutuphaneOtomasyonu.Data` (+ `Data.Migrations`) | Veri erişimi: EF Core `DbContext`, şema migration'ları, demo verisi |
| `KutuphaneOtomasyonu.Models` | Alan (domain) sınıfları: `Uye`, `Kitap`, `Odunc` … |

Bağımlılıklar yukarıdan aşağıya doğrudur; `Models` hiçbir pakete bağımlı değildir, alt katmanlar üst katmanları bilmez.

## 4. Dağıtım Diyagramı

![Dağıtım diyagramı](4-dagitim-diyagrami.png)

Üç katmanlı dağıtım gösterilmiştir:

- **Kütüphane bankosu PC:** WPF masaüstü istemcisi (`KutuphaneOtomasyonu.exe`) ve USB barkod okuyucu.
- **Öğrenci cihazı:** tarayıcı veya mobilden katalog arama (OPAC).
- **Uygulama sunucusu:** ASP.NET Core Web API; istemciler HTTPS üzerinden REST/JSON ile bağlanır. API, projedeki
  `Services`, `Models` ve `Data` paketlerini kullanır.
- **Veritabanı sunucusu:** ilişkisel veritabanı; API'ye EF Core üzerinden TCP/IP ile bağlanır.

> **Not:** Projenin şu anki sürümü tek makinede çalışır: WPF istemcisi, EF Core ile yerel bir SQLite dosyasına
> (`%APPDATA%\KutuphaneOtomasyonu\kutuphane.db`) doğrudan bağlanır ve depoda bir Web API projesi yoktur. Bu diyagram,
> ödevde istenen Web API'li çok kullanıcılı **hedef dağıtımı** gösterir. Katmanlı yapı sayesinde `Services`/`Models`/`Data`
> paketleri değişmeden bir Web API projesine taşınabilir; EF Core sağlayıcısı SQLite'tan PostgreSQL'e değiştirilir.

---

## Diyagramları yeniden üretmek

GitHub `.mmd` dosyalarını doğrudan göstermez; görseller [mermaid-cli](https://github.com/mermaid-js/mermaid-cli) ile üretilir:

```bash
npx -p @mermaid-js/mermaid-cli@11.4.2 mmdc -i 1-sinif-diyagrami.mmd -o 1-sinif-diyagrami.png -b white -s 2
```

Kaynaklar [mermaid.live](https://mermaid.live) üzerinde de düzenlenebilir.
