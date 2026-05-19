// src/Frontend/AssetFlow.BlazorUI/Services/ScrapingBackgroundService.cs
using Microsoft.AspNetCore.SignalR.Client;
using AssetFlow.BlazorUI.DTOs;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services;

public class ScrapingBackgroundService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly HttpClient _http;
    public string? GroupId  { get; private set; }
    public string? UserId   { get; private set; }  // ← NOUVEAU

    public bool EnCours { get; private set; } = false;
    public bool ResultatsPrets => DernierResultat != null && !EnCours;
    public string? QueryEnCours { get; private set; }
    public ScrapingResultatDto? DernierResultat { get; private set; }

    public event Action? OnChanged;
    public event Action<ScrapingResultatDto>? OnTermine;

    public ScrapingBackgroundService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("ApiClient");
    }

    public async Task InitAsync(string token, string userId)  // ← userId en paramètre
    {
        if (_hub != null) return;

        GroupId = Guid.NewGuid().ToString("N");
        UserId  = userId;  // ← stocker

        var hubUrl = _http.BaseAddress!.ToString().TrimEnd('/') + "/scrapinghub";

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, opts =>
                opts.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<ScrapingNotificationDto>("ScrapingTermine", notif =>
        {
            EnCours      = false;
            QueryEnCours = null;

            DernierResultat = notif.Succes && !string.IsNullOrEmpty(notif.JsonResultat)
                ? new ScrapingResultatDto
                {
                    Succes          = true,
                    Query           = notif.Query,
                    NombreResultats = notif.NombreResultats,
                    JsonResultat    = notif.JsonResultat
                }
                : new ScrapingResultatDto
                {
                    Succes = false,
                    Query  = notif.Query,
                    Erreur = notif.Erreur
                };

            OnChanged?.Invoke();
            OnTermine?.Invoke(DernierResultat);
        });

        await _hub.StartAsync();
        await _hub.InvokeAsync("Subscribe", GroupId);
    }

    public async Task LancerAsync(string query)
    {
        if (EnCours) return;
        EnCours         = true;
        QueryEnCours    = query;
        DernierResultat = null;
        OnChanged?.Invoke();

        await _http.PostAsJsonAsync("api/scraping/lancer", new
        {
            Query   = query,
            GroupId = GroupId,
            UserId  = UserId   // ← envoyer au backend
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