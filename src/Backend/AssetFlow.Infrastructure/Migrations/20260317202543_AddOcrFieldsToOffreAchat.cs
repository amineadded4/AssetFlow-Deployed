using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrFieldsToOffreAchat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DelaiLivraison",
                table: "OffreAchat",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FraisLivraison",
                table: "OffreAchat",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Garantie",
                table: "OffreAchat",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrixTotal",
                table: "OffreAchat",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelaiLivraison",
                table: "OffreAchat");

            migrationBuilder.DropColumn(
                name: "FraisLivraison",
                table: "OffreAchat");

            migrationBuilder.DropColumn(
                name: "Garantie",
                table: "OffreAchat");

            migrationBuilder.DropColumn(
                name: "PrixTotal",
                table: "OffreAchat");
        }
    }
}
