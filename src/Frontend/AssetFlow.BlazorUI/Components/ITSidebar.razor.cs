using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Components
{
    public partial class ITSidebar:ComponentBase
    {
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

        /// <summary>
        /// Valeurs acceptées : "dashboard" | "equipements" | "employes" |
        /// "affectation" | "incidents" | "inventaire" | "achats" | "messagerie"
        /// </summary>
        [Parameter] public string ActivePage { get; set; } = string.Empty;

        private string UserName { get; set; } = "IT";

        protected override async Task OnInitializedAsync()
        {
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
        }

        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }
    }
}