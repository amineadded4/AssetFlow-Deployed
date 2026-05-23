// AssetFlow.BlazorUI / Components / BackButton.razor.cs
// Navigue vers la route racine (/)

using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Components
{
    public partial class BackButton
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private void GoHome()
        {
            Navigation.NavigateTo("/");
        }
    }
}
