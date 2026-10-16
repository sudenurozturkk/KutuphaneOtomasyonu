using Microsoft.EntityFrameworkCore;
using KutuphaneOtomasyonu.Models;
using System;
using System.IO;

namespace KutuphaneOtomasyonu.Data
{
    /// <summary>
    /// Entity Framework Core DbContext.
    /// SQLite veritabanı kullanır.
    /// Repository Pattern için temel sağlar.
    /// </summary>
    public class KutuphaneDbContext : DbContext
    {
        public KutuphaneDbContext()
        {
        }

        public KutuphaneDbContext(DbContextOptions<KutuphaneDbContext> options)
            : base(options)
        {
        }

        public DbSet<Kitap> Kitaplar { get; set; }
        public DbSet<Uye> Uyeler { get; set; }
        public DbSet<Odunc> Oduncler { get; set; }
        public DbSet<Rezervasyon> Rezervasyonlar { get; set; }
        public DbSet<KitapYorum> KitapYorumlari { get; set; }
        public DbSet<Favori> Favoriler { get; set; }
        public DbSet<Bildirim> Bildirimler { get; set; }
        public DbSet<CezaKaydi> CezaKayitlari { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<SistemAyarlari> SistemAyarlari { get; set; }
        public DbSet<AktiviteLog> AktiviteLoglar { get; set; }
        public DbSet<AkademikYayin> AkademikYayinlar { get; set; }
        public DbSet<MateryalTransfer> MateryalTransferler { get; set; }
        public DbSet<SalonRezervasyonu> SalonRezervasyonlari { get; set; }
        public DbSet<KitapNushasi> KitapNushalari { get; set; }
        public DbSet<CuzdanHareketi> CuzdanHareketleri { get; set; }

        /// <summary>
        /// Ortam değişkeni ile veritabanı dosyası değiştirilebilir (test ve taşınabilir kurulumlar için).
        /// </summary>
        public const string VeritabaniYoluOrtamDegiskeni = "KUTUPHANE_DB_PATH";

        public const string StokTetikleyiciEkleme = "TR_Kitaplar_Stok_Ekleme";
        public const string StokTetikleyiciGuncelleme = "TR_Kitaplar_Stok_Guncelleme";

        /// <summary>Uygulamanın kullandığı SQLite dosyasının tam yolu.</summary>
        public static string VeritabaniYolu
        {
            get
            {
                var ozel = Environment.GetEnvironmentVariable(VeritabaniYoluOrtamDegiskeni);
                if (!string.IsNullOrWhiteSpace(ozel))
                    return Path.GetFullPath(ozel);

                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "KutuphaneOtomasyonu",
                    "kutuphane.db");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;

            var dbPath = VeritabaniYolu;
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            optionsBuilder.EnableSensitiveDataLogging(false);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Kitap index'leri
            modelBuilder.Entity<Kitap>()
                .HasIndex(k => k.ISBN)
                .IsUnique()
                .HasFilter("[ISBN] != ''");

            // Stok tetikleyicileri taban migration içinde oluşturulur; EF'e bildirilmesi
            // tetikleyicili tablolarda güvenli SQL (RETURNING'siz) üretilmesini sağlar.
            modelBuilder.Entity<Kitap>().ToTable(tb =>
            {
                tb.HasTrigger(StokTetikleyiciEkleme);
                tb.HasTrigger(StokTetikleyiciGuncelleme);
            });

            modelBuilder.Entity<Kitap>()
                .HasIndex(k => k.Baslik);

            modelBuilder.Entity<Kitap>()
                .HasIndex(k => k.Yazar);

            modelBuilder.Entity<Kitap>()
                .HasIndex(k => k.Kategori);

            // Uye index'leri
            modelBuilder.Entity<Uye>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Kimlik numaraları yalnızca girildiklerinde benzersiz olmalıdır.
            modelBuilder.Entity<Uye>()
                .HasIndex(u => u.TCKimlikNo)
                .IsUnique()
                .HasFilter("[TCKimlikNo] != ''");

            modelBuilder.Entity<Uye>()
                .HasIndex(u => u.PasaportNo)
                .IsUnique()
                .HasFilter("[PasaportNo] != ''");

            modelBuilder.Entity<Uye>()
                .HasIndex(u => u.OgrenciNo)
                .HasFilter("[OgrenciNo] != ''");

            // Kullanici index'leri
            modelBuilder.Entity<Kullanici>()
                .HasIndex(k => k.KullaniciAdi)
                .IsUnique();

            // Favori composite key
            modelBuilder.Entity<Favori>()
                .HasIndex(f => new { f.UyeId, f.KitapId })
                .IsUnique();

            // Bir üye aynı kitabı aynı anda yalnızca bir kez ödünç alabilir.
            modelBuilder.Entity<Odunc>()
                .HasIndex(o => new { o.UyeId, o.KitapId })
                .IsUnique()
                .HasFilter("[Durumu] = 0");

            // Nüsha barkodları benzersizdir; bir nüsha aynı anda yalnızca bir aktif ödünçte olabilir.
            modelBuilder.Entity<KitapNushasi>()
                .HasIndex(n => n.Barkod)
                .IsUnique();

            modelBuilder.Entity<KitapNushasi>()
                .HasIndex(n => new { n.KitapId, n.Durum });

            modelBuilder.Entity<KitapNushasi>()
                .HasOne(n => n.Kitap)
                .WithMany(k => k.Nushalar)
                .HasForeignKey(n => n.KitapId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Odunc>()
                .HasOne(o => o.Nusha)
                .WithMany()
                .HasForeignKey(o => o.NushaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Odunc>()
                .HasIndex(o => o.NushaId)
                .IsUnique()
                .HasFilter("[Durumu] = 0 AND [NushaId] IS NOT NULL");

            // Soft delete / cascade için konfigürasyonlar
            modelBuilder.Entity<Odunc>()
                .HasOne(o => o.Kitap)
                .WithMany(k => k.Oduncler)
                .HasForeignKey(o => o.KitapId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Odunc>()
                .HasOne(o => o.Uye)
                .WithMany(u => u.Oduncler)
                .HasForeignKey(o => o.UyeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rezervasyon>()
                .HasOne(r => r.Kitap)
                .WithMany(k => k.Rezervasyonlar)
                .HasForeignKey(r => r.KitapId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KitapYorum>()
                .HasOne(y => y.Kitap)
                .WithMany(k => k.Yorumlar)
                .HasForeignKey(y => y.KitapId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal precision
            modelBuilder.Entity<Odunc>()
                .Property(o => o.CezaTutari)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Uye>()
                .Property(u => u.ToplamCeza)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Uye>()
                .Property(u => u.OdenenCeza)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Kitap>()
                .Property(k => k.SatinAlmaFiyati)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<CezaKaydi>()
                .Property(c => c.Tutar)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<CuzdanHareketi>()
                .Property(c => c.Tutar)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<CuzdanHareketi>()
                .HasIndex(c => new { c.UyeId, c.Tarih });

            modelBuilder.Entity<CuzdanHareketi>()
                .HasOne(c => c.Uye)
                .WithMany()
                .HasForeignKey(c => c.UyeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalonRezervasyonu>()
                .HasIndex(r => new { r.MasaKodu, r.Tarih, r.ZamanAraligi })
                .IsUnique()
                .HasFilter("[Durum] = 'Aktif'");
        }
    }
}
