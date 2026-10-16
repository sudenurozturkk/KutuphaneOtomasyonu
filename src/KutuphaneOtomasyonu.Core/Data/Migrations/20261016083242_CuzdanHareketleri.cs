using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KutuphaneOtomasyonu.Data.Migrations
{
    /// <inheritdoc />
    public partial class CuzdanHareketleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuzdanHareketleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UyeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tip = table.Column<int>(type: "INTEGER", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuzdanHareketleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuzdanHareketleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CuzdanHareketleri_Uyeler_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Uyeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuzdanHareketleri_KullaniciId",
                table: "CuzdanHareketleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_CuzdanHareketleri_UyeId_Tarih",
                table: "CuzdanHareketleri",
                columns: new[] { "UyeId", "Tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuzdanHareketleri");
        }
    }
}
