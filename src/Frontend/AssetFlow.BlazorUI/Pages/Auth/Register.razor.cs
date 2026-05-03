using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Pages.Auth
{
    public partial class Register
    {
        [Inject] private AuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // Champs du formulaire
        private string FirstName { get; set; } = string.Empty;
        private string LastName { get; set; } = string.Empty;
        private string Email { get; set; } = string.Empty;
        private string Password { get; set; } = string.Empty;
        private string ConfirmPassword { get; set; } = string.Empty;
        private string Department { get; set; } = string.Empty;
        private string SelectedRole { get; set; } = "IT";
        private bool AcceptTerms { get; set; } = false;

        // États UI
        private bool IsLoading { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;
        private string SuccessMessage { get; set; } = string.Empty;

        // Couleur dynamique selon le rôle sélectionné (comme Login)
        private string RoleColor => SelectedRole switch
        {
            "IT"          => "#7C3AED",
            "EquipeAchat" => "#136dec",
            "Employe"     => "#F59E0B",
            _             => "#2563EB"
        };

        private string RoleColorShadow => SelectedRole switch
        {
            "IT"          => "rgba(124,58,237,0.35)",
            "EquipeAchat" => "rgba(19,109,236,0.35)",
            "Employe"     => "rgba(245,158,11,0.35)",
            _             => "rgba(37,99,235,0.35)"
        };

        /// Calcule la force du mot de passe (1=faible, 2=moyen, 3=fort)
        private int PasswordStrength
        {
            get
            {
                if (string.IsNullOrEmpty(Password)) return 0;
                int score = 0;
                if (Password.Length >= 8) score++;
                if (Password.Any(char.IsUpper)) score++;
                if (Password.Any(char.IsDigit)) score++;
                return score;
            }
        }

        /// Traite la soumission du formulaire d'inscription
        private async Task HandleRegister()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            {
                ErrorMessage = "Prénom et nom requis.";
                return;
            }

            if (!Email.Contains("@"))
            {
                ErrorMessage = "Adresse email invalide.";
                return;
            }

            if (Password.Length < 8)
            {
                ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Les mots de passe ne correspondent pas.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedRole))
            {
                ErrorMessage = "Veuillez sélectionner un rôle.";
                return;
            }

            IsLoading = true;

            var request = new RegisterRequest
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Password = Password,
                Department = Department,
                RequestedRole = SelectedRole
            };

            var (success, message) = await AuthService.RegisterAsync(request);

            IsLoading = false;

            if (success)
            {
                SuccessMessage = message;
                await Task.Delay(2000);
                Navigation.NavigateTo("/");
            }
            else
            {
                ErrorMessage = message;
            }
        }
    }
}