using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class AgentChatService
    {
        private readonly HttpClient _http;
        private const string Base = "api/agent";

        public AgentChatService(HttpClient http) => _http = http;

        public async Task<AgentChatResponse?> GetInitialAlertsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<AgentChatResponse>($"{Base}/alerts");
            }
            catch { return null; }
        }

        public async Task<AgentChatResponse?> ChatAsync(AgentChatRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{Base}/chat", request);
                return await resp.Content.ReadFromJsonAsync<AgentChatResponse>();
            }
            catch { return null; }
        }

        public async Task<AgentApprovalResponse?> ApproveAsync(AgentApprovalRequest request, string userName)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"{Base}/approve");
                req.Headers.Add("X-User-Name", userName);
                req.Content = JsonContent.Create(request);
                var resp = await _http.SendAsync(req);
                return await resp.Content.ReadFromJsonAsync<AgentApprovalResponse>();
            }
            catch { return null; }
        }

        // ════════════════════════════════════════════════════════════════════
        //  ── NOUVEAU : Workflow Demande d'achat ─────────────────────────────
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Liste des demandes d'achat à afficher dans le dropdown du chat.</summary>
        public async Task<List<DemandePendingDto>> GetPendingDemandesAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<DemandePendingDto>>($"{Base}/demandes-pending")
                    ?? new List<DemandePendingDto>();
            }
            catch { return new List<DemandePendingDto>(); }
        }

        /// <summary>Étape 1 — Lance le workflow pour une demande : recherche web et 5 offres.</summary>
        public async Task<AgentChatResponse?> StartDemandeWorkflowAsync(int idDemande)
        {
            try
            {
                var resp = await _http.PostAsync($"{Base}/demande/{idDemande}/start", null);
                return await resp.Content.ReadFromJsonAsync<AgentChatResponse>();
            }
            catch { return null; }
        }

        /// <summary>Étape 2 — L'utilisateur a cliqué une carte d'offre : retourne le formulaire pré-rempli.</summary>
        public async Task<AgentChatResponse?> SelectOfferAsync(int idDemande, OffreSearchResultDto offre)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync(
                    $"{Base}/demande/{idDemande}/select-offer",
                    new SelectOfferRequest { Offre = offre });
                return await resp.Content.ReadFromJsonAsync<AgentChatResponse>();
            }
            catch { return null; }
        }
    }
}
