using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using AssetFlow.BlazorUI.CircuitBreaker;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class OffresDemandeIT : ComponentBase,IAsyncDisposable
    {
        [Parameter] public int DemandeId { get; set; }

        [Inject] private OffreDemandeService  OffreService { get; set; } = default!;
        [Inject] private NavigationManager    Navigation   { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
        [Inject] private OffreCircuitBreakerService _cbService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        private HubConnection? _hubConnection;

        private OffreCircuit _cbOcr  => _cbService.Ocr;
        private OffreCircuit _cbChat => _cbService.Chat;
        private int _countOcr  = 0;
        private int _countChat = 0;
        private System.Threading.Timer? _cbTimer;

        private bool    _isLoading = true;
        private bool    _menuOpen  = false;
        private bool    _isSaving  = false;
        private string  _userName  = "IT";
        private string  _userId    = string.Empty;
        private string? _saveError;

        private List<OffreAchatDto>              _offres    = new();
        private Guid?                            _expandedId;
        private Guid?                            _selectedId;
        private Guid?                            _confirmedId;
        private Dictionary<Guid, string>         _pdfUrls   = new();
        private Dictionary<Guid, OffreFormState> _states    = new();
        private Dictionary<Guid, OcrStatus>      _ocrStatus = new();
        private Dictionary<Guid, string>         _ocrError  = new();

        private string?        _pdfModalUrl;
        private string         _pdfModalName = string.Empty;
        private OffreAchatDto? _confirmModalOffre;

        // ── Chat ─────────────────────────────────────────────────
        private bool                    _chatOpen        = false;
        private bool                    _chatLoading     = false;
        private string                  _chatInput       = string.Empty;
        private int                     _unreadCount     = 0;
        private string?                 _recommendedOffre;
        private List<ChatbotMessageDto> _chatMessages    = new();
        private string                  _roleUtilisateur = "Service IT";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        // ── Chat UI ──────────────────────────────────────────────
        private void ToggleChat()
        {
            _chatOpen = !_chatOpen;
            if (_chatOpen) _unreadCount = 0;
            StateHasChanged();
        }

        private async Task OnChatKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_chatInput) && !_chatLoading)
                await SendChatMessage();
        }

        private async Task SendChatMessage()
        {
            if (string.IsNullOrWhiteSpace(_chatInput) || _chatLoading) return;
        
            // ── Circuit Breaker Chat ─────────────────────────────────────────────────
            if (_cbChat.TryTransitionHalfOpen())
            {
                StateHasChanged();
                await Task.Delay(3000);
            }
        
            if (!_cbChat.CanSend())
            {
                // La bannière affiche déjà le message — ne pas polluer le chat
                return;
            }
            // ── Fin Circuit Breaker ──────────────────────────────────────────────────
        
            var userMsg  = _chatInput.Trim();
            _chatInput   = string.Empty;
            _chatLoading = true;
        
            _chatMessages.Add(new ChatbotMessageDto { Role = "user", Content = userMsg });
            StateHasChanged();
        
            try
            {
                var offresCtx = _offres.Select(o =>
                {
                    var fs = GetOrCreate(o.IdOffre);
                    return new
                    {
                        nomFichier     = o.NomFichier,
                        prixTotal      = !string.IsNullOrEmpty(fs.TotalTtc)       ? fs.TotalTtc       : o.PrixTotal,
                        delaiLivraison = !string.IsNullOrEmpty(fs.DelaiLivraison) ? fs.DelaiLivraison : o.DelaiLivraison,
                        garantie       = !string.IsNullOrEmpty(fs.Garantie)       ? fs.Garantie       : o.Garantie,
                        fraisLivraison = !string.IsNullOrEmpty(fs.FraisLivraison) ? fs.FraisLivraison : o.FraisLivraison
                    };
                }).ToList();
        
                var payload = new
                {
                    userId    = _userId,
                    idDemande = DemandeId,
                    message   = userMsg,
                    offres    = offresCtx
                };
        
                var result = await OffreService.SendChatMessageAsync(payload);
                if (result != null)
                {
                    _cbChat.RecordSuccess();
                    _chatMessages.Add(new ChatbotMessageDto { Role = "assistant", Content = result.Reply });
        
                    if (!string.IsNullOrEmpty(result.RecommendedOffre))
                        _recommendedOffre = result.RecommendedOffre;
        
                    if (!_chatOpen) _unreadCount++;
                }
                else
                {
                    // ✅ AJOUT : traiter null comme un échec
                    Console.WriteLine($"[CB-CHAT] ❌ result == null → RecordFailure()");
                    _cbChat.RecordFailure();
                    Console.WriteLine($"[CB-CHAT] 📊 État après RecordFailure : {_cbChat.State}");
                    _chatMessages.Add(new ChatbotMessageDto
                    {
                        Role    = "assistant",
                        Content = "Je ne suis pas disponible en ce moment. Veuillez réessayer dans quelques instants."
                    });
                }
            }
            catch (HttpRequestException)
            {
                _cbChat.RecordFailure();
                _chatMessages.Add(new ChatbotMessageDto
                {
                    Role    = "assistant",
                    Content = "Je ne suis pas disponible en ce moment. Veuillez réessayer dans quelques instants."
                });
            }
            catch (Exception ex)
            {
                _cbChat.RecordFailure();
                _chatMessages.Add(new ChatbotMessageDto
                {
                    Role    = "assistant",
                    Content = ex.Message.Contains("401") || ex.Message.Contains("Unauthorized")
                        ? "Service non autorisé. Vérifiez la configuration du chatbot."
                        : "Une erreur est survenue. Veuillez réessayer."
                });
            }
        
            _chatLoading = false;
            StateHasChanged();
        }

        private static string FormatChatMessage(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"""([^""]+\.pdf)""",
                "<strong class=\"oda-chat-pdf-name\">\"$1\"</strong>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"\[\[([^\]]+)\]\]",
                "<strong class=\"oda-chat-pdf-name\">$1</strong>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"^- (.+)$",
                "<span class=\"oda-chat-list-item\">• $1</span>",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            content = content.Replace("\n", "<br/>");

            return content;
        }

        protected override async Task OnInitializedAsync()
        {
            _userName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            _userId   = await LocalStorage.GetItemAsync<string>("user_id")   ?? "unknown";

            await LoadOffres();
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
                        try { return await JS.InvokeAsync<string?>("eval",
                            "localStorage.getItem('access_token') || localStorage.getItem('token')"); }
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
                        var nouvelles = await OffreService.GetOffresByDemandeAsync(DemandeId);
                        // Préserver l'état OCR/formulaire des offres existantes
                        foreach (var offre in nouvelles)
                        {
                            if (_confirmedId == null)
                            {
                                var dejaChoisie = nouvelles.FirstOrDefault(o => o.EstChoisie);
                                if (dejaChoisie != null) _confirmedId = dejaChoisie.IdOffre;
                            }
                        }
                        _offres = nouvelles;
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

        // Modifier le DisposeAsync existant pour y ajouter le hub :
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
        private void DemarrerCbTimer()
        {
            _cbTimer = new System.Threading.Timer(async _ =>
            {
                _countOcr  = _cbOcr.SecondsRemaining;
                _countChat = _cbChat.SecondsRemaining;
        
                // Tenter les transitions Open→HalfOpen si timeout expiré
                _cbOcr.TryTransitionHalfOpen();
                _cbChat.TryTransitionHalfOpen();
        
                await InvokeAsync(StateHasChanged);
            }, null, 0, 1000);
        }

        private async Task LoadOffres()
        {
            _isLoading = true;
            try
            {
                _offres = await OffreService.GetOffresByDemandeAsync(DemandeId);

                if (_offres.Any())
                {
                    _selectedId = _offres.First().IdOffre;
                    _expandedId = _offres.First().IdOffre;

                    // ── RESTAURER l'offre déjà confirmée depuis SQL ──
                    var dejaChoisie = _offres.FirstOrDefault(o => o.EstChoisie);
                    if (dejaChoisie != null)
                    {
                        _confirmedId = dejaChoisie.IdOffre;
                        _selectedId  = dejaChoisie.IdOffre;
                        _expandedId  = dejaChoisie.IdOffre;
                    }

                    // Récupérer silencieusement les caches OCR existants
                    foreach (var offre in _offres)
                    {
                        try
                        {
                            var invoice = await OffreService.GetOcrCacheAsync(offre.IdOffre);
                            if (invoice != null)
                            {
                                ApplyInvoiceToState(offre.IdOffre, invoice);
                                _ocrStatus[offre.IdOffre] = OcrStatus.Done;
                            }
                        }
                        catch { }
                    }

                    // Charger historique chat
                    try
                    {
                        var chatHistory = await OffreService.GetChatHistoryAsync(_userId, DemandeId);
                        if (chatHistory.Any()) _chatMessages = chatHistory;
                    }
                    catch { }

                    // Charger recommandation persistée
                    try
                    {
                        var rec = await OffreService.GetRecommendationAsync(_userId, DemandeId);
                        if (rec != null && !string.IsNullOrEmpty(rec.RecommendedOffre))
                            _recommendedOffre = rec.RecommendedOffre;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OffresDemandeIT] Erreur chargement : {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // ── OCR helpers ──────────────────────────────────────────
        private void ApplyInvoiceToState(Guid offreId, InvoiceOcrDto invoice)
        {
            var fs = GetOrCreate(offreId);
            fs.FraisLivraison = invoice.InformationsAdditionnelles.FraisLivraison ?? "";
            fs.DelaiLivraison = invoice.InformationsAdditionnelles.DelaiLivraison ?? "";
            fs.Garantie       = invoice.InformationsAdditionnelles.Garantie       ?? "";
            fs.TotalHt        = invoice.Totaux.TotalHt;
            fs.TotalTva       = invoice.Totaux.TotalTva;
            fs.TotalTtc       = invoice.Totaux.TotalTtc;
            fs.Lignes = invoice.Lignes.Select(l => new LigneFormState
            {
                Description    = l.Description,
                Quantite       = l.Quantite,
                Unite          = l.Unite,
                PrixUnitaireHt = l.PrixUnitaireHt,
                TvaPct         = l.TvaPct,
                TotalTva       = l.TotalTva,
                TotalTtc       = l.TotalTtc
            }).ToList();
        }

        // ── Sélection radio ──────────────────────────────────────
        private void SelectOffre(Guid offreId)
        {
            if (_confirmModalOffre != null || _confirmedId.HasValue) return;
            _selectedId = offreId;
            StateHasChanged();
        }

        // ── Modal de confirmation ────────────────────────────────
        private void OpenConfirmModal(OffreAchatDto offre)
        {
            if (_confirmedId.HasValue) return;
            _confirmModalOffre = offre;
            StateHasChanged();
        }

        private void CloseConfirmModal()
        {
            if (_isSaving) return;
            _confirmModalOffre = null;
            StateHasChanged();
        }

        private async Task DoConfirm()
        {
            if (_confirmModalOffre == null) return;
            await ConfirmForm(_confirmModalOffre);
            _confirmModalOffre = null;
        }

        // ── OCR ──────────────────────────────────────────────────
        private async Task RunOcr(OffreAchatDto offre)
        {
            if (_confirmedId.HasValue) return;
        
            // ── Circuit Breaker OCR ──────────────────────────────────────────────────
            // Étape 1 : tenter la transition Open → HalfOpen si timeout expiré
            if (_cbOcr.TryTransitionHalfOpen())
            {
                StateHasChanged();         // affiche la bannière ambre
                await Task.Delay(3500);    // laisse l'utilisateur voir la bannière
            }
        
            // Étape 2 : bloquer si circuit encore OPEN
            if (!_cbOcr.CanSend())
            {
                // Ne pas afficher l'erreur technique brute — la bannière suffit
                return;
            }
            // ── Fin Circuit Breaker ──────────────────────────────────────────────────
        
            _ocrStatus[offre.IdOffre] = OcrStatus.Running;
            _ocrError.Remove(offre.IdOffre);
            StateHasChanged();
        
            try
            {
                var (invoice, error) = await OffreService.AnalyzeOcrAsync(offre.IdOffre);
        
                if (error != null)
                {
                    // Enregistrer l'échec DANS le circuit breaker
                    _cbOcr.RecordFailure();
        
                    // Afficher un message propre (jamais le message technique brut)
                    _ocrError[offre.IdOffre]  = FormatOcrError(error);
                    _ocrStatus[offre.IdOffre] = OcrStatus.Error;
                    StateHasChanged();
                    return;
                }
        
                if (invoice == null)
                {
                    _cbOcr.RecordFailure();
                    _ocrError[offre.IdOffre]  = "Le service d'analyse n'a retourné aucune donnée. Veuillez réessayer.";
                    _ocrStatus[offre.IdOffre] = OcrStatus.Error;
                    StateHasChanged();
                    return;
                }
        
                // Succès
                _cbOcr.RecordSuccess();
                ApplyInvoiceToState(offre.IdOffre, invoice);
                _ocrStatus[offre.IdOffre] = OcrStatus.Done;
            }
            catch (Exception ex)
            {
                _cbOcr.RecordFailure();
                _ocrError[offre.IdOffre]  = FormatOcrError(ex.Message);
                _ocrStatus[offre.IdOffre] = OcrStatus.Error;
            }
        
            StateHasChanged();
        }
        
        /// Transforme les erreurs techniques en messages lisibles pour l'utilisateur.
        private static string FormatOcrError(string raw)
        {
            if (raw.Contains("401") || raw.Contains("Unauthorized"))
                return "Le service d'analyse OCR n'est pas autorisé. Vérifiez la configuration.";
        
            if (raw.Contains("403") || raw.Contains("Forbidden"))
                return "Accès refusé au service d'analyse OCR.";
        
            if (raw.Contains("404") || raw.Contains("Not Found"))
                return "Le service d'analyse OCR est introuvable. Vérifiez l'URL de configuration.";
        
            if (raw.Contains("429") || raw.Contains("Too Many"))
                return "Trop de requêtes envoyées. Patientez quelques instants avant de réessayer.";
        
            if (raw.Contains("500") || raw.Contains("Internal Server"))
                return "Le service d'analyse OCR rencontre une erreur interne. Réessayez plus tard.";
        
            if (raw.Contains("timeout") || raw.Contains("Timeout") || raw.Contains("TaskCanceled"))
                return "Le service d'analyse OCR a mis trop de temps à répondre. Réessayez.";
        
            if (raw.Contains("HttpRequestException") || raw.Contains("Connection") || raw.Contains("Network"))
                return "Impossible de joindre le service d'analyse OCR. Vérifiez votre connexion.";
        
            // Message générique — ne jamais exposer le message technique brut
            return "Une erreur est survenue lors de l'analyse. Veuillez réessayer.";
        }

        // ── Confirmer → Redis ────────────────────────────────────
        private async Task ConfirmForm(OffreAchatDto offre)
        {
            _isSaving  = true;
            _saveError = null;
            StateHasChanged();

            try
            {
                var fs = GetOrCreate(offre.IdOffre);

                var payload = new
                {
                    offreId        = offre.IdOffre,
                    idDemande      = DemandeId,
                    userId         = _userId,
                    prixTotal      = fs.TotalTtc,
                    fraisLivraison = fs.FraisLivraison,
                    delaiLivraison = fs.DelaiLivraison,
                    garantie       = fs.Garantie,
                    autresOffres   = _offres
                        .Where(o => o.IdOffre != offre.IdOffre && GetOcrStatus(o.IdOffre) == OcrStatus.Done)
                        .Select(o =>
                        {
                            var s = GetOrCreate(o.IdOffre);
                            return new
                            {
                                offreId        = o.IdOffre,
                                prixTotal      = s.TotalTtc,
                                fraisLivraison = s.FraisLivraison,
                                delaiLivraison = s.DelaiLivraison,
                                garantie       = s.Garantie
                            };
                        }).ToList()
                };

                var (success, error) = await OffreService.ConfirmOffreAsync(payload);

                if (!success)
                {
                    _saveError = error;
                    _isSaving  = false;
                    StateHasChanged();
                    return;
                }

                _confirmedId = offre.IdOffre;
                _selectedId  = offre.IdOffre;

                offre.PrixTotal      = fs.TotalTtc;
                offre.FraisLivraison = fs.FraisLivraison;
                offre.DelaiLivraison = fs.DelaiLivraison;
                offre.Garantie       = fs.Garantie;
                offre.EstChoisie     = true;
            }
            catch (Exception ex)
            {
                _saveError = $"Erreur réseau : {ex.Message}";
            }

            _isSaving = false;
            StateHasChanged();
        }

        // ── PDF modal ────────────────────────────────────────────
        private async Task OpenPdfModal(OffreAchatDto offre)
        {
            if (!_pdfUrls.ContainsKey(offre.IdOffre))
            {
                try
                {
                    var bytes = await OffreService.GetPdfBytesAsync(offre.IdOffre);
                    if (bytes.Length > 0)
                        _pdfUrls[offre.IdOffre] = $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OffresDemandeIT] Erreur PDF : {ex.Message}");
                    return;
                }
            }
            _pdfModalUrl  = _pdfUrls[offre.IdOffre];
            _pdfModalName = offre.NomFichier;
            StateHasChanged();
        }

        private void ClosePdfModal()
        {
            _pdfModalUrl  = null;
            _pdfModalName = string.Empty;
        }

        private OffreFormState GetOrCreate(Guid id)
        {
            if (!_states.ContainsKey(id))
                _states[id] = new OffreFormState();
            return _states[id];
        }

        private void Toggle(Guid id) =>
            _expandedId = _expandedId == id ? null : id;

        private void ResetForm(Guid offreId)
        {
            if (_confirmedId.HasValue) return;
            _states[offreId]    = new OffreFormState();
            _ocrStatus[offreId] = OcrStatus.Idle;
            _ocrError.Remove(offreId);
            _saveError = null;
            StateHasChanged();
        }

        private OcrStatus GetOcrStatus(Guid id) =>
            _ocrStatus.TryGetValue(id, out var s) ? s : OcrStatus.Idle;

        private string? GetOcrError(Guid id) =>
            _ocrError.TryGetValue(id, out var e) ? e : null;

        private static bool IsDelaiRapide(string delai)
        {
            if (int.TryParse(delai.Replace(" jours", "").Replace(" j", "").Trim(), out var d))
                return d <= 5;
            return false;
        }

        private static string FormatBytes(long bytes) => bytes switch
        {
            >= 1_000_000 => $"{bytes / 1_000_000.0:F1} MB",
            >= 1_000     => $"{bytes / 1_000.0:F0} KB",
            _            => $"{bytes} B"
        };

        private string GetInitials()
        {
            var parts = _userName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }
    }

    // ── Classes frontend uniquement ──────────────────────────────────────────

    public enum OcrStatus { Idle, Running, Done, Error }

    public class OffreFormState
    {
        public string FraisLivraison       { get; set; } = string.Empty;
        public string DelaiLivraison       { get; set; } = string.Empty;
        public string Garantie             { get; set; } = string.Empty;
        public string TotalHt              { get; set; } = string.Empty;
        public string TotalTva             { get; set; } = string.Empty;
        public string TotalTtc             { get; set; } = string.Empty;
        public List<LigneFormState> Lignes { get; set; } = new();
    }

    public class LigneFormState
    {
        public string Description    { get; set; } = string.Empty;
        public string Quantite       { get; set; } = string.Empty;
        public string Unite          { get; set; } = string.Empty;
        public string PrixUnitaireHt { get; set; } = string.Empty;
        public string TvaPct         { get; set; } = string.Empty;
        public string TotalTva       { get; set; } = string.Empty;
        public string TotalTtc       { get; set; } = string.Empty;
    }
}