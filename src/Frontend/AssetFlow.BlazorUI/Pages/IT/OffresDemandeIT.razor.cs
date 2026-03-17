// ============================================================
// AssetFlow.BlazorUI / Pages / IT / OffresDemandeIT.razor.cs
// FINAL: sélection unique, modal confirm, Redis save
// ============================================================

using AssetFlow.Application.DTOs;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class OffresDemandeIT : ComponentBase
    {
        [Parameter] public int DemandeId { get; set; }

        [Inject] private HttpClient           Http         { get; set; } = default!;
        [Inject] private NavigationManager    Navigation   { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

        private bool   _isLoading = true;
        private bool   _menuOpen  = false;
        private bool   _isSaving  = false;
        private string _userName  = "IT";
        private string _userId    = string.Empty;
        private string? _saveError;

        private List<OffreAchatDto>              _offres    = new();
        private Guid?                            _expandedId;
        private Guid?                            _selectedId;          // offre sélectionnée (radio)
        private Guid?                            _confirmedId;         // UNE SEULE offre confirmée
        private Dictionary<Guid, string>         _pdfUrls   = new();
        private Dictionary<Guid, OffreFormState> _states    = new();
        private Dictionary<Guid, OcrStatus>      _ocrStatus = new();
        private Dictionary<Guid, string>         _ocrError  = new();

        private string?       _pdfModalUrl;
        private string        _pdfModalName = string.Empty;
        private OffreAchatDto? _confirmModalOffre;   // null = modal fermé

        protected override async Task OnInitializedAsync()
        {
            _userName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            _userId   = await LocalStorage.GetItemAsync<string>("user_id")   ?? "unknown";
            await LoadOffres();
        }

        private async Task LoadOffres()
        {
            _isLoading = true;
            try
            {
                _offres = await Http.GetFromJsonAsync<List<OffreAchatDto>>(
                    $"api/offreachat/demande/{DemandeId}") ?? new();

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
                            var response = await Http.GetAsync($"api/ocr/cache/{offre.IdOffre}");
                            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                                continue;
                            if (response.IsSuccessStatusCode)
                            {
                                var invoice = await response.Content.ReadFromJsonAsync<InvoiceOcrDto>();
                                if (invoice != null)
                                {
                                    ApplyInvoiceToState(offre.IdOffre, invoice);
                                    _ocrStatus[offre.IdOffre] = OcrStatus.Done;
                                }
                            }
                        }
                        catch { }
                    }
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

        // Méthode utilitaire pour éviter la duplication dans RunOcr et LoadOffres
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
            // Bloqué si modal ouvert ou déjà une confirmée
            if (_confirmModalOffre != null || _confirmedId.HasValue) return;
            _selectedId = offreId;
            StateHasChanged();
        }

        // ── Modal de confirmation ────────────────────────────────
        private void OpenConfirmModal(OffreAchatDto offre)
        {
            if (_confirmedId.HasValue) return;   // déjà une confirmée → bloquer
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

            _ocrStatus[offre.IdOffre] = OcrStatus.Running;
            _ocrError.Remove(offre.IdOffre);
            StateHasChanged();

            try
            {
                var response = await Http.PostAsync($"api/ocr/analyze/{offre.IdOffre}", null);

                if (!response.IsSuccessStatusCode)
                {
                    _ocrError[offre.IdOffre]  = await response.Content.ReadAsStringAsync();
                    _ocrStatus[offre.IdOffre] = OcrStatus.Error;
                    StateHasChanged();
                    return;
                }

                var invoice = await response.Content.ReadFromJsonAsync<InvoiceOcrDto>();
                if (invoice == null)
                {
                    _ocrError[offre.IdOffre]  = "Aucune donnée extraite.";
                    _ocrStatus[offre.IdOffre] = OcrStatus.Error;
                    StateHasChanged();
                    return;
                }

                ApplyInvoiceToState(offre.IdOffre, invoice);
                _ocrStatus[offre.IdOffre] = OcrStatus.Done;
            }
            catch (Exception ex)
            {
                _ocrError[offre.IdOffre]  = ex.Message;
                _ocrStatus[offre.IdOffre] = OcrStatus.Error;
            }

            StateHasChanged();
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
                    garantie       = fs.Garantie
                };

                var response = await Http.PostAsJsonAsync("api/offre-selection/confirm", payload);

                if (!response.IsSuccessStatusCode)
                {
                    _saveError = $"Erreur : {response.StatusCode}";
                    _isSaving  = false;
                    StateHasChanged();
                    return;
                }

                _confirmedId = offre.IdOffre;
                _selectedId  = offre.IdOffre;

                // Mettre à jour l'objet local pour affichage immédiat sans rechargement
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
                    var bytes = await Http.GetByteArrayAsync($"api/offreachat/{offre.IdOffre}/pdf");
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

        // ── Helpers ──────────────────────────────────────────────
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

    public enum OcrStatus { Idle, Running, Done, Error }

    public class OffreFormState
    {
        public string FraisLivraison { get; set; } = string.Empty;
        public string DelaiLivraison { get; set; } = string.Empty;
        public string Garantie       { get; set; } = string.Empty;
        public string TotalHt        { get; set; } = string.Empty;
        public string TotalTva       { get; set; } = string.Empty;
        public string TotalTtc       { get; set; } = string.Empty;
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