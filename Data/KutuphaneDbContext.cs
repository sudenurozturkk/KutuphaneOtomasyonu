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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KutuphaneOtomasyonu",
                "kutuphane.db"
            );

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

            modelBuilder.Entity<Uye>()
                .HasIndex(u => u.TCKimlikNo)
                .IsUnique();

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

            modelBuilder.Entity<SalonRezervasyonu>()
                .HasIndex(r => new { r.MasaKodu, r.Tarih, r.ZamanAraligi })
                .IsUnique()
                .HasFilter("[Durum] = 'Aktif'");
        }
    }
}
