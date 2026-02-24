// ============================================================
// AssetFlow.BlazorUI / Program.cs - MISE À JOUR
// Ajout de EmployeService dans l'injection de dépendances
// ============================================================

using AssetFlow.BlazorUI;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// === HTTP CLIENT ===
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5235/")
});

// === LOCAL STORAGE ===
builder.Services.AddBlazoredLocalStorage();

// === SERVICES ===
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmployeService>();  // ← NOUVEAU
builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<FournisseurService>();
builder.Services.AddScoped<MaterielService>();

await builder.Build().RunAsync();