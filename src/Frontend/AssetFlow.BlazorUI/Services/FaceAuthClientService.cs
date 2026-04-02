using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace AssetFlow.BlazorUI.Services
{
    public class FaceLoginRequest
    {
        public string Email      { get; set; } = string.Empty;
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }

    public class RegisterFaceRequest
    {
        public string Email      { get; set; } = string.Empty;
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }

    public class FaceAuthClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public FaceAuthClientService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient   = httpClient;
            _localStorage = localStorage;
        }

        // Login par reconnaissance faciale — stocke le token si succès
        public async Task<(bool Success, string Message)> FaceLoginAsync(FaceLoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/face-auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result != null)
                    {
                        await _localStorage.SetItemAsync("user_id",          result.UserId);
                        await _localStorage.SetItemAsync("access_token",     result.AccessToken);
                        await _localStorage.SetItemAsync("refresh_token",    result.RefreshToken);
                        await _localStorage.SetItemAsync("token_expires_at", DateTime.UtcNow.AddSeconds(result.ExpiresIn).ToString("o"));
                        await _localStorage.SetItemAsync("user_role",        result.Role);
                        await _localStorage.SetItemAsync("user_name",        result.FullName);
                        await _localStorage.SetItemAsync("user_email",        result.Email);
                        return (true, "Connexion réussie");
                    }
                }

                return (false, "Visage non reconnu. Réessayez ou utilisez le login classique.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur réseau: {ex.Message}");
            }
        }

        // Enregistre le visage pour un utilisateur existant
        public async Task<(bool Success, string Message)> RegisterFaceAsync(RegisterFaceRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/face-auth/register", request);
                if (response.IsSuccessStatusCode)
                    return (true, "Visage enregistré avec succès !");
                return (false, "Erreur lors de l'enregistrement.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur réseau: {ex.Message}");
            }
        }
    }
}