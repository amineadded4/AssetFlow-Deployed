using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFournisseurTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fournisseur",
                columns: table => new
                {
                    IdFournisseur = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "varchar(100)", nullable: false),
                    Telephone = table.Column<string>(type: "varchar(20)", nullable: true),
                    Adresse = table.Column<string>(type: "varchar(255)", nullable: true),
                    Mail = table.Column<string>(type: "varchar(150)", nullable: true),
                    
                    // === NOUVEAUX CHAMPS AJOUTÉS ===
                    CommandesTotales = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TauxLivraisonATemps = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    ScoreFiabilite = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    DerniereCommande = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fournisseur", x => x.IdFournisseur);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fournisseur");
        }
    }
}