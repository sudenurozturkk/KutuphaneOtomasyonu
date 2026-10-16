using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KutuphaneOtomasyonu.Data.Migrations
{
    /// <inheritdoc />
    public partial class KitapNushalari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NushaId",
                table: "Oduncler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KitapNushalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KitapId = table.Column<int>(type: "INTEGER", nullable: false),
                    Barkod = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Durum = table.Column<int>(type: "INTEGER", nullable: false),
                    RafKonumu = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurumTarihi = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitapNushalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitapNushalari_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Oduncler_NushaId",
                table: "Oduncler",
                column: "NushaId",
                unique: true,
                filter: "[Durumu] = 0 AND [NushaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KitapNushalari_Barkod",
                table: "KitapNushalari",
                column: "Barkod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitapNushalari_KitapId_Durum",
                table: "KitapNushalari",
                columns: new[] { "KitapId", "Durum" });

            migrationBuilder.AddForeignKey(
                name: "FK_Oduncler_KitapNushalari_NushaId",
                table: "Oduncler",
                column: "NushaId",
                principalTable: "KitapNushalari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // --- Veri geçişi: mevcut sayaçlardan nüsha kayıtları üret ---

            // 1) Her kitap için ToplamAdet kadar rafta nüsha (barkod: KB + 6 hane kitap no + 4 hane sıra).
            migrationBuilder.Sql("""
                WITH RECURSIVE sira(n) AS (
                    SELECT 1 WHERE (SELECT COALESCE(MAX("ToplamAdet"), 0) FROM "Kitaplar") >= 1
                    UNION ALL
                    SELECT n + 1 FROM sira WHERE n < (SELECT MAX("ToplamAdet") FROM "Kitaplar")
                )
                INSERT INTO "KitapNushalari" ("KitapId", "Barkod", "Durum", "RafKonumu", "EklenmeTarihi")
                SELECT k."Id", 'KB' || printf('%06d%04d', k."Id", sira.n), 0, k."RafKonumu", k."EklenmeTarihi"
                FROM "Kitaplar" k JOIN sira ON sira.n <= k."ToplamAdet";
                """);

            // 2) Aktif ödünçleri aynı kitabın farklı nüshalarıyla eşleştir ve bu nüshaları "Oduncte" yap.
            migrationBuilder.Sql("""
                UPDATE "Oduncler" SET "NushaId" = (
                    SELECT n."Id"
                    FROM (SELECT "Id", "KitapId", ROW_NUMBER() OVER (PARTITION BY "KitapId" ORDER BY "Id") AS rn
                          FROM "KitapNushalari") n
                    JOIN (SELECT "Id", "KitapId", ROW_NUMBER() OVER (PARTITION BY "KitapId" ORDER BY "Id") AS rn
                          FROM "Oduncler" WHERE "Durumu" = 0) o
                      ON o."KitapId" = n."KitapId" AND o.rn = n.rn
                    WHERE o."Id" = "Oduncler"."Id")
                WHERE "Durumu" = 0;
                """);
            migrationBuilder.Sql("""
                UPDATE "KitapNushalari" SET "Durum" = 1
                WHERE "Id" IN (SELECT "NushaId" FROM "Oduncler" WHERE "Durumu" = 0 AND "NushaId" IS NOT NULL);
                """);

            // 3) Ne rafta (MevcutAdet) ne ödünçte olan nüshalar eski hasarlı-iade hatasından kalmadır: dolaşım dışı işaretle.
            migrationBuilder.Sql("""
                UPDATE "KitapNushalari" SET "Durum" = 2
                WHERE "Id" IN (
                    SELECT r."Id"
                    FROM (SELECT "Id", "KitapId", ROW_NUMBER() OVER (PARTITION BY "KitapId" ORDER BY "Id") AS rn
                          FROM "KitapNushalari" WHERE "Durum" = 0) r
                    JOIN "Kitaplar" k ON k."Id" = r."KitapId"
                    WHERE r.rn > k."MevcutAdet");
                """);

            // 4) Sayaçları nüsha durumlarından yeniden hesapla (tek UPDATE: stok tetikleyicisi son durumu doğrular).
            migrationBuilder.Sql("""
                UPDATE "Kitaplar" SET
                    "ToplamAdet" = (SELECT COUNT(*) FROM "KitapNushalari" n WHERE n."KitapId" = "Kitaplar"."Id" AND n."Durum" IN (0, 1)),
                    "MevcutAdet" = (SELECT COUNT(*) FROM "KitapNushalari" n WHERE n."KitapId" = "Kitaplar"."Id" AND n."Durum" = 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Oduncler_KitapNushalari_NushaId",
                table: "Oduncler");

            migrationBuilder.DropTable(
                name: "KitapNushalari");

            migrationBuilder.DropIndex(
                name: "IX_Oduncler_NushaId",
                table: "Oduncler");

            migrationBuilder.DropColumn(
                name: "NushaId",
                table: "Oduncler");
        }
    }
}
