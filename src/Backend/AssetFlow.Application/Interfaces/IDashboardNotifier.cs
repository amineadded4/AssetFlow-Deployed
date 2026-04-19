namespace AssetFlow.Application.Interfaces
{
    public interface IDashboardNotifier
    {
        Task NotifyAsync();
        Task NotifyITAsync();
        Task NotifyMemoryAsync(string eventName, object? payload = null);
    }
}