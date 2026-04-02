// Logique du composant sidebar employé réutilisable
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Components
{
    public partial class EmployeSidebar
    {
        [Inject] private EmployeService EmployeService { get; set; } = default!;

        [Parameter] public string ActivePage  { get; set; } = string.Empty;
        [Parameter] public bool   ForceOpen   { get; set; } = false;  // ← nouveau

        private bool   _drawerOpen = false;
        private string UserName    { get; set; } = "Utilisateur";
        private string UserRole    { get; set; } = "Employé";

        protected override async Task OnInitializedAsync()
        {
            UserName = await EmployeService.GetCurrentUserNameAsync();
            UserRole = await EmployeService.GetCurrentUserRoleAsync();
        }

        protected override void OnParametersSet()
        {
            if (ForceOpen) _drawerOpen = true;   // ← synchronise avec la page parente
        }

        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "??";
        }
    }
}