using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;

namespace KutuphaneOtomasyonu.Api.Dto;

// API veritabanı varlıklarını doğrudan döndürmez: döngüsel ilişkiler, parola özetleri ve gereksiz kişisel veriler
// yanıtlara karışmasın diye her uç nokta yalnızca bu kayıtları döner.

public sealed record SayfaliSonuc<T>(int Toplam, int Sayfa, int Boyut, IReadOnlyList<T> Kayitlar);

// ---------- Oturum ----------

public sealed record GirisIstegi(string? KullaniciAdi, string? Sifre);

public sealed record SifreDegistirIstegi(string? MevcutSifre, string? YeniSifre);

public sealed record OturumDto(int Id, string KullaniciAdi, string AdSoyad, KullaniciRolu Rol, bool SifreDegistirmeli);

public sealed record UyeGirisIstegi(string? Giris, string? Sifre);

public sealed record UyeSifreDegistirIstegi(string? MevcutSifre, string? YeniSifre);

public sealed record UyeOturumDto(int Id, string Giris, string AdSoyad, bool SifreDegistirmeli);

// ---------- Öğrenci portalı (Hesabım) ----------

/// <summary>Üyenin kendi bilgileri; kendi verisi olduğu için maskelenmez.</summary>
public sealed record HesapDto(
    int Id, string AdSoyad, string OgrenciNo, string Email, string Bolum, UyeTipi UyeTipi, UyeDurumu Durumu,
    int MaxOduncAdet, int MaxOduncSuresi, int AktifOduncSayisi, int GecikmisOduncSayisi, decimal KalanCeza,
    DateTime? UyelikBitisTarihi, int OkunmamisBildirim);

public sealed record BildirimDto(int Id, string Baslik, string Mesaj, BildirimTipi Tipi, DateTime OlusturmaTarihi, bool Okundu)
{
    public static BildirimDto Olustur(Bildirim b) => new(b.Id, b.Baslik, b.Mesaj, b.Tipi, b.OlusturmaTarihi, b.Okundu);
}

public sealed record HesapRezervasyonIstegi(int KitapId);

// ---------- Katalog ----------

public sealed record KitapDto(
    int Id, string Baslik, string Yazar, string ISBN, string Yayinevi, int YayinYili, string Kategori,
    string AltKategori, string Dil, int SayfaSayisi, int ToplamAdet, int MevcutAdet, string RafKonumu,
    string Aciklama, double Puan)
{
    public static KitapDto Olustur(Kitap k) => new(
        k.Id, k.Baslik, k.Yazar, k.ISBN, k.Yayinevi, k.YayinYili, k.Kategori, k.AltKategori, k.Dil,
        k.SayfaSayisi, k.ToplamAdet, k.MevcutAdet, k.RafKonumu, k.Aciklama, k.Puan);
}

public sealed record NushaDto(string Barkod, NushaDurumu Durum, string DurumMetni, string RafKonumu)
{
    public static NushaDto Olustur(KitapNushasi n) => new(n.Barkod, n.Durum, n.DurumMetni, n.RafKonumu);
}

public sealed record KitapDetayDto(KitapDto Kitap, IReadOnlyList<NushaDto> Nushalar);

public sealed record KitapIstegi(
    string? Baslik, string? Yazar, string? ISBN, string? Yayinevi, int YayinYili, string? Kategori,
    string? AltKategori, string? Dil, int SayfaSayisi, int ToplamAdet, string? RafKonumu, string? Aciklama,
    decimal SatinAlmaFiyati)
{
    public Kitap VarligaDonustur(int id = 0) => new()
    {
        Id = id,
        Baslik = Baslik?.Trim() ?? string.Empty,
        Yazar = Yazar?.Trim() ?? string.Empty,
        ISBN = ISBN?.Trim() ?? string.Empty,
        Yayinevi = Yayinevi?.Trim() ?? string.Empty,
        YayinYili = YayinYili,
        Kategori = Kategori?.Trim() ?? string.Empty,
        AltKategori = AltKategori?.Trim() ?? string.Empty,
        Dil = string.IsNullOrWhiteSpace(Dil) ? "Türkçe" : Dil.Trim(),
        SayfaSayisi = SayfaSayisi,
        ToplamAdet = ToplamAdet,
        RafKonumu = RafKonumu?.Trim() ?? string.Empty,
        Aciklama = Aciklama?.Trim() ?? string.Empty,
        SatinAlmaFiyati = SatinAlmaFiyati,
    };
}

// ---------- Üyeler ----------

public sealed record UyeOzetDto(
    int Id, string AdSoyad, string OgrenciNo, UyeTipi UyeTipi, UyeDurumu Durumu, decimal KalanCeza, bool AnonimMi)
{
    public static UyeOzetDto Olustur(Uye u) =>
        new(u.Id, u.TamAd, u.OgrenciNo, u.UyeTipi, u.Durumu, u.KalanCeza, u.AnonimMi);
}

