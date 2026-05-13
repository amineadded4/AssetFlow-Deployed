// src/Backend/AssetFlow.Infrastructure/Services/ScrapingService.cs
using System.Net.Http.Json;
using System.Text.Json;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;

namespace AssetFlow.Infrastructure.Services;

public class ScrapingService : IScrapingService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IRedisScrapingService _redis;
    private readonly IScrapingNotifier _notifier;

    public ScrapingService(
        IHttpClientFactory httpFactory,
        IRedisScrapingService redis,
        IScrapingNotifier notifier)
    {
        _httpFactory = httpFactory;
        _redis = redis;
        _notifier = notifier;
    }

    public async Task LancerScrapingAsync(string query, string groupId)
    {
        try
        {
            var http = _httpFactory.CreateClient("PythonScraper");
            var reponse = await http.GetFromJsonAsync<JsonElement>(
                $"scrape?q={Uri.EscapeDataString(query)}");

            var json = reponse.GetRawText();
            var succes = reponse.TryGetProperty("succes", out var s) && s.GetBoolean();
            var count  = reponse.TryGetProperty("nombre_resultats", out var n) ? n.GetInt32() : 0;

            if (succes)
            {
                try
                {
                    await _redis.SaveResultatAsync(json);
                }
                catch (Exception redisEx)
                {
                    Console.WriteLine($"[Redis] Erreur sauvegarde : {redisEx.Message}");
                    // On continue quand même — Redis non critique ici
                }
            }

            await _notifier.NotifierTermineAsync(groupId, new ScrapingNotificationDto
            {
                Succes = succes,
                Query = query,
                NombreResultats = count,
                JsonResultat = succes ? json : string.Empty
            });
        }
        catch (Exception ex)
        {
            await _notifier.NotifierTermineAsync(groupId, new ScrapingNotificationDto
            {
                Succes = false,
                Query = query,
                Erreur = ex.Message
            });
        }
    }

    public async Task<string?> GetCachedResultAsync(string query)
    {
        return await _redis.GetResultatAsync();
    }
}