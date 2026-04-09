using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Services
{
    public class CommandeService
    {
        private readonly HttpClient _http;
        private const string Base = "api/commandes";
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

        public CommandeService(HttpClient http, ILocalStorageService localStorage)
        {
            _http = http;
            LocalStorage = localStorage;
        }

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
            var userName = (await LocalStorage.GetItemAsync<string>("user_name") ?? "Inconnu").Trim('"');

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{Base}/{id}");
            request.Headers.Add("X-User-Name", userName);

            var resp = await _http.SendAsync(request);
            return await resp.Content.ReadFromJsonAsync<CommandeReponseDto>()
                ?? new() { Succes = false, Message = "Réponse vide." };
        }
    }
}