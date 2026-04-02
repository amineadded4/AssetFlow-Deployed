using AssetFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

        // GET api/users/it — liste des agents IT
        [HttpGet("it")]
        public async Task<IActionResult> GetITUsers()
        {
            var users = await _db.Users
                .Where(u => u.Role == "IT")
                .OrderBy(u => u.FirstName)
                .Select(u => new
                {
                    Id       = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    Initials = (u.FirstName.Length > 0 ? u.FirstName.Substring(0, 1) : "") +
                               (u.LastName.Length  > 0 ? u.LastName.Substring(0, 1)  : "")
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET api/users/achat — liste des agents du service achat
        [HttpGet("achat")]
        public async Task<IActionResult> GetAchatUsers()
        {
            var users = await _db.Users
                .Where(u => u.Role == "EquipeAchat")
                .OrderBy(u => u.FirstName)
                .Select(u => new
                {
                    Id       = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    Initials = (u.FirstName.Length > 0 ? u.FirstName.Substring(0, 1) : "") +
                               (u.LastName.Length  > 0 ? u.LastName.Substring(0, 1)  : "")
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
