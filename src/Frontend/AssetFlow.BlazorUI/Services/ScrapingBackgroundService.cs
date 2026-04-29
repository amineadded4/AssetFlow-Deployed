// src/Frontend/AssetFlow.BlazorUI/Services/ScrapingBackgroundService.cs
using Microsoft.AspNetCore.SignalR.Client;
using AssetFlow.BlazorUI.DTOs;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services;

public class ScrapingBackgroundService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly HttpClient _http;
    public string? GroupId { get; private set; }

    // État observable
    public bool EnCours { get; private set; } = false;
    public string? QueryEnCours { get; private set; }
    public ScrapingResultatDto? DernierResultat { get; private set; }

    // Events
    public event Action? OnChanged;
    public event Action<ScrapingResultatDto>? OnTermine;

    public ScrapingBackgroundService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("ApiClient");
    }

    public async Task InitAsync(string token)
    {
        // ── Éviter double initialisation
        if (_hub != null) return;

        GroupId = Guid.NewGuid().ToString("N");

        _hub = new HubConnectionBuilder()
            .WithUrl("http://localhost:5235/scrapinghub", opts =>
                opts.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<ScrapingNotificationDto>("ScrapingTermine", notif =>
        {
            EnCours = false;
            QueryEnCours = null;

            if (notif.Succes && !string.IsNullOrEmpty(notif.JsonResultat))
            {
                DernierResultat = new ScrapingResultatDto
                {
                    Succes = true,
                    Query = notif.Query,
                    NombreResultats = notif.NombreResultats,
                    JsonResultat = notif.JsonResultat
                };
            }
            else
            {
                DernierResultat = new ScrapingResultatDto
                {
                    Succes = false,
                    Query = notif.Query,
                    Erreur = notif.Erreur
                };
            }

            OnChanged?.Invoke();
            OnTermine?.Invoke(DernierResultat);
        });

        await _hub.StartAsync();
        await _hub.InvokeAsync("Subscribe", GroupId);
    }

    public async Task LancerAsync(string query)
    {
        if (EnCours) return;
        EnCours = true;
        QueryEnCours = query;
        DernierResultat = null;
        OnChanged?.Invoke();

        await _http.PostAsJsonAsync("api/scraping/lancer", new
        {
            Query = query,
            GroupId = GroupId
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub != null)
        {
            try { await _hub.InvokeAsync("Unsubscribe", GroupId); } catch { }
            await _hub.DisposeAsync();
            _hub = null;
        }
    }
}