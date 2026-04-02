using System.Net.Http.Json;

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

    public class ProjectDto
    {
        public int       Id          { get; set; }
        public string    Nom         { get; set; } = string.Empty;
        public string?   Description { get; set; }
        public string    Statut      { get; set; } = "Planifie";
        public string    Priorite    { get; set; } = "Moyenne";
        public string?   Responsable { get; set; }
        public decimal?  Budget      { get; set; }
        public DateTime? DateDebut   { get; set; }
        public DateTime? DateFin     { get; set; }
        public DateTime  CreatedAt   { get; set; }
        public DateTime  UpdatedAt   { get; set; }
    }

    public class ProjetAffectationDto
    {
        public int       AffectationId    { get; set; }
        public string    Designation      { get; set; } = string.Empty;
        public string    Reference        { get; set; } = string.Empty;
        public int       QuantiteAffectee { get; set; }
        public DateTime  DateAffectation  { get; set; }
        public DateTime? DateRetourPrevue { get; set; }
        public string    Etat             { get; set; } = string.Empty;
    }
}