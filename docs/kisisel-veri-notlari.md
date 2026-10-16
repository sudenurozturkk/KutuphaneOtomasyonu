# Kişisel Veri İşleme Notları (KVKK)

Bu belge uygulamanın teknik olarak hangi kişisel verileri nasıl işlediğini açıklar. **Hukuki uygunluk
değerlendirmesi değildir**; veri sorumlusu kurumun KVKK aydınlatma metni, açık rıza, VERBİS kaydı ve saklama-imha
politikası gibi yükümlülükleri ayrıca karşılaması gerekir.

## İşlenen veriler

| Kategori | Alanlar | Amaç |
|---|---|---|
| Kimlik | Ad, soyad, T.C./Yabancı Kimlik No veya Pasaport No, öğrenci/sicil no | Üyeyi tekil tanıma, bankoda doğrulama |
| İletişim | E-posta, telefon, adres | Bildirim, iletişim |
| Eğitim | Bölüm, fakülte, üye tipi | Ödünç kotası, istatistik |
| Dolaşım | Ödünç/iade/rezervasyon geçmişi, cezalar, cüzdan hareketleri | Hizmetin yürütülmesi, mali kayıt |
| Personel | Kullanıcı adı, parola özeti (PBKDF2), giriş zamanları | Yetkilendirme, denetim |

Veriler yalnızca yerel SQLite veritabanında (`%APPDATA%\KutuphaneOtomasyonu\kutuphane.db`) tutulur; uygulama
veriyi ağ üzerinden göndermez.

## Uygulanan teknik tedbirler

- **Rol bazlı erişim ve veri minimizasyonu:** Yardımcı Personel, üye detayında kimlik no, pasaport no ve telefonu
  yalnızca son 4 karakteriyle görür ve üye kimlik bilgilerini düzenleyemez. Kütüphaneci ve Yönetici tam veriyi görür.
- **Toplu dışa aktarma** yalnızca Yöneticiye açıktır; kişisel veri uyarısı gösterilir ve denetim kaydı oluşturulur.
- **Silme talebi (anonimleştirme):** Yönetici, üye detayındaki "KVKK: Anonimleştir" ile üyenin ad, kimlik,
  pasaport, iletişim, öğrenci no ve notlarını kalıcı olarak siler; bildirimleri kaldırılır, bekleyen rezervasyonları
  iptal edilir, hesap pasifleşir. Ödünç/ceza/cüzdan kayıtları istatistik ve mali kayıt amacıyla kimliksiz olarak kalır.
  Aktif ödüncü veya ödenmemiş borcu olan üye anonimleştirilemez. İşlem geri alınamaz ve denetim kaydına yazılır.
- **Denetim kayıtları** (`AktiviteLoglar`) kişisel veri içermez; yalnızca kayıt numaraları (Üye #, Kitap #) tutulur.
  Bilinmeyen kullanıcı adıyla yapılan başarısız girişlerde girilen ad kaydedilmez.
- **Tanılama günlüğü** (`logs\app.log`) parola, kimlik numarası veya iletişim bilgisi içermez.
- **Parolalar** PBKDF2-SHA256 (600.000 iterasyon, kullanıcı başına tuz) ile saklanır.

## Kurumun karar vermesi gereken konular

1. **Saklama süreleri:** Ayrılan/pasif üyelerin ne kadar süre sonra anonimleştirileceği. Uygulama işlemi sağlar
   ancak otomatik bir saklama süresi uygulamaz.
2. **Yedekler:** Masaüstüne alınan veritabanı yedekleri tüm kişisel verileri içerir; şifreli ve erişimi kısıtlı bir
   konumda saklanmalıdır. Anonimleştirme mevcut yedeklerdeki veriyi silmez.
3. **Disk şifreleme:** Veritabanı dosyası şifrelenmez; BitLocker gibi disk şifrelemesi önerilir.
4. **Aydınlatma metni:** Üye kaydında üyeye sunulacak metin uygulama dışında hazırlanmalıdır.
