using KutuphaneOtomasyonu.Forms;

namespace KutuphaneOtomasyonu;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new AnaForm());
    }    
}