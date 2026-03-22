// ============================================================
// AssetFlow.Infrastructure / Services / FaceAuthService.cs
// Comparaison par distance euclidienne normalisée
// (même algorithme que FaceComparisonService de la version séparée)
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

        // Même seuil que la version séparée qui fonctionne
        private const double SimilarityThreshold = 0.035;

        private string KeycloakUrl  => _config["Keycloak:Authority"]!;
        private string ClientId     => _config["Keycloak:ClientId"]!;
        private string ClientSecret => _config["Keycloak:ClientSecret"]!;
        private string AdminPassword=> _config["Keycloak:AdminPassword"] ?? "Password123";

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

            Console.WriteLine($"[FACE] Email: '{request.Email}', User: {user != null}, Keypoints: {!string.IsNullOrEmpty(user?.FaceKeypoints)}");

            if (user == null || string.IsNullOrEmpty(user.FaceKeypoints))
                return null;

            // 2. Désérialiser les keypoints stockés
            List<double[]>? stored;
            try { stored = JsonSerializer.Deserialize<List<double[]>>(user.FaceKeypoints); }
            catch { return null; }

            if (stored == null || stored.Count == 0) return null;

            // 3. Convertir les keypoints reçus (float[][]) en List<double[]>
            var input = request.Keypoints
                .Select(p => new double[] { p[0], p[1] })
                .ToList();

            if (stored.Count != input.Count)
            {
                Console.WriteLine($"[FACE] Tailles différentes: stored={stored.Count}, input={input.Count}");
                return null;
            }

            // 4. Comparer par distance euclidienne normalisée
            double avgDistance = CompareFaces(stored, input);
            Console.WriteLine($"[FACE] Distance: {avgDistance:F6} (seuil: {SimilarityThreshold})");

            if (avgDistance >= SimilarityThreshold)
            {
                Console.WriteLine("[FACE] Visage non reconnu");
                return null;
            }

            // 5. Obtenir token Keycloak
            var tokenUrl = $"{KeycloakUrl}/protocol/openid-connect/token";
            var formData = new Dictionary<string, string>
            {
                { "grant_type",    "password"   },
                { "client_id",     ClientId     },
                { "client_secret", ClientSecret },
                { "username",      user.Email   },
                { "password",      AdminPassword},
                { "scope",         "openid profile email roles" }
            };

            using var freshClient = new HttpClient();
            var response = await freshClient.PostAsync(
                tokenUrl, new FormUrlEncodedContent(formData));

            Console.WriteLine($"[FACE] Keycloak: {response.StatusCode}");

            if (!response.IsSuccessStatusCode) return null;

            var json   = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<KeycloakTokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null) return null;

            Console.WriteLine($"[FACE] ✅ Login réussi pour {user.Email}");

            return new LoginResponseDto
            {
                UserId       = user.Id,
                AccessToken  = result.access_token,
                RefreshToken = result.refresh_token,
                ExpiresIn    = result.expires_in,
                Role         = user.Role,
                FullName     = $"{user.FirstName} {user.LastName}",
                Email        = user.Email
            };
        }

        public async Task<bool> RegisterFaceAsync(RegisterFaceRequestDto request)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || request.Keypoints.Length == 0) return false;

            // Stocker en List<double[]> pour cohérence avec la comparaison
            var keypoints = request.Keypoints
                .Select(p => new double[] { p[0], p[1] })
                .ToList();

            user.FaceKeypoints = JsonSerializer.Serialize(keypoints);
            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"[FACE REGISTER] ✅ {user.Email} — {keypoints.Count} points sauvegardés");
            return true;
        }

        // ────────────────────────────────────────────────────
        // Distance euclidienne normalisée
        // Identique à FaceComparisonService de la version séparée
        // ────────────────────────────────────────────────────
        private static double CompareFaces(List<double[]> stored, List<double[]> input)
        {
            var normStored = NormalizeKeypoints(stored);
            var normInput  = NormalizeKeypoints(input);

            double total = 0;
            for (int i = 0; i < normStored.Count; i++)
            {
                double dx = normStored[i][0] - normInput[i][0];
                double dy = normStored[i][1] - normInput[i][1];
                total += Math.Sqrt(dx * dx + dy * dy);
            }
            return total / normStored.Count;
        }

        private static List<double[]> NormalizeKeypoints(List<double[]> keypoints)
        {
            // Centroïde
            double cx = keypoints.Average(p => p[0]);
            double cy = keypoints.Average(p => p[1]);

            // Centrer
            var centered = keypoints
                .Select(p => new double[] { p[0] - cx, p[1] - cy })
                .ToList();

            // Échelle (distance max depuis le centroïde)
            double scale = centered.Max(p => Math.Sqrt(p[0] * p[0] + p[1] * p[1]));
            if (scale == 0) scale = 1;

            return centered
                .Select(p => new double[] { p[0] / scale, p[1] / scale })
                .ToList();
        }

        private class KeycloakTokenResponse
        {
            public string access_token  { get; set; } = string.Empty;
            public string refresh_token { get; set; } = string.Empty;
            public int    expires_in    { get; set; }
        }
    }
}