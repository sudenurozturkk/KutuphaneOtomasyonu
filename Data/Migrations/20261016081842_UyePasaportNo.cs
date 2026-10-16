using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KutuphaneOtomasyonu.Data.Migrations
{
    /// <inheritdoc />
    public partial class UyePasaportNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Uyeler_TCKimlikNo",
                table: "Uyeler");

            migrationBuilder.AddColumn<string>(
                name: "PasaportNo",
                table: "Uyeler",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Uyeler_PasaportNo",
                table: "Uyeler",
                column: "PasaportNo",
                unique: true,
                filter: "[PasaportNo] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Uyeler_TCKimlikNo",
                table: "Uyeler",
                column: "TCKimlikNo",
                unique: true,
                filter: "[TCKimlikNo] != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Uyeler_PasaportNo",
                table: "Uyeler");

            migrationBuilder.DropIndex(
                name: "IX_Uyeler_TCKimlikNo",
                table: "Uyeler");

            migrationBuilder.DropColumn(
                name: "PasaportNo",
                table: "Uyeler");

            migrationBuilder.CreateIndex(
                name: "IX_Uyeler_TCKimlikNo",
                table: "Uyeler",
                column: "TCKimlikNo",
                unique: true);
        }
    }
}
