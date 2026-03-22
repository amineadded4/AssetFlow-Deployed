// ============================================================
// RoleSelect.razor.cs — MISE À JOUR : onglet Face login
// ============================================================

using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.RoleSelection
{
    public partial class RoleSelect : IAsyncDisposable
    {
        [Inject] private NavigationManager    Navigation       { get; set; } = default!;
        [Inject] private AuthService          AuthService      { get; set; } = default!;
        [Inject] private FaceAuthClientService FaceAuthService { get; set; } = default!;
        [Inject] private IJSRuntime           JS               { get; set; } = default!;

        // ── Buffer clavier ──
        private string   KeyBuffer   { get; set; } = string.Empty;
        private DateTime LastKeyTime { get; set; } = DateTime.MinValue;
        private const int KeyTimeoutMs = 1500;

        // ── Modal ──
        private bool ShowAdminModal   { get; set; } = false;

        // ── Onglet actif : "classic" ou "face" ──
        private string ActiveTab { get; set; } = "classic";

        // ── Champs classic ──
        private string AdminEmail        { get; set; } = string.Empty;
        private string AdminPassword     { get; set; } = string.Empty;
        private bool   ShowAdminPassword { get; set; } = false;
        private bool   AdminIsLoading    { get; set; } = false;
        private string AdminErrorMessage { get; set; } = string.Empty;
        private bool   AdminEmailError   { get; set; } = false;

        // ── Champs face ──
        private string FaceEmail        { get; set; } = string.Empty;
        private bool   FaceIsLoading    { get; set; } = false;
        private string FaceErrorMessage { get; set; } = string.Empty;
        private bool   FaceDetected     { get; set; } = false;
        private bool   MediaPipeReady   { get; set; } = false;
        private bool   CameraStarted    { get; set; } = false;
        private DotNetObjectReference<RoleSelect>? _dotnetRef;

        // ── Keydown page principale ──
        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (ShowAdminModal) return;
            if (e.Key.Length != 1) return;

            var now = DateTime.Now;
            if ((now - LastKeyTime).TotalMilliseconds > KeyTimeoutMs)
                KeyBuffer = string.Empty;

            LastKeyTime = now;
            KeyBuffer  += e.Key.ToLower();
            if (KeyBuffer.Length > 5) KeyBuffer = KeyBuffer[^5..];

            if (KeyBuffer.EndsWith("admin"))
            {
                KeyBuffer = string.Empty;
                OpenAdminModal();
            }
        }

        private async Task HandleModalKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")  await HandleAdminLogin();
            if (e.Key == "Escape") CloseModal();
        }

        // ── Open / Close ──
        private void OpenAdminModal()
        {
            AdminEmail = AdminPassword = FaceEmail = string.Empty;
            AdminErrorMessage = FaceErrorMessage = string.Empty;
            AdminEmailError = AdminIsLoading = FaceIsLoading = false;
            ShowAdminPassword = false;
            ActiveTab       = "classic";
            FaceDetected    = false;
            MediaPipeReady  = false;
            CameraStarted   = false;
            ShowAdminModal  = true;
        }

        private async Task CloseModal()
        {
            if (CameraStarted)
            {
                await JS.InvokeVoidAsync("stopCamera", "face-video");
                CameraStarted = false;
            }
            ShowAdminModal    = false;
            AdminErrorMessage = FaceErrorMessage = string.Empty;
            AdminEmailError   = false;
            _dotnetRef?.Dispose();
            _dotnetRef = null;
        }

        // ── Switch tab ──
        private async Task SwitchTab(string tab)
        {
            ActiveTab = tab;
            FaceErrorMessage = string.Empty;
            FaceDetected     = false;

            if (tab == "face")
                await InitFaceLoginAsync();
            else
            {
                if (CameraStarted)
                {
                    await JS.InvokeVoidAsync("stopCamera", "face-video");
                    CameraStarted = false;
                }
            }
        }

        // ── Initialiser MediaPipe + caméra ──
        private async Task InitFaceLoginAsync()
        {
            if (!MediaPipeReady)
            {
                MediaPipeReady = await JS.InvokeAsync<bool>("initMediaPipe");
                if (!MediaPipeReady)
                {
                    FaceErrorMessage = "Impossible de charger MediaPipe. Vérifiez votre connexion.";
                    return;
                }
            }

            if (!CameraStarted)
            {
                CameraStarted = await JS.InvokeAsync<bool>("startCamera", "face-video");
                if (!CameraStarted) { FaceErrorMessage = "Caméra non disponible."; return; }
            }

            _dotnetRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("startPreview", "face-video", "face-canvas", _dotnetRef);
        }

        // ── Callback JS → Blazor : visage détecté ──
        [JSInvokable]
        public void OnFaceDetected(bool detected)
        {
            if (FaceDetected != detected)
            {
                FaceDetected = detected;
                InvokeAsync(StateHasChanged);
            }
        }

        // ── Classic login ──
        private void ToggleAdminPassword() => ShowAdminPassword = !ShowAdminPassword;

        private async Task HandleAdminLogin()
        {
            AdminErrorMessage = string.Empty;
            AdminEmailError   = false;

            if (!AdminEmail.Contains("@")) { AdminEmailError = true; return; }
            if (string.IsNullOrEmpty(AdminPassword))
            {
                AdminErrorMessage = "Veuillez entrer votre mot de passe.";
                return;
            }

            AdminIsLoading = true;
            var (success, message) = await AuthService.LoginAsync(new LoginRequest
            {
                Email    = AdminEmail,
                Password = AdminPassword,
                Role     = "Admin"
            });
            AdminIsLoading = false;

            if (success) { await CloseModal(); Navigation.NavigateTo("/admin/projets"); }
            else AdminErrorMessage = message;
        }

        // ── Face login ──
        private async Task HandleFaceLogin()
        {
            if (!FaceDetected) { FaceErrorMessage = "Aucun visage détecté. Regardez la caméra."; return; }
            if (!FaceEmail.Contains("@")) { FaceErrorMessage = "Email invalide."; return; }

            FaceIsLoading    = true;
            FaceErrorMessage = string.Empty;

            var keypointsJson = await JS.InvokeAsync<string?>("captureKeypoints", "face-video");

            if (string.IsNullOrEmpty(keypointsJson))
            {
                FaceIsLoading    = false;
                FaceErrorMessage = "Capture échouée. Restez face à la caméra.";
                return;
            }

            float[][]? keypoints;
            try { keypoints = System.Text.Json.JsonSerializer.Deserialize<float[][]>(keypointsJson); }
            catch { FaceIsLoading = false; FaceErrorMessage = "Erreur de lecture des keypoints."; return; }

            if (keypoints == null) { FaceIsLoading = false; FaceErrorMessage = "Keypoints invalides."; return; }

            var (success, message) = await FaceAuthService.FaceLoginAsync(new FaceLoginRequest
            {
                Email      = FaceEmail,
                Keypoints  = keypoints
            });

            FaceIsLoading = false;

            if (success) { await CloseModal(); Navigation.NavigateTo("/admin/projets"); }
            else FaceErrorMessage = message;
        }

        // ── Navigation rôles ──
        private void SelectRole(string role) => Navigation.NavigateTo($"/login?role={role}");

        public async ValueTask DisposeAsync()
        {
            if (CameraStarted)
                await JS.InvokeVoidAsync("stopCamera", "face-video");
            _dotnetRef?.Dispose();
        }
    }
}