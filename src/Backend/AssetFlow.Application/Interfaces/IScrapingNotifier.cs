// src/Backend/AssetFlow.Application/Interfaces/IScrapingNotifier.cs
namespace AssetFlow.Application.Interfaces
{
    public interface IScrapingNotifier
    {
        Task NotifierTermineAsync(string groupId, object notification);
    }
}