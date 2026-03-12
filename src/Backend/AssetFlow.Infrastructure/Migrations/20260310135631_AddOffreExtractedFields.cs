using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOffreExtractedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "ContenuPdf",
                table: "OffreAchat",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DelaiLivraison",
                table: "OffreAchat",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FraisLivraison",
                table: "OffreAchat",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Garantie",
                table: "OffreAchat",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomProduit",
                table: "OffreAchat",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Prix",
                table: "OffreAchat",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceProduit",
                table: "OffreAchat",
                type: "nvarchar(100)",
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
                name: "NomProduit",
                table: "OffreAchat");

            migrationBuilder.DropColumn(
                name: "Prix",
                table: "OffreAchat");

            migrationBuilder.DropColumn(
                name: "ReferenceProduit",
                table: "OffreAchat");

            migrationBuilder.AlterColumn<byte[]>(
                name: "ContenuPdf",
                table: "OffreAchat",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");
        }
    }
}
