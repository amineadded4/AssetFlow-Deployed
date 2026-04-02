using AssetFlow.Domain.Entities;

namespace AssetFlow.Application.Interfaces
{
    public interface IFournisseurService
    {
        Task<List<Fournisseur>> GetAllAsync();
        Task<Fournisseur?> GetByIdAsync(int id);
        Task<List<Fournisseur>> RechercherAsync(string terme);
        Task<Fournisseur> AjouterAsync(Fournisseur fournisseur);
        Task ModifierAsync(Fournisseur fournisseur);
        Task SupprimerAsync(int id);
    }
}
