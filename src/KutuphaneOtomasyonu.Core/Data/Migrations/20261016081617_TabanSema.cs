using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KutuphaneOtomasyonu.Data.Migrations
{
    /// <inheritdoc />
    public partial class TabanSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AkademikYayinlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Baslik = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Yazar = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Danisman = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Tur = table.Column<int>(type: "INTEGER", nullable: false),
                    Universite = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    EnstituFakulte = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    AnabilimDali = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Yil = table.Column<int>(type: "INTEGER", nullable: false),
                    DOI = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AnahtarKelimeler = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Ozet = table.Column<string>(type: "TEXT", nullable: false),
                    MevcutKampus = table.Column<int>(type: "INTEGER", nullable: false),
                    AlintiSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    IndirmeSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    Dil = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DosyaBoyutu = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AkademikYayinlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kitaplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Baslik = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Yazar = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ISBN = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Yayinevi = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    YayinYili = table.Column<int>(type: "INTEGER", nullable: false),
                    Kategori = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AltKategori = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Dil = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SayfaSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    ToplamAdet = table.Column<int>(type: "INTEGER", nullable: false),
                    MevcutAdet = table.Column<int>(type: "INTEGER", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    KapakResimYolu = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RafKonumu = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SatinAlmaFiyati = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false),
                    Puan = table.Column<double>(type: "REAL", nullable: false),
                    PuanSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    GoruntulemeSayisi = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kitaplar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Soyad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KullaniciAdi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SifreHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SonGirisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FotografYolu = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalonRezervasyonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MasaKodu = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Bolge = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Kullanici = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ZamanAraligi = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Sure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Amac = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Durum = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalonRezervasyonlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SistemAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KutuphaneAdi = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Adres = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    GunlukCezaTutari = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxOduncSuresi = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxOduncAdet = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxUzatmaSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    RezervasyonGecerlilikSuresi = table.Column<int>(type: "INTEGER", nullable: false),
                    OtomatikEmailBildirimi = table.Column<bool>(type: "INTEGER", nullable: false),
                    OtomatikSmsBildirimi = table.Column<bool>(type: "INTEGER", nullable: false),
                    IadeHatirlatmaGunSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    LogoYolu = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Tema = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemAyarlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Uyeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Soyad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Adres = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TCKimlikNo = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    OgrenciNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Bolum = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Fakulte = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UyeTipi = table.Column<int>(type: "INTEGER", nullable: false),
                    Durumu = table.Column<int>(type: "INTEGER", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UyelikBitisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SonGirisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FotografYolu = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MaxOduncAdet = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxOduncSuresi = table.Column<int>(type: "INTEGER", nullable: false),
                    ToplamCeza = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OdenenCeza = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Notlar = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uyeler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AktiviteLoglar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: true),
                    Islem = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Detay = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IPAdresi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Seviye = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AktiviteLoglar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AktiviteLoglar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UyeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Baslik = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Mesaj = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Tipi = table.Column<int>(type: "INTEGER", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Okundu = table.Column<bool>(type: "INTEGER", nullable: false),
                    OkunmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Uyeler_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Uyeler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Favoriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UyeId = table.Column<int>(type: "INTEGER", nullable: false),
                    KitapId = table.Column<int>(type: "INTEGER", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoriler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favoriler_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favoriler_Uyeler_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Uyeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitapYorumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KitapId = table.Column<int>(type: "INTEGER", nullable: false),
                    UyeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Puan = table.Column<int>(type: "INTEGER", nullable: false),
                    Yorum = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    YorumTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Onaylandi = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitapYorumlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitapYorumlari_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitapYorumlari_Uyeler_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Uyeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MateryalTransferler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KitapId = table.Column<int>(type: "INTEGER", nullable: true),
                    AkademikYayinId = table.Column<int>(type: "INTEGER", nullable: true),
                    TalepEdenUyeId = table.Column<int>(type: "INTEGER", nullable: false),
                    KaynakKampus = table.Column<int>(type: "INTEGER", nullable: false),
                    HedefKampus = table.Column<int>(type: "INTEGER", nullable: false),
                    TalepTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TahminiVarisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Durum = table.Column<int>(type: "INTEGER", nullable: false),
                    KuryeTakipKodu = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MateryalTransferler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MateryalTransferler_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MateryalTransferler_Uyeler_TalepEdenUyeId",
                        column: x => x.TalepEdenUyeId,
                        principalTable: "Uyeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Oduncler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KitapId = table.Column<int>(type: "INTEGER", nullable: false),
                    UyeId = table.Column<int>(type: "INTEGER", nullable: false),
                    KutuphanecId = table.Column<int>(type: "INTEGER", nullable: true),
                    OduncTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlanlananIadeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GercekIadeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Durumu = table.Column<int>(type: "INTEGER", nullable: false),
                    UzatmaSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxUzatmaSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    CezaTutari = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CezaOdendi = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notlar = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IadeNotlari = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IadedekiKitapDurumu = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oduncler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Oduncler_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oduncler_Kullanicilar_KutuphanecId",
                        column: x => x.KutuphanecId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Oduncler_Uyeler_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Uyeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rezervasyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KitapId = table.Column<int>(type: "INTEGER", nullable: false),
                    UyeId = table.Column<int>(type: "INTEGER", nullable: false),
                    RezervasyonTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GecerlilikTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Durumu = table.Column<int>(type: "INTEGER", nullable: false),
                    SiraNumarasi = table.Column<int>(type: "INTEGER", nullable: false),
                    Notlar = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rezervasyonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rezervasyonlar_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rezervasyonlar_Uyeler_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Uyeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CezaKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UyeId = table.Column<int>(type: "INTEGER", nullable: false),
                    OduncId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tipi = table.Column<int>(type: "INTEGER", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OlusmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Odendi = table.Column<bool>(type: "INTEGER", nullable: false),
                    OdemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CezaKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CezaKayitlari_Oduncler_OduncId",
                        column: x => x.OduncId,
                        principalTable: "Oduncler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CezaKayitlari_Uyeler_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Uyeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AktiviteLoglar_KullaniciId",
                table: "AktiviteLoglar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_UyeId",
                table: "Bildirimler",
                column: "UyeId");

            migrationBuilder.CreateIndex(
                name: "IX_CezaKayitlari_OduncId",
                table: "CezaKayitlari",
                column: "OduncId");

            migrationBuilder.CreateIndex(
                name: "IX_CezaKayitlari_UyeId",
                table: "CezaKayitlari",
                column: "UyeId");

            migrationBuilder.CreateIndex(
                name: "IX_Favoriler_KitapId",
                table: "Favoriler",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_Favoriler_UyeId_KitapId",
                table: "Favoriler",
                columns: new[] { "UyeId", "KitapId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_Baslik",
                table: "Kitaplar",
                column: "Baslik");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_ISBN",
                table: "Kitaplar",
                column: "ISBN",
                unique: true,
                filter: "[ISBN] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_Kategori",
                table: "Kitaplar",
                column: "Kategori");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_Yazar",
                table: "Kitaplar",
                column: "Yazar");

            migrationBuilder.CreateIndex(
                name: "IX_KitapYorumlari_KitapId",
                table: "KitapYorumlari",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_KitapYorumlari_UyeId",
                table: "KitapYorumlari",
                column: "UyeId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_KullaniciAdi",
                table: "Kullanicilar",
                column: "KullaniciAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MateryalTransferler_KitapId",
                table: "MateryalTransferler",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_MateryalTransferler_TalepEdenUyeId",
                table: "MateryalTransferler",
                column: "TalepEdenUyeId");

            migrationBuilder.CreateIndex(
                name: "IX_Oduncler_KitapId",
                table: "Oduncler",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_Oduncler_KutuphanecId",
                table: "Oduncler",
                column: "KutuphanecId");

            migrationBuilder.CreateIndex(
                name: "IX_Oduncler_UyeId_KitapId",
                table: "Oduncler",
                columns: new[] { "UyeId", "KitapId" },
                unique: true,
                filter: "[Durumu] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_KitapId",
                table: "Rezervasyonlar",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_UyeId",
                table: "Rezervasyonlar",
                column: "UyeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalonRezervasyonlari_MasaKodu_Tarih_ZamanAraligi",
                table: "SalonRezervasyonlari",
                columns: new[] { "MasaKodu", "Tarih", "ZamanAraligi" },
                unique: true,
                filter: "[Durum] = 'Aktif'");

            migrationBuilder.CreateIndex(
                name: "IX_Uyeler_Email",
                table: "Uyeler",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uyeler_OgrenciNo",
                table: "Uyeler",
                column: "OgrenciNo",
                filter: "[OgrenciNo] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Uyeler_TCKimlikNo",
                table: "Uyeler",
                column: "TCKimlikNo",
                unique: true);

            // Stok değişmezi: 0 <= MevcutAdet <= ToplamAdet (EF modelinde HasTrigger ile bildirilir).
            migrationBuilder.Sql(VeritabaniSemasi.StokTetikleyiciEklemeSql);
            migrationBuilder.Sql(VeritabaniSemasi.StokTetikleyiciGuncellemeSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS \"{KutuphaneDbContext.StokTetikleyiciEkleme}\";");
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS \"{KutuphaneDbContext.StokTetikleyiciGuncelleme}\";");

            migrationBuilder.DropTable(
                name: "AkademikYayinlar");

            migrationBuilder.DropTable(
                name: "AktiviteLoglar");

            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "CezaKayitlari");

            migrationBuilder.DropTable(
                name: "Favoriler");

            migrationBuilder.DropTable(
                name: "KitapYorumlari");

            migrationBuilder.DropTable(
                name: "MateryalTransferler");

            migrationBuilder.DropTable(
                name: "Rezervasyonlar");

            migrationBuilder.DropTable(
                name: "SalonRezervasyonlari");

            migrationBuilder.DropTable(
                name: "SistemAyarlari");

            migrationBuilder.DropTable(
                name: "Oduncler");

            migrationBuilder.DropTable(
                name: "Kitaplar");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "Uyeler");
        }
    }
}
