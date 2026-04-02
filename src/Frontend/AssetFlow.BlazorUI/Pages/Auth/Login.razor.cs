using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AssetFlow.BlazorUI.DTOs;
namespace AssetFlow.BlazorUI.Pages.Auth
{
    public partial class Login
    {
        // Services injectés
        [Inject] private AuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // Paramètre de l'URL : ex /login?role=IT
        [SupplyParameterFromQuery]
        private string Role { get; set; } = "IT";

        // Champs du formulaire liés au HTML
        private string Email { get; set; } = string.Empty;
        private string Password { get; set; } = string.Empty;

        // États de l'UI
        private bool IsLoading { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;
        private bool EmailError { get; set; } = false;

        // Propriétés calculées selon le rôle
        private string RoleLabel => Role switch
        {
            "IT" => "IT",
            "EquipeAchat" => "Équipe Achat",
            "Employe" => "Employé",
            "Admin" => "Admin",
            _ => Role
        };

        private string RoleColor => Role switch
        {
            "IT" => "#7C3AED",          // Violet
            "EquipeAchat" => "#10B981", // Vert
            "Employe" => "#F59E0B",     // Orange
            "Admin" => "#EF4444",       // Rouge
            _ => "#2563EB"
        };

        private string RoleDescription => Role switch
        {
            "IT" => "Vous gérez le matériel, les affectations et les demandes d'achat.",
            "EquipeAchat" => "Vous gérez les offres fournisseurs, l'OCR automatique et le scoring IA.",
            "Employe" => "Vous consultez vos équipements et signalez des incidents.",
            _ => "Accès administrateur complet."
        };

        // Valide les champs puis appelle le service d'auth
        private async Task HandleLogin()
        {
            // Réinitialiser les erreurs
            ErrorMessage = string.Empty;
            EmailError = false;

            // Validation email basique
            if (!Email.Contains("@"))
            {
                EmailError = true;
                return;
            }

            // Validation mot de passe
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Veuillez entrer votre mot de passe.";
                return;
            }

            IsLoading = true;

            // Appel au service d'authentification
            var request = new LoginRequest
            {
                Email = Email,
                Password = Password,
                Role = Role
            };

            var (success, message) = await AuthService.LoginAsync(request);

            IsLoading = false;

            if (success)
            {
                // Rediriger vers le tableau de bord selon le rôle
                var dashboard = Role switch
                {
                    "IT" => "/dashboard/it",
                    "EquipeAchat" => "/statistiques",
                    "Employe" => "/employe/equipements",
                    "Admin" => "/admin/projets",
                    _ => "/dashboard"
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