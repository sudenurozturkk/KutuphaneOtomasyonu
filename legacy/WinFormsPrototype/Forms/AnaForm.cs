using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using KutuphaneOtomasyonu.Core;
using KutuphaneOtomasyonu.Patterns;
using KutuphaneOtomasyonu.Services;

namespace KutuphaneOtomasyonu.Forms
{
    public partial class AnaForm : Form
    {
        private readonly KutuphaneYonetimServisi _servis = new KutuphaneYonetimServisi();
        private Kullanici _aktifKullanici;

        // Kontroller
        private ComboBox cmbKullaniciSecici;
        private Label lblKullaniciBilgi;
        private Label lblBakiyeBilgi;
        private Label lblCezaBilgi;
        private TabControl tabControl;

        // Dashboard
        private Label lblKpiToplamKitap;
        private Label lblKpiAktifOdunc;
        private Label lblKpiGecikmis;
        private Label lblKpiToplamCeza;
        private DataGridView dgvAktifOdunclerim;

        // Katalog
        private DataGridView dgvKatalog;
        private TextBox txtKatalogArama;
        private ComboBox cmbKatalogKategori;

        // Yeni Materyal Formu
        private TextBox txtYeniBaslik, txtYeniYazar, txtYeniIsbn, txtYeniKategori;
        private NumericUpDown numYeniStok, numYeniYil;
        private ComboBox cmbYeniTur;

        // Banko
        private ComboBox cmbBankoKullanici, cmbBankoMateryal, cmbBankoIadeIslem;

        // Üyeler & Cezalar
        private DataGridView dgvUyeler, dgvCezalar;

