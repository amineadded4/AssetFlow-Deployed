using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleIdToIncident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArticleId",
                table: "Incidents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ArticleId",
                table: "Incidents",
                column: "ArticleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_ArticlesIndividuels_ArticleId",
                table: "Incidents",
                column: "ArticleId",
                principalTable: "ArticlesIndividuels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_ArticlesIndividuels_ArticleId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ArticleId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "Incidents");
        }
    }
}
