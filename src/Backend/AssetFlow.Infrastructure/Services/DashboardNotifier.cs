using AssetFlow.Application.Interfaces;

namespace AssetFlow.Infrastructure.Services
{
    public class DashboardNotifier : IDashboardNotifier
    {
        private readonly Func<Task> _notify;
        private readonly Func<Task> _notifyIT;
        private readonly Func<string, object?, Task> _notifyMemory;
        private readonly Func<int, Task> _notifyBiographie;

        public DashboardNotifier(
            Func<Task> notify,
            Func<Task> notifyIT,
            Func<string, object?, Task> notifyMemory,
            Func<int, Task> notifyBiographie)
        {
            _notify           = notify;
            _notifyIT         = notifyIT;
            _notifyMemory     = notifyMemory;
            _notifyBiographie = notifyBiographie;
        }

        public Task NotifyAsync()        => _notify();
        public Task NotifyITAsync()      => _notifyIT();
        public Task NotifyMemoryAsync(string eventName, object? payload = null)
            => _notifyMemory(eventName, payload);
        public Task NotifyBiographieAsync(int articleId)
            => _notifyBiographie(articleId);
    }
}