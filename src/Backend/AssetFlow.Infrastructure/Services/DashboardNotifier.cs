using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AssetFlow.Infrastructure.Services
{
    public class DashboardNotifier : IDashboardNotifier
    {
        private readonly Func<Task> _notify;
        private readonly Func<Task> _notifyIT;
        private readonly Func<string, object?, Task> _notifyMemory;

        public DashboardNotifier(
            Func<Task> notify,
            Func<Task> notifyIT,
            Func<string, object?, Task> notifyMemory) 
        {
            _notify       = notify;
            _notifyIT     = notifyIT;
            _notifyMemory = notifyMemory; 
        }

        public Task NotifyAsync()    => _notify();
        public Task NotifyITAsync()  => _notifyIT();
        public Task NotifyMemoryAsync(string eventName, object? payload = null)
            => _notifyMemory(eventName, payload);
    }
}