using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Pages.Auth
{
    public partial class Login
    {
        [Inject] private AuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        [SupplyParameterFromQuery]
        private string Role { get; set; } = "IT";

        private string Email    { get; set; } = string.Empty;
        private string Password { get; set; } = string.Empty;
        private bool   IsLoading    { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;
        private bool   EmailError   { get; set; } = false;

        private string RoleLabel => Role switch
        {
            "IT"          => "IT",
            "EquipeAchat" => "Équipe Achat",
            "Employe"     => "Employé",
            "Admin"       => "Admin",
            _             => Role
        };

        private string RoleColor => Role switch
        {
            "IT"          => "#7C3AED",
            "EquipeAchat" => "#136dec",
            "Employe"     => "#F59E0B",
            "Admin"       => "#EF4444",
            _             => "#2563EB"
        };

        // Ombre colorée sous le bouton selon le rôle
        private string RoleColorShadow => Role switch
        {
            "IT"          => "rgba(124,58,237,0.35)",
            "EquipeAchat" => "rgba(19,109,236,0.35)",
            "Employe"     => "rgba(245,158,11,0.35)",
            "Admin"       => "rgba(239,68,68,0.35)",
            _             => "rgba(37,99,235,0.35)"
        };

        private string RoleDescription => Role switch
        {
            "IT"          => "Vous gérez le matériel, les affectations et les demandes d'achat.",
            "EquipeAchat" => "Vous gérez les offres fournisseurs, l'OCR automatique et le scoring IA.",
            "Employe"     => "Vous consultez vos équipements et signalez des incidents.",
            _             => "Accès administrateur complet."
        };

        private async Task HandleLogin()
        {
            ErrorMessage = string.Empty;
            EmailError   = false;

            if (!Email.Contains("@")) { EmailError = true; return; }

            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Veuillez entrer votre mot de passe.";
                return;
            }

            IsLoading = true;

            var (success, message) = await AuthService.LoginAsync(new LoginRequest
            {
                Email    = Email,
                Password = Password,
                Role     = Role
            });

            IsLoading = false;

            if (success)
            {
                var dashboard = Role switch
                {
                    "IT"          => "/dashboard/it",
                    "EquipeAchat" => "/statistiques",
                    "Employe"     => "/employe/equipements",
                    "Admin"       => "/admin/projets",
                    _             => "/dashboard"
                };
                Navigation.NavigateTo(dashboard);
            }
            else
            {
                ErrorMessage = message;
            }
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter"
                && !string.IsNullOrEmpty(Email)
                && !string.IsNullOrEmpty(Password)
                && !IsLoading)
            {
                await HandleLogin();
            }
        }
    }
}