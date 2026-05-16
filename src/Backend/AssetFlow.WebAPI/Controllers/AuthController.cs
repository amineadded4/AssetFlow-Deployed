using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        // Injection du service via l'interface (pas la classe concrète)
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // Validation basique des données reçues
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email et mot de passe requis.");

            var result = await _authService.LoginAsync(request);

            // Si Keycloak n'a pas validé les credentials
            if (result == null)
                return Unauthorized("Email ou mot de passe incorrect.");

            // Retourner le token et les infos utilisateur
            return Ok(result);
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // Validation basique
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Données incomplètes.");

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            // 201 Created avec message de succès
            return Created("", result);
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var result = await _authService.RefreshAsync(request.RefreshToken);
            if (result == null) return Unauthorized();
            
            return Ok(new {
                access_token  = result.AccessToken,
                refresh_token = result.RefreshToken,
                expires_in    = result.ExpiresIn
            });
        }
    }
}