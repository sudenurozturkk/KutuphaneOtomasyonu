using System;
using System.Linq;
using System.Collections.Generic;
using KutuphaneOtomasyonu.Core;
using KutuphaneOtomasyonu.Patterns;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Kütüphane İş Mantığı Servisi (Business Logic Layer)
    /// </summary>
    public class KutuphaneYonetimServisi
    {
        private readonly KutuphaneVeriDeposu _db = KutuphaneVeriDeposu.Ornek;

        public List<Kullanici> TumKullanicilar() => _db.Kullanicilar;
        public List<Materyal> TumMateryaller() => _db.Materyaller;
        public List<OduncIslemi> TumIslemler() => _db.Islemler;
        public List<CezaKaydi> TumCezalar() => _db.Cezalar;

        // 1. Ödünç Verme
        public OduncIslemi OduncVer(int kullaniciId, int materyalId)
        {
            var user = _db.Kullanicilar.FirstOrDefault(u => u.Id == kullaniciId) 
                ?? throw new Exception("Kullanıcı bulunamadı!");

            var mat = _db.Materyaller.FirstOrDefault(m => m.Id == materyalId)
                ?? throw new Exception("Materyal bulunamadı!");

            if (!user.OduncAlabilirMi())
                throw new Exception($"Ödünç verilemez! Kullanıcının {user.CezaBorcu:C2} borcu bulunmaktadır.");

            int aktifSayi = _db.Islemler.Count(i => i.KullaniciId == user.Id && i.Durum != "IADE_EDILDI");
            if (aktifSayi >= user.GetMaksimumOduncSayisi())
                throw new Exception($"Limit aşıldı! Maksimum ödünç hakkı: {user.GetMaksimumOduncSayisi()} kitap.");

            if (!mat.MevcutMu())
                throw new Exception("Materyal stokta kalmadı! Lütfen sıraya giriniz.");

            mat.StokDusur();

            int yeniId = _db.Islemler.Any() ? _db.Islemler.Max(i => i.IslemId) + 1 : 1001;
            var islem = new OduncIslemi(yeniId, user.Id, mat.Id, DateTime.Now, user.GetOduncSuresiGun());
            _db.Islemler.Add(islem);
            user.ToplamOduncSayisi++;

            return islem;
        }

        // 2. İade Alma (Strategy ve Observer Tetikleyicisi)
        public (OduncIslemi islem, decimal cezaTutari, bool bildirimGitti) IadeAl(int islemId)
        {
            var islem = _db.Islemler.FirstOrDefault(i => i.IslemId == islemId)
                ?? throw new Exception("Ödünç işlemi bulunamadı!");

            if (islem.Durum == "IADE_EDILDI")
                throw new Exception("Bu kitap zaten iade edilmiş!");

            var user = _db.Kullanicilar.FirstOrDefault(u => u.Id == islem.KullaniciId);
            var mat = _db.Materyaller.FirstOrDefault(m => m.Id == islem.MateryalId);

            islem.IadeEt();
            mat.StokArttir();

            decimal cezaTutari = 0.0m;
            int gecikmeGunu = islem.GecikmeGunu();

            // STRATEGY PATTERN İLE CEZA HESABI
            if (gecikmeGunu > 0)
            {
                ICezaStratejisi strateji;
                if (user is Akademisyen) strateji = new AkademisyenCezaStratejisi();
                else if (user is Kutuphaneci) strateji = new PersonelCezaStratejisi();
                else strateji = new OgrenciCezaStratejisi();

                var context = new CezaContext(strateji);
                cezaTutari = context.HesabiCalistir(gecikmeGunu);

                if (cezaTutari > 0)
                {
                    int yeniCezaId = _db.Cezalar.Any() ? _db.Cezalar.Max(c => c.CezaId) + 1 : 5001;
                    var ceza = new CezaKaydi(yeniCezaId, islem.IslemId, user.Id, cezaTutari, gecikmeGunu);
                    _db.Cezalar.Add(ceza);
                    user.CezaBorcu += cezaTutari;
                }
            }

            // OBSERVER PATTERN İLE SIRADAKİ OKUYUCUYA BİLDİRİM
            bool bildirimGitti = false;
            var rezervasyon = _db.Rezervasyonlar.FirstOrDefault(r => r.MateryalId == mat.Id && r.Durum == "BEKLEMEDE");
            if (rezervasyon != null)
            {
                rezervasyon.Durum = "BILDIRILDI";
                _db.BildirimMotoru.Bildir(mat.Id, mat);
                bildirimGitti = true;
            }

            return (islem, cezaTutari, bildirimGitti);
        }

        // 3. Süre Uzatma
        public DateTime SureUzat(int islemId)
        {
            var islem = _db.Islemler.FirstOrDefault(i => i.IslemId == islemId)
                ?? throw new Exception("İşlem bulunamadı!");

            bool rezervasyonVar = _db.Rezervasyonlar.Any(r => r.MateryalId == islem.MateryalId && r.Durum == "BEKLEMEDE");
            if (rezervasyonVar)
                throw new Exception("Bu kitap başkası tarafından rezerve edildiği için süresi uzatılamaz!");

            islem.SureUzat(7);
            return islem.SonTeslimTarihi;
        }

        // 4. Rezervasyon
        public Rezervasyon RezerveEt(int kullaniciId, int materyalId)
        {
            var user = _db.Kullanicilar.FirstOrDefault(u => u.Id == kullaniciId) ?? throw new Exception("Kullanıcı bulunamadı!");
            var mat = _db.Materyaller.FirstOrDefault(m => m.Id == materyalId) ?? throw new Exception("Materyal bulunamadı!");

            if (mat.MevcutMu())
                throw new Exception("Kitap şu anda rafta mevcut! Rezervasyon yerine doğrudan ödünç alabilirsiniz.");

            int yeniId = _db.Rezervasyonlar.Any() ? _db.Rezervasyonlar.Max(r => r.RezervasyonId) + 1 : 1;
            var rez = new Rezervasyon(yeniId, user.Id, mat.Id);
            _db.Rezervasyonlar.Add(rez);

            // Observer abonesi yap
            _db.BildirimMotoru.AboneOl(mat.Id, new KullaniciObserver(user));

            return rez;
        }

        // 5. Ceza Ödeme
        public string CezaOde(int cezaId)
        {
            var ceza = _db.Cezalar.FirstOrDefault(c => c.CezaId == cezaId) ?? throw new Exception("Ceza kaydı bulunamadı!");
            if (ceza.OdendiMi) throw new Exception("Bu ceza zaten ödenmiş!");

            var user = _db.Kullanicilar.FirstOrDefault(u => u.Id == ceza.KullaniciId) ?? throw new Exception("Kullanıcı bulunamadı!");

            if (user.Bakiye < ceza.Tutar)
                throw new Exception($"Yetersiz bakiye! Cüzdanda: {user.Bakiye:C2}, Gereken: {ceza.Tutar:C2}. Lütfen bakiye yükleyiniz.");

            user.BorcOde(ceza.Tutar);
            ceza.Ode();

            return ceza.MakbuzNo;
        }

        // 6. Bakiye Yükleme
        public decimal BakiyeYukle(int kullaniciId, decimal tutar)
        {
            var user = _db.Kullanicilar.FirstOrDefault(u => u.Id == kullaniciId) ?? throw new Exception("Kullanıcı bulunamadı!");
            user.BakiyeYukle(tutar);
            return user.Bakiye;
        }
    }
}
