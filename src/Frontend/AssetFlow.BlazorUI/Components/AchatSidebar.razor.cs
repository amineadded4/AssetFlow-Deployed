using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Components
{
    public partial class AchatSidebar : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;

        // Valeurs : "statistiques" | "equipements" | "materiel" |
        // "fournisseurs" | "demandes" | "scraping" | "messagerie" ...
        [Parameter] public string ActivePage      { get; set; } = string.Empty;
        [Parameter] public bool   ForceOpen       { get; set; } = false;

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