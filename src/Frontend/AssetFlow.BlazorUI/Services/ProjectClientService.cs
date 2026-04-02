using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class ProjectClientService
    {
        private readonly HttpClient _http;
        public ProjectClientService(HttpClient http) => _http = http;

        public Task<List<ProjectDto>?> GetAllAsync()
            => _http.GetFromJsonAsync<List<ProjectDto>>("api/projects");

        public Task<HttpResponseMessage> CreateAsync(object dto)
            => _http.PostAsJsonAsync("api/projects", dto);

        public Task<HttpResponseMessage> UpdateAsync(int id, object dto)
            => _http.PutAsJsonAsync($"api/projects/{id}", dto);

        public Task<HttpResponseMessage> DeleteAsync(int id)
            => _http.DeleteAsync($"api/projects/{id}");

        public async Task<List<ProjetAffectationDto>> GetAffectationsAsync(int projetId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ProjetAffectationDto>>(
                    $"api/projects/{projetId}/affectations") ?? new();
            }
            catch { return new(); }
        }
    }
}