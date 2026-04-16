using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.Services;

namespace AssetFlow.BlazorUI.Components
{
    public partial class AchatSidebar : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private DemandeAchatService DemandeAchatSvc { get; set; } = default!; // NOUVEAU

        [Parameter] public string ActivePage { get; set; } = string.Empty;
        [Parameter] public bool   ForceOpen  { get; set; } = false;
        [Parameter] public int    NombreNonVus { get; set; } = 0; // NOUVEAU

        private bool   _drawerOpen      = false;
        private string _nomUtilisateur  = "Agent Achat";
        private string _roleUtilisateur = "Service Achat";
        private string _initiales       = "AA";

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var nom = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_name') || localStorage.getItem('userFullName') || localStorage.getItem('currentUserName')");
                var role = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_role') || localStorage.getItem('currentUserRole')");

                if (!string.IsNullOrWhiteSpace(nom))
                {
                    _nomUtilisateur = Nettoyer(nom);
                    var parts = _nomUtilisateur.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    _initiales = parts.Length >= 2
                        ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                        : _nomUtilisateur[..Math.Min(2, _nomUtilisateur.Length)].ToUpper();
                }
                if (!string.IsNullOrWhiteSpace(role))
                    _roleUtilisateur = Nettoyer(role);
            }
            catch { }

            // NOUVEAU : charger le count des demandes non vues
            NombreNonVus = await DemandeAchatSvc.GetCountNonVusAsync();
        }

        protected override void OnParametersSet()
        {
            if (ForceOpen) _drawerOpen = true;
        }

        private static string Nettoyer(string v)
        {
            v = v.Trim();
            if (v.Length >= 2 &&
                ((v.StartsWith('"') && v.EndsWith('"')) ||
                 (v.StartsWith('\'') && v.EndsWith('\''))))
                v = v[1..^1].Trim();
            return v;
        }
    }
}