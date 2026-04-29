// src/Backend/AssetFlow.Infrastructure/Services/ScrapingNotifier.cs
using AssetFlow.Application.Interfaces;

namespace AssetFlow.Infrastructure.Services
{
    public class ScrapingNotifier : IScrapingNotifier
    {
        private readonly Func<string, object, Task> _notifier;

        public ScrapingNotifier(Func<string, object, Task> notifier)
        {
            _notifier = notifier;
        }

        public Task NotifierTermineAsync(string groupId, object notification)
            => _notifier(groupId, notification);
    }
}