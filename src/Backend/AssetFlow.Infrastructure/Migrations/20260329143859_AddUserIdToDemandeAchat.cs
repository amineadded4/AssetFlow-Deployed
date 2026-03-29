using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToDemandeAchat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DemandeAchat",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DemandeAchat_UserId",
                table: "DemandeAchat",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandeAchat_Users_UserId",
                table: "DemandeAchat",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandeAchat_Users_UserId",
                table: "DemandeAchat");

            migrationBuilder.DropIndex(
                name: "IX_DemandeAchat_UserId",
                table: "DemandeAchat");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DemandeAchat");
        }
    }
}
