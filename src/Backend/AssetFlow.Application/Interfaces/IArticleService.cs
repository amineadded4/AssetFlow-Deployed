namespace AssetFlow.Application.Interfaces
{
    public interface IArticleService
    {
        Task<(bool Success, string Message, string? NumeroSerie)> UpdateNumeroSerieAsync(int id, string? numeroSerie);
    }
}