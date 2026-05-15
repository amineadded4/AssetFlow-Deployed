// src/Backend/AssetFlow.WebAPI/Controllers/ScrapingController.cs
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScrapingController : ControllerBase
{
    private readonly IScrapingService _scrapingService;

    public ScrapingController(IScrapingService scrapingService)
    {
        _scrapingService = scrapingService;
    }

    // Lancement asynchrone — répond immédiatement
    [HttpPost("lancer")]
    public IActionResult LancerScraping([FromBody] ScrapingRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Query) || 
            string.IsNullOrWhiteSpace(req.GroupId) ||
            string.IsNullOrWhiteSpace(req.UserId))
            return BadRequest("Query, GroupId et UserId requis");

        _ = Task.Run(() => _scrapingService.LancerScrapingAsync(req.Query, req.GroupId, req.UserId));

        return Ok(new { message = "Scraping lancé", query = req.Query });
    }

    [HttpGet("cache")]
    public async Task<IActionResult> GetCache([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest("userId requis");
        var json = await _scrapingService.GetCachedResultAsync(userId);
        if (json == null) return NotFound();
        return Content(json, "application/json");
    }

    public record ScrapingRequest(string Query, string GroupId, string UserId);
}