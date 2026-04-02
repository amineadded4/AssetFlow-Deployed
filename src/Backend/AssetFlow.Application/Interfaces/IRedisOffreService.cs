namespace AssetFlow.Application.Interfaces
{
    public interface IRedisOffreService
    {
        // Sauvegarde la sélection d'une offre dans Redis.
        Task SaveOffreSelectionAsync(string key, string jsonValue, TimeSpan? expiry = null);

        // Récupère une sélection depuis Redis.
        Task<string?> GetOffreSelectionAsync(string key);

        // Supprime une sélection depuis Redis.
        Task DeleteOffreSelectionAsync(string key);
    }
}