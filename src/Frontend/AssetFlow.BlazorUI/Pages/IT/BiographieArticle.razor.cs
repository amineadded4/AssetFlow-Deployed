// src/Frontend/AssetFlow.BlazorUI/Pages/IT/BiographieArticle.razor.cs
using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class BiographieArticle : ComponentBase, IAsyncDisposable
    {
        [Inject] private ArticleBiographieClientService BiographieService { get; set; } = default!;
        [Inject] private ILocalStorageService           LocalStorage      { get; set; } = default!;
        [Inject] private HttpClient                     Http              { get; set; } = default!;

        private List<MaterielAvecArticlesDto>? _materiels;
        private MaterielAvecArticlesDto?       _materielSelectionne;
        private ArticleBiographieDto?          _bio;
        private bool _loading    = true;
        private bool _loadingBio = false;
        private bool _menuOpen   = false;

        // ── SignalR ────────────────────────────────────────────────────────
        private HubConnection? _hub;
        private int?           _articleIdActuel;   // article actuellement affiché
        private bool           _refreshing = false; // badge "en cours de mise à jour"

        protected override async Task OnInitializedAsync()
        {
            _materiels = await BiographieService.GetMaterielsAsync();
            _loading   = false;

            await ConnecterHubAsync();
        }

        // ── Connexion au DashboardHub ──────────────────────────────────────
        private async Task ConnecterHubAsync()
        {
            try
            {
                var token  = await LocalStorage.GetItemAsync<string>("access_token") ?? "";
                var hubUrl = Http.BaseAddress!.ToString().TrimEnd('/') + "/dashboardhub";

                _hub = new HubConnectionBuilder()
                    .WithUrl(hubUrl, opts =>
                        opts.AccessTokenProvider = () => Task.FromResult<string?>(token))
                    .WithAutomaticReconnect()
                    .Build();

                // ── Écouter BiographieUpdated ──────────────────────────────
                _hub.On<object>("DashboardUpdated", async payload =>
                {
                    // Ne rafraîchir que si l'article affiché est celui qui a changé.
                    // Le payload contient { ArticleId, TypeEvenement, EvenementId }.
                    // On recharge quoi qu'il arrive : on est dans le bon groupe bio-{id}.
                    await InvokeAsync(async () =>
                    {
                        if (_articleIdActuel.HasValue)
                        {
                            _refreshing = true;
                            StateHasChanged();

                            _bio = await BiographieService.GetBiographieAsync(_articleIdActuel.Value);

                            _refreshing = false;
                            StateHasChanged();
                        }
                    });
                });

                _hub.Reconnected += async _ =>
                {
                    // Re-rejoindre le groupe après reconnexion
                    if (_articleIdActuel.HasValue)
                        await RejoindreGroupeArticleAsync(_articleIdActuel.Value);
                };

                await _hub.StartAsync();
            }
            catch { /* SignalR optionnel — la page fonctionne sans */ }
        }

        // ── S'abonner au groupe de l'article ──────────────────────────────
        private async Task RejoindreGroupeArticleAsync(int articleId)
        {
            if (_hub?.State != HubConnectionState.Connected) return;
            try
            {
                await _hub.InvokeAsync("JoinBiographie", articleId);
            }
            catch { }
        }

        // ── Se désabonner de l'ancien groupe ──────────────────────────────
        private async Task QuitterGroupeArticleAsync(int articleId)
        {
            if (_hub?.State != HubConnectionState.Connected) return;
            try
            {
                await _hub.InvokeAsync("LeaveBiographie", articleId);
            }
            catch { }
        }

        // ── Sélection du matériel ──────────────────────────────────────────
        private void OnMaterielChanged(ChangeEventArgs e)
        {
            _bio = null;
            if (int.TryParse(e.Value?.ToString(), out var id))
                _materielSelectionne = _materiels?.FirstOrDefault(m => m.MaterielId == id);
            else
                _materielSelectionne = null;
        }

        // ── Sélection de l'article — s'abonner au groupe temps réel ───────
        private async Task OnArticleChanged(ChangeEventArgs e)
        {
            // Quitter le groupe de l'article précédent
            if (_articleIdActuel.HasValue)
                await QuitterGroupeArticleAsync(_articleIdActuel.Value);

            _bio = null;
            _articleIdActuel = null;

            if (!int.TryParse(e.Value?.ToString(), out var id)) return;

            _loadingBio = true;
            StateHasChanged();

            _bio             = await BiographieService.GetBiographieAsync(id);
            _articleIdActuel = id;
            _loadingBio      = false;

            // Rejoindre le groupe SignalR pour cet article
            await RejoindreGroupeArticleAsync(id);
        }

        // ── Helpers UI (inchangés) ─────────────────────────────────────────
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

        // ── Dispose : quitter le groupe + déconnecter le hub ──────────────
        public async ValueTask DisposeAsync()
        {
            if (_hub != null)
            {
                if (_articleIdActuel.HasValue)
                    await QuitterGroupeArticleAsync(_articleIdActuel.Value);

                await _hub.DisposeAsync();
            }
        }
    }
}