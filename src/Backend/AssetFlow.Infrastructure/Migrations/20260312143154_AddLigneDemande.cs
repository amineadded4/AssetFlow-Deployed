using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLigneDemande : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LigneDemande",
                columns: table => new
                {
                    IdLigne = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDemande = table.Column<int>(type: "int", nullable: false),
                    NomProduit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LigneDemande", x => x.IdLigne);
                    table.ForeignKey(
                        name: "FK_LigneDemande_DemandeAchat_IdDemande",
                        column: x => x.IdDemande,
                        principalTable: "DemandeAchat",
                        principalColumn: "IdDemande",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LigneDemande_IdDemande",
                table: "LigneDemande",
                column: "IdDemande");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LigneDemande");
        }
    }
}
