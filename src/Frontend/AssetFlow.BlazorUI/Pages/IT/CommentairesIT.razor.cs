using AssetFlow.BlazorUI.Services;
using AssetFlow.BlazorUI.CircuitBreaker;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class CommentairesIT : ComponentBase,IAsyncDisposable
    {
        [Inject] private EmployeService       EmployeService { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage   { get; set; } = default!;
        [Inject] private IJSRuntime           JS             { get; set; } = default!;
        [Inject] private CommentaireCircuitBreakerService _cbService { get; set; } = default!;
        private HubConnection? _hubConnection;

        private List<CommentaireITDto> Commentaires        { get; set; } = new();
        private List<CommentaireITDto> CommentairesFiltres { get; set; } = new();
        private bool   IsLoading    { get; set; } = true;
        private string ErrorMessage { get; set; } = string.Empty;
        private string SearchQuery { get; set; } = string.Empty;
        private string _roleFiltre = string.Empty;
        private bool              ModalOuvert            { get; set; } = false;
        private CommentaireITDto? CommentaireSelectionne { get; set; } = null;
        private string            ModalFeedback          { get; set; } = string.Empty;
        private bool              ModalFeedbackSucces    { get; set; } = false;
        private int? SupprimerEnCours { get; set; } = null;
        private HashSet<int> _groupesOuverts = new();
        private Dictionary<int, SentimentMaterielDto>  Sentiments          { get; set; } = new();
        private HashSet<int>                            SentimentEnCours    { get; set; } = new();
        private HashSet<int>                            SentimentDisponible { get; set; } = new();
        private bool   _menuOpen         = false;
        private string _nomUtilisateur   = "Agent IT";
        private string _initiales        = "IT";
        private string _roleUtilisateur  = "IT";
        private bool   _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private OffreCircuit _cbSentiment => _cbService.Sentiment;
        private int          _countSentiment = 0;
        private System.Threading.Timer? _cbTimer;

        // ── Toast ─────────────────────────────────────────────────
        private string? _toastMessage;
        private bool    _toastVisible = false;
        private System.Threading.CancellationTokenSource? _toastCts;

        protected override async Task OnInitializedAsync()
        {
            await ChargerInfosUtilisateur();
            await ChargerCommentaires();
            DemarrerCbTimer(); 
            await ConnecterSignalR();
        }
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

            // Nouveau commentaire ajouté/supprimé → recharger
            _hubConnection.On("DashboardUpdated", async () =>
            {
                await InvokeAsync(async () =>
                {
                    try
                    {
                        Commentaires = await EmployeService.GetTousLesCommentairesAsync(
                            string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery);
                        AppliquerFiltres();
                        // Invalider les sentiments calculés — données ont changé
                        Sentiments          = new();
                        SentimentDisponible = new();
                        SentimentEnCours    = new();
                    }
                    catch { }
                    finally { StateHasChanged(); }
                });
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
            }
            catch { }
        }
        private async Task AfficherToast(string message)
        {
            // Annuler un toast précédent encore en cours
            _toastCts?.Cancel();
            _toastCts = new System.Threading.CancellationTokenSource();

            _toastMessage = message;
            _toastVisible = true;
            StateHasChanged();

            try
            {
                await Task.Delay(4000, _toastCts.Token);
                _toastVisible = false;
                StateHasChanged();
            }
            catch (TaskCanceledException) { }
        }
        private void DemarrerCbTimer()
        {
            _cbTimer = new System.Threading.Timer(async _ =>
            {
                _countSentiment = _cbSentiment.SecondsRemaining;
                _cbSentiment.TryTransitionHalfOpen();
                await InvokeAsync(StateHasChanged);
            }, null, 0, 1000);
        }
        public async ValueTask DisposeAsync()
        {
            if (_cbTimer != null)
                await _cbTimer.DisposeAsync();
            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
        }

        private async Task ChargerInfosUtilisateur()
        {
            try
            {
                var nom = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_name') || localStorage.getItem('userFullName')");
                _roleUtilisateur = await LocalStorage.GetItemAsync<string>("user_role") ?? "IT";
                if (!string.IsNullOrWhiteSpace(nom))
                {
                    _nomUtilisateur = Nettoyer(nom);
                    var parts = _nomUtilisateur.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    _initiales = parts.Length >= 2
                        ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                        : _nomUtilisateur[..Math.Min(2, _nomUtilisateur.Length)].ToUpper();
                }
            }
            catch { }
        }

        private async Task ChargerCommentaires()
        {
            try
            {
                IsLoading = true; ErrorMessage = string.Empty; StateHasChanged();
                Commentaires = await EmployeService.GetTousLesCommentairesAsync(
                    string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery);
                AppliquerFiltres();
                Sentiments = new(); SentimentDisponible = new(); SentimentEnCours = new();
            }
            catch (Exception ex) { ErrorMessage = $"Erreur lors du chargement : {ex.Message}"; }
            finally { IsLoading = false; StateHasChanged(); }
        }

        private void AppliquerFiltres()
        {
            var liste = Commentaires.AsEnumerable();
            if (!string.IsNullOrEmpty(_roleFiltre))
                liste = liste.Where(c => c.AuteurRole == _roleFiltre);
            CommentairesFiltres = liste.ToList();
        }

        private void ToggleGroupe(int materielId)
        {
            if (_groupesOuverts.Contains(materielId)) _groupesOuverts.Remove(materielId);
            else _groupesOuverts.Add(materielId);
            StateHasChanged();
        }

        private void OnSearchInput(ChangeEventArgs e) { SearchQuery = e.Value?.ToString() ?? string.Empty; _ = ChargerCommentaires(); }
        private void ClearSearch() { SearchQuery = string.Empty; _ = ChargerCommentaires(); }
        private void SetRoleFiltre(string role) { _roleFiltre = role; AppliquerFiltres(); StateHasChanged(); }
        private void ResetFiltres() { SearchQuery = string.Empty; _roleFiltre = string.Empty; _ = ChargerCommentaires(); }

        private async Task AnalyserSentiment(int materielId)
        {
            // ── Circuit Breaker ──────────────────────────────────────
            if (_cbSentiment.TryTransitionHalfOpen()) StateHasChanged();

            if (!_cbSentiment.CanSend())
            {
                StateHasChanged();
                return;
            }
            // ────────────────────────────────────────────────────────

            if (SentimentEnCours.Contains(materielId)) return;
            SentimentEnCours.Add(materielId);
            StateHasChanged();
            try
            {
                var result = await EmployeService.GetSentimentMaterielAsync(materielId);
                if (result != null)
                {
                    _cbSentiment.RecordSuccess();   // ← AJOUT
                    result.MaterielId = materielId;
                    Sentiments[materielId] = result;
                    SentimentDisponible.Add(materielId);
                    StateHasChanged();
                    await Task.Yield();
                    await RenderApexDonut(materielId, result);
                }
                else
                {
                    _cbSentiment.RecordFailure();   // ← AJOUT (null = échec silencieux)
                    if (_cbSentiment.State != CbState.Open)
                    {
                        await AfficherToast("Problème serveur, réessayez dans quelques instants.");
                    }
                }
            }
            catch
            {
                _cbSentiment.RecordFailure();       // ← AJOUT
            }
            finally { SentimentEnCours.Remove(materielId); StateHasChanged(); }
        }

        private async Task AnalyserTousSentiments()
        {
            var ids = CommentairesFiltres
                .GroupBy(c => c.MaterielId).Select(g => g.Key)
                .Where(id => !SentimentDisponible.Contains(id)).ToList();
            foreach (var id in ids) await AnalyserSentiment(id);
        }

        private async Task RenderApexDonut(int materielId, SentimentMaterielDto sent)
        {
            var isDark = false;
            try { isDark = await JS.InvokeAsync<bool>("eval", "document.documentElement.classList.contains('dark')"); } catch { }

            await JS.InvokeVoidAsync(
                "ApexInterop.renderSentimentDonut",
                $"apex-sent-{materielId}",
                new
                {
                    positif  = sent.Positifs,
                    negatif  = sent.Negatifs,
                    neutre   = sent.Neutres,
                    pctPos   = sent.PourcentagePositif,
                    pctNeg   = sent.PourcentageNegatif,
                    pctNeu   = sent.PourcentageNeutre,
                    score    = sent.ScoreGlobal,
                    dominant = sent.SentimentDominant
                },
                isDark
            );
        }

        private void OuvrirModal(CommentaireITDto c) { CommentaireSelectionne = c; ModalFeedback = string.Empty; ModalFeedbackSucces = false; ModalOuvert = true; }
        private void FermerModal() { ModalOuvert = false; CommentaireSelectionne = null; ModalFeedback = string.Empty; }

        private async Task SupprimerCommentaire(int commentaireId)
        {
            SupprimerEnCours = commentaireId; ModalFeedback = string.Empty; StateHasChanged();
            var result = await EmployeService.SupprimerCommentaireITAsync(commentaireId);
            SupprimerEnCours = null;
            if (result.Succes)
            {
                Commentaires.RemoveAll(c => c.Id == commentaireId);
                AppliquerFiltres();
                var materielId = CommentaireSelectionne?.MaterielId ?? Commentaires.FirstOrDefault()?.MaterielId ?? 0;
                Sentiments.Remove(materielId); SentimentDisponible.Remove(materielId);
                if (ModalOuvert && CommentaireSelectionne?.Id == commentaireId)
                {
                    ModalFeedback = "Commentaire supprimé avec succès."; ModalFeedbackSucces = true;
                    StateHasChanged(); await Task.Delay(1200); FermerModal();
                }
            }
            else if (ModalOuvert && CommentaireSelectionne?.Id == commentaireId)
            { ModalFeedback = result.Message; ModalFeedbackSucces = false; }
            StateHasChanged();
        }

        private static string GetRoleClass(string role) => role switch { "IT" => "it", "EquipeAchat" => "achat", _ => "employe" };
        private static string GetRoleLabel(string role) => role switch { "IT" => "IT", "EquipeAchat" => "Achat", "Employe" => "Employé", _ => role };
        private static string GetSentimentColor(string sentiment) => sentiment switch { "Positif" => "#10b981", "Négatif" => "#ef4444", "Mitigé" => "#f59e0b", _ => "#6b7280" };
        private static string GetSentimentIcon(string sentiment) => sentiment switch
        {
            "Positif" => "M14 10h4.764a2 2 0 0 1 1.789 2.894l-3.5 7A2 2 0 0 1 15.263 21h-4.017c-.163 0-.326-.02-.485-.06L7 20m7-10V5a2 2 0 0 0-2-2h-.095c-.5 0-.905.405-.905.905a3.61 3.61 0 0 1-.608 2.006L7 11v9m7-10h-2M7 20H5a2 2 0 0 1-2-2v-6a2 2 0 0 1 2-2h2.5",
            "Négatif" => "M10 14H5.236a2 2 0 0 1-1.789-2.894l3.5-7A2 2 0 0 1 8.736 3h4.018a2 2 0 0 1 .485.06l3.76.94m-7 10v5a2 2 0 0 0 2 2h.096c.5 0 .905-.405.905-.904a3.61 3.61 0 0 1 .608-2.006L17 13V4m-7 10h2m5-10h2a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2h-2.5",
            "Mitigé"  => "M8 12h.01M12 12h.01M16 12h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z",
            _         => "M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z"
        };
        private static string Nettoyer(string v)
        {
            v = v.Trim();
            if (v.Length >= 2 && ((v.StartsWith('"') && v.EndsWith('"')) || (v.StartsWith('\'') && v.EndsWith('\''))))
                v = v[1..^1].Trim();
            return v;
        }
    }
}
