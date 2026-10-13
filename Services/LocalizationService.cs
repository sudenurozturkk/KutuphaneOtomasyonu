using System;
using System.Collections.Generic;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Dünya standartlarında çoklu dil desteği (i18n).
    /// Singleton Pattern kullanır.
    /// </summary>
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public event Action? DilDegisti;

        public string AktifDil { get; private set; } = "TR"; // "TR" veya "EN"

        private readonly Dictionary<string, string> _tr = new();
        private readonly Dictionary<string, string> _en = new();

        private LocalizationService()
        {
            TanımlaKelimeler();
        }

        public void DilDegistir(string dilKodu)
        {
            if (AktifDil == dilKodu) return;
            AktifDil = dilKodu.ToUpperInvariant();
            DilDegisti?.Invoke();
        }

        public string Get(string key, string varsayilan = "")
        {
            var dict = AktifDil == "EN" ? _en : _tr;
            if (dict.TryGetValue(key, out var val)) return val;
            return string.IsNullOrEmpty(varsayilan) ? key : varsayilan;
        }

        private void TanımlaKelimeler()
        {
            // Menü & Genel
            Ekle("Menu_Dashboard", "Genel Bakış", "Dashboard");
            Ekle("Menu_Kitaplar", "Kitap Kataloğu", "Book Catalog");
            Ekle("Menu_Oduncler", "Ödünç & İade", "Circulation & Loans");
            Ekle("Menu_Uyeler", "Üye Yönetimi", "Patron Management");
            Ekle("Menu_Salon", "Salon & Masa Haritası", "Campus 2D Spaces");
            Ekle("Menu_Rezervasyonlar", "Rezervasyon Kuyruğu", "Hold Queue");
            Ekle("Menu_Barkod", "Hızlı Barkod Bankosu", "Barcode Desk Terminal");
            Ekle("Menu_Cuzdan", "Akıllı Cüzdan & Kart", "Smart Wallet & ID");
            Ekle("Menu_EKitap", "E-Kitap Okuyucu", "E-Reader Hub");
            Ekle("Menu_Raporlar", "Raporlar & Analitik", "Analytics & Reports");
            Ekle("Menu_Ayarlar", "Sistem Parametreleri", "System Settings");
            Ekle("Menu_Mimari", "Canlı Mimari & UML", "Architecture & UML");
            Ekle("Menu_Akademik", "Akademik Tez & Makale", "Research & Theses Hub");
            Ekle("Menu_KampusTransfer", "Kampüsler Arası Transfer (ILL)", "Inter-Library Loan (ILL)");

            // Butonlar & Durumlar
            Ekle("Btn_Yenile", "Yenile", "Refresh");
            Ekle("Btn_Cikis", "Oturumu Kapat", "Sign Out");
            Ekle("Btn_OduncVer", "Ödünç Ver", "Check Out");
            Ekle("Btn_IadeAl", "İade Al", "Check In / Return");
            Ekle("Btn_Ara", "Ara...", "Search...");
            Ekle("Btn_Kaydet", "Kaydet", "Save Changes");
            Ekle("Badge_Aktif", "Aktif", "Active");
            Ekle("Badge_Rezerve", "Rezerve", "Reserved");
            Ekle("Badge_Dolu", "Dolu", "Occupied");
            Ekle("Badge_Bos", "Müsait", "Available");
        }

        private void Ekle(string key, string tr, string en)
        {
            _tr[key] = tr;
            _en[key] = en;
        }
    }
}
