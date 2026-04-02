using System.Net.Http.Json;
using AssetFlow.Application.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class CommandeService
    {
        private readonly HttpClient _http;
        private const string Base = "api/commandes";

        public CommandeService(HttpClient http) => _http = http;

        public async Task<List<CommandeDto>> GetAllAsync()
        {
            var r = await _http.GetFromJsonAsync<List<CommandeDto>>(Base);
            return r ?? new();
        }

        public async Task<List<CommandeDto>> GetByMaterielAsync(int materielId)
        {
            var r = await _http.GetFromJsonAsync<List<CommandeDto>>($"{Base}/materiel/{materielId}");
            return r ?? new();
        }

        // UNE LIGNE PAR MATERIEL avec commandes imbriquées
        public async Task<List<LigneMaterielDto>> GetLignesMaterielsAsync()
        {
            var r = await _http.GetFromJsonAsync<List<LigneMaterielDto>>($"{Base}/lignes-materiels");
            return r ?? new();
        }

        // Compatibilité : une ligne par commande
        public async Task<List<LigneCommandeMaterielDto>> GetLignesCommandesAsync()
        {
            var r = await _http.GetFromJsonAsync<List<LigneCommandeMaterielDto>>($"{Base}/lignes-commandes");
            return r ?? new();
        }

        public async Task<List<ArticleDto>> GetArticlesByMaterielAsync(int materielId)
        {
            var r = await _http.GetFromJsonAsync<List<ArticleDto>>($"{Base}/articles/{materielId}");
            return r ?? new();
        }

        public async Task<List<ArticleDto>> GetArticlesByCommandeAsync(int commandeId)
        {
            var r = await _http.GetFromJsonAsync<List<ArticleDto>>($"{Base}/{commandeId}/articles");
            return r ?? new();
        }

        public async Task<CommandeReponseDto> CreerAsync(CreerCommandeDto dto)
        {
            var resp = await _http.PostAsJsonAsync(Base, dto);
            return await resp.Content.ReadFromJsonAsync<CommandeReponseDto>()
                   ?? new() { Succes = false, Message = "Réponse vide." };
        }

        public async Task<CommandeReponseDto> ModifierAsync(int id, ModifierCommandeDto dto)
        {
            var resp = await _http.PutAsJsonAsync($"{Base}/{id}", dto);
            return await resp.Content.ReadFromJsonAsync<CommandeReponseDto>()
                   ?? new() { Succes = false, Message = "Réponse vide." };
        }

        public async Task<CommandeReponseDto> SupprimerAsync(int id)
        {
            var resp = await _http.DeleteAsync($"{Base}/{id}");
            return await resp.Content.ReadFromJsonAsync<CommandeReponseDto>()
                   ?? new() { Succes = false, Message = "Réponse vide." };
        }
    }
}