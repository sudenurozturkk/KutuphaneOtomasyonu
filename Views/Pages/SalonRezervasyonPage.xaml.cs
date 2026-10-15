using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.Services;

namespace KutuphaneOtomasyonu.Views.Pages
{
    // ─── Veri Modelleri ───────────────────────────────────────────────────────

    public class MasaModel
    {
        public string Kod { get; set; } = "";
        public string Bolge { get; set; } = "";        // "Sessiz", "Bireysel", "Grup", "Premium"
        public string Durum { get; set; } = "Boş";    // Boş | Dolu | Rezerve
        public string Kapasite { get; set; } = "1 Kişilik";
        public string Ozellikler { get; set; } = "";
        public string Kat { get; set; } = "Kat 1 · Kuzey Kanat";
        public string? RezerveEden { get; set; }
        public string? RezerveZamanAraligi { get; set; }
        public int? SalonRezervasyonId { get; set; }
    }

    public class AktifRezervasyonVM
    {
        public string Kod { get; set; } = "";
        public string Kisi { get; set; } = "";
        public string Zaman { get; set; } = "";
        public string Sure { get; set; } = "";
    }

    // ─── Sayfa ────────────────────────────────────────────────────────────────

    public partial class SalonRezervasyonPage : Page
    {
        private List<MasaModel> _masalar = new();
        private MasaModel? _secilenMasa;
        private Button? _secilenBtn;
        private string _aktifSlot = "09:00 – 11:00";

        private readonly DispatcherTimer _saatTimer = new()
        {
            Interval = TimeSpan.FromSeconds(30)
        };

        public SalonRezervasyonPage()
        {
            InitializeComponent();
            MasalariOlustur();
            KatPlaniniCiz();
            AktifRezervasyonlariGuncelle();
            SaatiGuncelle();

            _saatTimer.Tick += (_, _) => SaatiGuncelle();
            _saatTimer.Start();
            Loaded += async (_, _) => await SalonRezervasyonlariniYukleAsync();
        }

        // ── Veri Başlangıcı ──────────────────────────────────────────────────

        private void MasalariOlustur()
        {
            _masalar.Clear();

            // 🤫 Sessiz Çalışma (D-101 … D-108)
            var sessizDurumlar = new[] { "Boş","Dolu","Boş","Boş","Rezerve","Boş","Boş","Boş" };
            var sessizKisiler   = new[] { null,"Zeynep Kaya",null,null,"Canan Demir",null,null,null };
            for (int i = 1; i <= 8; i++)
            {
                _masalar.Add(new MasaModel
                {
                    Kod = $"D-{100 + i}",
                    Bolge = "Sessiz",
                    Durum = sessizDurumlar[i - 1],
                    Kapasite = "1 Kişilik",
                    Ozellikler = "⚡ 220V Priz  •  📶 5GHz Wi-Fi  •  🪟 Pencere Kenarı  •  💡 LED Masa Lambası",
                    Kat = "Kat 1 · Kuzey Kanat",
                    RezerveEden = sessizKisiler[i - 1],
                    RezerveZamanAraligi = sessizDurumlar[i - 1] != "Boş" ? _aktifSlot : null
                });
            }

            // 💻 Bireysel PC Masaları (B-201 … B-208)
            var bireyselDurumlar = new[] { "Dolu","Boş","Dolu","Boş","Boş","Boş","Rezerve","Boş" };
            var bireyselKisiler  = new[] { "Emre Şahin",null,"Fatma Arslan",null,null,null,"Mert Öztürk",null };
            for (int i = 1; i <= 8; i++)
            {
                _masalar.Add(new MasaModel
                {
                    Kod = $"B-{200 + i}",
                    Bolge = "Bireysel",
                    Durum = bireyselDurumlar[i - 1],
                    Kapasite = "1 Kişilik",
                    Ozellikler = "⚡ 220V  •  🖥️ 24\" Harici Monitör  •  🔌 Gigabit Ethernet  •  🎧 Kulaklık Soketi",
                    Kat = "Kat 1 · Güney Kanat",
                    RezerveEden = bireyselKisiler[i - 1],
                    RezerveZamanAraligi = bireyselDurumlar[i - 1] != "Boş" ? _aktifSlot : null
                });
            }

            // 👥 Grup Odaları (G-301 … G-304)
            var grupDurumlar = new[] { "Boş","Dolu","Boş","Boş" };
            var grupKisiler  = new[] { null,"Yazılım Tasarım Grubu (6 kişi)",null,null };
            for (int i = 1; i <= 4; i++)
            {
                _masalar.Add(new MasaModel
                {
                    Kod = $"G-{300 + i}",
                    Bolge = "Grup",
                    Durum = grupDurumlar[i - 1],
                    Kapasite = "6 Kişilik",
                    Ozellikler = "📽️ 4K Projeksiyon  •  📋 Manyetik Akıllı Tahta  •  🔊 Ses Yalıtımı  •  🌐 Konferans Ağı",
                    Kat = "Kat 2 · Batı Kanat",
                    RezerveEden = grupKisiler[i - 1],
                    RezerveZamanAraligi = grupDurumlar[i - 1] != "Boş" ? _aktifSlot : null
                });
            }

            // 🏆 Premium Araştırma Kabinleri (P-401 … P-404)
            var premiumDurumlar = new[] { "Dolu","Boş","Rezerve","Boş" };
            var premiumKisiler  = new[] { "Prof. Dr. Ayşe Şener","", "Arş. Gör. Burak Candan","" };
            for (int i = 1; i <= 4; i++)
            {
                _masalar.Add(new MasaModel
                {
                    Kod = $"P-{400 + i}",
                    Bolge = "Premium",
                    Durum = premiumDurumlar[i - 1],
                    Kapasite = "1 Kişilik (Kabin)",
                    Ozellikler = "🔇 Fonoabsorban Panel  •  ⚡ 220V + USB-C 140W  •  🖥️ 27\" 4K Ekran  •  📶 Özel 6GHz Wi-Fi 6E",
                    Kat = "Kat 2 · Doğu Kanat",
                    RezerveEden = premiumDurumlar[i - 1] != "Boş" ? premiumKisiler[i - 1] : null,
                    RezerveZamanAraligi = premiumDurumlar[i - 1] != "Boş" ? _aktifSlot : null
                });
            }
        }

