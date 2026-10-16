using KutuphaneOtomasyonu.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Linq;

namespace KutuphaneOtomasyonu.Data
{
    /// <summary>
    /// Veritabanı şemasının tek giriş noktası: EF Core migration'ları uygular.
    /// <para>
    /// Migration'lardan önceki sürümler şemayı <c>EnsureCreated</c> ile oluşturuyordu ve
    /// <c>__EFMigrationsHistory</c> tablosu yoktu. Böyle bir veritabanı algılandığında taban migration'ın
    /// eksik nesneleri <c>IF NOT EXISTS</c> ile oluşturulur, taban migration uygulanmış olarak işaretlenir
    /// ve sonraki migration'lar normal şekilde uygulanır. Veri silinmez.
    /// </para>
    /// </summary>
    public static class VeritabaniSemasi
    {
        public const string TabanMigrasyonId = "20261016081617_TabanSema";

        public static readonly string StokTetikleyiciEklemeSql = $"""
            CREATE TRIGGER IF NOT EXISTS "{KutuphaneDbContext.StokTetikleyiciEkleme}"
            BEFORE INSERT ON "Kitaplar"
            WHEN NEW."MevcutAdet" < 0 OR NEW."ToplamAdet" < 0 OR NEW."MevcutAdet" > NEW."ToplamAdet"
            BEGIN
                SELECT RAISE(ABORT, 'Kitap stok kurali ihlali');
            END;
            """;

        public static readonly string StokTetikleyiciGuncellemeSql = $"""
            CREATE TRIGGER IF NOT EXISTS "{KutuphaneDbContext.StokTetikleyiciGuncelleme}"
            BEFORE UPDATE OF "MevcutAdet", "ToplamAdet" ON "Kitaplar"
            WHEN NEW."MevcutAdet" < 0 OR NEW."ToplamAdet" < 0 OR NEW."MevcutAdet" > NEW."ToplamAdet"
            BEGIN
                SELECT RAISE(ABORT, 'Kitap stok kurali ihlali');
            END;
            """;

        public static void Hazirla(KutuphaneDbContext context)
        {
            if (context.Database.CanConnect() && TabloVarMi(context, "Oduncler"))
                YinelenenAktifOdunculeriOnar(context);

            if (MigrationOncesiKurulumMu(context))
                TabanlaEskiKurulumu(context);

            context.Database.Migrate();
        }

        /// <summary>
        /// Eski sürümlerdeki yarış hatası bir üyeye aynı kitabın birden fazla aktif ödüncünü verebiliyordu; yeni şemadaki
        /// benzersiz indeks bunu kabul etmez. En eski ödünç aktif kalır, sonrakiler açıklama notuyla kapatılır ve denetim
        /// kaydı yazılır. Sayaçlar değiştirilmez: nüsha geçişi karşılıksız kalan kopyayı dolaşım dışı işaretler
        /// (kopyanın rafta olduğu varsayılmaz).
        /// </summary>
        private static void YinelenenAktifOdunculeriOnar(KutuphaneDbContext context)
        {
            const string yinelenenKosulu = """
                "Durumu" = 0 AND EXISTS (
                    SELECT 1 FROM "Oduncler" o2
                    WHERE o2."Durumu" = 0 AND o2."UyeId" = "Oduncler"."UyeId"
                      AND o2."KitapId" = "Oduncler"."KitapId" AND o2."Id" < "Oduncler"."Id")
                """;

            var idler = context.Database
                .SqlQueryRaw<int>($"SELECT \"Id\" AS \"Value\" FROM \"Oduncler\" WHERE {yinelenenKosulu}")
                .ToList();
            if (idler.Count == 0) return;

            var liste = string.Join(", ", idler.Select(i => $"#{i}"));
            using var islem = context.Database.BeginTransaction();
            context.Database.ExecuteSqlRaw(
                $"UPDATE \"Oduncler\" SET \"Durumu\" = 1, \"GercekIadeTarihi\" = {{0}}, \"IadeNotlari\" = {{1}} WHERE {yinelenenKosulu}",
                DateTime.Now,
                "Otomatik veri onarimi: ayni uyeye ayni kitabin yinelenen aktif oduncu kapatildi (surum yukseltmesi).");
            if (TabloVarMi(context, "AktiviteLoglar"))
            {
                var detay = $"Yinelenen aktif odunc kapatildi: {liste}";
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO \"AktiviteLoglar\" (\"Islem\", \"Detay\", \"Tarih\", \"IPAdresi\", \"Seviye\") VALUES ({0}, {1}, {2}, '', {3})",
                    "VeriOnarimi", detay.Length <= 500 ? detay : detay[..500],
                    DateTime.Now, (int)Models.LogSeviyesi.Uyari);
            }
            islem.Commit();
            UygulamaLog.Yaz($"[UYARI] Yinelenen aktif odunc kayitlari kapatildi: {liste}");
        }

