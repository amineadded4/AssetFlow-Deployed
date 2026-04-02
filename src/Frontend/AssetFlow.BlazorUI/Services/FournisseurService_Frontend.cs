using System.Net.Http.Json;
using AssetFlow.Application.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    // Service Blazor pour les appels HTTP vers /api/fournisseurs.
    public class FournisseurService
    {
        private readonly HttpClient _http;

        public FournisseurService(HttpClient http)
        {
            _http = http;
        }

        // GET ALL
        public async Task<List<FournisseurDto>> GetAllAsync()
        {
            try
            {
                var result = await _http
                    .GetFromJsonAsync<List<FournisseurDto>>("api/fournisseurs");
                return result ?? new List<FournisseurDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FournisseurService.GetAll] {ex.Message}");
                return new List<FournisseurDto>();
            }
        }

        // GET BY ID
        public async Task<FournisseurDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _http
                    .GetFromJsonAsync<FournisseurDto>($"api/fournisseurs/{id}");
            }
            catch
            {
                return null;
            }
        }

        // RECHERCHER
        public async Task<List<FournisseurDto>> RechercherAsync(string terme)
        {
            try
            {
                var enc = Uri.EscapeDataString(terme);
                var result = await _http
                    .GetFromJsonAsync<List<FournisseurDto>>(
                        $"api/fournisseurs/recherche?terme={enc}");
                return result ?? new List<FournisseurDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FournisseurService.Rechercher] {ex.Message}");
                return new List<FournisseurDto>();
            }
        }

        // AJOUTER (POST)
        public async Task<FournisseurReponseDto> AjouterAsync(CreerFournisseurDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/fournisseurs", dto);

                if (response.IsSuccessStatusCode)
                {
                    var reponse = await response.Content
                        .ReadFromJsonAsync<FournisseurReponseDto>();
                    return reponse ?? Echec("Réponse vide du serveur.");
                }

                return Echec($"Erreur serveur : {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Echec(ex.Message);
            }
        }

        // MODIFIER (PUT)
        public async Task<FournisseurReponseDto> ModifierAsync(ModifierFournisseurDto dto)
        {
            try
            {
                var response = await _http
                    .PutAsJsonAsync($"api/fournisseurs/{dto.IdFournisseur}", dto);

                if (response.IsSuccessStatusCode)
                    return new FournisseurReponseDto
                    {
                        Succes  = true,
                        Message = "Fournisseur modifié avec succès."
                    };

                return Echec($"Erreur serveur : {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Echec(ex.Message);
            }
        }

        // SUPPRIMER (DELETE)
        public async Task<FournisseurReponseDto> SupprimerAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/fournisseurs/{id}");

                if (response.IsSuccessStatusCode)
                    return new FournisseurReponseDto
                    {
                        Succes  = true,
                        Message = "Fournisseur supprimé avec succès."
                    };

                return Echec($"Erreur serveur : {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Echec(ex.Message);
            }
        }
        private static FournisseurReponseDto Echec(string message) =>
            new() { Succes = false, Message = message };
    }
}
