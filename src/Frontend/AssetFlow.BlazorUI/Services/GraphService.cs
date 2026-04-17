using AssetFlow.BlazorUI.DTOs;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class GraphService
    {
        private readonly HttpClient _http;

        public GraphService(HttpClient http)
        {
            _http = http;
        }

        // Stats globales
        public async Task<GraphStatsDto?> GetStatsAsync()
        {
            try { return await _http.GetFromJsonAsync<GraphStatsDto>("api/graph/stats"); }
            catch { return null; }
        }

        // Listes panneau gauche
        public async Task<List<GraphEntitySummaryDto>> GetMaterielsAsync()
        {
            try { return await _http.GetFromJsonAsync<List<GraphEntitySummaryDto>>("api/graph/entities/materiels") ?? new(); }
            catch { return new(); }
        }

        public async Task<List<GraphEntitySummaryDto>> GetUtilisateursAsync()
        {
            try { return await _http.GetFromJsonAsync<List<GraphEntitySummaryDto>>("api/graph/entities/utilisateurs") ?? new(); }
            catch { return new(); }
        }

        public async Task<List<GraphEntitySummaryDto>> GetDemandesAsync()
        {
            try { return await _http.GetFromJsonAsync<List<GraphEntitySummaryDto>>("api/graph/entities/demandes") ?? new(); }
            catch { return new(); }
        }

        public async Task<List<GraphEntitySummaryDto>> GetProjetsAsync()
        {
            try { return await _http.GetFromJsonAsync<List<GraphEntitySummaryDto>>("api/graph/entities/projets") ?? new(); }
            catch { return new(); }
        }

        // Graphes contextuels
        public async Task<GraphResponseDto?> GetGraphForMaterielAsync(int id)
        {
            try { return await _http.GetFromJsonAsync<GraphResponseDto>($"api/graph/context/materiel/{id}"); }
            catch { return null; }
        }

        public async Task<GraphResponseDto?> GetGraphForUtilisateurAsync(int id)
        {
            try { return await _http.GetFromJsonAsync<GraphResponseDto>($"api/graph/context/utilisateur/{id}"); }
            catch { return null; }
        }

        public async Task<GraphResponseDto?> GetGraphForDemandeAsync(int id)
        {
            try { return await _http.GetFromJsonAsync<GraphResponseDto>($"api/graph/context/demande/{id}"); }
            catch { return null; }
        }

        public async Task<GraphResponseDto?> GetGraphForProjetAsync(int id)
        {
            try { return await _http.GetFromJsonAsync<GraphResponseDto>($"api/graph/context/projet/{id}"); }
            catch { return null; }
        }

        // Legacy
        public async Task<GraphResponseDto?> GetGraphAsync()
        {
            try { return await _http.GetFromJsonAsync<GraphResponseDto>("api/graph"); }
            catch { return null; }
        }

        public async Task<GraphInsightDto?> GetNodeInsightAsync(string nodeId)
        {
            try { return await _http.GetFromJsonAsync<GraphInsightDto>($"api/graph/insight/{nodeId}"); }
            catch { return null; }
        }
    }
}