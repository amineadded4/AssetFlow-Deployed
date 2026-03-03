// Services/RoleAuthGuard.cs
// Un composant Blazor pour protéger l'accès à certaines pages selon les rôles de l'utilisateur
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace AssetFlow.BlazorUI.Services
{
    public class RoleAuthGuard : ComponentBase
    {
        [Inject] private AuthService AuthService    { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        [Parameter] public string[]       AllowedRoles { get; set; } = Array.Empty<string>();
        [Parameter] public RenderFragment? ChildContent { get; set; }

        private bool _authorized = false;
        private bool _checked    = false;

        protected override async Task OnInitializedAsync()
        {
            var isAuth = await AuthService.IsAuthenticatedAsync();
            if (!isAuth)
            {
                Navigation.NavigateTo("/login");
                return;
            }

            var role = await AuthService.GetUserRoleAsync();
            _authorized = AllowedRoles.Length == 0 || AllowedRoles.Contains(role);

            if (!_authorized)
                Navigation.NavigateTo("/acces-refuse");

            _checked = true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_checked && _authorized)
                builder.AddContent(0, ChildContent);
        }
    }
}