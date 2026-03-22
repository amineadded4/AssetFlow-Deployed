// ============================================================
// AdminFaceSetup.razor.cs
// ============================================================

using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace AssetFlow.BlazorUI.Pages.Admin
{
    public partial class AdminFaceSetup : IAsyncDisposable
    {
        [Inject] private FaceAuthClientService FaceAuthService { get; set; } = default!;
        [Inject] private AuthService           AuthService     { get; set; } = default!;
        [Inject] private IJSRuntime            JS              { get; set; } = default!;

        // ── État global ──
        private int    Step         { get; set; } = 1;
        private string ErrorMessage { get; set; } = string.Empty;

        // ── Caméra / MediaPipe ──
        private bool   FaceDetected  { get; set; } = false;
        private bool   MediaPipeReady{ get; set; } = false;
        private bool   CameraStarted { get; set; } = false;
        private bool   IsCapturing   { get; set; } = false;
        private DotNetObjectReference<AdminFaceSetup>? _dotnetRef;

        // ── Captures (on moyenne plusieurs frames pour plus de robustesse) ──
        private const int MaxCaptures = 5;
        private int CaptureCount { get; set; } = 0;
        private List<float[][]> AllCaptures { get; set; } = new();

        // ── Sauvegarde ──
        private bool IsSaving    { get; set; } = false;
        private bool SaveSuccess { get; set; } = false;
        // ── Modèle position ──
        private record FacePosition(string Label, string Instruction, string SvgContent);
        
        // ── Les 5 positions ──
        private FacePosition[] Positions => new[]
        {
            new FacePosition(
                Label: "Face",
                Instruction: "Regardez droit vers la caméra",
                SvgContent: @"
                    <ellipse cx='30' cy='34' rx='18' ry='22' stroke='currentColor' stroke-width='1.5'/>
                    <ellipse cx='22' cy='27' rx='4' ry='2.5' stroke='currentColor' stroke-width='1.2'/>
                    <ellipse cx='38' cy='27' rx='4' ry='2.5' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M30 33 L27 41 Q30 43 33 41 Z' stroke='currentColor' stroke-width='1'/>
                    <path d='M23 47 Q30 52 37 47' stroke='currentColor' stroke-width='1.2' stroke-linecap='round'/>
                "
            ),
            new FacePosition(
                Label: "Gauche",
                Instruction: "Tournez légèrement la tête vers la gauche",
                SvgContent: @"
                    <ellipse cx='27' cy='34' rx='16' ry='22' stroke='currentColor' stroke-width='1.5'/>
                    <ellipse cx='20' cy='27' rx='3.5' ry='2.5' stroke='currentColor' stroke-width='1.2'/>
                    <ellipse cx='33' cy='26' rx='4.5' ry='2.5' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M27 33 L24 41 Q27 43 30 41 Z' stroke='currentColor' stroke-width='1'/>
                    <path d='M20 47 Q27 51 34 47' stroke='currentColor' stroke-width='1.2' stroke-linecap='round'/>
                    <path d='M44 34 L50 34' stroke='currentColor' stroke-width='1.5' stroke-linecap='round' marker-end='url(#ag)'/>
                    <defs><marker id='ag' viewBox='0 0 6 6' refX='5' refY='3' markerWidth='4' markerHeight='4' orient='auto'><path d='M1 1L5 3L1 5' fill='none' stroke='currentColor' stroke-width='1.2'/></marker></defs>
                "
            ),
            new FacePosition(
                Label: "Droite",
                Instruction: "Tournez légèrement la tête vers la droite",
                SvgContent: @"
                    <ellipse cx='33' cy='34' rx='16' ry='22' stroke='currentColor' stroke-width='1.5'/>
                    <ellipse cx='27' cy='26' rx='4.5' ry='2.5' stroke='currentColor' stroke-width='1.2'/>
                    <ellipse cx='40' cy='27' rx='3.5' ry='2.5' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M33 33 L30 41 Q33 43 36 41 Z' stroke='currentColor' stroke-width='1'/>
                    <path d='M26 47 Q33 51 40 47' stroke='currentColor' stroke-width='1.2' stroke-linecap='round'/>
                    <path d='M16 34 L10 34' stroke='currentColor' stroke-width='1.5' stroke-linecap='round' marker-end='url(#ad)'/>
                    <defs><marker id='ad' viewBox='0 0 6 6' refX='5' refY='3' markerWidth='4' markerHeight='4' orient='auto'><path d='M5 1L1 3L5 5' fill='none' stroke='currentColor' stroke-width='1.2'/></marker></defs>
                "
            ),
            new FacePosition(
                Label: "Haut",
                Instruction: "Levez légèrement le menton vers le haut",
                SvgContent: @"
                    <ellipse cx='30' cy='30' rx='18' ry='20' stroke='currentColor' stroke-width='1.5'/>
                    <ellipse cx='22' cy='24' rx='4' ry='2' stroke='currentColor' stroke-width='1.2'/>
                    <ellipse cx='38' cy='24' rx='4' ry='2' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M30 29 L27 37 Q30 39 33 37 Z' stroke='currentColor' stroke-width='1'/>
                    <path d='M23 43 Q30 48 37 43' stroke='currentColor' stroke-width='1.2' stroke-linecap='round'/>
                    <path d='M30 56 L30 62' stroke='currentColor' stroke-width='1.5' stroke-linecap='round' marker-end='url(#au)'/>
                    <defs><marker id='au' viewBox='0 0 6 6' refX='3' refY='5' markerWidth='4' markerHeight='4' orient='auto'><path d='M1 5L3 1L5 5' fill='none' stroke='currentColor' stroke-width='1.2'/></marker></defs>
                "
            ),
            new FacePosition(
                Label: "Bas",
                Instruction: "Baissez légèrement le menton vers le bas",
                SvgContent: @"
                    <ellipse cx='30' cy='38' rx='18' ry='20' stroke='currentColor' stroke-width='1.5'/>
                    <ellipse cx='22' cy='32' rx='4' ry='2' stroke='currentColor' stroke-width='1.2'/>
                    <ellipse cx='38' cy='32' rx='4' ry='2' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M30 37 L27 45 Q30 47 33 45 Z' stroke='currentColor' stroke-width='1'/>
                    <path d='M23 51 Q30 56 37 51' stroke='currentColor' stroke-width='1.2' stroke-linecap='round'/>
                    <path d='M30 10 L30 16' stroke='currentColor' stroke-width='1.5' stroke-linecap='round' marker-end='url(#ab)'/>
                    <defs><marker id='ab' viewBox='0 0 6 6' refX='3' refY='1' markerWidth='4' markerHeight='4' orient='auto'><path d='M1 1L3 5L5 1' fill='none' stroke='currentColor' stroke-width='1.2'/></marker></defs>
                "
            ),
        };
        
        // ── Position courante ──
        private FacePosition CurrentPosition => Positions[Math.Min(CaptureCount, MaxCaptures - 1)];

        // ────────────────────────────────────────────────────
        // STEP 1 → 2 : démarrer caméra
        // ────────────────────────────────────────────────────
        private async Task GoToScan()
        {
            ErrorMessage = string.Empty;
            Step = 2;
            StateHasChanged();

            // Petite pause pour que le DOM rende le canvas
            await Task.Delay(100);

            if (!MediaPipeReady)
            {
                MediaPipeReady = await JS.InvokeAsync<bool>("initMediaPipe");
                if (!MediaPipeReady)
                {
                    ErrorMessage = "Impossible de charger MediaPipe. Vérifiez votre connexion internet.";
                    Step = 1;
                    StateHasChanged();
                    return;
                }
            }

            if (!CameraStarted)
            {
                CameraStarted = await JS.InvokeAsync<bool>("startCamera", "fs-video");
                if (!CameraStarted)
                {
                    ErrorMessage = "Caméra non disponible. Vérifiez les permissions du navigateur.";
                    Step = 1;
                    StateHasChanged();
                    return;
                }
            }

            _dotnetRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("startPreview", "fs-video", "fs-canvas", _dotnetRef);
        }

        // ────────────────────────────────────────────────────
        // Callback JS → Blazor : détection visage
        // ────────────────────────────────────────────────────
        [JSInvokable]
        public void OnFaceDetected(bool detected)
        {
            if (FaceDetected != detected)
            {
                FaceDetected = detected;
                InvokeAsync(StateHasChanged);
            }
        }

        // ────────────────────────────────────────────────────
        // Capturer un frame
        // ────────────────────────────────────────────────────
        private async Task CaptureFrame()
        {
            if (!FaceDetected || IsCapturing) return;

            IsCapturing  = true;
            ErrorMessage = string.Empty;

            var json = await JS.InvokeAsync<string?>("captureKeypoints", "fs-video");

            if (string.IsNullOrEmpty(json))
            {
                ErrorMessage = "Capture échouée. Restez immobile et bien éclairé.";
                IsCapturing  = false;
                StateHasChanged();
                return;
            }

            try
            {
                var kp = JsonSerializer.Deserialize<float[][]>(json);
                if (kp != null)
                {
                    AllCaptures.Add(kp);
                    CaptureCount++;
                }
            }
            catch
            {
                ErrorMessage = "Erreur lors de la lecture des keypoints.";
            }

            IsCapturing = false;

            // Quand on atteint le max de captures → passer à l'étape 3
            if (CaptureCount >= MaxCaptures)
                await GoToConfirm();

            StateHasChanged();
        }

        // ────────────────────────────────────────────────────
        // STEP 2 → 3
        // ────────────────────────────────────────────────────
        private async Task GoToConfirm()
        {
            // Arrêter la caméra
            await JS.InvokeVoidAsync("stopCamera", "fs-video");
            CameraStarted = false;
            FaceDetected  = false;
            Step = 3;
            StateHasChanged();
        }

        // ────────────────────────────────────────────────────
        // Sauvegarder le visage (moyenne des captures)
        // ────────────────────────────────────────────────────
        private async Task SaveFace()
        {
            IsSaving     = true;
            ErrorMessage = string.Empty;

            var email = await AuthService.GetUserNameAsync(); // récupère le nom stocké
            // On utilise l'email depuis localStorage directement
            var storedEmail = await JS.InvokeAsync<string?>("eval", "localStorage.getItem('user_id')");
            var userEmail   = await GetAdminEmailAsync();

            if (string.IsNullOrEmpty(userEmail))
            {
                ErrorMessage = "Email introuvable. Reconnectez-vous.";
                IsSaving     = false;
                return;
            }

            // Moyenner les captures pour plus de robustesse
            var averaged = AverageKeypoints(AllCaptures);

            var (success, message) = await FaceAuthService.RegisterFaceAsync(new RegisterFaceRequest
            {
                Email     = userEmail,
                Keypoints = averaged
            });

            IsSaving = false;

            if (success)
                SaveSuccess = true;
            else
                ErrorMessage = message;

            StateHasChanged();
        }

        // ────────────────────────────────────────────────────
        // Retour
        // ────────────────────────────────────────────────────
        private async Task GoBack()
        {
            ErrorMessage = string.Empty;
            if (Step == 2)
            {
                await JS.InvokeVoidAsync("stopCamera", "fs-video");
                CameraStarted = false;
                FaceDetected  = false;
            }
            Step--;
            StateHasChanged();
        }

        // ────────────────────────────────────────────────────
        // Reset complet
        // ────────────────────────────────────────────────────
        private async Task Reset()
        {
            if (CameraStarted)
            {
                await JS.InvokeVoidAsync("stopCamera", "fs-video");
                CameraStarted = false;
            }
            Step         = 1;
            CaptureCount = 0;
            AllCaptures  = new();
            SaveSuccess  = false;
            FaceDetected = false;
            ErrorMessage = string.Empty;
            StateHasChanged();
        }

        // ────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────
        private async Task<string?> GetAdminEmailAsync()
        {
            try
            {
                var email = await JS.InvokeAsync<string?>(
                    "eval", "localStorage.getItem('user_email')");
                if (!string.IsNullOrEmpty(email))
                    return email.Trim('"', '\'', ' ');
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Moyenne des N captures pour obtenir un profil facial stable
        /// </summary>
        private static float[][] AverageKeypoints(List<float[][]> captures)
        {
            if (captures.Count == 0) return Array.Empty<float[]>();

            int points = captures[0].Length;
            int dims   = captures[0][0].Length;
            var result = new float[points][];

            for (int p = 0; p < points; p++)
            {
                result[p] = new float[dims];
                for (int d = 0; d < dims; d++)
                {
                    result[p][d] = captures.Average(c => c[p][d]);
                }
            }
            return result;
        }

        public async ValueTask DisposeAsync()
        {
            if (CameraStarted)
                await JS.InvokeVoidAsync("stopCamera", "fs-video");
            _dotnetRef?.Dispose();
        }
    }
}