using System.Security.Cryptography;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Üye (öğrenci) portalı hesabı. Üye, öğrenci numarası veya e-postası ve parolasıyla giriş yapar.
    /// İlk parolayı personel oluşturur ve yalnızca bir kez gösterilir; üye ilk girişte kendi parolasını belirler.
    /// Personel hesaplarıyla aynı parola politikası, özetleme (PBKDF2) ve hatalı deneme kilidi uygulanır.
    /// </summary>
    public sealed class UyeHesapService
    {
        public const int GeciciSifreUzunlugu = 10;

        // Karıştırılabilecek karakterler (0/O, 1/l/I) çıkarıldı: parola bankoda sözlü veya kâğıtla iletilir.
        private const string GeciciSifreKarakterleri = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        private readonly GirisKilidi _kilit;

        public UyeHesapService(TimeProvider zaman)
        {
            _kilit = new GirisKilidi(zaman);
        }

        /// <summary>Üyenin portalı kullanıp kullanamayacağı (anonimleştirilmiş veya yasaklı üyeler kullanamaz).</summary>
        public static bool GirisYapabilirMi(Uye uye) =>
            uye.PortalHesabiVar && !uye.AnonimMi && uye.Durumu != UyeDurumu.Yasakli;

        /// <summary>Öğrenci numarası veya e-posta ile giriş. Başarılıysa üyeyi döner ve denetim kaydı yazar.</summary>
        public async Task<(GirisSonuc Sonuc, Uye? Uye)> DogrulaAsync(string giris, string sifre, KutuphaneDbContext context)
        {
            giris = giris?.Trim() ?? string.Empty;
            sifre ??= string.Empty;
            var anahtar = "uye:" + giris;

            var kilitMesaji = _kilit.KilitMesaji(anahtar);
            if (kilitMesaji != null) return (GirisSonuc.Hata(kilitMesaji), null);

            Uye? uye = null;
            if (giris.Length > 0)
            {
                var eposta = giris.ToLowerInvariant();
                // Öğrenci numarası veritabanında benzersiz olmak zorunda değildir; birden çok eşleşme girişi reddeder.
                var adaylar = await context.Uyeler
                    .Where(u => u.PortalSifreHash != "" && (u.OgrenciNo == giris || u.Email.ToLower() == eposta))
                    .Take(2)
                    .ToListAsync();
                if (adaylar.Count == 1) uye = adaylar[0];
            }

            if (!await KimlikDogrulamaService.SifreDogruMuAsync(sifre, uye?.PortalSifreHash))
            {
                _kilit.HataKaydet(anahtar);
                await DenetimService.KaydetAsync(context, DenetimService.Islemler.UyeGirisBasarisiz,
                    uye != null ? $"Uye #{uye.Id}" : "Bilinmeyen uye", null, LogSeviyesi.Uyari);
                return (GirisSonuc.Hata("Öğrenci numarası/e-posta veya şifre hatalı."), null);
            }

            _kilit.Temizle(anahtar);

            // Parola doğrulandıktan sonra söylenir; aksi hâlde hesabın varlığı dışarıdan sınanabilirdi.
            if (!GirisYapabilirMi(uye!))
                return (GirisSonuc.Hata("Hesabınız çevrimiçi kullanıma kapalı. Lütfen kütüphaneye başvurun."), null);

            if (SifreHasher.YukseltmeGerekli(uye!.PortalSifreHash))
                uye.PortalSifreHash = await Task.Run(() => SifreHasher.Hashle(sifre));

            uye.SonGirisTarihi = DateTime.Now;
            DenetimService.Ekle(context, DenetimService.Islemler.UyeGirisBasarili, $"Uye #{uye.Id}", null);
            await context.SaveChangesAsync();

            return (GirisSonuc.BasariliSonuc(KimlikDogrulamaService.SifreDegistirilmeli(sifre)), uye);
        }

        /// <summary>
        /// Personel, üye için yeni bir geçici parola üretir (ilk hesap açılışı veya "şifremi unuttum").
        /// Parola yalnızca bu dönüşte düz metin olarak bulunur; veritabanında özeti tutulur.
        /// </summary>
        public async Task<(UyeSonuc Sonuc, string? GeciciSifre)> GeciciSifreOlusturAsync(
            KutuphaneDbContext context, int uyeId, int? personelId)
        {
            var uye = await context.Uyeler.FindAsync(uyeId);
            if (uye == null) return (UyeSonuc.Hata("Üye bulunamadı."), null);
            if (uye.AnonimMi) return (UyeSonuc.Hata("Anonimleştirilmiş üyeye hesap açılamaz."), null);
            if (uye.Durumu == UyeDurumu.Yasakli) return (UyeSonuc.Hata("Yasaklı üyeye çevrimiçi hesap açılamaz."), null);
            if (string.IsNullOrWhiteSpace(uye.OgrenciNo) && string.IsNullOrWhiteSpace(uye.Email))
                return (UyeSonuc.Hata("Üyenin giriş için öğrenci numarası veya e-postası olmalıdır."), null);

            var sifre = RandomNumberGenerator.GetString(GeciciSifreKarakterleri, GeciciSifreUzunlugu);
            uye.PortalSifreHash = await Task.Run(() => SifreHasher.Hashle(sifre));
            uye.PortalSifreGecici = true;
            DenetimService.Ekle(context, DenetimService.Islemler.UyePortalSifresiOlusturuldu, $"Uye #{uye.Id}", personelId);
            await context.SaveChangesAsync();
            _kilit.Temizle("uye:" + uye.OgrenciNo);
            _kilit.Temizle("uye:" + uye.Email);
            return (UyeSonuc.Basarili(uye), sifre);
        }

        /// <summary>Üye kendi parolasını değiştirir; geçici parola işareti kalkar.</summary>
        public async Task<GirisSonuc> SifreDegistirAsync(KutuphaneDbContext context, int uyeId, string mevcutSifre, string yeniSifre)
        {
            mevcutSifre ??= string.Empty;
            var uye = await context.Uyeler.FindAsync(uyeId);
            if (uye == null || !GirisYapabilirMi(uye)) return GirisSonuc.Hata("Hesap bulunamadı.");
            if (!await KimlikDogrulamaService.SifreDogruMuAsync(mevcutSifre, uye.PortalSifreHash))
                return GirisSonuc.Hata("Mevcut şifre hatalı.");

            var hata = KimlikDogrulamaService.YeniSifreHatasi(mevcutSifre, yeniSifre, uye.OgrenciNo);
            if (hata != null) return GirisSonuc.Hata(hata);

            uye.PortalSifreHash = await Task.Run(() => SifreHasher.Hashle(yeniSifre));
            uye.PortalSifreGecici = false;
            DenetimService.Ekle(context, DenetimService.Islemler.UyeSifreDegistirildi, $"Uye #{uye.Id}", null);
            await context.SaveChangesAsync();
            return GirisSonuc.BasariliSonuc();
        }
    }
}
