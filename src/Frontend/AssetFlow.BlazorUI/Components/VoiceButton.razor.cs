using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Components
{
    public partial class VoiceButton : ComponentBase, IAsyncDisposable
    {
        [Inject] private VoiceCommandService VoiceSvc { get; set; } = default!;
        [Inject] private NavigationManager   Nav      { get; set; } = default!;
        [Inject] private IJSRuntime          JS       { get; set; } = default!;

        private bool   _listening  = false;
        private string _transcript = string.Empty;
        private string _feedback   = string.Empty;
        private bool   _isError    = false;
        private DotNetObjectReference<VoiceButton>? _dotNetRef;

        protected override void OnInitialized()
        {
            VoiceSvc.OnListeningChanged += OnListeningChanged;
            VoiceSvc.OnTranscript       += OnTranscript;
            VoiceSvc.OnCommand          += HandleNavigation;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            // Charger le rôle depuis localStorage et l'injecter dans le service
            try
            {
                var role = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_role')");
                if (!string.IsNullOrWhiteSpace(role))
                    VoiceSvc.SetRole(role);
            }
            catch { }

            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("VoiceAssistant.init", _dotNetRef);
        }

        private async Task ToggleListen()
        {
            if (_listening)
            {
                await JS.InvokeVoidAsync("VoiceAssistant.stop");
                _listening = false;
                VoiceSvc.SetListening(false);
            }
            else
            {
                // Rafraîchir le rôle à chaque écoute (au cas où il change)
                try
                {
                    var role = await JS.InvokeAsync<string?>("eval",
                        "localStorage.getItem('user_role')");
                    if (!string.IsNullOrWhiteSpace(role))
                        VoiceSvc.SetRole(role);
                }
                catch { }

                _transcript = string.Empty;
                _feedback   = string.Empty;
                _listening  = true;
                VoiceSvc.SetListening(true);
                await JS.InvokeVoidAsync("VoiceAssistant.start");
            }
            StateHasChanged();
        }

        [JSInvokable("OnResult")]
        public async Task OnResult(string transcript)
        {
            _transcript = transcript;
            _listening  = false;
            VoiceSvc.SetListening(false);
            VoiceSvc.NotifyTranscript(transcript);
            await VoiceSvc.ProcessCommand(transcript);
            StateHasChanged();

            await Task.Delay(3000);
            _transcript = string.Empty;
            StateHasChanged();
        }

        [JSInvokable("OnError")]
        public async Task OnError(string error)
        {
            _listening = false;
            VoiceSvc.SetListening(false);
            await ShowFeedback("Erreur : " + error, true);
            StateHasChanged();
        }

        [JSInvokable("OnFeedback")]
        public async Task OnFeedback(string msg) => await ShowFeedback(msg, false);

        // Gestion navigation globale
        private Task HandleNavigation(VoiceCommand cmd)
        {
            if (cmd.Type != VoiceCommandType.Navigation || cmd.NavigateTo == null)
                return Task.CompletedTask;

            return InvokeAsync(() =>
            {
                Nav.NavigateTo(cmd.NavigateTo);
                StateHasChanged();
            });
        }

        private void OnListeningChanged(bool v)
        {
            _listening = v;
            InvokeAsync(StateHasChanged);
        }

        private void OnTranscript(string t)
        {
            _transcript = t;
            InvokeAsync(StateHasChanged);
        }

        private async Task ShowFeedback(string msg, bool isError)
        {
            _feedback = msg; _isError = isError;
            await InvokeAsync(StateHasChanged);
            await Task.Delay(3500);
            _feedback = string.Empty;
            await InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync()
        {
            VoiceSvc.OnListeningChanged -= OnListeningChanged;
            VoiceSvc.OnTranscript       -= OnTranscript;
            VoiceSvc.OnCommand          -= HandleNavigation;
            try { await JS.InvokeVoidAsync("VoiceAssistant.stop"); } catch { }
            _dotNetRef?.Dispose();
        }
    }
}