        private static bool TabloVarMi(KutuphaneDbContext context, string tablo) =>
            context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {0}", tablo)
                .Single() > 0;

        private static bool MigrationOncesiKurulumMu(KutuphaneDbContext context)
        {
            if (!context.Database.CanConnect() || !TabloVarMi(context, "Kitaplar"))
                return false;

            if (!TabloVarMi(context, "__EFMigrationsHistory"))
                return true;

            return !context.Database.GetAppliedMigrations().Any();
        }

        private static void TabanlaEskiKurulumu(KutuphaneDbContext context)
        {
            UygulamaLog.Yaz("[Sema] Migration oncesi kurulum algilandi; taban sema isaretleniyor.");

            var migrationsAssembly = context.GetService<IMigrationsAssembly>();
            var sqlUretici = context.GetService<IMigrationsSqlGenerator>();
            var gecmis = context.GetService<IHistoryRepository>();

            var tip = migrationsAssembly.Migrations[TabanMigrasyonId];
            var taban = migrationsAssembly.CreateMigration(tip, context.Database.ProviderName!);

            var tabanModeli = taban.TargetModel is IMutableModel degisken
                ? context.GetService<IModelRuntimeInitializer>().Initialize(degisken.FinalizeModel(), designTime: true, validationLogger: null)
                : taban.TargetModel;

            using var islem = context.Database.BeginTransaction();
            foreach (var komut in sqlUretici.Generate(taban.UpOperations, tabanModeli))
            {
                var sql = VarOlaniAtla(komut.CommandText);
                try
                {
                    context.Database.ExecuteSqlRaw(sql);
                }
                catch (SqliteException ex) when (sql.Contains("CREATE UNIQUE INDEX", StringComparison.Ordinal))
                {
                    // Eski veride yinelenen kayıtlar varsa benzersiz indeks oluşturulamaz; uygulama yine de açılmalı.
                    UygulamaLog.Yaz($"[UYARI] Benzersiz indeks olusturulamadi (yinelenen kayitlar olabilir): {ex.Message}");
                }
            }

            context.Database.ExecuteSqlRaw(gecmis.GetCreateIfNotExistsScript());
            context.Database.ExecuteSqlRaw(gecmis.GetInsertScript(new HistoryRow(TabanMigrasyonId, ProductInfo.GetVersion())));
            islem.Commit();
        }

        /// <summary>Taban komutlarını mevcut nesneleri atlayacak biçime çevirir.</summary>
        private static string VarOlaniAtla(string sql) => sql
            .Replace("CREATE TABLE \"", "CREATE TABLE IF NOT EXISTS \"", StringComparison.Ordinal)
            .Replace("CREATE UNIQUE INDEX \"", "CREATE UNIQUE INDEX IF NOT EXISTS \"", StringComparison.Ordinal)
            .Replace("CREATE INDEX \"", "CREATE INDEX IF NOT EXISTS \"", StringComparison.Ordinal);
    }
}
