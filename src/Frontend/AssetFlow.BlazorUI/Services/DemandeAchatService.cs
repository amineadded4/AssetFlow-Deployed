using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class DemandeAchatService
    {
        private readonly HttpClient _http;

        public DemandeAchatService(HttpClient http)
        {
            _http = http;
        }

        // ────────────────────────────────────────────────────────
        // GET ALL — toutes les demandes
        // ────────────────────────────────────────────────────────

        public async Task<List<DemandeAchatDto>> GetAllAsync()
        {
            try
            {
                var result = await _http
                    .GetFromJsonAsync<List<DemandeAchatDto>>("api/demandes");
                return result ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DemandeAchatService.GetAll] {ex.Message}");
                return new();
            }
        }

        // ────────────────────────────────────────────────────────
        // GET BY ID — une demande par son ID
        // ────────────────────────────────────────────────────────

        public async Task<DemandeAchatDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _http
                    .GetFromJsonAsync<DemandeAchatDto>($"api/demandes/{id}");
            }
            catch
            {
                return null;
            }
        }

        // ────────────────────────────────────────────────────────
        // CHANGER STATUT
        // ────────────────────────────────────────────────────────

        public async Task<DemandeAchatReponseDto> ChangerStatutAsync(
            int id, string statut, string userName,string? motifRefus = null)
        {
            try
            {
                var dto = new ChangerStatutDto
                {
                    Statut     = statut,
                    MotifRefus = motifRefus,
                    Utilisateur = userName
                };

                var response = await _http
                    .PutAsJsonAsync($"api/demandes/{id}/statut", dto);

                if (response.IsSuccessStatusCode)
                {
                    var rep = await response.Content
                        .ReadFromJsonAsync<DemandeAchatReponseDto>();
                    return rep ?? Succes("Statut mis à jour.", id);
                }

                return Echec($"Erreur serveur : {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Echec(ex.Message);
            }
        }

        // ────────────────────────────────────────────────────────
        // AJOUTER UNE OFFRE PDF (multipart)
        // ────────────────────────────────────────────────────────

        public async Task<DemandeAchatReponseDto> AjouterOffreAsync(
            int idDemande, string nomFichier, byte[] contenu)
        {
            try
            {
                using var form        = new MultipartFormDataContent();
                var       fileContent = new ByteArrayContent(contenu);
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue("application/pdf");
                form.Add(fileContent, "fichier", nomFichier);

                var response = await _http
                    .PostAsync($"api/demandes/{idDemande}/offres", form);

                if (response.IsSuccessStatusCode)
                {
                    var rep = await response.Content
                        .ReadFromJsonAsync<DemandeAchatReponseDto>();
                    return rep ?? Succes("Offre ajoutée.", idDemande);
                }

                return Echec($"Erreur serveur : {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Echec(ex.Message);
            }
        }

        // ────────────────────────────────────────────────────────
        // SUPPRIMER UNE OFFRE
        // ────────────────────────────────────────────────────────

        public async Task<DemandeAchatReponseDto> SupprimerOffreAsync(
            int idDemande, Guid idOffre)
        {
            try
            {
                var response = await _http
                    .DeleteAsync($"api/demandes/{idDemande}/offres/{idOffre}");

                if (response.IsSuccessStatusCode)
                    return Succes("Offre supprimée.", idDemande);

                return Echec($"Erreur serveur : {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Echec(ex.Message);
            }
        }

        // ────────────────────────────────────────────────────────
        // URL PDF — pour l'aperçu dans l'iframe
        // Construit l'URL directe du PDF à partir de l'ID de l'offre
        // ────────────────────────────────────────────────────────

        public string GetPdfUrl(int idDemande, Guid idOffre)
        {
            var baseUrl = _http.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
            return $"{baseUrl}/api/demandes/{idDemande}/offres/{idOffre}/pdf";
        }
        public async Task MarquerVuAsync(int idDemande)
        {
            try { await _http.PutAsync($"api/demandes/{idDemande}/vu", null); }
            catch { }
        }

        public async Task<int> GetCountNonVusAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<int>("api/demandes/non-vus/count");
            }
            catch { return 0; }
        }

        // ── Helpers privés ──────────────────────────────────────

        private static DemandeAchatReponseDto Echec(string msg) =>
            new() { Succes = false, Message = msg };

        private static DemandeAchatReponseDto Succes(string msg, int? id = null) =>
            new() { Succes = true, Message = msg, IdDemande = id };
    }
}
