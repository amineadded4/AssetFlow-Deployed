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

        // POST api/face-auth/login
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

        // POST api/face-auth/register
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