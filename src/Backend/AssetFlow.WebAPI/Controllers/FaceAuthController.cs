// ============================================================
// AssetFlow.WebAPI / Controllers / FaceAuthController.cs
// POST /api/face-auth/login
// POST /api/face-auth/register
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/face-auth")]
    [AllowAnonymous]
    public class FaceAuthController : ControllerBase
    {
        private readonly IFaceAuthService _faceAuthService;

        public FaceAuthController(IFaceAuthService faceAuthService)
        {
            _faceAuthService = faceAuthService;
        }

        /// <summary>
        /// POST api/face-auth/login
        /// Authentifie par reconnaissance faciale
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> FaceLogin([FromBody] FaceLoginRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || request.Keypoints.Length == 0)
                return BadRequest("Email et keypoints requis.");
            var result = await _faceAuthService.FaceLoginAsync(request);

            if (result == null)
                return Unauthorized("Visage non reconnu ou compte non approuvé.");

            return Ok(result);
        }

        /// <summary>
        /// POST api/face-auth/register
        /// Enregistre le visage d'un utilisateur existant
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterFace([FromBody] RegisterFaceRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || request.Keypoints.Length == 0)
                return BadRequest("Email et keypoints requis.");

            var ok = await _faceAuthService.RegisterFaceAsync(request);

            if (!ok)
                return NotFound("Utilisateur introuvable.");

            return Ok(new { message = "Visage enregistré avec succès." });
        }
    }
}