        // ── Kat Planı Çizimi ─────────────────────────────────────────────────

        private void KatPlaniniCiz()
        {
            gridSessiz.Children.Clear();
            gridBireysel.Children.Clear();
            pnlGrup.Children.Clear();
            pnlPremium.Children.Clear();

            int bosS = 0, bosB = 0, bosG = 0, bosP = 0;

            foreach (var masa in _masalar)
            {
                var btn = MasaButonuOlustur(masa);

                switch (masa.Bolge)
                {
                    case "Sessiz":
                        gridSessiz.Children.Add(btn);
                        if (masa.Durum == "Boş") bosS++;
                        break;
                    case "Bireysel":
                        gridBireysel.Children.Add(btn);
                        if (masa.Durum == "Boş") bosB++;
                        break;
                    case "Grup":
                        pnlGrup.Children.Add(btn);
                        if (masa.Durum == "Boş") bosG++;
                        break;
                    case "Premium":
                        pnlPremium.Children.Add(btn);
                        if (masa.Durum == "Boş") bosP++;
                        break;
                }
            }

            // Sayaç metinlerini güncelle
            txtSessizSayac.Text    = $"{bosS}/8 boş";
            txtBireyselSayac.Text  = $"{bosB}/8 boş";
            txtGrupSayac.Text      = $"{bosG}/4 boş";
            txtPremiumSayac.Text   = $"{bosP}/4 boş";

            int toplamBos  = _masalar.Count(m => m.Durum == "Boş");
            int toplamMasa = _masalar.Count;
            txtSalonDurum.Text = $"{toplamBos} / {toplamMasa} Masa Müsait";

            double doluOran = Math.Round((double)(toplamMasa - toplamBos) / toplamMasa * 100);
            txtDolulukOran.Text = $"Doluluk: %{doluOran:0}";

            // Varsayılan seçim: ilk boş masa
            var ilkBos = _masalar.FirstOrDefault(m => m.Durum == "Boş") ?? _masalar[0];
            MasaSec(ilkBos, null);
        }

