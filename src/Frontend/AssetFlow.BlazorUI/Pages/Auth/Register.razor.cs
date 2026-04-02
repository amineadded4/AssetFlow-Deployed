using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;

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
        private string SelectedRole { get; set; } = "IT"; // Rôle sélectionné par défaut
        private bool AcceptTerms { get; set; } = false;

        // États UI
        private bool IsLoading { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;
        private string SuccessMessage { get; set; } = string.Empty;

        /// Calcule la force du mot de passe (1=faible, 2=moyen, 3=fort)
        private int PasswordStrength
        {
            get
            {
                if (string.IsNullOrEmpty(Password)) return 0;

                int score = 0;
                if (Password.Length >= 8) score++;                        // Longueur
                if (Password.Any(char.IsUpper)) score++;                  // Majuscule
                if (Password.Any(char.IsDigit)) score++;                  // Chiffre

                return score;
            }
        }

        /// Traite la soumission du formulaire d'inscription
        private async Task HandleRegister()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            // === Validations ===

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

            // === Appel API ===
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
                // Afficher le message de succès et rediriger après 2 secondes
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