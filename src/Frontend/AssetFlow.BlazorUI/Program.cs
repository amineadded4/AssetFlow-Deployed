using AssetFlow.BlazorUI;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;  // ← si besoin

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// === AUTH TOKEN HANDLER ===
builder.Services.AddScoped<AuthTokenHandler>();

// === HTTP CLIENT — API .NET (avec token automatique) ===
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5235/");
    client.Timeout = TimeSpan.FromMinutes(2);
})
.AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient("RefreshClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Rendre ce client disponible comme HttpClient par défaut
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

// === HTTP CLIENT — API Python ===
builder.Services.AddHttpClient("PythonScraper", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/");
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

await builder.Build().RunAsync();