using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AssetFlow.Infrastructure.Services
{
    public class KeycloakAuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _config;

        private string KeycloakUrl => _config["Keycloak:Authority"]!;
        private string ClientId => _config["Keycloak:ClientId"]!;
        private string ClientSecret => _config["Keycloak:ClientSecret"]!;

        public KeycloakAuthService(HttpClient httpClient, AppDbContext dbContext, IConfiguration config)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _config = config;
        }

        // Login : appelle Keycloak avec email+password, récupère le token JWT
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var tokenUrl = $"{KeycloakUrl}/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "username", request.Email },
                { "password", request.Password },
                { "scope", "openid profile email roles" }
            };

            var response = await _httpClient.PostAsync(
                tokenUrl,
                new FormUrlEncodedContent(formData)
            );

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (keycloakResponse == null) return null;

            // ===== RÉCUPÉRER L'UTILISATEUR DEPUIS SQL SERVER =====
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Si l'utilisateur n'existe pas en base (ne devrait pas arriver)
            if (user == null)
                return null;
            // ===== VÉRIFIER LE RÔLE =====
            if (!string.Equals(user.Role, request.Role, StringComparison.OrdinalIgnoreCase))
                return null; // Rôle incorrect → login refusé

            return new LoginResponseDto
            {
                UserId = user.Id,  // ← ID DE L'UTILISATEUR
                AccessToken = keycloakResponse.access_token,
                RefreshToken = keycloakResponse.refresh_token,
                ExpiresIn = keycloakResponse.expires_in,
                Role = request.Role,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email
            };
        }
        // Register : crée l'utilisateur dans Keycloak + SQL Server
        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var exists = await _dbContext.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
                return new RegisterResponseDto { Success = false, Message = "Cet email existe déjà." };

            var adminToken = await GetAdminTokenAsync();
            if (adminToken == null)
                return new RegisterResponseDto { Success = false, Message = "Erreur connexion Keycloak admin." };

            var createUserUrl = $"http://localhost:8080/admin/realms/assetflow/users";

            var keycloakUser = new
            {
                username = request.Email,
                email = request.Email,
                firstName = request.FirstName,
                lastName = request.LastName,
                enabled = true,
                credentials = new[]
                {
                    new { type = "password", value = request.Password, temporary = false }
                }
            };

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var createResponse = await _httpClient.PostAsync(
                createUserUrl,
                new StringContent(JsonSerializer.Serialize(keycloakUser), Encoding.UTF8, "application/json")
            );

            if (!createResponse.IsSuccessStatusCode)
                return new RegisterResponseDto { Success = false, Message = "Erreur création compte Keycloak." };
            // Après création réussie, récupérer l'ID Keycloak du user
            var getUsersUrl = $"http://localhost:8080/admin/realms/assetflow/users?username={Uri.EscapeDataString(request.Email)}";
            var getUsersResp = await _httpClient.GetAsync(getUsersUrl);
            var usersJson = await getUsersResp.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<KeycloakUserInfo>>(usersJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var keycloakUserId = users?.FirstOrDefault()?.Id;

            if (keycloakUserId != null)
            {
                // Récupérer l'ID du rôle
                var roleUrl = $"http://localhost:8080/admin/realms/assetflow/roles/{request.RequestedRole}";
                var roleResp = await _httpClient.GetAsync(roleUrl);
                if (roleResp.IsSuccessStatusCode)
                {
                    var roleJson = await roleResp.Content.ReadAsStringAsync();
                    var role = JsonSerializer.Deserialize<KeycloakRoleInfo>(roleJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (role != null)
                    {
                        // Assigner le rôle au user
                        var assignUrl = $"http://localhost:8080/admin/realms/assetflow/users/{keycloakUserId}/role-mappings/realm";
                        var rolesPayload = JsonSerializer.Serialize(new[] { new { id = role.Id, name = role.Name } });
                        await _httpClient.PostAsync(assignUrl,
                            new StringContent(rolesPayload, Encoding.UTF8, "application/json"));
                    }
                }
            }

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Department = request.Department,
                Role = request.RequestedRole,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return new RegisterResponseDto
            {
                Success = true,
                Message = "Compte créé. En attente d'approbation par l'administrateur."
            };
        }

        private async Task<string?> GetAdminTokenAsync()
        {
            var tokenUrl = "http://localhost:8080/realms/master/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                { "grant_type", "password"    },
                { "client_id",  "admin-cli"   },
                { "username",   "amine" },
                { "password",   "Password123" }
            };

            using var freshClient = new HttpClient();
            var response = await freshClient.PostAsync(tokenUrl, new FormUrlEncodedContent(formData));
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return tokenResponse?.access_token;
        }

        private class KeycloakTokenResponse
        {
            public string access_token { get; set; } = string.Empty;
            public string refresh_token { get; set; } = string.Empty;
            public int expires_in { get; set; }
        }
        // Classes helper
        private class KeycloakUserInfo { public string Id { get; set; } = string.Empty; }
        private class KeycloakRoleInfo  { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
        }
}