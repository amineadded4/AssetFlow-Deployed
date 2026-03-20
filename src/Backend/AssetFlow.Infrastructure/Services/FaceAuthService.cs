// ============================================================
// AssetFlow.Infrastructure / Services / FaceAuthService.cs
// Même approche Keycloak que KeycloakAuthService
// ============================================================

using System.Text.Json;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AssetFlow.Infrastructure.Services
{
    public class FaceAuthService : IFaceAuthService
    {
        private readonly AppDbContext   _dbContext;
        private readonly IConfiguration _config;
        private readonly HttpClient     _httpClient;

        private const double SimilarityThreshold = 0.97;

        private string KeycloakUrl   => _config["Keycloak:Authority"]!;
        private string ClientId      => _config["Keycloak:ClientId"]!;
        private string ClientSecret  => _config["Keycloak:ClientSecret"]!;
        private string AdminUsername => _config["Keycloak:AdminUsername"] ?? "amine";
        private string AdminPassword => _config["Keycloak:AdminPassword"] ?? "Password123";

        public FaceAuthService(AppDbContext dbContext, IConfiguration config, HttpClient httpClient)
        {
            _dbContext  = dbContext;
            _config     = config;
            _httpClient = httpClient;
        }

        public async Task<LoginResponseDto?> FaceLoginAsync(FaceLoginRequestDto request)
        {
            // 1. Trouver l'utilisateur
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsApproved);

            Console.WriteLine($"[FACE] Email: '{request.Email}'");
            Console.WriteLine($"[FACE] User trouvé: {user != null}");
            Console.WriteLine($"[FACE] FaceKeypoints null: {string.IsNullOrEmpty(user?.FaceKeypoints)}");

            if (user == null || string.IsNullOrEmpty(user.FaceKeypoints))
                return null;

            // 2. Désérialiser les keypoints stockés
            float[][]? stored;
            try { stored = JsonSerializer.Deserialize<float[][]>(user.FaceKeypoints); }
            catch { return null; }

            if (stored == null || stored.Length == 0) return null;

            // 3. Vérifier la similarité
            var similarity = CosineSimilarity(request.Keypoints, stored);
            Console.WriteLine($"[FACE] Similarité: {similarity:F6} (seuil: {SimilarityThreshold})");

            if (similarity < SimilarityThreshold)
                return null;

            // 4. Obtenir token Keycloak — même méthode que KeycloakAuthService
            //    On utilise les credentials admin (l'utilisateur facial est l'admin)
            var tokenUrl = $"{KeycloakUrl}/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                { "grant_type",    "password"     },
                { "client_id",     ClientId       },
                { "client_secret", ClientSecret   },
                { "username",      user.Email     }, // ← email de l'utilisateur
                { "password",      AdminPassword  }, // ← mot de passe Keycloak de l'admin
                { "scope",         "openid profile email roles" }
            };

            using var freshClient = new HttpClient();
            var response = await freshClient.PostAsync(
                tokenUrl, new FormUrlEncodedContent(formData));

            Console.WriteLine($"[FACE] Keycloak status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json           = await response.Content.ReadAsStringAsync();
            var keycloakResult = JsonSerializer.Deserialize<KeycloakTokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (keycloakResult == null) return null;

            Console.WriteLine($"[FACE] ✅ Login réussi pour {user.Email}");

            return new LoginResponseDto
            {
                UserId       = user.Id,
                AccessToken  = keycloakResult.access_token,
                RefreshToken = keycloakResult.refresh_token,
                ExpiresIn    = keycloakResult.expires_in,
                Role         = user.Role,
                FullName     = $"{user.FirstName} {user.LastName}",
                Email        = user.Email
            };
        }

        public async Task<bool> RegisterFaceAsync(RegisterFaceRequestDto request)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            Console.WriteLine($"[FACE REGISTER] Email: '{request.Email}', Keypoints: {request.Keypoints.Length}");

            if (user == null || request.Keypoints.Length == 0)
                return false;

            user.FaceKeypoints = JsonSerializer.Serialize(request.Keypoints);
            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"[FACE REGISTER] ✅ Sauvegardé");
            return true;
        }

        private static double CosineSimilarity(float[][] a, float[][] b)
        {
            var vecA = a.SelectMany(p => p).ToArray();
            var vecB = b.SelectMany(p => p).ToArray();
            int len  = Math.Min(vecA.Length, vecB.Length);
            if (len == 0) return 0;

            double dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < len; i++)
            {
                dot   += vecA[i] * vecB[i];
                normA += vecA[i] * vecA[i];
                normB += vecB[i] * vecB[i];
            }
            if (normA == 0 || normB == 0) return 0;
            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private class KeycloakTokenResponse
        {
            public string access_token  { get; set; } = string.Empty;
            public string refresh_token { get; set; } = string.Empty;
            public int    expires_in    { get; set; }
        }
    }
}