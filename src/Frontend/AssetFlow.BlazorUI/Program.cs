using AssetFlow.BlazorUI;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;  // ← si besoin

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// === HTTP CLIENT — API .NET ===
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5235/"),
    Timeout = TimeSpan.FromMinutes(2)
});

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

await builder.Build().RunAsync();