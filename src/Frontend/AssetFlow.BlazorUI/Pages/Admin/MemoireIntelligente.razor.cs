// src/Frontend/AssetFlow.BlazorUI/Pages/Admin/MemoireIntelligente.razor.cs

using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Admin
{
    public partial class MemoireIntelligente : ComponentBase, IAsyncDisposable
    {
        [Inject] private GraphService GraphSvc { get; set; } = default!;
        [Inject] private IJSRuntime   JS       { get; set; } = default!;

        // ── State ──────────────────────────────────────────────────────────────
        private bool   _sidebarOpen  = false;
        private bool   _listLoading  = true;
        private bool   _graphLoading = false;
        private string _graphError   = string.Empty;
        private string _tab          = "materiel";
        private string _search       = string.Empty;
        private string? _selectedId  = null;
        private GraphEntitySummaryDto? _selectedEntity = null;
        private GraphStatsDto? _stats;
        private int _graphNodeCount  = 0;

        private List<GraphEntitySummaryDto> _materiels    = new();
        private List<GraphEntitySummaryDto> _utilisateurs = new();
        private List<GraphEntitySummaryDto> _demandes     = new();
        private List<GraphEntitySummaryDto> _projets      = new();

        private DotNetObjectReference<MemoireIntelligente>? _dotnetRef;
        private HubConnection? _hubConnection; // NOUVEAU

        // ── Computed ──────────────────────────────────────────────────────────
        private List<GraphEntitySummaryDto> CurrentList => _tab switch
        {
            "utilisateur" => _utilisateurs,
            "demande"     => _demandes,
            "projet"      => _projets,
            _             => _materiels
        };

        private List<GraphEntitySummaryDto> FilteredEntities =>
            string.IsNullOrWhiteSpace(_search)
                ? CurrentList
                : CurrentList.Where(e =>
                    e.Label.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                    e.Detail.Contains(_search, StringComparison.OrdinalIgnoreCase)).ToList();

        // ── Lifecycle ─────────────────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            await Task.WhenAll(
                LoadStats(),
                LoadList("materiel")
            );
            await ConnecterSignalR(); // NOUVEAU
        }

        // ── NOUVEAU : SignalR ─────────────────────────────────────────────────
        private async Task ConnecterSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5235/dashboardhub", options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        try
                        {
                            return await JS.InvokeAsync<string?>("eval",
                                "localStorage.getItem('access_token') || localStorage.getItem('token')");
                        }
                        catch { return null; }
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            // Stats globales (équipements, incidents, utilisateurs, anomalies)
            _hubConnection.On("DashboardUpdated", async () =>
            {
                var nouvelles = await GraphSvc.GetStatsAsync();
                if (nouvelles == null) return;
                await InvokeAsync(() =>
                {
                    _stats = nouvelles;
                    StateHasChanged();
                });
            });

            // Un nœud spécifique a changé → refresh liste + graphe si affiché
            _hubConnection.On<GraphNodeUpdatedPayload>("GraphNodeUpdated", async payload =>
            {
                // 1. Invalider la liste en cache pour forcer un rechargement
                InvaliderCache(payload.Type);

                // 2. Si le graphe affiché concerne ce nœud, le rafraîchir
                await InvokeAsync(async () =>
                {
                    if (_selectedEntity != null &&
                        (payload.NodeId == _selectedId ||
                         _selectedEntity.Type == payload.Type))
                    {
                        await RafraichirGrapheActuel();
                    }
                    StateHasChanged();
                });
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
                await _hubConnection.InvokeAsync("JoinMemory"); // NOUVEAU groupe
            }
            catch { /* SignalR non dispo, reste statique */ }
        }

        private void InvaliderCache(string type)
        {
            switch (type)
            {
                case "materiel":    _materiels    = new(); break;
                case "utilisateur": _utilisateurs = new(); break;
                case "demande":     _demandes     = new(); break;
                case "projet":      _projets      = new(); break;
                case "incident":                          
                _materiels    = new();                 // un incident impacte les stats matériel
                _utilisateurs = new();                 // et utilisateur
                break;
            }
        }

        private async Task RafraichirGrapheActuel()
        {
            if (_selectedEntity == null) return;
            if (!int.TryParse(_selectedEntity.Id.Split('-').Last(), out int numId)) return;

            GraphResponseDto? graphData = _selectedEntity.Type switch
            {
                "materiel"    => await GraphSvc.GetGraphForMaterielAsync(numId),
                "utilisateur" => await GraphSvc.GetGraphForUtilisateurAsync(numId),
                "demande"     => await GraphSvc.GetGraphForDemandeAsync(numId),
                "projet"      => await GraphSvc.GetGraphForProjetAsync(numId),
                _             => null
            };

            if (graphData == null) return;

            _graphNodeCount = graphData.Nodes.Count;
            await InitGraph(graphData);
            StateHasChanged();
        }

        // ── Load ──────────────────────────────────────────────────────────────
        private async Task LoadStats()
        {
            _stats = await GraphSvc.GetStatsAsync();
            StateHasChanged();
        }

        private async Task LoadList(string tab)
        {
            _listLoading = true;
            StateHasChanged();

            switch (tab)
            {
                case "materiel":
                    if (!_materiels.Any()) _materiels = await GraphSvc.GetMaterielsAsync();
                    break;
                case "utilisateur":
                    if (!_utilisateurs.Any()) _utilisateurs = await GraphSvc.GetUtilisateursAsync();
                    break;
                case "demande":
                    if (!_demandes.Any()) _demandes = await GraphSvc.GetDemandesAsync();
                    break;
                case "projet":
                    if (!_projets.Any()) _projets = await GraphSvc.GetProjetsAsync();
                    break;
            }

            _listLoading = false;
            StateHasChanged();
        }

        // ── Tab / Search ──────────────────────────────────────────────────────
        private async Task SwitchTab(string tab)
        {
            _tab = tab;
            _search = string.Empty;
            StateHasChanged();
            await LoadList(tab);
        }

        private void OnSearch(ChangeEventArgs e)
        {
            _search = e.Value?.ToString() ?? string.Empty;
            StateHasChanged();
        }

        // ── Select entity → build graph ───────────────────────────────────────
        private async Task SelectEntity(GraphEntitySummaryDto ent)
        {
            if (_selectedId == ent.Id) return;

            _selectedId     = ent.Id;
            _selectedEntity = ent;
            _graphError     = string.Empty;
            _graphLoading   = true;
            StateHasChanged();

            try
            {
                if (!int.TryParse(ent.Id.Split('-').Last(), out int numId))
                {
                    _graphError = "Identifiant invalide.";
                    return;
                }

                GraphResponseDto? graphData = ent.Type switch
                {
                    "materiel"    => await GraphSvc.GetGraphForMaterielAsync(numId),
                    "utilisateur" => await GraphSvc.GetGraphForUtilisateurAsync(numId),
                    "demande"     => await GraphSvc.GetGraphForDemandeAsync(numId),
                    "projet"      => await GraphSvc.GetGraphForProjetAsync(numId),
                    _             => null
                };

                if (graphData == null)
                {
                    _graphError = "Impossible de charger le graphe.";
                    return;
                }

                _graphNodeCount = graphData.Nodes.Count;
                _graphLoading   = false;
                StateHasChanged();

                await Task.Delay(30);
                await InitGraph(graphData);
            }
            catch (Exception ex)
            {
                _graphError = $"Erreur : {ex.Message}";
            }
            finally
            {
                _graphLoading = false;
                StateHasChanged();
            }
        }

        private async Task InitGraph(GraphResponseDto data)
        {
            try
            {
                _dotnetRef ??= DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("GraphEngine.init", "mi-canvas", _dotnetRef);
                await JS.InvokeVoidAsync("GraphEngine.setData", data.Nodes, data.Links);
            }
            catch { }
        }

        // ── UI Helpers ────────────────────────────────────────────────────────
        internal string GetEntColor(GraphEntitySummaryDto e) => e.Type switch
        {
            "materiel"    => e.Status == "critical" ? "#ef4444" : e.Status == "warning" ? "#f59e0b" : "#3b82f6",
            "utilisateur" => "#8b5cf6",
            "demande"     => "#14b8a6",
            "projet"      => "#10b981",
            _             => "#94a3b8"
        };

        internal string GetEntBg(GraphEntitySummaryDto e) => e.Type switch
        {
            "materiel"    => e.Status == "critical" ? "rgba(239,68,68,0.10)" : e.Status == "warning" ? "rgba(245,158,11,0.10)" : "rgba(59,130,246,0.10)",
            "utilisateur" => "rgba(139,92,246,0.10)",
            "demande"     => "rgba(20,184,166,0.10)",
            "projet"      => "rgba(16,185,129,0.10)",
            _             => "rgba(148,163,184,0.10)"
        };

        internal string GetEntIcon(GraphEntitySummaryDto e) => e.Type switch
        {
            "materiel"    => "◈",
            "utilisateur" => "◉",
            "demande"     => "◇",
            "projet"      => "▣",
            _             => "●"
        };

        internal string GetBadgeText(GraphEntitySummaryDto e) => e.Type switch
        {
            "materiel"    => $"{e.Count} inc.",
            "utilisateur" => $"{e.Count} inc.",
            "demande"     => $"{e.Count} offres",
            "projet"      => $"{e.Count} mat.",
            _             => $"{e.Count}"
        };

        internal string GetBadgeBg(GraphEntitySummaryDto e) => e.Type switch
        {
            "materiel"    => "rgba(239,68,68,0.12)",
            "utilisateur" => "rgba(139,92,246,0.12)",
            "demande"     => "rgba(20,184,166,0.12)",
            "projet"      => "rgba(16,185,129,0.12)",
            _             => "rgba(148,163,184,0.12)"
        };

        internal string GetBadgeColor(GraphEntitySummaryDto e) => e.Type switch
        {
            "materiel"    => "#ef4444",
            "utilisateur" => "#8b5cf6",
            "demande"     => "#14b8a6",
            "projet"      => "#10b981",
            _             => "#94a3b8"
        };

        internal string GetBadgeBorder(GraphEntitySummaryDto e) => e.Type switch
        {
            "materiel"    => "rgba(239,68,68,0.20)",
            "utilisateur" => "rgba(139,92,246,0.20)",
            "demande"     => "rgba(20,184,166,0.20)",
            "projet"      => "rgba(16,185,129,0.20)",
            _             => "rgba(148,163,184,0.20)"
        };

        // ── Dispose ───────────────────────────────────────────────────────────
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                try
                {
                    await _hubConnection.InvokeAsync("LeaveMemory");
                    await _hubConnection.InvokeAsync("LeaveDashboard");
                }
                catch { }
                await _hubConnection.DisposeAsync();
            }
            try { await JS.InvokeVoidAsync("GraphEngine.destroy"); } catch { }
            _dotnetRef?.Dispose();
        }
    }

    // ── DTO payload SignalR ────────────────────────────────────────────────────
    public record GraphNodeUpdatedPayload(string Type, string NodeId);
}