/// <summary>Kimlik, pasaport ve telefon bilgileri Yardımcı Personel için maskelenir (KVKK veri minimizasyonu).</summary>
public sealed record UyeDetayDto(
    int Id, string Ad, string Soyad, string Email, string Telefon, string TCKimlikNo, string PasaportNo,
    string OgrenciNo, string Bolum, string Fakulte, UyeTipi UyeTipi, UyeDurumu Durumu, int MaxOduncAdet,
    int MaxOduncSuresi, decimal KalanCeza, DateTime KayitTarihi, DateTime? UyelikBitisTarihi, bool AnonimMi,
    bool KisiselVeriMaskeli, bool PortalHesabiVar, bool PortalSifreGecici,
    IReadOnlyList<OduncDto> Oduncler, IReadOnlyList<RezervasyonDto> Rezervasyonlar)
{
    public static UyeDetayDto Olustur(Uye u, KullaniciRolu rol) => new(
        u.Id, u.Ad, u.Soyad, u.Email,
        KisiselVeri.Goster(u.Telefon, rol),
        KisiselVeri.Goster(u.TCKimlikNo, rol),
        KisiselVeri.Goster(u.PasaportNo, rol),
        u.OgrenciNo, u.Bolum, u.Fakulte, u.UyeTipi, u.Durumu, u.MaxOduncAdet, u.MaxOduncSuresi, u.KalanCeza,
        u.KayitTarihi, u.UyelikBitisTarihi, u.AnonimMi,
        !KisiselVeri.TamGorebilirMi(rol), u.PortalHesabiVar, u.PortalSifreGecici,
        u.Oduncler.OrderByDescending(o => o.OduncTarihi).Take(20).Select(o => OduncDto.Olustur(o, u)).ToList(),
        u.Rezervasyonlar.OrderByDescending(r => r.RezervasyonTarihi).Take(20).Select(r => RezervasyonDto.Olustur(r, u)).ToList());
}

public sealed record UyeIstegi(
    string? Ad, string? Soyad, string? Email, string? Telefon, string? Adres, string? TCKimlikNo,
    string? PasaportNo, string? OgrenciNo, string? Bolum, string? Fakulte, UyeTipi UyeTipi,
    DateTime? UyelikBitisTarihi)
{
    public Uye VarligaDonustur(int id = 0) => new()
    {
        Id = id,
        Ad = Ad ?? string.Empty,
        Soyad = Soyad ?? string.Empty,
        Email = Email ?? string.Empty,
        Telefon = Telefon ?? string.Empty,
        Adres = Adres ?? string.Empty,
        TCKimlikNo = TCKimlikNo ?? string.Empty,
        PasaportNo = PasaportNo ?? string.Empty,
        OgrenciNo = OgrenciNo ?? string.Empty,
        Bolum = Bolum ?? string.Empty,
        Fakulte = Fakulte ?? string.Empty,
        UyeTipi = UyeTipi,
        UyelikBitisTarihi = UyelikBitisTarihi,
    };
}

public sealed record CezaOdemeIstegi(decimal Tutar);

// ---------- Ödünç ----------

public sealed record OduncDto(
    int Id, int KitapId, string KitapBaslik, int UyeId, string UyeAdSoyad, string? NushaBarkod,
    DateTime OduncTarihi, DateTime PlanlananIadeTarihi, DateTime? GercekIadeTarihi, OduncDurumu Durumu,
    int UzatmaSayisi, int MaxUzatmaSayisi, bool GecikmisMi, int GecikmeGunSayisi, decimal CezaTutari)
{
    public static OduncDto Olustur(Odunc o, Uye? uye = null)
    {
        uye ??= o.Uye;
        return new(o.Id, o.KitapId, o.Kitap?.Baslik ?? string.Empty, o.UyeId, uye?.TamAd ?? string.Empty,
            o.Nusha?.Barkod, o.OduncTarihi, o.PlanlananIadeTarihi, o.GercekIadeTarihi, o.Durumu, o.UzatmaSayisi,
            o.MaxUzatmaSayisi, o.GecikmisMi, o.GecikmeGunSayisi, o.CezaTutari);
    }
}

/// <summary>
/// Banko işlemi: kitap <see cref="KitapId"/> ya da nüsha barkoduyla, üye <see cref="UyeId"/> ya da
/// T.C./pasaport/öğrenci no/ad ile (<see cref="Uye"/>) belirtilir. Belirsiz eşleşmeler reddedilir.
/// </summary>
public sealed record OduncVerIstegi(int? KitapId, string? NushaBarkodu, int? UyeId, string? Uye);

public sealed record IadeIstegi(KitapDurumu Durum = KitapDurumu.Iyi, string? Notlar = null);

public sealed record BarkodIadeIstegi(string? Barkod, KitapDurumu Durum = KitapDurumu.Iyi, string? Notlar = null);

// ---------- Rezervasyon ----------

public sealed record RezervasyonDto(
    int Id, int KitapId, string KitapBaslik, int UyeId, string UyeAdSoyad, DateTime RezervasyonTarihi,
    DateTime GecerlilikTarihi, RezervasyonDurumu Durumu, int SiraNumarasi)
{
    public static RezervasyonDto Olustur(Rezervasyon r, Uye? uye = null)
    {
        uye ??= r.Uye;
        return new(r.Id, r.KitapId, r.Kitap?.Baslik ?? string.Empty, r.UyeId, uye?.TamAd ?? string.Empty,
            r.RezervasyonTarihi, r.GecerlilikTarihi, r.Durumu, r.SiraNumarasi);
    }
}

public sealed record RezervasyonIstegi(int KitapId, int UyeId, string? Notlar);

// ---------- Pano ----------

public sealed record PanoDto(
    int ToplamKitap, int ToplamKopya, int MevcutKopya, int ToplamUye, int AktifUye, int CezaliUye,
    int AktifOdunc, int Gecikmis, int BugunOdunc, int BugunIade, decimal OdenmemisCeza,
    IReadOnlyList<OduncDto> YaklasanIadeler);
