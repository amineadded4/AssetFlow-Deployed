using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using AssetFlow.Application.Interfaces;

namespace AssetFlow.Infrastructure.Services
{
    public class TeamsIncidentNotifier : ITeamsIncidentNotifier
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<TeamsIncidentNotifier> _logger;
        private readonly HttpClient _http;

        public TeamsIncidentNotifier(
            AppDbContext db,
            IConfiguration config,
            ILogger<TeamsIncidentNotifier> logger,
            IHttpClientFactory httpClientFactory)
        {
            _db     = db;
            _config = config;
            _logger = logger;
            _http   = httpClientFactory.CreateClient("PowerAutomate");
        }

        public async Task NotifierIncidentResoluAsync(int incidentId, string? commentaireResolution)
        {
            try
            {
                var incident = await _db.Incidents
                    .Include(i => i.Affectation)
                        .ThenInclude(a => a.Utilisateur)
                    .Include(i => i.Affectation)
                        .ThenInclude(a => a.Materiel)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == incidentId);

                if (incident is null) return;

                var employe = incident.Affectation?.Utilisateur;
                if (employe is null || string.IsNullOrWhiteSpace(employe.Email)) return;

                var estCompteTeams = employe.Email.EndsWith("@bizerte.r-iset.tn",
                    StringComparison.OrdinalIgnoreCase);

                if (estCompteTeams)
                    await EnvoyerTeamsAsync(incident, employe, commentaireResolution);
                else
                    await EnvoyerBrevoAsync(incident, employe, commentaireResolution);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur TeamsIncidentNotifier: {ex.Message}");
            }
        }

        private async Task EnvoyerTeamsAsync(Domain.Entities.Incident incident, Domain.Entities.User employe, string? commentaire)
        {
            var webhookUrl = _config["PowerAutomate:IncidentResoluWebhookUrl"];
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            var payload = new
            {
                employeEmail   = employe.Email,
                employeNom     = $"{employe.FirstName} {employe.LastName}",
                numeroIncident = $"INC-{incident.DateIncident.Year}-{incident.Id:D3}",
                typeIncident   = incident.TypeIncident,
                materielNom    = incident.Affectation?.Materiel?.Designation ?? "—",
                commentaire    = commentaire?.Trim() ?? "Aucun commentaire",
                dateResolution = DateTime.UtcNow.ToString("dd/MM/yyyy à HH:mm")
            };

            var json     = JsonSerializer.Serialize(payload);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(webhookUrl, content);

            if (response.IsSuccessStatusCode)
                Console.WriteLine($"Notification Teams envoyée à {employe.Email} pour l'incident #{incident.Id}.");
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Échec Teams pour incident #{incident.Id}: {response.StatusCode} — {err}");
            }
        }

        private async Task EnvoyerBrevoAsync(Domain.Entities.Incident incident, Domain.Entities.User employe, string? commentaire)
        {
            var apiKey      = _config["Brevo:ApiKey"];
            var from        = _config["Brevo:From"]     ?? "amineadded4@gmail.com";
            var fromName    = _config["Brevo:FromName"] ?? "AssetFlow";

            if (string.IsNullOrWhiteSpace(apiKey)) return;

            var numeroIncident   = $"INC-{incident.DateIncident.Year}-{incident.Id:D3}";
            var materielNom      = incident.Affectation?.Materiel?.Designation ?? "—";
            var dateResolution   = DateTime.UtcNow.ToString("dd/MM/yyyy à HH:mm");
            var commentaireFinal = commentaire?.Trim() ?? "Aucun commentaire";

            var corps = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <div style='max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 24px;'>
        <h2 style='color: #136dec;'>✅ Incident résolu — {numeroIncident}</h2>
        <p>Bonjour <strong>{employe.FirstName} {employe.LastName}</strong>,</p>
        <p>Votre incident a été résolu. Voici le récapitulatif :</p>
        <table style='width: 100%; border-collapse: collapse;'>
            <tr><td style='padding: 8px; background: #f5f5f5;'><strong>Numéro</strong></td><td style='padding: 8px;'>{numeroIncident}</td></tr>
            <tr><td style='padding: 8px; background: #f5f5f5;'><strong>Type</strong></td><td style='padding: 8px;'>{incident.TypeIncident}</td></tr>
            <tr><td style='padding: 8px; background: #f5f5f5;'><strong>Matériel</strong></td><td style='padding: 8px;'>{materielNom}</td></tr>
            <tr><td style='padding: 8px; background: #f5f5f5;'><strong>Commentaire</strong></td><td style='padding: 8px;'>{commentaireFinal}</td></tr>
            <tr><td style='padding: 8px; background: #f5f5f5;'><strong>Date de résolution</strong></td><td style='padding: 8px;'>{dateResolution}</td></tr>
        </table>
        <br/>
        <p style='color: #888; font-size: 12px;'>Cet email a été envoyé automatiquement par AssetFlow.</p>
    </div>
</body>
</html>";

            var payload = new
            {
                sender     = new { name = fromName, email = from },
                to         = new[] { new { email = employe.Email, name = $"{employe.FirstName} {employe.LastName}" } },
                subject    = $"✅ Incident résolu — {numeroIncident}",
                htmlContent = corps
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("api-key", apiKey);

            var response = await _http.PostAsync("https://api.brevo.com/v3/smtp/email", content);

            if (response.IsSuccessStatusCode)
                Console.WriteLine($"Email Brevo envoyé à {employe.Email} pour l'incident #{incident.Id}.");
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Échec Brevo pour incident #{incident.Id}: {response.StatusCode} — {err}");
            }
        }
    }
}