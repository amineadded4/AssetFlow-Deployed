using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using AssetFlow.Application.Interfaces;

namespace AssetFlow.Infrastructure.Services
{
    /// <summary>
    /// Envoie une notification Teams à l'employé concerné
    /// via Power Automate (HTTP trigger) lorsqu'un incident est résolu.
    /// Le flow Power Automate expose une URL publique —
    /// c'est votre app locale qui l'appelle (requête sortante), pas l'inverse.
    /// </summary>
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

        /// <summary>
        /// Appelle le webhook Power Automate pour notifier l'employé.
        /// À appeler depuis IncidentService.ChangerStatutAsync() quand Statut == Résolu.
        /// </summary>
        public async Task NotifierIncidentResoluAsync(int incidentId, string? commentaireResolution)
        {
            try
            {
                // Charger l'incident avec toutes les relations nécessaires
                var incident = await _db.Incidents
                    .Include(i => i.Affectation)
                        .ThenInclude(a => a.Utilisateur)
                    .Include(i => i.Affectation)
                        .ThenInclude(a => a.Materiel)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == incidentId);

                if (incident is null)
                {
                    Console.WriteLine("TeamsIncidentNotifier: incident #{Id} introuvable.", incidentId);
                    return;
                }

                var employe = incident.Affectation?.Utilisateur;
                if (employe is null)
                {
                    Console.WriteLine("TeamsIncidentNotifier: pas d'employé rattaché à l'incident #{Id}.", incidentId);
                    return;
                }

                if (string.IsNullOrWhiteSpace(employe.Email))
                {
                    Console.WriteLine("TeamsIncidentNotifier: employé #{UserId} sans email — notification ignorée.", employe.Id);
                    return;
                }

                var webhookUrl = _config["PowerAutomate:IncidentResoluWebhookUrl"];
                if (string.IsNullOrWhiteSpace(webhookUrl))
                {
                    Console.WriteLine("TeamsIncidentNotifier: PowerAutomate:IncidentResoluWebhookUrl non configuré.");
                    return;
                }

                var payload = new
                {
                    employeEmail   = employe.Email,
                    employeNom     = $"{employe.FirstName} {employe.LastName}",
                    numeroIncident = $"INC-{incident.DateIncident.Year}-{incident.Id:D3}",
                    typeIncident   = incident.TypeIncident,
                    materielNom    = incident.Affectation?.Materiel?.Designation ?? "—",
                    commentaire    = commentaireResolution?.Trim() ?? "Aucun commentaire",
                    dateResolution = DateTime.Now.ToString("dd/MM/yyyy à HH:mm")
                };

                var json    = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(webhookUrl, content);

                if (response.IsSuccessStatusCode)
                    Console.WriteLine(
                        "Notification Teams envoyée à {Email} pour l'incident #{Id}.",
                        employe.Email, incidentId);
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(
                        "Échec notification Teams pour incident #{Id}: {Status} — {Error}",
                        incidentId, response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                // On ne propage pas l'exception : l'échec de notification
                // ne doit pas faire échouer la résolution de l'incident.
                Console.WriteLine("Erreur inattendue dans TeamsIncidentNotifier pour incident #{Id}.");
            }
        }
    }
}