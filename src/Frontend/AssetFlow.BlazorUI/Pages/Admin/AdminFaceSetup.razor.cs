using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using AssetFlow.BlazorUI.DTOs;

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
                    <ellipse cx='30' cy='34' rx='18' ry='22' fill='none' stroke='currentColor' stroke-width='1.8'/>
                    <ellipse cx='22' cy='27' rx='4' ry='3' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <ellipse cx='38' cy='27' rx='4' ry='3' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <path d='M26 33 L24 40 Q30 44 36 40 L34 33 Z' fill='none' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M23 47 Q30 52 37 47' fill='none' stroke='currentColor' stroke-width='1.4' stroke-linecap='round'/>
                "
            ),
            new FacePosition(
                Label: "Gauche",
                Instruction: "Tournez légèrement la tête vers la gauche",
                SvgContent: @"
                    <ellipse cx='28' cy='34' rx='17' ry='22' fill='none' stroke='currentColor' stroke-width='1.8'/>
                    <ellipse cx='20' cy='27' rx='3.5' ry='3' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <ellipse cx='36' cy='27' rx='4.5' ry='3' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <path d='M24 33 L22 40 Q28 44 34 40 L32 33 Z' fill='none' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M21 47 Q28 52 35 47' fill='none' stroke='currentColor' stroke-width='1.4' stroke-linecap='round'/>
                    <line x1='52' y1='34' x2='42' y2='34' stroke='currentColor' stroke-width='2.2' stroke-linecap='round'/>
                    <polyline points='46,29 41,34 46,39' fill='none' stroke='currentColor' stroke-width='2.2' stroke-linecap='round' stroke-linejoin='round'/>
                "
            ),
            new FacePosition(
                Label: "Droite",
                Instruction: "Tournez légèrement la tête vers la droite",
                SvgContent: @"
                    <ellipse cx='32' cy='34' rx='17' ry='22' fill='none' stroke='currentColor' stroke-width='1.8'/>
                    <ellipse cx='24' cy='27' rx='4.5' ry='3' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <ellipse cx='40' cy='27' rx='3.5' ry='3' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <path d='M28 33 L26 40 Q32 44 38 40 L36 33 Z' fill='none' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M25 47 Q32 52 39 47' fill='none' stroke='currentColor' stroke-width='1.4' stroke-linecap='round'/>
                    <line x1='8' y1='34' x2='18' y2='34' stroke='currentColor' stroke-width='2.2' stroke-linecap='round'/>
                    <polyline points='14,29 19,34 14,39' fill='none' stroke='currentColor' stroke-width='2.2' stroke-linecap='round' stroke-linejoin='round'/>
                "
            ),
            new FacePosition(
                Label: "Haut",
                Instruction: "Levez légèrement le menton vers le haut",
                SvgContent: @"
                    <ellipse cx='30' cy='36' rx='18' ry='22' fill='none' stroke='currentColor' stroke-width='1.8'/>
                    <ellipse cx='22' cy='29' rx='4' ry='2.5' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <ellipse cx='38' cy='29' rx='4' ry='2.5' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <path d='M26 35 L24 42 Q30 46 36 42 L34 35 Z' fill='none' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M23 50 Q30 55 37 50' fill='none' stroke='currentColor' stroke-width='1.4' stroke-linecap='round'/>
                    <line x1='30' y1='12' x2='30' y2='4' stroke='currentColor' stroke-width='2.2' stroke-linecap='round'/>
                    <polyline points='25,9 30,4 35,9' fill='none' stroke='currentColor' stroke-width='2.2' stroke-linecap='round' stroke-linejoin='round'/>
                "
            ),
            new FacePosition(
                Label: "Bas",
                Instruction: "Baissez légèrement le menton vers le bas",
                SvgContent: @"
                    <ellipse cx='30' cy='30' rx='18' ry='22' fill='none' stroke='currentColor' stroke-width='1.8'/>
                    <ellipse cx='22' cy='23' rx='4' ry='2.5' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <ellipse cx='38' cy='23' rx='4' ry='2.5' fill='none' stroke='currentColor' stroke-width='1.4'/>
                    <path d='M26 29 L24 36 Q30 40 36 36 L34 29 Z' fill='none' stroke='currentColor' stroke-width='1.2'/>
                    <path d='M23 44 Q30 49 37 44' fill='none' stroke='currentColor' stroke-width='1.4' stroke-linecap='round'/>
                    <line x1='30' y1='56' x2='30' y2='64' stroke='currentColor' stroke-width='2.2' stroke-linecap='round'/>
                    <polyline points='25,59 30,64 35,59' fill='none' stroke='currentColor' stroke-width='2.2' stroke-linecap='round' stroke-linejoin='round'/>
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