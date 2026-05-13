using AssetFlow.BlazorUI;
using AssetFlow.BlazorUI.Services;
using AssetFlow.BlazorUI.CircuitBreaker;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;  // ← si besoin
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// === AUTH TOKEN HANDLER ===
builder.Services.AddScoped<AuthTokenHandler>();

// === HTTP CLIENT — API .NET (avec token automatique) ===
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://pleuropneumonic-ferromagnetic-conrad.ngrok-free.dev/");
    client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    client.Timeout = TimeSpan.FromMinutes(2);
})
.AddHttpMessageHandler<AuthTokenHandler>();

// Rendre ce client disponible comme HttpClient par défaut
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

// === HTTP CLIENT — API Python ===
builder.Services.AddHttpClient("PythonScraper", client =>
{
    var scraperUrl = builder.Configuration["ScraperUrl"] ?? "http://localhost:5000/";
    client.BaseAddress = new Uri(scraperUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// === LOCAL STORAGE ===
builder.Services.AddBlazoredLocalStorage();

// === SERVICES ===
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmployeService>();
builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<FournisseurService>();
builder.Services.AddScoped<MaterielService>();
builder.Services.AddScoped<CommandeService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<DemandeAchatService>();
builder.Services.AddScoped<StatistiquesService>();
builder.Services.AddScoped<AffectationClientService>();
builder.Services.AddScoped<EmployeManagementService>();
builder.Services.AddScoped<ITIncidentService>();
builder.Services.AddScoped<StockClientService>();
builder.Services.AddScoped<DemandeAchatITClientService>();
builder.Services.AddScoped<MessagerieService>();
builder.Services.AddScoped<ProjectClientService>();
builder.Services.AddScoped<StatistiquesITService>();
builder.Services.AddScoped<FaceAuthClientService>();
builder.Services.AddScoped<OffreDemandeService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<ScraperCircuitBreakerService>();
builder.Services.AddScoped<OffreCircuitBreakerService>();
builder.Services.AddScoped<CommentaireCircuitBreakerService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<ArticleBiographieClientService>();
builder.Services.AddScoped<GraphService>();
builder.Services.AddScoped<AgentChatService>();
builder.Services.AddSingleton<StockAlertService>();
// Singleton : une seule instance partagée pour toute la durée de vie de l'app
builder.Services.AddSingleton<UnreadMessagesService>();
builder.Services.AddScoped<ConversationService>();
// Singleton car partagé entre pages (navigation sans rechargement)
builder.Services.AddSingleton<ScrapingBackgroundService>();

await builder.Build().RunAsync();