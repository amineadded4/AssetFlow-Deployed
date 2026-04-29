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
        if (string.IsNullOrWhiteSpace(req.Query) || string.IsNullOrWhiteSpace(req.GroupId))
            return BadRequest("Query et GroupId requis");

        // Fire & Forget — ne bloque pas
        _ = Task.Run(() => _scrapingService.LancerScrapingAsync(req.Query, req.GroupId));

        return Ok(new { message = "Scraping lancé", query = req.Query });
    }

    // Récupérer le cache Redis
    [HttpGet("cache")]
    public async Task<IActionResult> GetCache()
    {
        var json = await _scrapingService.GetCachedResultAsync(string.Empty);
        if (json == null) return NotFound();
        return Content(json, "application/json");
    }
}

public record ScrapingRequest(string Query, string GroupId);