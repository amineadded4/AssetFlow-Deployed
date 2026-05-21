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
        private bool CodeSent        { get; set; } = false;
        private bool PasswordChanged { get; set; } = false;

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
            {
                CodeSent = true;
                Step = 2;
            } 
            else
                ErrorMessage = "Erreur lors de l'envoi. Vérifiez votre email.";
        }

                private async Task HandleResetPassword()
        {
            ErrorMessage     = string.Empty;
            TokenError       = false;
            PasswordMismatch = false;

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
                PasswordChanged = true;
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

        private string RoleColor => Role switch
        {
            "IT"          => "#7C3AED",
            "EquipeAchat" => "#136dec",
            "Employe"     => "#F59E0B",
            "Admin"       => "#EF4444",
            _             => "#136dec"
        };

        private string RoleColorShadow => Role switch
        {
            "IT"          => "rgba(124,58,237,0.35)",
            "EquipeAchat" => "rgba(19,109,236,0.35)",
            "Employe"     => "rgba(245,158,11,0.35)",
            "Admin"       => "rgba(239,68,68,0.35)",
            _             => "rgba(19,109,236,0.35)"
        };
        private string RoleColor15 => Role switch
        {
            "IT"          => "rgba(124,58,237,0.08)",
            "EquipeAchat" => "rgba(19,109,236,0.08)",
            "Employe"     => "rgba(245,158,11,0.08)",
            "Admin"       => "rgba(239,68,68,0.08)",
            _             => "rgba(19,109,236,0.08)"
        };

        private string RoleColor30 => Role switch
        {
            "IT"          => "rgba(124,58,237,0.20)",
            "EquipeAchat" => "rgba(19,109,236,0.20)",
            "Employe"     => "rgba(245,158,11,0.20)",
            "Admin"       => "rgba(239,68,68,0.20)",
            _             => "rgba(19,109,236,0.20)"
        };
    }
}