        private Button MasaButonuOlustur(MasaModel masa)
        {
            bool grupMu    = masa.Bolge == "Grup";
            bool premiumMu = masa.Bolge == "Premium";

            // Durum rengi
            string renk = masa.Durum switch
            {
                "Boş"    => "#10B981",
                "Dolu"   => "#EF4444",
                "Rezerve"=> "#F59E0B",
                _        => "#94A3B8"
            };
            var borderColor = (Color)ColorConverter.ConvertFromString(renk);

            // İçerik paneli
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

            // Kabin ikonu
            var ikon = masa.Bolge switch
            {
                "Sessiz" => PackIconKind.VolumeMute,
                "Bireysel" => PackIconKind.Laptop,
                "Grup" => PackIconKind.AccountGroup,
                "Premium" => PackIconKind.StarFourPoints,
                _ => PackIconKind.ViewDashboard
            };

            var tbIkon = new PackIcon
            {
                Kind = ikon,
                Width = grupMu ? 21 : 18,
                Height = grupMu ? 21 : 18,
                Foreground = new SolidColorBrush(borderColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            };

            var tbKod = new TextBlock
            {
                Text = masa.Kod,
                FontSize = grupMu ? 14 : 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Kişi sayısı badge (grup için)
            var tbKap = grupMu || premiumMu
                ? new TextBlock
                {
                    Text = grupMu ? "6 kişilik" : "Kabin",
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
                : null;

            var tbDurum = new TextBlock
            {
                Text = masa.Durum.ToUpper(),
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(renk)),
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            sp.Children.Add(tbIkon);
            sp.Children.Add(tbKod);
            if (tbKap != null) sp.Children.Add(tbKap);
            sp.Children.Add(tbDurum);

            // Buton genişlikleri
            int genislik  = grupMu ? 155 : premiumMu ? 125 : 88;
            int yukseklik = grupMu ? 95  : premiumMu ? 90  : 82;

            // Arkaplan ve kenarlık
            var bgColor = masa.Durum switch
            {
                "Boş"    => Color.FromRgb(4, 47, 46),
                "Dolu"   => Color.FromRgb(69, 10, 10),
                "Rezerve"=> Color.FromRgb(69, 26, 3),
                _        => Color.FromRgb(30, 41, 59)
            };

            var btn = new Button
            {
                Width   = genislik,
                Height  = yukseklik,
                Margin  = new Thickness(0, 0, 10, 10),
                Tag     = masa,
                Cursor  = Cursors.Hand,
                Content = sp,
                BorderThickness = new Thickness(1.5),
                BorderBrush = new SolidColorBrush(borderColor),
                Background  = new SolidColorBrush(bgColor),
                Template    = BuildButtonTemplate(new CornerRadius(10))
            };

            btn.Click += (s, e) => MasaSec(masa, btn);
            return btn;
        }

        // Yuvarlak kenarlıklı buton şablonu (ControlTemplate)
        private static ControlTemplate BuildButtonTemplate(CornerRadius radius)
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetValue(Border.CornerRadiusProperty, radius);
            var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(cpFactory);
            template.VisualTree = borderFactory;
            return template;
        }

        // ── Masa Seçimi ──────────────────────────────────────────────────────

        private void MasaSec(MasaModel masa, Button? btn)
        {
            // Önceki seçimi sıfırla
            if (_secilenBtn != null)
            {
                var oncekiMasa = _secilenBtn.Tag as MasaModel;
                if (oncekiMasa != null)
                {
                    var normalColor = oncekiMasa.Durum switch
                    {
                        "Boş" => Color.FromRgb(52, 211, 153),
                        "Dolu" => Color.FromRgb(248, 113, 113),
                        "Rezerve" => Color.FromRgb(251, 191, 36),
                        _ => Color.FromRgb(148, 163, 184)
                    };
                    _secilenBtn.BorderBrush = new SolidColorBrush(normalColor);
                    _secilenBtn.BorderThickness = new Thickness(1.5);
                    _secilenBtn.Background = new SolidColorBrush(oncekiMasa.Durum switch
                    {
                        "Boş" => Color.FromRgb(12, 55, 55),
                        "Dolu" => Color.FromRgb(65, 25, 30),
                        "Rezerve" => Color.FromRgb(67, 47, 15),
                        _ => Color.FromRgb(30, 41, 59)
                    });
                }
            }

            _secilenMasa = masa;
            _secilenBtn  = btn;

            // Sağ Panel güncelle
            txtMasaKodu.Text    = masa.Kod;
            txtMasaBolum.Text   = masa.Bolge switch
            {
                "Sessiz"   => "🤫  Sessiz Çalışma Bölgesi",
                "Bireysel" => "💻  Bireysel Bilgisayar Masaları",
                "Grup"     => "👥  Grup Çalışma Odası",
                "Premium"  => "🏆  Premium Araştırma Kabini",
                _          => masa.Bolge
            };
            txtKapasite.Text    = masa.Kapasite;
            txtKat.Text         = masa.Kat;
            txtOzellikler.Text  = masa.Ozellikler;

            // Durum badge
            (string badgeText, string bgHex, string fgHex) = masa.Durum switch
            {
                "Boş"    => ("✅  MÜSAIT", "#064E3B", "#34D399"),
                "Dolu"   => ("🔴  DOLU",   "#450A0A", "#FCA5A5"),
                "Rezerve"=> ("🟡  REZERVE","#451A03", "#FCD34D"),
                _        => ("❓ BILINMIYOR","#1E293B","#94A3B8")
            };
            txtDurumBadge.Text = badgeText;
            bdDurumBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex));
            txtDurumBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex));

