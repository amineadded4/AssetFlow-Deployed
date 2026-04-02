using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Components
{
    public partial class InputField
    {
        // === Paramètres reçus du parent ===

        /// <summary>Label affiché au-dessus du champ</summary>
        [Parameter] public string Label { get; set; } = string.Empty;

        /// <summary>Texte placeholder dans le champ</summary>
        [Parameter] public string Placeholder { get; set; } = string.Empty;

        /// <summary>Type HTML : text, email, password</summary>
        [Parameter] public string InputType { get; set; } = "text";

        /// <summary>Valeur du champ (liaison bidirectionnelle)</summary>
        [Parameter] public string Value { get; set; } = string.Empty;

        /// <summary>Callback quand la valeur change</summary>
        [Parameter] public EventCallback<string> ValueChanged { get; set; }

        /// <summary>Affiche le bouton pour voir/cacher le mot de passe</summary>
        [Parameter] public bool ShowPasswordToggle { get; set; } = false;

        /// <summary>Indique si le champ est en erreur</summary>
        [Parameter] public bool HasError { get; set; } = false;

        /// <summary>Message d'erreur affiché sous le champ</summary>
        [Parameter] public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Texte d'aide affiché sous le champ (si pas d'erreur)</summary>
        [Parameter] public string HelperText { get; set; } = string.Empty;

        // === État interne ===

        /// <summary>Contrôle si le mot de passe est visible</summary>
        private bool ShowPassword { get; set; } = false;

        /// <summary>
        /// Appelé quand l'utilisateur tape dans le champ
        /// Met à jour la valeur et notifie le parent
        /// </summary>
        private async Task OnInput(ChangeEventArgs e)
        {
            Value = e.Value?.ToString() ?? string.Empty;
            await ValueChanged.InvokeAsync(Value);
        }

        /// <summary>Bascule l'affichage du mot de passe</summary>
        private void TogglePassword()
        {
            ShowPassword = !ShowPassword;
        }
    }
}