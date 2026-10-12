using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyonu.Views.Pages
{
    public partial class MimariUmlPage : Page
    {
        public MimariUmlPage()
        {
            InitializeComponent();
            Loaded += (s, e) => GuncelleBellek();
        }

        private void GuncelleBellek()
        {
            try
            {
                var proc = Process.GetCurrentProcess();
                var ramMb = proc.WorkingSet64 / (1024 * 1024);
                txtCanliBellek.Text = $"{ramMb} MB";
            }
            catch
            {
                txtCanliBellek.Text = "~50 MB";
            }
        }

        private void BtnBellekTopla_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GuncelleBellek();
            MessageBox.Show("Bellek başarıyla optimize edildi (Garbage Collection tamamlandı).", "Sistem Optimizasyonu", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
