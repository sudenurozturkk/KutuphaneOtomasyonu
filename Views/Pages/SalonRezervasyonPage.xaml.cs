using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public class MasaModel
    {
        public string Kod { get; set; } = "";
        public string Bolum { get; set; } = "";
        public string Durum { get; set; } = "Boş"; // Boş, Dolu, Rezerve
        public string Kapasite { get; set; } = "1 Kişilik";
        public string Ozellikler { get; set; } = "";
        public string? RezerveEden { get; set; }
    }

    public partial class SalonRezervasyonPage : Page
    {
        private List<MasaModel> _masalar = new();
        private MasaModel? _secilenMasa;
        private Button? _secilenBtn;

        public SalonRezervasyonPage()
        {
            InitializeComponent();
            MasalariOlustur();
            ArayuzuDoldur();
        }

        private void MasalariOlustur()
        {
            _masalar.Clear();

            // Sessiz Alan (D-101 .. D-108)
            for (int i = 1; i <= 8; i++)
            {
                var durum = i switch { 2 => "Dolu", 5 => "Rezerve", _ => "Boş" };
                _masalar.Add(new MasaModel
                {
                    Kod = $"D-10{i}",
                    Bolum = "Sessiz Çalışma Alanı",
                    Durum = durum,
                    Kapasite = "1 Kişilik",
                    Ozellikler = "⚡ 220V Priz • 🪟 Pencere Kenarı • 📶 5GHz Wi-Fi",
                    RezerveEden = durum == "Dolu" ? "Zeynep Kaya" : (durum == "Rezerve" ? "Canan Demir" : null)
                });
            }

            // Bireysel Alan (B-201 .. B-208)
            for (int i = 1; i <= 8; i++)
            {
                var durum = i switch { 1 => "Dolu", 3 => "Dolu", 7 => "Rezerve", _ => "Boş" };
                _masalar.Add(new MasaModel
                {
                    Kod = $"B-20{i}",
                    Bolum = "Bireysel Çalışma Alanı",
                    Durum = durum,
                    Kapasite = "1 Kişilik",
                    Ozellikler = "⚡ 220V Priz • 🖥️ Harici Monitör • 🔌 Ethernet",
                    RezerveEden = durum != "Boş" ? "Öğrenci Kaydı" : null
                });
            }

            // Grup Alan (G-301 .. G-304)
            for (int i = 1; i <= 4; i++)
            {
                var durum = i == 2 ? "Dolu" : "Boş";
                _masalar.Add(new MasaModel
                {
                    Kod = $"G-30{i}",
                    Bolum = "Grup Çalışma Odası",
                    Durum = durum,
                    Kapasite = "6 Kişilik",
                    Ozellikler = "📽️ 4K Projeksiyon • 📋 Manyetik Beyaz Tahta • 🔊 Ses Yalıtımlı",
                    RezerveEden = durum == "Dolu" ? "Yazılım Tasarım Çalışma Grubu" : null
                });
            }
        }

        private void ArayuzuDoldur()
        {
            pnlSessizAlan.Children.Clear();
            pnlBireyselAlan.Children.Clear();
            pnlGrupAlan.Children.Clear();

            int bosSayisi = _masalar.Count(m => m.Durum == "Boş");
            txtSalonDurum.Text = $"Kat 1 - Ana Çalışma Salonu ({bosSayisi}/{_masalar.Count} Masa Müsait)";

            foreach (var masa in _masalar)
            {
                var btn = MasaButonuUret(masa);
                if (masa.Bolum.StartsWith("Sessiz"))
                    pnlSessizAlan.Children.Add(btn);
                else if (masa.Bolum.StartsWith("Bireysel"))
                    pnlBireyselAlan.Children.Add(btn);
                else
                    pnlGrupAlan.Children.Add(btn);
            }

            // Varsayılan ilk boş masayı seç
            var ilkBos = _masalar.FirstOrDefault(m => m.Durum == "Boş") ?? _masalar[0];
            MasaSec(ilkBos, null);
        }

        private Button MasaButonuUret(MasaModel masa)
        {
            var btn = new Button
            {
                Width = masa.Kapasite.StartsWith("6") ? 130 : 96,
                Height = 72,
                Margin = new Thickness(0, 0, 12, 12),
                Tag = masa,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(1.5)
            };

            var borderBrush = masa.Durum switch
            {
                "Boş" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                "Dolu" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                "Rezerve" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                _ => Brushes.Gray
            };

            var bgBrush = masa.Durum switch
            {
                "Boş" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#064E3B")),
                "Dolu" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#450A0A")),
                "Rezerve" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#451A03")),
                _ => Brushes.DarkSlateGray
            };

            btn.BorderBrush = borderBrush;
            btn.Background = bgBrush;

            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var tbKod = new TextBlock
            {
                Text = masa.Kod,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var tbDurum = new TextBlock
            {
                Text = masa.Durum,
                FontSize = 11,
                Foreground = borderBrush,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            sp.Children.Add(tbKod);
            sp.Children.Add(tbDurum);
            btn.Content = sp;

            btn.Click += (s, e) => MasaSec(masa, btn);
            return btn;
        }

        private void MasaSec(MasaModel masa, Button? btn)
        {
            _secilenMasa = masa;
            _secilenBtn = btn;

            txtMasaKodu.Text = masa.Kod;
            txtMasaBolum.Text = $"Bölüm: {masa.Bolum}";
            txtMasaKapasite.Text = $"Kapasite: {masa.Kapasite}";
            txtMasaOzellikler.Text = masa.Ozellikler;

            txtMasaDurumBadge.Text = masa.Durum.ToUpper();
            bdMasaDurum.Background = masa.Durum switch
            {
                "Boş" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                "Dolu" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"))
            };

            btnRezerveEt.IsEnabled = (masa.Durum == "Boş");
            btnIptalEt.IsEnabled = (masa.Durum != "Boş");
        }

        private void BtnRezerveEt_Click(object sender, RoutedEventArgs e)
        {
            if (_secilenMasa == null) return;
            if (_secilenMasa.Durum != "Boş")
            {
                MessageBox.Show("Bu masa zaten dolu veya rezerve!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sure = (cbSure.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Standart";
            _secilenMasa.Durum = "Rezerve";
            _secilenMasa.RezerveEden = "Aktif Kullanıcı";

            MessageBox.Show($"Tebrikler! {_secilenMasa.Kod} numaralı masa {sure} süresince adınıza rezerve edildi.",
                "Rezervasyon Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

            ArayuzuDoldur();
        }

        private void BtnIptalEt_Click(object sender, RoutedEventArgs e)
        {
            if (_secilenMasa == null) return;
            var sonuc = MessageBox.Show($"{_secilenMasa.Kod} numaralı masanın rezervasyonunu iptal edip boşaltmak istiyor musunuz?",
                "Masa Boşalt", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (sonuc == MessageBoxResult.Yes)
            {
                _secilenMasa.Durum = "Boş";
                _secilenMasa.RezerveEden = null;
                MessageBox.Show($"{_secilenMasa.Kod} masası başarıyla boşaltıldı ve kullanıma açıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                ArayuzuDoldur();
            }
        }
    }
}
