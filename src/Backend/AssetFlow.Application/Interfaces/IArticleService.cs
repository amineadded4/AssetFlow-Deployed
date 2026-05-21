namespace AssetFlow.Application.Interfaces
{
    public interface IArticleService
    {
        Task<(bool Success, string Message, string? NumeroSerie)> UpdateNumeroSerieAsync(int id, string? numeroSerie);
        Task<(bool Success, string Message)> SupprimerArticleAsync(int id);

        /// <summary>
        /// Met un article hors service :
        ///   – le retire de son affectation courante si affecté
        ///   – remet le stock du matériel +1 si l'article était affecté (retour en stock puis hors service)
        ///   – passe le statut à HorsService
        ///   – enregistre un événement dans la biographie
        /// </summary>
        Task<(bool Success, string Message)> MettreHorsServiceAsync(int id);
        Task<(bool Success, string Message)> RemettreEnServiceAsync(int id);
    }
}