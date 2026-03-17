// ============================================================
// AssetFlow.BlazorUI / Pages / IT / OffresDemandeIT.razor.cs
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
        private string _userName  = "IT";

        private List<OffreAchatDto>              _offres    = new();
        private Guid?                            _expandedId;
        private Dictionary<Guid, string>         _pdfUrls   = new();
        private Dictionary<Guid, OffreFormState> _states    = new();
        private Dictionary<Guid, OcrStatus>      _ocrStatus = new();
        private Dictionary<Guid, string>         _ocrError  = new();
        private Dictionary<Guid, bool>           _confirmed = new();

        private string? _pdfModalUrl;
        private string  _pdfModalName = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            _userName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
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
                    _expandedId = _offres.First().IdOffre;
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

        // ── OCR ──────────────────────────────────────────────────
        private async Task RunOcr(OffreAchatDto offre)
        {
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

                var fs = GetOrCreate(offre.IdOffre);

                // Maintenant ces champs sont des strings - on assigne directement
                fs.FraisLivraison = invoice.InformationsAdditionnelles.FraisLivraison ?? "";
                fs.DelaiLivraison = invoice.InformationsAdditionnelles.DelaiLivraison ?? "";
                fs.Garantie = invoice.InformationsAdditionnelles.Garantie ?? "";

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

                fs.TotalHt  = invoice.Totaux.TotalHt;
                fs.TotalTva = invoice.Totaux.TotalTva;
                fs.TotalTtc = invoice.Totaux.TotalTtc;

                _ocrStatus[offre.IdOffre] = OcrStatus.Done;
            }
            catch (Exception ex)
            {
                _ocrError[offre.IdOffre]  = ex.Message;
                _ocrStatus[offre.IdOffre] = OcrStatus.Error;
            }

            StateHasChanged();
        }

        // ── Confirmer ────────────────────────────────────────────
        private void ConfirmForm(Guid offreId)
        {
            _confirmed[offreId] = true;
            StateHasChanged();
        }

        // ── PDF modal ─────────────────────────────────────────────
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
            _states[offreId]    = new OffreFormState();
            _ocrStatus[offreId] = OcrStatus.Idle;
            _ocrError.Remove(offreId);
            _confirmed.Remove(offreId);
            StateHasChanged();
        }

        private OcrStatus GetOcrStatus(Guid id) =>
            _ocrStatus.TryGetValue(id, out var s) ? s : OcrStatus.Idle;

        private string? GetOcrError(Guid id) =>
            _ocrError.TryGetValue(id, out var e) ? e : null;

        private bool IsConfirmed(Guid id) =>
            _confirmed.TryGetValue(id, out var c) && c;

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
        public string FraisLivraison { get; set; } = string.Empty;  // Changé en string
        public string DelaiLivraison { get; set; } = string.Empty;  // Changé en string
        public string Garantie       { get; set; } = string.Empty;  // Changé en string
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