        public AnaForm()
        {
            InitializeCustomComponents();
            KullaniciSeciminiGuncelle();
            VerileriYukle();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Kütüphane Otomasyonu - Yazılım Tasarım Mimarisi (OOP Enterprise Application)";
            this.Size = new Size(1180, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. ÜST BAŞLIK & PROFİL PANELİ
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(20, 10, 20, 10)
            };

            Label lblLogo = new Label
            {
                Text = "🏛️ KÜTÜPHANE 360 OTOMASYONU",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
            };

            Label lblSubLogo = new Label
            {
                Text = "Yazılım Tasarım Mimarisi | Nesne Yönelimli Katmanlı Mimari & GoF Desenleri",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(24, 45)
            };

            // Aktif Kullanıcı Seçici
            Label lblUserPrompt = new Label
            {
                Text = "Aktif Profil:",
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Location = new Point(540, 18)
            };

            cmbKullaniciSecici = new ComboBox
            {
                Location = new Point(620, 15),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            foreach (var u in _servis.TumKullanicilar())
                cmbKullaniciSecici.Items.Add(u);
            cmbKullaniciSecici.SelectedIndex = 0;
            cmbKullaniciSecici.SelectedIndexChanged += (s, e) => KullaniciSeciminiGuncelle();

            lblKullaniciBilgi = new Label
            {
                Text = "Öğrenci (Lisans)",
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                Location = new Point(620, 46),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };

            lblBakiyeBilgi = new Label
            {
                Text = "Cüzdan: 0.00 TL",
                ForeColor = Color.FromArgb(74, 222, 128),
                AutoSize = true,
                Location = new Point(880, 18),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            lblCezaBilgi = new Label
            {
                Text = "Borç: 0.00 TL",
                ForeColor = Color.FromArgb(248, 113, 113),
                AutoSize = true,
                Location = new Point(880, 44),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            Button btnBakiyeYukle = new Button
            {
                Text = "+ Bakiye",
                Location = new Point(1040, 22),
                Size = new Size(90, 38),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBakiyeYukle.FlatAppearance.BorderSize = 0;
            btnBakiyeYukle.Click += BtnBakiyeYukle_Click;

            pnlTop.Controls.AddRange(new Control[] { lblLogo, lblSubLogo, lblUserPrompt, cmbKullaniciSecici, lblKullaniciBilgi, lblBakiyeBilgi, lblCezaBilgi, btnBakiyeYukle });
            this.Controls.Add(pnlTop);

            // 2. ANA SEKMELER (TABCONTROL)
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(16, 8),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            // Sekmeleri Ekle
            tabControl.TabPages.Add(OlusturDashboardSekmesi());
            tabControl.TabPages.Add(OlusturKatalogSekmesi());
            tabControl.TabPages.Add(OlusturBankoSekmesi());
            tabControl.TabPages.Add(OlusturUyeCezaSekmesi());
            tabControl.TabPages.Add(OlusturUMLRaporSekmesi());

            this.Controls.Add(tabControl);
            tabControl.BringToFront();
        }

        #region 1. DASHBOARD SEKMESİ
        private TabPage OlusturDashboardSekmesi()
        {
            TabPage tab = new TabPage("  📊 Genel Bakış  ");
            tab.BackColor = Color.FromArgb(248, 250, 252);

            // KPI Paneli
            Panel pnlKpi = new Panel { Dock = DockStyle.Top, Height = 110, Padding = new Padding(15) };
            
            lblKpiToplamKitap = KpiKartOlustur(pnlKpi, "Toplam Materyal", "0", Color.FromArgb(79, 70, 229), 20);
            lblKpiAktifOdunc = KpiKartOlustur(pnlKpi, "Aktif Ödünçler", "0", Color.FromArgb(2, 132, 199), 260);
            lblKpiGecikmis = KpiKartOlustur(pnlKpi, "Gecikmiş Kitaplar", "0", Color.FromArgb(225, 29, 72), 500);
            lblKpiToplamCeza = KpiKartOlustur(pnlKpi, "Tahsil Edilen Ceza", "0.00 TL", Color.FromArgb(22, 163, 74), 740);

            tab.Controls.Add(pnlKpi);

            // Üzerimdeki Aktif Ödünçler Tablosu
            GroupBox grpOdunc = new GroupBox
            {
                Text = "📌 Üzerinizdeki Aktif Ödünç Kitaplar (Online Süre Uzatma & İade)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15)
            };

            dgvAktifOdunclerim = DataGridViewOlustur();
            dgvAktifOdunclerim.Columns.Add("IslemId", "İşlem #");
            dgvAktifOdunclerim.Columns.Add("Kitap", "Materyal Başlığı");
            dgvAktifOdunclerim.Columns.Add("Kategori", "Kategori");
            dgvAktifOdunclerim.Columns.Add("AlisTarihi", "Alış Tarihi");
            dgvAktifOdunclerim.Columns.Add("SonTeslim", "Son Teslim");
            dgvAktifOdunclerim.Columns.Add("Uzatma", "Uzatma Sayısı");
            dgvAktifOdunclerim.Columns.Add("Durum", "Durum");

            Panel pnlIslemButonlar = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(5) };
            Button btnSureUzat = new Button
            {
                Text = "⏳ Süre Uzat (+7 Gün)",
                Size = new Size(160, 36),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, 8),
                Cursor = Cursors.Hand
            };
            btnSureUzat.Click += BtnSureUzat_Click;

            Button btnIadeEt = new Button
            {
                Text = "✅ Kitabı İade Et",
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(180, 8),
                Cursor = Cursors.Hand
            };
            btnIadeEt.Click += BtnIadeEt_Click;

            pnlIslemButonlar.Controls.AddRange(new Control[] { btnSureUzat, btnIadeEt });

            grpOdunc.Controls.Add(dgvAktifOdunclerim);
            grpOdunc.Controls.Add(pnlIslemButonlar);
            tab.Controls.Add(grpOdunc);

            return tab;
        }

        private Label KpiKartOlustur(Panel parent, string baslik, string deger, Color renk, int x)
        {
            Panel card = new Panel
            {
                Location = new Point(x, 15),
                Size = new Size(220, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblB = new Label
            {
                Text = baslik.ToUpper(),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Location = new Point(12, 10),
                AutoSize = true
            };

            Label lblV = new Label
            {
                Text = deger,
                ForeColor = renk,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Location = new Point(12, 32),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblB, lblV });
            parent.Controls.Add(card);
            return lblV;
        }
        #endregion

        #region 2. KATALOG & MATERYAL YÖNETİMİ SEKMESİ
        private TabPage OlusturKatalogSekmesi()
        {
            TabPage tab = new TabPage("  📚 Katalog & Materyal Yönetimi  ");
            tab.BackColor = Color.FromArgb(248, 250, 252);

            // Arama ve Filtre
            Panel pnlArama = new Panel { Dock = DockStyle.Top, Height = 55, Padding = new Padding(15, 10, 15, 10) };
            Label lblArama = new Label { Text = "Hızlı Ara:", Location = new Point(15, 16), AutoSize = true };
            txtKatalogArama = new TextBox { Location = new Point(85, 12), Width = 260 };
            txtKatalogArama.TextChanged += (s, e) => KatalogFiltrele();

            Label lblKat = new Label { Text = "Kategori:", Location = new Point(370, 16), AutoSize = true };
            cmbKatalogKategori = new ComboBox { Location = new Point(440, 12), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbKatalogKategori.Items.Add("Tümü (Hepsi)");
            cmbKatalogKategori.SelectedIndex = 0;
            cmbKatalogKategori.SelectedIndexChanged += (s, e) => KatalogFiltrele();

            Button btnOduncAl = new Button
            {
                Text = "⚡ Seçileni Ödünç Al",
                Location = new Point(650, 10),
                Size = new Size(160, 32),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOduncAl.Click += BtnOduncAl_Click;

            Button btnRezerveEt = new Button
            {
                Text = "⏳ Sıraya Gir (Rezerve)",
                Location = new Point(820, 10),
                Size = new Size(160, 32),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRezerveEt.Click += BtnRezerveEt_Click;

            pnlArama.Controls.AddRange(new Control[] { lblArama, txtKatalogArama, lblKat, cmbKatalogKategori, btnOduncAl, btnRezerveEt });
            tab.Controls.Add(pnlArama);

            // Katalog Tablosu
            dgvKatalog = DataGridViewOlustur();
            dgvKatalog.Columns.Add("Id", "ID");
            dgvKatalog.Columns.Add("Baslik", "Eser Adı");
            dgvKatalog.Columns.Add("Yazar", "Yazar / Araştırmacı");
            dgvKatalog.Columns.Add("Kategori", "Kategori");
            dgvKatalog.Columns.Add("Tur", "Materyal Türü");
            dgvKatalog.Columns.Add("Stok", "Mevcut / Toplam");
            dgvKatalog.Columns.Add("Konum", "Raf Konumu");

            // Yeni Materyal Ekleme Grubu (Factory Method Kanıtı)
            GroupBox grpEkle = new GroupBox
            {
                Text = "➕ Yeni Materyal Ekle (Factory Method Pattern ile Nesne Üretimi)",
                Dock = DockStyle.Bottom,
                Height = 140,
                Padding = new Padding(15)
            };

            cmbYeniTur = new ComboBox { Location = new Point(15, 30), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbYeniTur.Items.AddRange(new object[] { "KITAP", "EKITAP", "DERGI", "TEZ" });
            cmbYeniTur.SelectedIndex = 0;

            txtYeniBaslik = new TextBox { Location = new Point(170, 30), Width = 220, PlaceholderText = "Eser Adı / Başlık" };
            txtYeniYazar = new TextBox { Location = new Point(400, 30), Width = 180, PlaceholderText = "Yazar" };
            txtYeniIsbn = new TextBox { Location = new Point(590, 30), Width = 140, PlaceholderText = "ISBN / No" };
            txtYeniKategori = new TextBox { Location = new Point(740, 30), Width = 140, PlaceholderText = "Kategori" };
            numYeniStok = new NumericUpDown { Location = new Point(890, 30), Width = 70, Minimum = 1, Value = 3 };

            Button btnKaydet = new Button
            {
                Text = "Kataloğa Ekle",
                Location = new Point(980, 26),
                Size = new Size(130, 35),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnKaydet.Click += BtnYeniMateryalKaydet_Click;

            Label lblFactoryNote = new Label
            {
                Text = "💡 Bu formdaki veriler 'MateryalFabrikasi.MateryalUret()' çağrısı ile tipine göre polimorfik olarak üretilmektedir.",
                ForeColor = Color.FromArgb(79, 70, 229),
                Location = new Point(15, 75),
                AutoSize = true
            };

            grpEkle.Controls.AddRange(new Control[] { cmbYeniTur, txtYeniBaslik, txtYeniYazar, txtYeniIsbn, txtYeniKategori, numYeniStok, btnKaydet, lblFactoryNote });

            tab.Controls.Add(dgvKatalog);
            tab.Controls.Add(grpEkle);
            return tab;
        }
        #endregion

        #region 3. HIZLI ÖDÜNÇ & İADE BANKOSU (GÖREVLİ)
        private TabPage OlusturBankoSekmesi()
        {
            TabPage tab = new TabPage("  🏢 Hızlı Banko (Ödünç / İade)  ");
            tab.BackColor = Color.FromArgb(248, 250, 252);

            GroupBox grpVer = new GroupBox
            {
                Text = "📤 Hızlı Ödünç Masası",
                Location = new Point(40, 30),
                Size = new Size(500, 320),
                Padding = new Padding(20)
            };

            Label lblK = new Label { Text = "Öğrenci / Üye Seç:", Location = new Point(20, 40), AutoSize = true };
            cmbBankoKullanici = new ComboBox { Location = new Point(20, 65), Width = 440, DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblM = new Label { Text = "Verilecek Materyal:", Location = new Point(20, 115), AutoSize = true };
            cmbBankoMateryal = new ComboBox { Location = new Point(20, 140), Width = 440, DropDownStyle = ComboBoxStyle.DropDownList };

            Button btnBankoOdunc = new Button
            {
                Text = "⚡ Ödünç İşlemini Onayla",
                Location = new Point(20, 210),
                Size = new Size(440, 45),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBankoOdunc.Click += (s, e) =>
            {
                if (cmbBankoKullanici.SelectedItem is Kullanici u && cmbBankoMateryal.SelectedItem is Materyal m)
                {
                    try
                    {
                        var islem = _servis.OduncVer(u.Id, m.Id);
                        MessageBox.Show($"Kitap başarıyla ödünç verildi!\nSon Teslim Tarihi: {islem.SonTeslimTarihi:dd.MM.yyyy}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        VerileriYukle();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };

            grpVer.Controls.AddRange(new Control[] { lblK, cmbBankoKullanici, lblM, cmbBankoMateryal, btnBankoOdunc });

            // İade Masası
            GroupBox grpAl = new GroupBox
            {
                Text = "📥 Hızlı İade Masası",
                Location = new Point(580, 30),
                Size = new Size(500, 320),
                Padding = new Padding(20)
            };

            Label lblI = new Label { Text = "İade Alınacak Aktif İşlem:", Location = new Point(20, 40), AutoSize = true };
            cmbBankoIadeIslem = new ComboBox { Location = new Point(20, 65), Width = 440, DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblInfo = new Label
            {
                Text = "📌 İade işlemi sırasında:\n- Gecikme varsa 'Strategy Pattern' ile rolüne uygun ceza kesilir.\n- Kitaba rezervasyon yapılmışsa 'Observer Pattern' sıradaki üyeye bildirim iletir.",
                Location = new Point(20, 120),
                Size = new Size(440, 70),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            Button btnBankoIade = new Button
            {
                Text = "✅ İadeyi Onayla & Raflara Gönder",
                Location = new Point(20, 210),
                Size = new Size(440, 45),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBankoIade.Click += (s, e) =>
            {
                if (cmbBankoIadeIslem.SelectedItem is ComboboxItem item)
                {
                    try
                    {
                        var sonuc = _servis.IadeAl((int)item.Value);
                        string msg = "Kitap başarıyla iade alındı!";
                        if (sonuc.cezaTutari > 0)
                            msg += $"\n⚠️ DİKKAT: {sonuc.cezaTutari:C2} gecikme cezası yansıtıldı!";
                        if (sonuc.bildirimGitti)
                            msg += "\n🔔 [Observer]: Rezerve eden sıradaki kullanıcıya bildirim iletildi!";

                        MessageBox.Show(msg, "İşlem Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        VerileriYukle();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };

            grpAl.Controls.AddRange(new Control[] { lblI, cmbBankoIadeIslem, lblInfo, btnBankoIade });

            tab.Controls.AddRange(new Control[] { grpVer, grpAl });
            return tab;
        }
        #endregion

        #region 4. ÜYELER & CEZALAR SEKMESİ
        private TabPage OlusturUyeCezaSekmesi()
        {
            TabPage tab = new TabPage("  💳 Üyeler & Ceza Yönetimi  ");
            tab.BackColor = Color.FromArgb(248, 250, 252);

            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            // Üst: Üyeler Tablosu
            GroupBox grpUye = new GroupBox { Text = "👥 Kayıtlı Kütüphane Üyeleri (Polimorfik Sınıflar)", Dock = DockStyle.Fill };
            dgvUyeler = DataGridViewOlustur();
            dgvUyeler.Columns.Add("Id", "ID");
            dgvUyeler.Columns.Add("AdSoyad", "Ad Soyad");
            dgvUyeler.Columns.Add("Rol", "Rol");
            dgvUyeler.Columns.Add("Kota", "Maksimum Ödünç");
            dgvUyeler.Columns.Add("Sure", "Ödünç Süresi (Gün)");
            dgvUyeler.Columns.Add("Bakiye", "Cüzdan Bakiyesi");
            dgvUyeler.Columns.Add("Borc", "Ceza Borcu");
            dgvUyeler.Columns.Add("Barkod", "Üye Barkodu");
            grpUye.Controls.Add(dgvUyeler);

            // Alt: Cezalar Tablosu
            GroupBox grpCeza = new GroupBox { Text = "💸 Kesilen Cezalar & Tahsilat", Dock = DockStyle.Fill };
            dgvCezalar = DataGridViewOlustur();
            dgvCezalar.Columns.Add("CezaId", "Ceza #");
            dgvCezalar.Columns.Add("Uye", "Üye Adı");
            dgvCezalar.Columns.Add("Gun", "Gecikme Günü");
            dgvCezalar.Columns.Add("Tutar", "Tutar");
            dgvCezalar.Columns.Add("Durum", "Ödeme Durumu");
            dgvCezalar.Columns.Add("Makbuz", "Dekont No");

            Button btnCezaOde = new Button
            {
                Text = "💳 Seçili Cezayı Cüzdandan Öde",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCezaOde.Click += (s, e) =>
            {
                if (dgvCezalar.SelectedRows.Count > 0)
                {
                    int cezaId = Convert.ToInt32(dgvCezalar.SelectedRows[0].Cells["CezaId"].Value);
                    try
                    {
                        string dekont = _servis.CezaOde(cezaId);
                        MessageBox.Show($"Ceza başarıyla ödendi!\nMakbuz No: {dekont}", "Tahsil Edildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        VerileriYukle();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ödeme Başarısız", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };

            grpCeza.Controls.Add(dgvCezalar);
            grpCeza.Controls.Add(btnCezaOde);

            split.Panel1.Controls.Add(grpUye);
            split.Panel2.Controls.Add(grpCeza);
            tab.Controls.Add(split);
            return tab;
        }
        #endregion

        #region 5. UML & MİMARİ RAPOR SEKMESİ (HOCAYA SUNUM)
        private TabPage OlusturUMLRaporSekmesi()
        {
            TabPage tab = new TabPage("  🏛️ Canlı UML & Mimari Kanıtı  ");
            tab.BackColor = Color.White;

            RichTextBox rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10f),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(226, 232, 240),
                Padding = new Padding(20)
            };

            rtb.Text = @"========================================================================================
YAZILIM TASARIM MİMARİSİ DERSİ - ONLİNE KÜTÜPHANE YÖNETİM SİSTEMİ MİMARİ RAPORU
========================================================================================

1. SINIFLAR VE ÇOKLUKLAR (CLASS DIAGRAM İLİŞKİLERİ):
----------------------------------------------------------------------------------------
- Kullanici (Abstract) <|-- Ogrenci, Akademisyen, Kutuphaneci (Kalıtım / Inheritance)
- Materyal  (Abstract) <|-- Kitap, EKitap, Dergi, Tez (Kalıtım / Inheritance)
- Kitap *-- RafKonumu (Kompozisyon İlişkisi, 1 Kitap barındırır 1 RafKonumu)
- Kullanici (1) -------- (0..*) OduncIslemi (İlişki: gerçekleştirir)
- Materyal  (1) -------- (0..*) OduncIslemi (İlişki: konu olur)
- OduncIslemi (1) ------ (0..1) CezaKaydi   (İlişki: gecikme doğurur)
- Kullanici (1) -------- (0..*) Rezervasyon (İlişki: sıraya girer)

2. UYGULANAN GOF TASARIM DESENLERİ (DESIGN PATTERNS):
----------------------------------------------------------------------------------------
A) STRATEGY PATTERN (Ceza ve Süre Hesaplama):
   - ICezaStratejisi arayüzü; OgrenciCezaStratejisi (5 TL/gün) ve AkademisyenCezaStratejisi (2 TL/gün)
   - Runtime anında kullanıcının tipine göre 'CezaContext' nesnesi stratejiyi dinamik işletir.

B) OBSERVER PATTERN (Rezervasyon Bildirimi):
   - KitapSubject ve IKitapObserver arayüzleri.
   - Tükenmiş bir kitap iade edildiğinde ('IadeAl'), KitapSubject sıradaki öğrenciye anında bildirim gönderir.

C) FACTORY METHOD PATTERN (Materyal Üretimi):
   - 'MateryalFabrikasi.MateryalUret()' metodu; Kitap, E-Kitap, Dergi veya Tez nesnelerini
     polimorfik olarak ortak fabrika üzerinden üretir.

D) SINGLETON PATTERN (Merkezi Veri Havuzu):
   - 'KutuphaneVeriDeposu.Ornek' nesnesi; tüm sistem genelinde tekil bir durum ve veri deposu sağlar.

3. MANTIKSAL PAKETLER (PACKAGE DIAGRAM - 4 KATMAN):
----------------------------------------------------------------------------------------
[KutuphaneOtomasyonu.UI]       -> Formlar, DataGridView'ler, Kullanıcı Arayüzü
[KutuphaneOtomasyonu.Services] -> KutuphaneYonetimServisi (İş Mantığı ve Kurallar)
[KutuphaneOtomasyonu.Core]     -> Kullanici, Materyal, Islemler, RafKonumu (Etki Alanı Varlıkları)
[KutuphaneOtomasyonu.Patterns] -> Strategy, Observer, Factory, Singleton (Mimari Desenler)
========================================================================================";

            tab.Controls.Add(rtb);
            return tab;
        }
        #endregion

        #region YARDIMCI METOTLAR & VERİ YÜKLEME
        private DataGridView DataGridViewOlustur()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false
            };
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(79, 70, 229);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            return dgv;
        }

        private void KullaniciSeciminiGuncelle()
        {
            if (cmbKullaniciSecici.SelectedItem is Kullanici u)
            {
                _aktifKullanici = u;
                lblKullaniciBilgi.Text = $"{u.Rol} (Ödünç Limiti: {u.GetMaksimumOduncSayisi()} Kitap - {u.GetOduncSuresiGun()} Gün)";
                lblBakiyeBilgi.Text = $"Cüzdan: {u.Bakiye:C2}";
                lblCezaBilgi.Text = $"Borç: {u.CezaBorcu:C2}";
                VerileriYukle();
            }
        }

        private void VerileriYukle()
        {
            if (_aktifKullanici == null) return;

            // 1. KPI'lar
            lblKpiToplamKitap.Text = _servis.TumMateryaller().Count.ToString();
            lblKpiAktifOdunc.Text = _servis.TumIslemler().Count(i => i.Durum != "IADE_EDILDI").ToString();
            lblKpiGecikmis.Text = _servis.TumIslemler().Count(i => i.GeciktiMi()).ToString();
            decimal toplamTahsilat = _servis.TumCezalar().Where(c => c.OdendiMi).Sum(c => c.Tutar);
            lblKpiToplamCeza.Text = toplamTahsilat.ToString("C2");

            // 2. Aktif Ödünçlerim
            dgvAktifOdunclerim.Rows.Clear();
            var benimIslemlerim = _servis.TumIslemler().Where(i => i.KullaniciId == _aktifKullanici.Id && i.Durum != "IADE_EDILDI");
            foreach (var i in benimIslemlerim)
            {
                var mat = _servis.TumMateryaller().FirstOrDefault(m => m.Id == i.MateryalId);
                dgvAktifOdunclerim.Rows.Add(i.IslemId, mat?.Baslik, mat?.Kategori, i.AlisTarihi.ToString("dd.MM.yyyy"), i.SonTeslimTarihi.ToString("dd.MM.yyyy"), $"{i.UzatmaSayisi}/2", i.GeciktiMi() ? "⚠️ GECİKTİ" : "Ödünçte");
            }

            // 3. Katalog
            KatalogFiltrele();

            // 4. Banko ComboBox'ları
            cmbBankoKullanici.Items.Clear();
            foreach (var u in _servis.TumKullanicilar()) cmbBankoKullanici.Items.Add(u);
            if (cmbBankoKullanici.Items.Count > 0) cmbBankoKullanici.SelectedIndex = 0;

            cmbBankoMateryal.Items.Clear();
            foreach (var m in _servis.TumMateryaller().Where(mat => mat.MevcutMu())) cmbBankoMateryal.Items.Add(m);
            if (cmbBankoMateryal.Items.Count > 0) cmbBankoMateryal.SelectedIndex = 0;

            cmbBankoIadeIslem.Items.Clear();
            foreach (var i in _servis.TumIslemler().Where(islem => islem.Durum != "IADE_EDILDI"))
            {
                var usr = _servis.TumKullanicilar().FirstOrDefault(u => u.Id == i.KullaniciId);
                var mtr = _servis.TumMateryaller().FirstOrDefault(m => m.Id == i.MateryalId);
                cmbBankoIadeIslem.Items.Add(new ComboboxItem { Text = $"#{i.IslemId} - {usr?.AdSoyad} : {mtr?.Baslik}", Value = i.IslemId });
            }
            if (cmbBankoIadeIslem.Items.Count > 0) cmbBankoIadeIslem.SelectedIndex = 0;

            // 5. Üyeler Tablosu
            dgvUyeler.Rows.Clear();
            foreach (var u in _servis.TumKullanicilar())
            {
                dgvUyeler.Rows.Add(u.Id, u.AdSoyad, u.Rol, u.GetMaksimumOduncSayisi(), u.GetOduncSuresiGun(), u.Bakiye.ToString("C2"), u.CezaBorcu.ToString("C2"), u.Barkod);
            }

            // 6. Cezalar Tablosu
            dgvCezalar.Rows.Clear();
            foreach (var c in _servis.TumCezalar())
            {
                var usr = _servis.TumKullanicilar().FirstOrDefault(u => u.Id == c.KullaniciId);
                dgvCezalar.Rows.Add(c.CezaId, usr?.AdSoyad, c.GecikmeGunu, c.Tutar.ToString("C2"), c.OdendiMi ? "✅ ÖDENDİ" : "❌ ÖDENMEDİ", c.MakbuzNo);
            }

            // Bakiye & Ceza yazıları
            lblBakiyeBilgi.Text = $"Cüzdan: {_aktifKullanici.Bakiye:C2}";
            lblCezaBilgi.Text = $"Borç: {_aktifKullanici.CezaBorcu:C2}";
        }

        private void KatalogFiltrele()
        {
            dgvKatalog.Rows.Clear();
            string arama = txtKatalogArama?.Text?.ToLower() ?? "";
            string kat = cmbKatalogKategori?.SelectedItem?.ToString() ?? "Tümü (Hepsi)";

            var liste = _servis.TumMateryaller().Where(m =>
            {
                bool kOk = kat == "Tümü (Hepsi)" || m.Kategori == kat;
                bool aOk = string.IsNullOrEmpty(arama) || m.Baslik.ToLower().Contains(arama) || (m.Yazar != null && m.Yazar.ToLower().Contains(arama));
                return kOk && aOk;
            });

            foreach (var m in liste)
            {
                string konum = (m is Kitap k) ? k.Konum.TamKonum() : "Dijital / Online";
                dgvKatalog.Rows.Add(m.Id, m.Baslik, m.Yazar, m.Kategori, m.GetMateryalTuru(), $"{m.MevcutStok}/{m.ToplamStok}", konum);
            }
        }
        #endregion

        #region BUTON OLAYLARI
        private void BtnSureUzat_Click(object sender, EventArgs e)
        {
            if (dgvAktifOdunclerim.SelectedRows.Count > 0)
            {
                int islemId = Convert.ToInt32(dgvAktifOdunclerim.SelectedRows[0].Cells["IslemId"].Value);
                try
                {
                    var yeniTarih = _servis.SureUzat(islemId);
                    MessageBox.Show($"Kitap süresi 7 gün uzatıldı!\nYeni Teslim Tarihi: {yeniTarih:dd.MM.yyyy}", "Süre Uzatıldı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    VerileriYukle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnIadeEt_Click(object sender, EventArgs e)
        {
            if (dgvAktifOdunclerim.SelectedRows.Count > 0)
            {
                int islemId = Convert.ToInt32(dgvAktifOdunclerim.SelectedRows[0].Cells["IslemId"].Value);
                try
                {
                    var sonuc = _servis.IadeAl(islemId);
                    string msg = "Kitap başarıyla iade edildi!";
                    if (sonuc.cezaTutari > 0) msg += $"\n⚠️ {sonuc.cezaTutari:C2} gecikme cezası yansıtıldı!";
                    if (sonuc.bildirimGitti) msg += "\n🔔 [Observer]: Rezerve eden sıradaki kullanıcıya bildirim gitti!";

                    MessageBox.Show(msg, "İade Edildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    VerileriYukle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnOduncAl_Click(object sender, EventArgs e)
        {
            if (dgvKatalog.SelectedRows.Count > 0)
            {
                int matId = Convert.ToInt32(dgvKatalog.SelectedRows[0].Cells["Id"].Value);
                try
                {
                    var islem = _servis.OduncVer(_aktifKullanici.Id, matId);
                    MessageBox.Show($"Kitap ödünç alındı!\nSon Teslim Tarihi: {islem.SonTeslimTarihi:dd.MM.yyyy}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    VerileriYukle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnRezerveEt_Click(object sender, EventArgs e)
        {
            if (dgvKatalog.SelectedRows.Count > 0)
            {
                int matId = Convert.ToInt32(dgvKatalog.SelectedRows[0].Cells["Id"].Value);
                try
                {
                    _servis.RezerveEt(_aktifKullanici.Id, matId);
                    MessageBox.Show("Rezervasyon sırasına alındınız!\nKitap iade edildiğinde Observer bildirim gönderecektir.", "Rezerve Edildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    VerileriYukle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnYeniMateryalKaydet_Click(object sender, EventArgs e)
        {
            string baslik = txtYeniBaslik.Text.Trim();
            string yazar = txtYeniYazar.Text.Trim();
            string isbn = txtYeniIsbn.Text.Trim();
            string kat = txtYeniKategori.Text.Trim();
            int stok = (int)numYeniStok.Value;
            string tur = cmbYeniTur.SelectedItem?.ToString() ?? "KITAP";

            if (string.IsNullOrEmpty(baslik))
            {
                MessageBox.Show("Lütfen eser adını giriniz!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int yeniId = _servis.TumMateryaller().Any() ? _servis.TumMateryaller().Max(m => m.Id) + 1 : 101;
            
            // Factory Method ile Materyal Üretimi
            var yeniMat = MateryalFabrikasi.MateryalUret(tur, yeniId, isbn, baslik, yazar, "Üniversite Yayınları", 2026, kat, stok);
            _servis.TumMateryaller().Add(yeniMat);

            MessageBox.Show($"Yeni materyal başarıyla üretildi ve kataloğa eklendi!\nTürü: {yeniMat.GetMateryalTuru()}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtYeniBaslik.Clear();
            txtYeniYazar.Clear();
            txtYeniIsbn.Clear();
            VerileriYukle();
        }

        private void BtnBakiyeYukle_Click(object sender, EventArgs e)
        {
            _servis.BakiyeYukle(_aktifKullanici.Id, 50.0m);
            MessageBox.Show($"Hesabınıza 50.00 TL yüklendi!\nYeni Bakiye: {_aktifKullanici.Bakiye:C2}", "Bakiye Yüklendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            VerileriYukle();
        }
        #endregion
    }

    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public override string ToString() => Text;
    }
}
