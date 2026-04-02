using AssetFlow.Application.Interfaces;

namespace AssetFlow.Infrastructure.Services
{
    public class ConnectionTracker : IConnectionTracker
    {
        private static readonly Dictionary<int, HashSet<string>> _connections = new();
        private static readonly Dictionary<string, int> _connToUser = new();
        private static readonly object _lock = new();

        public void Add(int userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.ContainsKey(userId))
                    _connections[userId] = new HashSet<string>();
                _connections[userId].Add(connectionId);
                _connToUser[connectionId] = userId;
            }
        }

        public bool Remove(int userId, string connectionId)
        {
            lock (_lock)
            {
                _connToUser.Remove(connectionId);
                if (_connections.TryGetValue(userId, out var conns))
                {
                    conns.Remove(connectionId);
                    if (conns.Count == 0)
                    {
                        _connections.Remove(userId);
                        return true; // plus aucune connexion active
                    }
                }
                return false;
            }
        }

        public int? GetUserId(string connectionId)
        {
            lock (_lock)
            {
                return _connToUser.TryGetValue(connectionId, out var id) ? id : null;
            }
        }

        public List<int> GetOnlineUserIds()
        {
            lock (_lock)
            {
                return _connections.Keys.ToList();
            }
        }
    }
}