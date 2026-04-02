using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Components
{
    public partial class RoleCard
    {
        [Parameter] public string Title { get; set; } = string.Empty;
        [Parameter] public string Description { get; set; } = string.Empty;
        [Parameter] public string IconClass { get; set; } = string.Empty;
        [Parameter] public string CardColor { get; set; } = "#2563EB";
        [Parameter] public EventCallback OnSelect { get; set; }

        private async Task HandleClick()
        {
            await OnSelect.InvokeAsync();
        }
    }
}
