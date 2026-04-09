using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IAuditLogService
    {
        Task<AuditLogPagedDto> GetLogsAsync(AuditLogQueryDto query);
        Task LogAsync(CreateAuditLogDto dto);

        // Helpers statiques pour les catégories et actions
        static class Categories
        {
            public const string Inscription  = "Inscription";
            public const string Materiel     = "Matériel";
            public const string Affectation  = "Affectation";
            public const string DemandeAchat = "DemandeAchat";
        }

        static class Actions
        {
            public const string Connexion    = "CONNEXION";
            public const string Inscription  = "INSCRIPTION";
            public const string Creation     = "CREATION";
            public const string Modification = "MODIFICATION";
            public const string Suppression  = "SUPPRESSION";
            public const string Affectation  = "AFFECTATION";
            public const string Revocation   = "REVOCATION";
            public const string Changement   = "CHANGEMENT ÉTAT";
        }
    }
}