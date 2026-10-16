using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyonu.Views.Pages
{
    /// <summary>Bir veya birden fazla listeden zorunlu seçim yapılan genel diyalog.</summary>
    public partial class SecimDialog : Window
    {
        private readonly List<SecimAlani> _alanlar;

        public SecimDialog(string baslik, string aciklama, string onayMetni, params SecimAlani[] alanlar)
        {
            InitializeComponent();
            txtBaslik.Text = baslik;
            txtAciklama.Text = aciklama;
            btnOnayla.Content = onayMetni;
            _alanlar = alanlar.ToList();
            icAlanlar.ItemsSource = _alanlar;
        }

        /// <summary>Alanların sırasıyla seçilen öğeler (onaylandıktan sonra hepsi dolu).</summary>
        public IReadOnlyList<object> Secilenler => _alanlar.Select(a => a.Secilen!).ToList();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnKapat_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnOnayla_Click(object sender, RoutedEventArgs e)
        {
            var eksik = _alanlar.FirstOrDefault(a => a.Secilen == null);
            if (eksik != null)
            {
                MessageBox.Show($"Lütfen listeden bir '{eksik.Etiket.TrimEnd(' ', '*')}' seçin.", "Eksik Bilgi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }

    public sealed class SecimAlani
    {
        public SecimAlani(string etiket, IEnumerable secenekler, string gosterimAlani)
        {
            Etiket = etiket;
            Secenekler = secenekler.Cast<object>().ToList();
            GosterimAlani = gosterimAlani;
        }

        public string Etiket { get; }
        public IList Secenekler { get; }
        public string GosterimAlani { get; }
        public object? Secilen { get; set; }
    }
}
