// ============================================================
// AssetFlow.WebAPI / Program.cs - VERSION MISE À JOUR
// Ajout du service EmployeService dans l'injection de dépendances
// ============================================================

using AssetFlow.Application.Interfaces;
using AssetFlow.Infrastructure.Data;
using AssetFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// === BASE DE DONNÉES ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// === AUTHENTIFICATION JWT ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// === INJECTION DES SERVICES ===
builder.Services.AddHttpClient<IAuthService, KeycloakAuthService>();
builder.Services.AddScoped<IEmployeService, EmployeService>(); // ← NOUVEAU
builder.Services.AddScoped<IIncidentService, IncidentService>(); 
builder.Services.AddScoped<IFournisseurService, FournisseurService>();
builder.Services.AddScoped<IMaterielService, MaterielService>();

// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("BlazorPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// === MIGRATION AUTOMATIQUE AU DÉMARRAGE ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();