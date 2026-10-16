using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using System;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Önemli işlemler için denetim kaydı (<see cref="AktiviteLog"/>).
    /// <see cref="Ekle"/> kaydı yalnızca bağlama ekler; işlemle aynı SaveChanges/transaction
    /// içinde kalıcı olur, böylece başarısız işlemler için denetim kaydı oluşmaz.
    /// Detay alanına ad, TC kimlik no, e-posta gibi kişisel veriler yazılmamalı; kimlik numaraları (Id) kullanılmalıdır.
    /// </summary>
    public static class DenetimService
    {
        public static class Islemler
        {
            public const string GirisBasarili = "GirisBasarili";
            public const string GirisBasarisiz = "GirisBasarisiz";
            public const string SifreDegistirildi = "SifreDegistirildi";
            public const string OduncVerildi = "OduncVerildi";
            public const string IadeAlindi = "IadeAlindi";
            public const string SureUzatildi = "SureUzatildi";
            public const string RezervasyonOlusturuldu = "RezervasyonOlusturuldu";
            public const string RezervasyonIptal = "RezervasyonIptal";
            public const string RezervasyonOdunceDonustu = "RezervasyonOdunceDonustu";
            public const string KitapEklendi = "KitapEklendi";
            public const string KitapGuncellendi = "KitapGuncellendi";
            public const string KitapArsivlendi = "KitapArsivlendi";
            public const string UyeEklendi = "UyeEklendi";
            public const string UyeGuncellendi = "UyeGuncellendi";
            public const string UyeAnonimlestirildi = "UyeAnonimlestirildi";
            public const string CezaOdendi = "CezaOdendi";
            public const string CuzdanYukleme = "CuzdanYukleme";
            public const string AyarlarGuncellendi = "AyarlarGuncellendi";
            public const string VeritabaniYedeklendi = "VeritabaniYedeklendi";
            public const string VeritabaniSifirlandi = "VeritabaniSifirlandi";
            public const string VeriDisaAktarildi = "VeriDisaAktarildi";
        }

        public static void Ekle(KutuphaneDbContext context, string islem, string detay,
            int? kullaniciId, LogSeviyesi seviye = LogSeviyesi.Bilgi)
        {
            context.AktiviteLoglar.Add(new AktiviteLog
            {
                KullaniciId = kullaniciId,
                Islem = Kisalt(islem, 100),
                Detay = Kisalt(detay, 500),
                Seviye = seviye,
                Tarih = DateTime.Now
            });
        }

        public static async Task KaydetAsync(KutuphaneDbContext context, string islem, string detay,
            int? kullaniciId, LogSeviyesi seviye = LogSeviyesi.Bilgi)
        {
            Ekle(context, islem, detay, kullaniciId, seviye);
            await context.SaveChangesAsync();
        }

        private static string Kisalt(string deger, int uzunluk) =>
            deger.Length <= uzunluk ? deger : deger[..uzunluk];
    }
}
