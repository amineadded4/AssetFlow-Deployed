namespace AssetFlow.Application.Interfaces
{
    using AssetFlow.Application.DTOs;
 
    public interface INotificationService
    {
        /// <summary>Retourne toutes les notifications non lues (ou récentes)</summary>
        Task<List<NotificationDto>> GetNotificationsAsync(string? role = null, bool nonLuesSeulement = false);
 
        /// <summary>Retourne le nombre de notifications non lues</summary>
        Task<int> GetNombreNonLuesAsync(string? role = null);
 
        /// <summary>Marque une notification comme lue</summary>
        Task MarquerCommeLueAsync(int notificationId);
 
        /// <summary>Marque toutes les notifications comme lues</summary>
        Task MarquerToutesCommeLuesAsync(string? role = null);
 
        /// <summary>Génère les notifications d'affectations expirées (appelé périodiquement ou à la demande)</summary>
        Task GenererNotificationsAffectationsExpireesAsync();
 
        /// <summary>Supprime les notifications lues de plus de 30 jours</summary>
        Task NettoyerAnciennesNotificationsAsync();
    }
}