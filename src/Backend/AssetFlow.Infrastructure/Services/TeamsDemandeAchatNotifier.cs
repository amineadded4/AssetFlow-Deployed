using AssetFlow.Application.Interfaces;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace AssetFlow.Infrastructure.Services
{
    public class TeamsDemandeAchatNotifier : ITeamsDemandeAchatNotifier
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public TeamsDemandeAchatNotifier(
            AppDbContext db,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _db     = db;
            _config = config;
            _http   = httpClientFactory.CreateClient("PowerAutomate");
        }

       public async Task NotifierNouvelleDemandeAsync(int demandeId)
        {
            try
            {
                // ✅ Récupérer et matérialiser TOUTES les données d'abord
                var demande = await _db.DemandeAchat
                    .Include(d => d.Lignes)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.IdDemande == demandeId);

                if (demande is null) return;

                var webhookUrl = _config["PowerAutomate:NouvelleDemandeAchatWebhookUrl"];
                if (string.IsNullOrWhiteSpace(webhookUrl)) return;

                // ✅ Calculer tout AVANT de quitter le scope EF
                var quantite = demande.Lignes.Any()
                    ? demande.Lignes.Sum(l => l.Quantite)
                    : demande.Quantite;

                var payload = new
                {
                    demandeReference = demande.Reference,
                    nomProduit       = demande.NomProduit,
                    quantite         = quantite,
                    demandeurNom     = demande.DemandeurNom,
                    dateCreation = demande.DateCreation.ToLocalTime().ToString("dd/MM/yyyy à HH:mm"),
                    description      = demande.Description ?? "Aucune description"
                };

                var json    = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ✅ Appel HTTP après que toutes les données EF sont prêtes
                var response = await _http.PostAsync(webhookUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Teams notification échouée: {response.StatusCode} — {err}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur TeamsDemandeAchatNotifier: {ex.Message}");
            }
        }
    }
}