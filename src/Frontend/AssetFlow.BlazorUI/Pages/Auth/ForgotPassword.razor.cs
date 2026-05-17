using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Pages.Auth
{
    public partial class ForgotPassword
    {
        [Inject] private HttpClient        Http       { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime        JS         { get; set; } = default!;

        [SupplyParameterFromQuery]
        private string Role { get; set; } = "IT";

        private int    Step            { get; set; } = 1;
        private string Email           { get; set; } = string.Empty;
        private string NewPassword     { get; set; } = string.Empty;
        private string ConfirmPassword { get; set; } = string.Empty;
        private bool   IsLoading       { get; set; } = false;
        private bool   EmailError      { get; set; } = false;
        private bool   TokenError      { get; set; } = false;
        private bool   PasswordMismatch{ get; set; } = false;
        private string ErrorMessage    { get; set; } = string.Empty;
        private string SuccessMessage  { get; set; } = string.Empty;

        // 6 cases du token
        private string[] TokenDigits = new string[6];

        private string TokenValue => string.Join("", TokenDigits);

        private async Task HandleSendCode()
        {
            EmailError   = false;
            ErrorMessage = string.Empty;
            if (!Email.Contains("@")) { EmailError = true; return; }

            IsLoading = true;
            var response = await Http.PostAsJsonAsync("api/auth/forgot-password", new { Email });
            IsLoading = false;

            if (response.IsSuccessStatusCode)
                Step = 2;
            else
                ErrorMessage = "Erreur lors de l'envoi. Vérifiez votre email.";
        }

        private async Task HandleResetPassword()
        {
            ErrorMessage    = string.Empty;
            TokenError      = false;
            PasswordMismatch= false;

            if (TokenValue.Length < 6) { TokenError = true; return; }
            if (NewPassword != ConfirmPassword) { PasswordMismatch = true; return; }
            if (string.IsNullOrEmpty(NewPassword)) { ErrorMessage = "Entrez un nouveau mot de passe."; return; }

            IsLoading = true;
            var response = await Http.PostAsJsonAsync("api/auth/reset-password", new
            {
                Email       = Email,
                Token       = TokenValue,
                NewPassword = NewPassword
            });
            IsLoading = false;

            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = "Mot de passe changé ! Redirection...";
                await Task.Delay(1500);
                Navigation.NavigateTo($"/login?role={Role}");
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                ErrorMessage = msg.Trim('"');
            }
        }

        // Navigation entre les cases du token
        private async Task OnTokenDigitInput(ChangeEventArgs e, int index)
        {
            var val = e.Value?.ToString() ?? "";

            // Gestion du collage (paste) — si plus d'un caractère
            if (val.Length > 1)
            {
                var digits = val.Where(char.IsDigit).Take(6).ToArray();
                for (int i = 0; i < digits.Length && i < 6; i++)
                    TokenDigits[i] = digits[i].ToString();

                // Focus sur la dernière case remplie
                var lastIndex = Math.Min(digits.Length - 1, 5);
                await JS.InvokeVoidAsync("eval", $"document.getElementById('token-{lastIndex}').focus()");
                StateHasChanged();
                return;
            }

            // Saisie normale — un seul chiffre
            if (!string.IsNullOrEmpty(val) && !char.IsDigit(val[0])) return;
            TokenDigits[index] = val;

            if (!string.IsNullOrEmpty(val) && index < 5)
                await JS.InvokeVoidAsync("eval", $"document.getElementById('token-{index + 1}').focus()");
        }

        private async Task OnTokenKeyDown(KeyboardEventArgs e, int index)
        {
            if (e.Key == "Backspace" && string.IsNullOrEmpty(TokenDigits[index]) && index > 0)
            {
                TokenDigits[index - 1] = string.Empty;
                await JS.InvokeVoidAsync("eval", $"document.getElementById('token-{index - 1}').focus()");
            }
        }
        private async Task OnTokenPaste(ClipboardEventArgs e, int index)
        {
            // Lire le texte collé via JS car ClipboardEventArgs ne l'expose pas directement
            var pasted = await JS.InvokeAsync<string>("eval", 
                "window.__pastedText || ''");
            
            var digits = pasted.Where(char.IsDigit).Take(6).ToArray();
            if (digits.Length == 0) return;

            for (int i = 0; i < digits.Length && i < 6; i++)
                TokenDigits[i] = digits[i].ToString();

            var lastIndex = Math.Min(digits.Length - 1, 5);
            await JS.InvokeVoidAsync("eval", 
                $"document.getElementById('token-{lastIndex}').focus()");
            StateHasChanged();
        }
    }
}