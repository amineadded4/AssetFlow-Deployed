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

        private List<OffreAchatDto>              _offres  = new();
        private Guid?                            _expandedId;
        private Dictionary<Guid, string>         _pdfUrls = new();
        private Dictionary<Guid, OffreFormState> _states  = new();

        // ── Modale PDF ───────────────────────────────────────────
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

        // ── Ouvrir la modale PDF (charge si nécessaire) ──────────
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

        private void Toggle(Guid id)
        {
            _expandedId = _expandedId == id ? null : id;
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

        private void ResetForm(Guid offreId)
        {
            _states[offreId] = new OffreFormState();
            StateHasChanged();
        }
    }

    public class OffreFormState
    {
        public string   NomProduit       { get; set; } = string.Empty;
        public string   ReferenceProduit { get; set; } = string.Empty;
        public decimal? Prix             { get; set; }
        public decimal? FraisLivraison   { get; set; }
        public int?     DelaiLivraison   { get; set; }
        public int?     Garantie         { get; set; }
    }
}
