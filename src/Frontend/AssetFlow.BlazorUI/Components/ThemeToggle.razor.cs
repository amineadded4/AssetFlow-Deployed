// Utilise JSInterop pour ajouter/retirer la classe "dark" sur <html>

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Components
{
    public partial class ThemeToggle
    {
        // JSRuntime pour appeler du JavaScript depuis Blazor
        [Inject] private IJSRuntime JS { get; set; } = default!;

        // État actuel du thème
        private bool IsDark { get; set; } = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Lire le thème sauvegardé dans localStorage au démarrage
                var saved = await JS.InvokeAsync<string>("localStorage.getItem", "theme");
                IsDark = saved == "dark";
                await ApplyTheme();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Bascule entre dark et light mode
        /// </summary>
        private async Task ToggleTheme()
        {
            IsDark = !IsDark;
            await ApplyTheme();
            // Sauvegarder le choix dans localStorage
            await JS.InvokeVoidAsync("localStorage.setItem", "theme", IsDark ? "dark" : "light");
        }

        /// <summary>
        /// Applique le thème : ajoute ou retire la classe "dark" sur <html>
        /// </summary>
        private async Task ApplyTheme()
        {
            if (IsDark)
                await JS.InvokeVoidAsync("document.documentElement.classList.add", "dark");
            else
                await JS.InvokeVoidAsync("document.documentElement.classList.remove", "dark");
        }
    }
}