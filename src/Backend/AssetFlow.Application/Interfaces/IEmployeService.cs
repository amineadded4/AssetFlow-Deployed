// ============================================================
// AssetFlow.Application / Interfaces / IEmployeService.cs
// MISE À JOUR : ajout GetMaterielsGroupesAsync
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    /// <summary>
    /// Service pour les opérations de l'employé
    /// </summary>
    public interface IEmployeService
    {
        /// <summary>
        /// Récupère tous les équipements affectés à un employé donné
        /// </summary>
        Task<List<EquipementAffecteDto>> GetEquipementsAffectesAsync(int utilisateurId);

        /// <summary>
        /// Récupère le détail d'une affectation spécifique
        /// </summary>
        Task<EquipementAffecteDto?> GetEquipementDetailAsync(int affectationId, int articleId = 0);

        /// <summary>
        /// Récupère les matériels groupés avec leurs articles pour un employé.
        /// Chaque matériel contient la liste des articles (affectations) qui lui appartiennent.
        /// </summary>
        /// <param name="utilisateurId">ID de l'employé</param>
        Task<List<MaterielAffecteGroupeDto>> GetMaterielsGroupesAsync(int utilisateurId);
    }
}