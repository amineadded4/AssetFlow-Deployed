using AssetFlow.Application.DTOs;
 
namespace AssetFlow.Application.Interfaces
{
    public interface IConversationHistoryService
    {
        /// <summary>Créer une nouvelle conversation et retourner son ID</summary>
        Task<string> CreateConversationAsync(int userId, string title);
 
        /// <summary>Récupérer toutes les conversations d'un utilisateur (sans les messages)</summary>
        Task<List<ConversationSummary>> GetConversationsAsync(int userId);
 
        /// <summary>Récupérer les messages d'une conversation</summary>
        Task<List<ConversationMessage>> GetMessagesAsync(string conversationId);
 
        /// <summary>Ajouter un message à une conversation</summary>
        Task AddMessageAsync(string conversationId, ConversationMessage message);
 
        /// <summary>Mettre à jour le titre d'une conversation</summary>
        Task UpdateTitleAsync(string conversationId, string title);
 
        /// <summary>Supprimer une conversation</summary>
        Task DeleteConversationAsync(string conversationId, int userId);
 
        /// <summary>Supprimer toutes les conversations d'un utilisateur</summary>
        Task DeleteAllConversationsAsync(int userId);
 
        /// <summary>Supprimer les conversations de plus de 30 jours (appelé par le job)</summary>
        Task PurgeExpiredConversationsAsync();
    }
}