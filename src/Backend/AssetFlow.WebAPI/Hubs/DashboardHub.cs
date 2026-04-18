// AssetFlow.WebAPI/Hubs/DashboardHub.cs
using Microsoft.AspNetCore.SignalR;

namespace AssetFlow.WebAPI.Hubs
{
    public class DashboardHub : Hub
    {
        public async Task JoinDashboard()
            => await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");

        public async Task LeaveDashboard()
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");

        public async Task JoinDashboardIT()
        => await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard-it");

        public async Task LeaveDashboardIT()
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard-it");
        public async Task JoinMemory()
            => await Groups.AddToGroupAsync(Context.ConnectionId, "MemoryGroup");

        public async Task LeaveMemory()
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "MemoryGroup");
    }
}