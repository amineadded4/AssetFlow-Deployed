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
        private readonly IAuditLogService _audit;
        private readonly IDashboardNotifier _notifier;

        // ── Clés de configuration ────────────────────────────────────────────
        private string KeycloakUrl    => _config["Keycloak:Authority"]!;
        private string ClientId       => _config["Keycloak:ClientId"]!;
        private string ClientSecret   => _config["Keycloak:ClientSecret"]!;

        private string KeycloakBase   => _config["Keycloak:BaseUrl"]!;

        private string Realm          => _config["Keycloak:Realm"]!;

        private string AdminUsername  => _config["Keycloak:AdminUsername"]!;
        private string AdminPassword  => _config["Keycloak:AdminPassword"]!;

        public KeycloakAuthService(
            HttpClient httpClient,
            AppDbContext dbContext,
            IConfiguration config,
            IAuditLogService audit,
            IDashboardNotifier notifier)
        {
            _httpClient = httpClient;
            _dbContext  = dbContext;
            _config     = config;
            _audit      = audit;
            _notifier   = notifier;
        }

        // ── LOGIN ────────────────────────────────────────────────────────────
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var tokenUrl = $"{KeycloakUrl}/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                { "grant_type",    "password" },
                { "client_id",     ClientId   },
                { "client_secret", ClientSecret },
                { "username",      request.Email },
                { "password",      request.Password },
                { "scope",         "openid profile email roles" }
            };

            var response = await _httpClient.PostAsync(
                tokenUrl,
                new FormUrlEncodedContent(formData));

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (keycloakResponse == null) return null;

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return null;

            if (!string.Equals(user.Role, request.Role, StringComparison.OrdinalIgnoreCase))
                return null;

            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = $"{user.FirstName} {user.LastName}",
                Email       = user.Email,
                Action      = IAuditLogService.Actions.Connexion,
                Categorie   = IAuditLogService.Categories.Inscription,
                Entite      = "Session Utilisateur",
                Details     = "Authentification réussie via Keycloak SSO",
                UserId      = user.Id
            });

            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();

            return new LoginResponseDto
            {
                UserId       = user.Id,
                AccessToken  = keycloakResponse.access_token,
                RefreshToken = keycloakResponse.refresh_token,
                ExpiresIn    = keycloakResponse.expires_in,
                Role         = request.Role,
                FullName     = $"{user.FirstName} {user.LastName}",
                Email        = user.Email
            };
        }

        // ── REGISTER ─────────────────────────────────────────────────────────
        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var exists = await _dbContext.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
                return new RegisterResponseDto { Success = false, Message = "Cet email existe déjà." };

            var adminToken = await GetAdminTokenAsync();
            if (adminToken == null)
                return new RegisterResponseDto { Success = false, Message = "Erreur connexion Keycloak admin." };

            // ── Création de l'utilisateur dans Keycloak ──────────────────────
            var createUserUrl = $"{KeycloakBase}/admin/realms/{Realm}/users";

            var keycloakUser = new
            {
                username    = request.Email,
                email       = request.Email,
                firstName   = request.FirstName,
                lastName    = request.LastName,
                enabled     = true,
                credentials = new[]
                {
                    new { type = "password", value = request.Password, temporary = false }
                }
            };

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var createResponse = await _httpClient.PostAsync(
                createUserUrl,
                new StringContent(JsonSerializer.Serialize(keycloakUser), Encoding.UTF8, "application/json"));

            if (!createResponse.IsSuccessStatusCode)
                return new RegisterResponseDto { Success = false, Message = "Erreur création compte Keycloak." };

            // ── Récupération de l'ID Keycloak du nouvel utilisateur ──────────
            var getUsersUrl = $"{KeycloakBase}/admin/realms/{Realm}/users?username={Uri.EscapeDataString(request.Email)}";
            var getUsersResp = await _httpClient.GetAsync(getUsersUrl);
            var usersJson    = await getUsersResp.Content.ReadAsStringAsync();
            var users        = JsonSerializer.Deserialize<List<KeycloakUserInfo>>(
                usersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var keycloakUserId = users?.FirstOrDefault()?.Id;

            if (keycloakUserId != null)
            {
                // ── Récupération et assignation du rôle ──────────────────────
                var roleUrl  = $"{KeycloakBase}/admin/realms/{Realm}/roles/{request.RequestedRole}";
                var roleResp = await _httpClient.GetAsync(roleUrl);

                if (roleResp.IsSuccessStatusCode)
                {
                    var roleJson = await roleResp.Content.ReadAsStringAsync();
                    var role     = JsonSerializer.Deserialize<KeycloakRoleInfo>(
                        roleJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (role != null)
                    {
                        var assignUrl    = $"{KeycloakBase}/admin/realms/{Realm}/users/{keycloakUserId}/role-mappings/realm";
                        var rolesPayload = JsonSerializer.Serialize(new[] { new { id = role.Id, name = role.Name } });
                        await _httpClient.PostAsync(
                            assignUrl,
                            new StringContent(rolesPayload, Encoding.UTF8, "application/json"));
                    }
                }
            }

            // ── Persistance en base locale ───────────────────────────────────
            var user = new User
            {
                FirstName  = request.FirstName,
                LastName   = request.LastName,
                Email      = request.Email,
                Department = request.Department,
                Role       = request.RequestedRole,
                IsApproved = false,
                CreatedAt  = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();

            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = $"{user.FirstName} {user.LastName}",
                Email       = user.Email,
                Action      = IAuditLogService.Actions.Inscription,
                Categorie   = IAuditLogService.Categories.Inscription,
                Entite      = "Session Utilisateur",
                Details     = $"Nouvel utilisateur enregistré : rôle {user.Role}",
                UserId      = user.Id
            });

            return new RegisterResponseDto
            {
                Success = true,
                Message = "Compte créé. En attente d'approbation par l'administrateur."
            };
        }
        // KeycloakAuthService.cs — ajouter RefreshAsync
        public async Task<LoginResponseDto?> RefreshAsync(string refreshToken)
        {
            var tokenUrl = $"{KeycloakUrl}/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                { "grant_type",    "refresh_token" },
                { "client_id",     ClientId        },
                { "client_secret", ClientSecret    },  // ← reste côté backend
                { "refresh_token", refreshToken    }
            };

            var response = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(formData));
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (keycloakResponse == null) return null;

            return new LoginResponseDto
            {
                AccessToken  = keycloakResponse.access_token,
                RefreshToken = keycloakResponse.refresh_token,
                ExpiresIn    = keycloakResponse.expires_in
            };
        }

        // ── ADMIN TOKEN ──────────────────────────────────────────────────────
        private async Task<string?> GetAdminTokenAsync()
        {
            // Le realm "master" est toujours utilisé pour l'admin Keycloak
            var tokenUrl = $"{KeycloakBase}/realms/master/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                { "grant_type", "password"     },
                { "client_id",  "admin-cli"    },
                { "username",   AdminUsername  },
                { "password",   AdminPassword  }
            };

            using var freshClient = new HttpClient();
            var response = await freshClient.PostAsync(tokenUrl, new FormUrlEncodedContent(formData));
            if (!response.IsSuccessStatusCode) return null;

            var json          = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return tokenResponse?.access_token;
        }
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return true; // Ne pas révéler si l'email existe

            var token = new Random().Next(100000, 999999).ToString();
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _dbContext.SaveChangesAsync();

            // ← Plus besoin du bloc Keycloak, on envoie directement
            await SendResetEmailAsync(email, token, user.FirstName);
            return true;
        }

        private async Task SendResetEmailAsync(string email, string token, string firstName)
        {
            var apiKey   = _config["Brevo:ApiKey"]!;
            var from     = _config["Brevo:From"]!;
            var fromName = _config["Brevo:FromName"] ?? "AssetFlow";

            var payload = new
            {
                sender = new { name = fromName, email = from },
                to = new[] { new { email = email, name = firstName } },
                subject = "AssetFlow — Code de réinitialisation",
                htmlContent = $@"<div style='font-family:sans-serif;max-width:480px;margin:auto'>
                    <h2 style='color:#0d1b35'>Réinitialisation de mot de passe</h2>
                    <p>Bonjour {firstName},</p>
                    <p>Votre code de réinitialisation est :</p>
                    <div style='font-size:2.5rem;font-weight:700;letter-spacing:0.5rem;
                                color:#136dec;text-align:center;padding:1rem;
                                background:#f0f4ff;border-radius:12px;margin:1rem 0'>{token}</div>
                    <p style='color:#666'>Ce code expire dans <strong>15 minutes</strong>.</p>
                    <p style='color:#999;font-size:0.8rem'>Si vous n'avez pas demandé cette réinitialisation, ignorez cet email.</p>
                </div>"
            };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            var response = await httpClient.PostAsync(
                "https://api.brevo.com/v3/smtp/email",
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Brevo error: {error}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return (false, "Email introuvable.");

            if (user.PasswordResetToken != request.Token)
                return (false, "Code incorrect.");

            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return (false, "Code expiré. Veuillez recommencer.");

            // Changer le mot de passe dans Keycloak
            var adminToken = await GetAdminTokenAsync();
            if (adminToken == null) return (false, "Erreur serveur.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var getUserUrl = $"{KeycloakBase}/admin/realms/{Realm}/users?email={Uri.EscapeDataString(request.Email)}";
            var resp = await _httpClient.GetAsync(getUserUrl);
            var json = await resp.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<KeycloakUserInfo>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var keycloakUserId = users?.FirstOrDefault()?.Id;
            if (keycloakUserId == null) return (false, "Utilisateur introuvable.");

            var resetUrl = $"{KeycloakBase}/admin/realms/{Realm}/users/{keycloakUserId}/reset-password";
            var payload  = JsonSerializer.Serialize(new { type = "password", value = request.NewPassword, temporary = false });
            var resetResp = await _httpClient.PutAsync(resetUrl,
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (!resetResp.IsSuccessStatusCode)
                return (false, "Erreur lors du changement de mot de passe.");

            // Nettoyer le token
            user.PasswordResetToken       = null;
            user.PasswordResetTokenExpiry = null;
            await _dbContext.SaveChangesAsync();

            return (true, "Mot de passe modifié avec succès.");
        }

        // ── DTOs internes ────────────────────────────────────────────────────
        private class KeycloakTokenResponse
        {
            public string access_token  { get; set; } = string.Empty;
            public string refresh_token { get; set; } = string.Empty;
            public int    expires_in    { get; set; }
        }

        private class KeycloakUserInfo { public string Id   { get; set; } = string.Empty; }
        private class KeycloakRoleInfo  { public string Id   { get; set; } = string.Empty;
                                          public string Name { get; set; } = string.Empty; }
    }
}