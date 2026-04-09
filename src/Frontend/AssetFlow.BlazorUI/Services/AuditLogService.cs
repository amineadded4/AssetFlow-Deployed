using System.Net.Http.Json;
using System.Text;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class AuditLogService
    {
        private readonly HttpClient _http;
        public AuditLogService(HttpClient http) => _http = http;

        public async Task<AuditLogPagedDto?> GetLogsAsync(
            DateTime? dateDebut   = null,
            DateTime? dateFin     = null,
            string?   utilisateur = null,
            string?   action      = null,
            string?   categorie   = null,
            string?   search      = null,
            int       page        = 1,
            int       pageSize    = 50)
        {
            var sb = new StringBuilder($"api/audit-logs?page={page}&pageSize={pageSize}");

            if (dateDebut.HasValue)
                sb.Append($"&dateDebut={dateDebut.Value:yyyy-MM-dd}");
            if (dateFin.HasValue)
                sb.Append($"&dateFin={dateFin.Value:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(utilisateur) && utilisateur != "Tous les utilisateurs")
                sb.Append($"&utilisateur={Uri.EscapeDataString(utilisateur)}");
            if (!string.IsNullOrWhiteSpace(action) && action != "Toutes les actions")
                sb.Append($"&action={Uri.EscapeDataString(action)}");
            if (!string.IsNullOrWhiteSpace(categorie) && categorie != "Toutes")
                sb.Append($"&categorie={Uri.EscapeDataString(categorie)}");
            if (!string.IsNullOrWhiteSpace(search))
                sb.Append($"&search={Uri.EscapeDataString(search)}");

            try
            {
                return await _http.GetFromJsonAsync<AuditLogPagedDto>(sb.ToString());
            }
            catch
            {
                return null;
            }
        }
    }
}