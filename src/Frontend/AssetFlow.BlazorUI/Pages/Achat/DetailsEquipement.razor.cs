using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;
using AssetFlow.BlazorUI.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class DetailsEquipement : IAsyncDisposable
    {
        // ── Injections ─────────────────────────────────────────
        [Inject] private EmployeService    EmployeService  { get; set; } = default!;
        [Inject] private IncidentService   IncidentService { get; set; } = default!;
        [Inject] private NavigationManager Navigation      { get; set; } = default!;
        [Inject] private IJSRuntime        JS              { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;

        // ── Paramètres URL ─────────────────────────────────────
        [Parameter] public int AffectationId { get; set; }
        [Parameter] public int ArticleId     { get; set; } = 0;

        // ── Données équipement ─────────────────────────────────
        private EquipementAffecteDto? Equipement    { get; set; }
        private bool                  IsLoading     { get; set; } = true;

        // ── Données incidents ──────────────────────────────────
        private List<IncidentDto> Incidents          { get; set; } = new();
        private bool              IsLoadingIncidents { get; set; } = true;

        // ── Infos utilisateur ──────────────────────────────────
        private string UserName      { get; set; } = "Utilisateur";
        private string UserRole      { get; set; } = "Employé";
        private bool   _roleCharge   = false;
        private bool   _estAdmin     => UserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        // ── Sidebar ────────────────────────────────────────────
        private bool _sidebarOpen = false;
        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

        // ── QR Code ────────────────────────────────────────────
        private string FicheUrl => $"{Navigation.BaseUri}fiche/{AffectationId}/article/{ArticleId}";
        private bool _qrGenere = false;

        // ── SignalR ────────────────────────────────────────────
        private HubConnection? _hubConnection;

        // ── Toast ──────────────────────────────────────────────
        private string _toastMsg  = string.Empty;
        private string _toastType = "toast-success";
        private System.Timers.Timer? _toastTimer;

        // ══════════════════════════════════════════════════════
        // Init
        // ══════════════════════════════════════════════════════

        protected override async Task OnInitializedAsync()
        {
            UserName    = await EmployeService.GetCurrentUserNameAsync();
            UserRole    = await EmployeService.GetCurrentUserRoleAsync();
            _roleCharge = true;

            await Task.WhenAll(
                ChargerEquipement(),
                ChargerIncidents()
            );

            await ConnecterSignalR();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!IsLoading && Equipement != null && !_qrGenere)
            {
                _qrGenere = true;
                try
                {
                    await JS.InvokeVoidAsync("generateQrCode", "qr-canvas", FicheUrl);
                }
                catch { }
            }
        }

        // ══════════════════════════════════════════════════════
        // SignalR — même pattern que la page Employé
        // Écoute DashboardUpdated (NotifyAsync) :
        //   → émis quand un incident est signalé
        //   → émis quand l'IT change le statut d'un incident
        // ══════════════════════════════════════════════════════

        private async Task ConnecterSignalR()
        {
            var hubUrl = Http.BaseAddress!.ToString().TrimEnd('/') + "/dashboardhub";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
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

            _hubConnection.On("DashboardUpdated", async () =>
            {
                await InvokeAsync(async () =>
                {
                    try
                    {
                        var nouveauxIncidents = await IncidentService.GetIncidentsByAffectationAsync(AffectationId);

                        bool aChange = DetecterChangement(Incidents, nouveauxIncidents);
                        if (!aChange) return;

                        Incidents = nouveauxIncidents;

                        // Recharger l'équipement : son état Panne/Bon a pu changer
                        await ChargerEquipement();
                    }
                    catch { /* silencieux — mise à jour non critique */ }
                    finally
                    {
                        StateHasChanged();
                    }
                });
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
            }
            catch { /* page reste fonctionnelle en mode statique si SignalR indisponible */ }
        }

        // ══════════════════════════════════════════════════════
        // Détection de changement — évite un re-render inutile
        // sur les broadcasts qui ne concernent pas cet équipement
        // ══════════════════════════════════════════════════════

        private static bool DetecterChangement(List<IncidentDto> anciens, List<IncidentDto> nouveaux)
        {
            if (anciens.Count != nouveaux.Count) return true;

            var ancienMap = anciens.ToDictionary(i => i.Id, i => i.Statut);
            foreach (var n in nouveaux)
            {
                if (!ancienMap.TryGetValue(n.Id, out var ancienStatut)) return true;
                if (ancienStatut != n.Statut)                           return true;
            }
            return false;
        }

        // ══════════════════════════════════════════════════════
        // Chargement
        // ══════════════════════════════════════════════════════

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

        // ══════════════════════════════════════════════════════
        // Navigation
        // ══════════════════════════════════════════════════════

        private void NaviguerVersSignalement()
        {
            Navigation.NavigateTo($"/achat/incident/{AffectationId}/article/{ArticleId}");
        }

        // ══════════════════════════════════════════════════════
        // UI helpers
        // ══════════════════════════════════════════════════════

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

        // Couleur du point de la timeline selon le statut de l'incident
        private string GetIncidentDotClass(string statut) => statut switch
        {
            "Resolu"    => "resolu",
            "Cloture"   => "cloture",
            "EnCours"   => "encours",
            "EnAttente" => "attente",
            _           => "attente"
        };

        // Classe CSS pour le badge d'urgence
        private string GetUrgenceClass(int urgence)
        {
            if (urgence <= 33) return "faible";
            if (urgence <= 66) return "moyen";
            return "critique";
        }

        private void AfficherToast(string msg, string type)
        {
            _toastMsg  = msg;
            _toastType = type;
            StateHasChanged();

            _toastTimer?.Dispose();
            _toastTimer = new System.Timers.Timer(3000) { AutoReset = false };
            _toastTimer.Elapsed += async (_, _) =>
            {
                _toastMsg = string.Empty;
                await InvokeAsync(StateHasChanged);
            };
            _toastTimer.Start();
        }

        // ══════════════════════════════════════════════════════
        // Impression QR
        // ══════════════════════════════════════════════════════

        private async Task ImprimerQR()
        {
            var designation = Equipement?.Designation ?? "Équipement";
            var reference   = Equipement?.NumeroSerie ?? "";

            // Récupérer le canvas comme image base64
            var dataUrl = await JS.InvokeAsync<string>("eval",
                "document.getElementById('qr-canvas').querySelector('img')?.src ?? ''");

            var printHtml = $@"<!DOCTYPE html>
        <html lang=""fr"">
        <head>
        <meta charset=""utf-8""/>
        <title>QR — {designation}</title>
        <style>
            body {{ font-family: sans-serif; display: flex; flex-direction: column;
                    align-items: center; padding: 2rem; background: white; color: #111; }}
            h2  {{ font-size: 1.2rem; font-weight: 800; margin: 1rem 0 0.25rem; }}
            p   {{ font-size: 0.8rem; color: #555; margin: 0; }}
            code{{ font-size: 0.65rem; color: #333; margin-top: 0.75rem; display: block; }}
            @media print {{ body {{ padding: 0; }} }}
        </style>
        </head>
        <body>
        <img src=""{dataUrl}"" width=""200"" height=""200"" />
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

        // ══════════════════════════════════════════════════════
        // Dispose
        // ══════════════════════════════════════════════════════

        public async ValueTask DisposeAsync()
        {
            _toastTimer?.Dispose();

            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
        }
    }
}