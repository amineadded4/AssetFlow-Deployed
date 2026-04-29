// src/Backend/AssetFlow.WebAPI/Hubs/ScrapingHub.cs
using Microsoft.AspNetCore.SignalR;

namespace AssetFlow.WebAPI.Hubs
{
    public class ScrapingHub : Hub
    {
        public async Task Subscribe(string groupId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, groupId);

        public async Task Unsubscribe(string groupId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
    }
}