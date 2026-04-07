using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Policy = "ITOrAdmin")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _svc;
        public NotificationsController(INotificationService svc) => _svc = svc;

        /// <summary>GET api/notifications?nonLuesSeulement=false</summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool nonLuesSeulement = false)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var notifications = await _svc.GetNotificationsAsync(role, nonLuesSeulement);
            return Ok(notifications);
        }

        /// <summary>GET api/notifications/count</summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var count = await _svc.GetNombreNonLuesAsync(role);
            return Ok(new { NombreNonLues = count });
        }

        /// <summary>PATCH api/notifications/{id}/lue</summary>
        [HttpPatch("{id:int}/lue")]
        public async Task<IActionResult> MarquerCommeLue(int id)
        {
            await _svc.MarquerCommeLueAsync(id);
            return NoContent();
        }

        /// <summary>PATCH api/notifications/tout-lire</summary>
        [HttpPatch("tout-lire")]
        public async Task<IActionResult> MarquerToutesCommeLues()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            await _svc.MarquerToutesCommeLuesAsync(role);
            return NoContent();
        }

        /// <summary>POST api/notifications/generer — force la génération</summary>
        [HttpPost("generer")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GenererNotifications()
        {
            await _svc.GenererNotificationsAffectationsExpireesAsync();
            return Ok(new { Message = "Notifications générées avec succès." });
        }
    }
}