// ============================================================
// AssetFlow.Application / Interfaces / IConnectionTracker.cs
// ============================================================

namespace AssetFlow.Application.Interfaces
{
    public interface IConnectionTracker
    {
        void Add(int userId, string connectionId);
        /// <returns>true si l'utilisateur n'a plus aucune connexion active</returns>
        bool Remove(int userId, string connectionId);
        int? GetUserId(string connectionId);
        List<int> GetOnlineUserIds();
    }
}