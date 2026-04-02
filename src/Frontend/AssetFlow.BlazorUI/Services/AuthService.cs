using System.Net.Http.Json;
using Blazored.LocalStorage;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    // Service d'authentification côté Blazor
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient   = httpClient;
            _localStorage = localStorage;
        }

        // Appelle POST api/auth/login et stocke les infos dans localStorage
        public async Task<(bool Success, string Message)> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result != null)
                    {
                        // ===== STOCKAGE DANS LOCALSTORAGE =====
                        await _localStorage.SetItemAsync("user_id",          result.UserId);
                        await _localStorage.SetItemAsync("access_token",     result.AccessToken);
                        await _localStorage.SetItemAsync("refresh_token",    result.RefreshToken);  // ← pour refresh auto
                        await _localStorage.SetItemAsync("token_expires_at", DateTime.UtcNow.AddSeconds(result.ExpiresIn).ToString("o")); // ← date expiration
                        await _localStorage.SetItemAsync("user_role",        result.Role);
                        await _localStorage.SetItemAsync("user_name",        result.FullName);
                        await _localStorage.SetItemAsync("user_email",        result.Email);

                        return (true, "Connexion réussie");
                    }
                }

                return (false, "Email ou mot de passe incorrect.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur réseau: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
                var result   = await response.Content.ReadFromJsonAsync<RegisterResponse>();

                if (result != null)
                    return (result.Success, result.Message);

                return (false, "Erreur inconnue.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur réseau: {ex.Message}");
            }
        }

        // Déconnexion : supprime toutes les clés du localStorage
        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("user_id");
            await _localStorage.RemoveItemAsync("access_token");
            await _localStorage.RemoveItemAsync("refresh_token");
            await _localStorage.RemoveItemAsync("token_expires_at");
            await _localStorage.RemoveItemAsync("user_role");
            await _localStorage.RemoveItemAsync("user_name");
            await _localStorage.RemoveItemAsync("user_email");
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("access_token");
            return !string.IsNullOrEmpty(token);
        }

        public async Task<string> GetUserRoleAsync()
        {
            return await _localStorage.GetItemAsync<string>("user_role") ?? string.Empty;
        }

        public async Task<int?> GetUserIdAsync()
        {
            return await _localStorage.GetItemAsync<int?>("user_id");
        }

        public async Task<string> GetUserNameAsync()
        {
            return await _localStorage.GetItemAsync<string>("user_name") ?? "Utilisateur";
        }
    }
}