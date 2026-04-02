using Blazored.LocalStorage;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthTokenHandler(ILocalStorageService localStorage, IHttpClientFactory httpClientFactory)
        {
            _localStorage = localStorage;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 1. Vérifier si le token est expiré (ou proche de l'expiration)
            var token = await GetValidTokenAsync();

            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }

        // Retourne un access_token valide.
        // Si expiré (ou expire dans moins de 60s), tente un refresh silencieux.
        private async Task<string?> GetValidTokenAsync()
        {
            var accessToken  = await _localStorage.GetItemAsync<string>("access_token");
            var expiresAtStr = await _localStorage.GetItemAsync<string>("token_expires_at");

            if (string.IsNullOrEmpty(accessToken)) return null;

            // ← Forcer la culture invariante pour le parsing ISO 8601
            if (DateTime.TryParse(expiresAtStr,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiresAt))
            {
                var isExpiringSoon = DateTime.UtcNow >= expiresAt.AddSeconds(-60);
                if (isExpiringSoon)
                {
                    var refreshed = await TryRefreshTokenAsync();
                    if (refreshed != null) return refreshed;

                    await ClearSessionAsync();
                    return null;
                }
            }

            return accessToken;
        }

        // Appelle Keycloak avec le refresh_token pour obtenir un nouveau access_token.
        private async Task<string?> TryRefreshTokenAsync()
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refresh_token");
            if (string.IsNullOrEmpty(refreshToken)) return null;

            try
            {
                // ← Créer un HttpClient RAW, sans passer par le handler
                using var rawClient = new HttpClient();

                var formData = new Dictionary<string, string>
                {
                    { "grant_type",    "refresh_token"                      },
                    { "client_id",     "assetflow-client"                   },
                    { "client_secret", "tU5jkiUUchoZMyr7V5nDYBAlD52PCOod"  },
                    { "refresh_token", refreshToken                         }
                };

                var response = await rawClient.PostAsync(
                    "http://localhost:8080/realms/assetflow/protocol/openid-connect/token",
                    new FormUrlEncodedContent(formData)
                );

                if (!response.IsSuccessStatusCode) return null;

                var json   = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<KeycloakTokenResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null) return null;

                await _localStorage.SetItemAsync("access_token",     result.access_token);
                await _localStorage.SetItemAsync("refresh_token",    result.refresh_token);
                await _localStorage.SetItemAsync("token_expires_at",
                    DateTime.UtcNow.AddSeconds(result.expires_in).ToString("o"));

                return result.access_token;
            }
            catch
            {
                return null;
            }
        }

        private async Task ClearSessionAsync()
        {
            await _localStorage.RemoveItemAsync("access_token");
            await _localStorage.RemoveItemAsync("refresh_token");
            await _localStorage.RemoveItemAsync("token_expires_at");
            await _localStorage.RemoveItemAsync("user_role");
            await _localStorage.RemoveItemAsync("user_id");
        }

        private class KeycloakTokenResponse
        {
            public string access_token  { get; set; } = string.Empty;
            public string refresh_token { get; set; } = string.Empty;
            public int    expires_in    { get; set; }
        }
    }
}