using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IEmployeService
    {
        Task<List<EquipementAffecteDto>> GetEquipementsAffectesAsync(int utilisateurId);
        Task<EquipementAffecteDto?> GetEquipementDetailAsync(int affectationId, int articleId = 0);
        Task<List<MaterielAffecteGroupeDto>> GetMaterielsGroupesAsync(int utilisateurId);
    }
}