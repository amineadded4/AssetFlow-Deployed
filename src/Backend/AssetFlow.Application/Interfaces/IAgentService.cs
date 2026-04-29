using AssetFlow.Application.DTOs.AgentDtos;

namespace AssetFlow.Application.Interfaces
{
    public interface IAgentService
    {
        Task<AgentChatResponse>     ProcessMessageAsync(AgentChatRequest request);
        Task<AgentChatResponse>     GetInitialAlertsAsync();
        Task<AgentApprovalResponse> ApproveActionAsync(AgentApprovalRequest request);

        // ── NOUVEAU : workflow Demande d'achat ──────────────────────────────
        /// <summary>Liste des demandes d'achat en attente (statut ≠ traite / commande / refuse).</summary>
        Task<List<DemandePendingDto>> GetPendingDemandesAsync();

        /// <summary>Étape 1 : récupère la demande, lance la recherche web et renvoie 5 offres.</summary>
        Task<AgentChatResponse> StartDemandeWorkflowAsync(int idDemande);

        /// <summary>Étape 2 : à partir de l'offre choisie, pré-remplit la proposition matériel + commande.</summary>
        Task<AgentChatResponse> SelectOfferAsync(int idDemande, OffreSearchResultDto offre);
    }

    public interface IWebSearchAgentService
    {
        /// <param name="query">Requête de l'utilisateur</param>
        /// <param name="history">Historique de la conversation (optionnel)</param>
        Task<string> SearchAsync(string query,
            List<AgentChatHistory>? history = null);

        // ── NOUVEAU : recherche structurée de 5 offres pour une demande d'achat ──
        Task<List<OffreSearchResultDto>> SearchOffersAsync(string nomProduit, int quantite, string? description = null);
    }

    public interface IDatabaseAgentService
    {
        Task<List<AlerteStock>> GetStockAlertsAsync();

        /// <param name="question">Question de l'utilisateur</param>
        /// <param name="history">Historique de la conversation (optionnel)</param>
        Task<string> QueryAsync(string question,
            List<AgentChatHistory>? history = null);
    }

    public interface IOrchestratorAgentService
    {
        Task<string> DetermineAgentAsync(string userMessage,
            List<AgentChatHistory>? history = null);

        Task<AgentAction?> ExtractActionAsync(string userMessage,
            List<AgentChatHistory>? history = null);

        Task<AgentMaterielProposal> GenerateMaterielProposalAsync(AlerteStock alerte);
    }
}