            // Rezerve eden
            if (!string.IsNullOrWhiteSpace(masa.RezerveEden))
            {
                bdRezerveEden.Visibility = Visibility.Visible;
                txtRezerveEden.Text = $"{masa.RezerveEden}  •  {masa.RezerveZamanAraligi}";
            }
            else
            {
                bdRezerveEden.Visibility = Visibility.Collapsed;
            }

            // Buton durumları
            btnRezerveEt.IsEnabled = masa.Durum == "Boş";
            btnIptalEt.IsEnabled   = masa.SalonRezervasyonId.HasValue;

            // Seçili butonun görünümünü vurgula (arka plan mavi border)
            if (btn != null)
            {
                btn.BorderBrush     = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // #38BDF8
                btn.BorderThickness = new Thickness(2.5);
                btn.Background      = new SolidColorBrush(Color.FromRgb(8, 47, 78));   // #082F4E
            }
        }

        // ── Rezervasyon İşlemleri ────────────────────────────────────────────

        private async void BtnRezerveEt_Click(object sender, RoutedEventArgs e)
        {
            if (_secilenMasa == null) return;
            if (_secilenMasa.Durum != "Boş")
            {
                MessageBox.Show("Bu masa zaten dolu veya rezerve edilmiş.\nLütfen başka bir masa seçin.",
                    "Masa Müsait Değil", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string kisi = txtKullanici.Text.Trim();
            if (string.IsNullOrWhiteSpace(kisi))
            {
                MessageBox.Show("Lütfen rezervasyonu yapacak kişinin adını veya öğrenci numarasını girin.",
                    "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                txtKullanici.Focus();
                return;
            }

            var sure = (cbSure.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "2 Saat";
            var amac = txtAkademikNot.Text.Trim();
            string amacBilgi = string.IsNullOrWhiteSpace(amac) ? "" : $"\nAmaç: {amac}";

            var onay = MessageBox.Show(
                $"Aşağıdaki rezervasyon oluşturulsun mu?\n\n" +
                $"📌 Masa       : {_secilenMasa.Kod} — {_secilenMasa.Bolge}\n" +
                $"👤 Kişi       : {kisi}\n" +
                $"⏱️ Süre       : {sure}\n" +
                $"📅 Zaman Dilimi: {_aktifSlot}" +
                amacBilgi,
                "Rezervasyonu Onayla", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (onay != MessageBoxResult.Yes) return;

            try
            {
                using var context = new KutuphaneDbContext();
                var servis = new SalonRezervasyonService(context);
                var sonuc = await servis.OlusturAsync(
                    _secilenMasa.Kod,
                    _secilenMasa.Bolge,
                    kisi,
                    _aktifSlot,
                    sure,
                    amac,
                    DateTime.Today);
                if (!sonuc.BasariliMi)
                {
                    MessageBox.Show($"{sonuc.Mesaj} Liste yenilendi.",
                        "Masa Müsait Değil", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await SalonRezervasyonlariniYukleAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyon kaydedilemedi: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var rezerveEdilenMasa = _secilenMasa;
            txtKullanici.Clear();
            txtAkademikNot.Clear();
            await SalonRezervasyonlariniYukleAsync();

            MessageBox.Show(
                $"✅ Rezervasyon başarıyla oluşturuldu!\n\n" +
                $"Masa {rezerveEdilenMasa.Kod} • {_aktifSlot}\n{kisi} adına {sure} süresince ayrıldı.",
                "Rezervasyon Tamamlandı", MessageBoxButton.OK, MessageBoxImage.None);
        }

        private async void BtnIptalEt_Click(object sender, RoutedEventArgs e)
        {
            if (_secilenMasa?.SalonRezervasyonId is not int rezervasyonId) return;

            var onaySonucu = MessageBox.Show(
                $"{_secilenMasa.Kod} numaralı masanın rezervasyonu iptal edilsin mi?\n" +
                $"Kullanan: {_secilenMasa.RezerveEden ?? "—"}\n\n" +
                "Masa tüm öğrencilere açılacaktır.",
                "Rezervasyonu İptal Et", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (onaySonucu != MessageBoxResult.Yes) return;

            try
            {
                using var context = new KutuphaneDbContext();
                var sonuc = await new SalonRezervasyonService(context).IptalEtAsync(rezervasyonId);
                if (!sonuc.BasariliMi)
                {
                    MessageBox.Show($"{sonuc.Mesaj} Liste yenilendi.",
                        "İşlem Yapılamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await SalonRezervasyonlariniYukleAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyon iptal edilemedi: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await SalonRezervasyonlariniYukleAsync();

            MessageBox.Show($"{_secilenMasa?.Kod} masası başarıyla boşaltıldı ve kullanıma açıldı.",
                "Masa Boşaltıldı", MessageBoxButton.OK, MessageBoxImage.None);
        }

        // ── Zaman Dilimi Seçici ──────────────────────────────────────────────

        private async void BdSlot_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border clickedBorder) return;
            _aktifSlot = clickedBorder.Tag?.ToString() ?? _aktifSlot;

            // Tüm slot sınırlarını sıfırla
            var slots = new[] { bdSlot1, bdSlot2, bdSlot3, bdSlot4, bdSlot5, bdSlot6 };
            foreach (var slot in slots)
            {
                slot.Background = new SolidColorBrush(Color.FromRgb(22, 32, 50));
                if (slot.Child is TextBlock tb) tb.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }

            // Seçili slot
            clickedBorder.Background = new SolidColorBrush(Color.FromRgb(15, 42, 74));
            if (clickedBorder.Child is TextBlock selTb)
            {
                selTb.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                selTb.FontWeight = FontWeights.SemiBold;
            }

            await SalonRezervasyonlariniYukleAsync();
        }

        // ── Aktif Rezervasyonlar Listesi ─────────────────────────────────────

        private void AktifRezervasyonlariGuncelle()
        {
            var aktif = _masalar
                .Where(m => m.Durum != "Boş" && !string.IsNullOrWhiteSpace(m.RezerveEden))
                .Select(m => new AktifRezervasyonVM
                {
                    Kod   = m.Kod,
                    Kisi  = m.RezerveEden ?? "—",
                    Zaman = m.RezerveZamanAraligi ?? _aktifSlot,
                    Sure  = m.Durum == "Dolu" ? "Aktif" : "Rezerve"
                })
                .ToList();

            lstAktifRezervasyonlar.ItemsSource = aktif;
        }

        private async Task SalonRezervasyonlariniYukleAsync()
        {
            try
            {
                var seciliKod = _secilenMasa?.Kod;
                using var context = new KutuphaneDbContext();
                var rezervasyonlar = await new SalonRezervasyonService(context)
                    .GetAktifAsync(DateTime.Today, _aktifSlot);

                MasalariOlustur();
                foreach (var rezervasyon in rezervasyonlar)
                {
                    var masa = _masalar.FirstOrDefault(m => m.Kod == rezervasyon.MasaKodu);
                    if (masa == null) continue;
                    masa.Durum = "Rezerve";
                    masa.RezerveEden = rezervasyon.Kullanici;
                    masa.RezerveZamanAraligi = rezervasyon.ZamanAraligi;
                    masa.SalonRezervasyonId = rezervasyon.Id;
                }

                KatPlaniniCiz();
                AktifRezervasyonlariGuncelle();
                var yeniSecim = _masalar.FirstOrDefault(m => m.Kod == seciliKod)
                    ?? _masalar.FirstOrDefault(m => m.Durum == "Boş")
                    ?? _masalar[0];
                MasaSec(yeniSecim, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Salon rezervasyonları yüklenemedi: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Saat Güncelleme ──────────────────────────────────────────────────

        private void SaatiGuncelle()
        {
            var simdi = DateTime.Now;
            txtSaat.Text  = simdi.ToString("HH:mm");
            txtTarih.Text = simdi.ToString("dddd, d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        }
    }
}
