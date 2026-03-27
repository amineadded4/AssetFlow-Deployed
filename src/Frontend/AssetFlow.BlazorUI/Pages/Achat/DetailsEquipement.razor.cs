// ============================================================
// Pages/Achat/DetailsEquipement.razor.cs
// MISE À JOUR : Chargement des incidents de l'affectation
// ============================================================

using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class DetailsEquipement
    {
        // ── Injections ─────────────────────────────────────────
        [Inject] private EmployeService  EmployeService  { get; set; } = default!;
        [Inject] private IncidentService IncidentService { get; set; } = default!;
        [Inject] private NavigationManager Navigation    { get; set; } = default!;
        [Inject] private IJSRuntime JS                   { get; set; } = default!;

        // ── Paramètre URL ──────────────────────────────────────
        [Parameter] public int AffectationId { get; set; }
        [Parameter] public int ArticleId { get; set; } = 0;


        // ── Données équipement ─────────────────────────────────
        private EquipementAffecteDto? Equipement    { get; set; }
        private bool                  IsLoading     { get; set; } = true;

        // ── Données incidents ──────────────────────────────────
        private List<IncidentDto> Incidents          { get; set; } = new();
        private bool              IsLoadingIncidents { get; set; } = true;

        // ── Infos utilisateur ──────────────────────────────────
        private string UserName { get; set; } = "Utilisateur";
        private string UserRole { get; set; } = "Employé";

        // ── QR Code ────────────────────────────────────────────
        private string FicheUrl => $"{Navigation.BaseUri}fiche/{AffectationId}/article/{ArticleId}";
        private string QrSvg    { get; set; } = string.Empty;
        private bool        _sidebarOpen     = false;

        private bool _estAdmin => UserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private void ToggleSidebar() => _sidebarOpen  = !_sidebarOpen;
        private bool _roleCharge = false;

        // ── Init ───────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            UserName = await EmployeService.GetCurrentUserNameAsync();
            UserRole = await EmployeService.GetCurrentUserRoleAsync();
            _roleCharge = true; 

            // Charger équipement et incidents en parallèle
            await Task.WhenAll(
                ChargerEquipement(),
                ChargerIncidents()
            );
        }

        protected override void OnParametersSet()
        {
            QrSvg = BuildQrSvg(FicheUrl);
        }

        // ── Chargement équipement ──────────────────────────────
        private async Task ChargerEquipement()
        {
            IsLoading = true;
            try
            {
                Equipement = await EmployeService.GetEquipementDetailAsync(AffectationId, ArticleId);
            }
            catch
            {
                Equipement = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── Chargement incidents ───────────────────────────────
        private async Task ChargerIncidents()
        {
            IsLoadingIncidents = true;
            try
            {
                Incidents = await IncidentService.GetIncidentsByAffectationAsync(AffectationId);
            }
            catch
            {
                Incidents = new List<IncidentDto>();
            }
            finally
            {
                IsLoadingIncidents = false;
            }
        }

        // ── Navigation ─────────────────────────────────────────
        private void NaviguerVersSignalement()
        {
            Navigation.NavigateTo($"/achat/incident/{AffectationId}/article/{ArticleId}");
        }

        // ── Helpers UI ─────────────────────────────────────────
        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "??";
        }

        private string GetStatutLabel(string statut) => statut switch
        {
            "EnCours"   => "En Service",
            "Retourne"  => "Retourné",
            "Perdu"     => "Perdu",
            "Endommage" => "Endommagé",
            _           => statut
        };

        /// <summary>
        /// Couleur du point de la timeline selon le statut de l'incident
        /// </summary>
        private string GetIncidentDotClass(string statut) => statut switch
        {
            "Resolu"   => "resolu",
            "Cloture"  => "cloture",
            "EnCours"  => "encours",
            "EnAttente"=> "attente",
            _          => "attente"
        };

        /// <summary>
        /// Classe CSS pour le badge d'urgence
        /// </summary>
        private string GetUrgenceClass(int urgence)
        {
            if (urgence <= 33) return "faible";
            if (urgence <= 66) return "moyen";
            return "critique";
        }

        // ── Impression QR ──────────────────────────────────────
        private async Task ImprimerQR()
        {
            var designation = Equipement?.Designation ?? "Équipement";
            var reference   = Equipement?.NumeroSerie   ?? "";

            var printHtml = $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
  <meta charset=""utf-8""/>
  <title>QR — {designation}</title>
  <style>
    body {{
      font-family: sans-serif;
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 2rem;
      background: white;
      color: #111;
    }}
    h2  {{ font-size: 1.2rem; font-weight: 800; margin: 1rem 0 0.25rem; }}
    p   {{ font-size: 0.8rem; color: #555; margin: 0; }}
    code{{ font-size: 0.65rem; color: #333; margin-top: 0.75rem; display: block; }}
    @media print {{ body {{ padding: 0; }} }}
  </style>
</head>
<body>
  {QrSvg}
  <h2>{designation}</h2>
  <p>Numéro de série : {reference}</p>
  <code>{FicheUrl}</code>
  <script>window.onload = () => window.print();<\/script>
</body>
</html>";

            await JS.InvokeVoidAsync("eval", $@"
                var w = window.open('','_blank','width=400,height=500');
                w.document.write({System.Text.Json.JsonSerializer.Serialize(printHtml)});
                w.document.close();
            ");
        }

        // ── Génération QR Code SVG ─────────────────────────────
        private string BuildQrSvg(string url)
        {
            const int Size   = 25;
            const int CellPx = 8;
            const int Margin  = 16;

            var grid = new bool[Size, Size];

            PlaceFinder(grid, 0,      0,      Size);
            PlaceFinder(grid, Size-7, 0,      Size);
            PlaceFinder(grid, 0,      Size-7, Size);

            for (int i = 8; i < Size - 8; i++)
            {
                grid[6, i] = (i % 2 == 0);
                grid[i, 6] = (i % 2 == 0);
            }

            if (Size > 8) grid[Size - 8, 8] = true;

            var bytes = new List<byte>();
            bytes.Add((byte)url.Length);
            foreach (var c in url)
                bytes.Add((byte)(c < 128 ? c : '?'));

            var bits = new List<bool>();
            foreach (var b in bytes)
                for (int k = 7; k >= 0; k--)
                    bits.Add((b >> k & 1) == 1);

            int bitIndex = 0;
            bool goingUp = true;

            for (int col = Size - 1; col >= 0; col -= 2)
            {
                if (col == 6) col--;
                for (int rowStep = 0; rowStep < Size; rowStep++)
                {
                    int row = goingUp ? (Size - 1 - rowStep) : rowStep;
                    for (int cx = 0; cx <= 1; cx++)
                    {
                        int c = col - cx;
                        if (c < 0 || grid[row, c]) continue;
                        if (bitIndex < bits.Count)
                            grid[row, c] = bits[bitIndex++];
                        else
                            grid[row, c] = (row + c) % 2 == 0;
                    }
                }
                goingUp = !goingUp;
            }

            int svgSize = Size * CellPx + Margin * 2;
            var sb = new StringBuilder();

            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgSize}\" height=\"{svgSize}\" viewBox=\"0 0 {svgSize} {svgSize}\" class=\"qr-svg\">");
            sb.Append($"<rect width=\"{svgSize}\" height=\"{svgSize}\" fill=\"white\"/>");

            for (int r = 0; r < Size; r++)
                for (int c2 = 0; c2 < Size; c2++)
                    if (grid[r, c2])
                        sb.Append($"<rect x=\"{c2 * CellPx + Margin}\" y=\"{r * CellPx + Margin}\" width=\"{CellPx}\" height=\"{CellPx}\" fill=\"#0F1E3C\"/>");

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static void PlaceFinder(bool[,] g, int row, int col, int size)
        {
            for (int r = 0; r < 7; r++)
                for (int c = 0; c < 7; c++)
                {
                    if (row + r >= size || col + c >= size) continue;
                    bool border = r == 0 || r == 6 || c == 0 || c == 6;
                    bool center = r >= 2 && r <= 4 && c >= 2 && c <= 4;
                    g[row + r, col + c] = border || center;
                }

            for (int r = -1; r <= 7; r++)
                for (int c = -1; c <= 7; c++)
                {
                    int rr = row + r, cc = col + c;
                    if (rr < 0 || rr >= size || cc < 0 || cc >= size) continue;
                    if (r == -1 || r == 7 || c == -1 || c == 7)
                        g[rr, cc] = false;
                }
        }
    }
}