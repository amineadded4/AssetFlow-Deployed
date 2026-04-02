using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesInFrench : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Affectations_Projects_ProjetId",
                table: "Affectations");

            migrationBuilder.DropForeignKey(
                name: "FK_Affectations_Users_UtilisateurId",
                table: "Affectations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_ReceiverId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_CommentairesMateriel_Users_UtilisateurId",
                table: "CommentairesMateriel");

            migrationBuilder.DropForeignKey(
                name: "FK_DemandeAchat_Users_UserId",
                table: "DemandeAchat");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Utilisateurs");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Projets");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "Utilisateurs",
                newName: "IX_Utilisateurs_Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Utilisateurs",
                table: "Utilisateurs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projets",
                table: "Projets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Affectations_Projets_ProjetId",
                table: "Affectations",
                column: "ProjetId",
                principalTable: "Projets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Affectations_Utilisateurs_UtilisateurId",
                table: "Affectations",
                column: "UtilisateurId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Utilisateurs_ReceiverId",
                table: "ChatMessages",
                column: "ReceiverId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Utilisateurs_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentairesMateriel_Utilisateurs_UtilisateurId",
                table: "CommentairesMateriel",
                column: "UtilisateurId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DemandeAchat_Utilisateurs_UserId",
                table: "DemandeAchat",
                column: "UserId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Affectations_Projets_ProjetId",
                table: "Affectations");

            migrationBuilder.DropForeignKey(
                name: "FK_Affectations_Utilisateurs_UtilisateurId",
                table: "Affectations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Utilisateurs_ReceiverId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Utilisateurs_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_CommentairesMateriel_Utilisateurs_UtilisateurId",
                table: "CommentairesMateriel");

            migrationBuilder.DropForeignKey(
                name: "FK_DemandeAchat_Utilisateurs_UserId",
                table: "DemandeAchat");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Utilisateurs",
                table: "Utilisateurs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projets",
                table: "Projets");

            migrationBuilder.RenameTable(
                name: "Utilisateurs",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Projets",
                newName: "Projects");

            migrationBuilder.RenameIndex(
                name: "IX_Utilisateurs_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Affectations_Projects_ProjetId",
                table: "Affectations",
                column: "ProjetId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Affectations_Users_UtilisateurId",
                table: "Affectations",
                column: "UtilisateurId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_ReceiverId",
                table: "ChatMessages",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentairesMateriel_Users_UtilisateurId",
                table: "CommentairesMateriel",
                column: "UtilisateurId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DemandeAchat_Users_UserId",
                table: "DemandeAchat",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
