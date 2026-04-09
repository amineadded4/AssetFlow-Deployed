using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Services
{
    public class MaterielService
    {
        private readonly HttpClient _http;
        private const string Base = "api/materiel";
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

        public MaterielService(HttpClient http) => _http = http;

        // ── Lecture ───────────────────────────────────────────────
        public async Task<List<MaterielDto>> GetAllAsync()
        {
            var result = await _http.GetFromJsonAsync<List<MaterielDto>>(Base);
            return result ?? new();
        }

        public async Task<MaterielDto?> GetByIdAsync(int id)
            => await _http.GetFromJsonAsync<MaterielDto>($"{Base}/{id}");

        public async Task<List<MaterielDto>> SearchAsync(
            string? terme = null, string? categorie = null)
        {
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(terme))     qs.Add($"terme={Uri.EscapeDataString(terme)}");
            if (!string.IsNullOrWhiteSpace(categorie)) qs.Add($"categorie={Uri.EscapeDataString(categorie)}");
            var url    = qs.Count > 0 ? $"{Base}/search?{string.Join("&", qs)}" : $"{Base}/search";
            var result = await _http.GetFromJsonAsync<List<MaterielDto>>(url);
            return result ?? new();
        }

        public async Task<MaterielStatsDto?> GetStatsAsync()
            => await _http.GetFromJsonAsync<MaterielStatsDto>($"{Base}/stats");

        // ── Écriture ──────────────────────────────────────────────
        public async Task<MaterielResultDto> AjouterAsync(CreerMaterielDto dto)
        {
            var resp = await _http.PostAsJsonAsync(Base, dto);
            return await resp.Content.ReadFromJsonAsync<MaterielResultDto>()
                   ?? new() { Succes = false, Message = "Réponse vide du serveur." };
        }

        public async Task<MaterielResultDto> ModifierAsync(ModifierMaterielDto dto)
        {
            var resp = await _http.PutAsJsonAsync($"{Base}/{dto.Id}", dto);
            return await resp.Content.ReadFromJsonAsync<MaterielResultDto>()
                   ?? new() { Succes = false, Message = "Réponse vide du serveur." };
        }

        public async Task<MaterielResultDto> SupprimerAsync(int id)
        {
            var userName  = await LocalStorage.GetItemAsync<string>("user_name")  ?? "Inconnu";

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{Base}/{id}");
            request.Headers.Add("X-User-Name", userName);

            var resp = await _http.SendAsync(request);
            return await resp.Content.ReadFromJsonAsync<MaterielResultDto>()
                ?? new() { Succes = false, Message = "Réponse vide du serveur." };
        }

        public async Task<MaterielResultDto> SupprimerAvecCascadeAsync(int id)
        {
            var userName  = await LocalStorage.GetItemAsync<string>("user_name")  ?? "Inconnu";

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{Base}/{id}/cascade");
            request.Headers.Add("X-User-Name", userName);

            var resp = await _http.SendAsync(request);
            return await resp.Content.ReadFromJsonAsync<MaterielResultDto>()
                ?? new() { Succes = false, Message = "Réponse vide du serveur." };
        }
    }
}