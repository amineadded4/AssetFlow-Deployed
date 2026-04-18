using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AssetFlow.Infrastructure.Services
{
    public class FaceAuthService : IFaceAuthService
    {
        private readonly AppDbContext    _dbContext;
        private readonly IConfiguration  _config;
        private readonly HttpClient      _httpClient;
        private readonly IDashboardNotifier _notifier;

        private const double SimilarityThreshold = 0.035;

        private string KeycloakUrl  => _config["Keycloak:Authority"]!;
        private string JwtSecret    => _config["FaceAuth:JwtSecret"]!;

        public FaceAuthService(AppDbContext dbContext, IConfiguration config, HttpClient httpClient, IDashboardNotifier notifier)
        {
            _dbContext  = dbContext;
            _config     = config;
            _httpClient = httpClient;
            _notifier = notifier;
        }

        public async Task<LoginResponseDto?> FaceLoginAsync(FaceLoginRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email))
                return null;

            // 1. Trouver l'utilisateur
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsApproved);

            Console.WriteLine($"[FACE] Email: '{request.Email}', User: {user != null}");

            if (user == null || string.IsNullOrEmpty(user.FaceKeypoints))
                return null;

            // 2. Désérialiser les keypoints stockés
            List<double[]>? stored;
            try { stored = JsonSerializer.Deserialize<List<double[]>>(user.FaceKeypoints); }
            catch { return null; }

            if (stored == null || stored.Count == 0) return null;

            // 3. Convertir les keypoints reçus
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

            // 5. Générer un JWT custom identique à Keycloak
            var (accessToken, refreshToken, expiresIn) = GenerateFaceAuthTokens(user);

            Console.WriteLine($"[FACE] Login réussi pour {user.Email}");
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();

            return new LoginResponseDto
            {
                UserId       = user.Id,
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn    = expiresIn,
                Role         = user.Role,
                FullName     = $"{user.FirstName} {user.LastName}",
                Email        = user.Email
            };
        }

        // ────────────────────────────────────────────────────
        // Génère un JWT custom avec les mêmes claims que Keycloak
        // ────────────────────────────────────────────────────
        private (string accessToken, string refreshToken, int expiresIn) GenerateFaceAuthTokens(
            AssetFlow.Domain.Entities.User user)
        {
            var secret      = JwtSecret;
            var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresIn  = 3600; // 1 heure comme Keycloak
            var now        = DateTime.UtcNow;
            var expiry     = now.AddSeconds(expiresIn);

            // Claims identiques à ce que Keycloak retourne
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,   DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim("preferred_username",          user.Email),
                new Claim("given_name",                  user.FirstName),
                new Claim("family_name",                 user.LastName),
                new Claim("name",                        $"{user.FirstName} {user.LastName}"),
                // realm_access claim — identique à Keycloak
                new Claim("realm_access",
                    JsonSerializer.Serialize(new { roles = new[] { user.Role } })),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("iss", KeycloakUrl), // même issuer que Keycloak
                new Claim("azp", "assetflow-client"),
            };

            var accessToken = new JwtSecurityToken(
                issuer:             KeycloakUrl,
                audience:           "assetflow-client",
                claims:             claims,
                notBefore:          now,
                expires:            expiry,
                signingCredentials: credentials
            );

            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            // Refresh token — simple token opaque (pas JWT)
            var refreshTokenString = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{user.Email}:{Guid.NewGuid()}:{now.AddDays(1):o}"));

            return (accessTokenString, refreshTokenString, expiresIn);
        }

        public async Task<bool> RegisterFaceAsync(RegisterFaceRequestDto request)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || request.Keypoints.Length == 0) return false;

            var keypoints = request.Keypoints
                .Select(p => new double[] { p[0], p[1] })
                .ToList();

            user.FaceKeypoints = JsonSerializer.Serialize(keypoints);
            await _dbContext.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();

            Console.WriteLine($"[FACE REGISTER] ✅ {user.Email} — {keypoints.Count} points sauvegardés");
            return true;
        }

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
            double cx = keypoints.Average(p => p[0]);
            double cy = keypoints.Average(p => p[1]);

            var centered = keypoints
                .Select(p => new double[] { p[0] - cx, p[1] - cy })
                .ToList();

            double scale = centered.Max(p => Math.Sqrt(p[0] * p[0] + p[1] * p[1]));
            if (scale == 0) scale = 1;

            return centered
                .Select(p => new double[] { p[0] / scale, p[1] / scale })
                .ToList();
        }
    }
}