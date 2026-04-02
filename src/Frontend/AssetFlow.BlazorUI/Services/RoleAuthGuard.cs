using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace AssetFlow.BlazorUI.Services
{
    public class RoleAuthGuard : ComponentBase
{
    [Inject] private AuthService      AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation  { get; set; } = default!;

    [Parameter] public string[]        AllowedRoles { get; set; } = Array.Empty<string>();
    [Parameter] public RenderFragment<string>? ChildContent { get; set; } // ← RenderFragment<string>

    private bool   _authorized = false;
    private bool   _checked    = false;
    private string _role       = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var isAuth = await AuthService.IsAuthenticatedAsync();
        if (!isAuth) { Navigation.NavigateTo("/"); return; }

        _role       = await AuthService.GetUserRoleAsync();
        _authorized = AllowedRoles.Length == 0 || AllowedRoles.Contains(_role);

        if (!_authorized) Navigation.NavigateTo("/acces-refuse");

        _checked = true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_checked && _authorized)
            builder.AddContent(0, ChildContent?.Invoke(_role));
    }
}
}