using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class BiographieArticle : ComponentBase
    {
        [Inject] private ArticleBiographieClientService BiographieService { get; set; } = default!;

        private List<MaterielAvecArticlesDto>? _materiels;
        private MaterielAvecArticlesDto?       _materielSelectionne;
        private ArticleBiographieDto?          _bio;
        private bool _loading    = true;
        private bool _loadingBio = false;
        private bool _menuOpen = false;

        protected override async Task OnInitializedAsync()
        {
            _materiels = await BiographieService.GetMaterielsAsync();
            _loading = false;
        }

        private void OnMaterielChanged(ChangeEventArgs e)
        {
            _bio = null;
            if (int.TryParse(e.Value?.ToString(), out var id))
                _materielSelectionne = _materiels?.FirstOrDefault(m => m.MaterielId == id);
            else
                _materielSelectionne = null;
        }

        private async Task OnArticleChanged(ChangeEventArgs e)
        {
            _bio = null;
            if (!int.TryParse(e.Value?.ToString(), out var id)) return;

            _loadingBio = true;
            StateHasChanged();
            _bio = await BiographieService.GetBiographieAsync(id);
            _loadingBio = false;
        }

        private static string GetEvenementLabel(string type) => type switch
        {
            "Acquisition"   => "Acquisition",
            "Affectation"   => "Affecté à",
            "Retrait"       => "Retiré",
            "PanneDeclaree" => "Panne déclarée",
            "Reparation"    => "Réparation",
            "MiseEnStock"   => "Mis en stock",
            "Reforme"       => "Réformé",
            _               => type
        };

        private static MarkupString GetEvenementIcon(string type)
        {
            var svg = type switch
            {
                "Acquisition"   => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M20 7H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2z'/><path d='M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16'/></svg>",
                "Affectation"   => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2'/><circle cx='12' cy='7' r='4'/></svg>",
                "Retrait"       => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4'/><polyline points='16 17 21 12 16 7'/><line x1='21' y1='12' x2='9' y2='12'/></svg>",
                "PanneDeclaree" => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3ZM12 9v4M12 17h.01'/></svg>",
                "Reparation"    => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z'/></svg>",
                "MiseEnStock"   => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z'/></svg>",
                "Reforme"       => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><polyline points='3 6 5 6 21 6'/><path d='M19 6l-1 14H6L5 6'/><path d='M10 11v6M14 11v6'/><path d='M9 6V4h6v2'/></svg>",
                _               => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><circle cx='12' cy='12' r='10'/></svg>"
            };
            return new MarkupString(svg);
        }
    }
}
