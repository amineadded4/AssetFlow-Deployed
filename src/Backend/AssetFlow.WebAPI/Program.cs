// ============================================================
// AssetFlow.WebAPI / Program.cs — CORRIGÉ SignalR + CORS
// ============================================================

using AssetFlow.Application.Interfaces;
using AssetFlow.WebAPI.Controllers;
using AssetFlow.Infrastructure.Data;
using AssetFlow.Infrastructure.Services;
using AssetFlow.WebAPI.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// === BASE DE DONNÉES ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Connexion Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connStr = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connStr);
});

// === AUTHENTIFICATION JWT ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer   = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero,
            RoleClaimType    = System.Security.Claims.ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            // ── AJOUT : SignalR envoie le token en query string ──
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var identity = ctx.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity == null) return Task.CompletedTask;
                var realmAccess = ctx.Principal?.FindFirst("realm_access");
                if (realmAccess != null)
                {
                    var doc = System.Text.Json.JsonDocument.Parse(realmAccess.Value);
                    if (doc.RootElement.TryGetProperty("roles", out var roles))
                        foreach (var role in roles.EnumerateArray())
                            identity.AddClaim(new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.Role, role.GetString()!));
                }
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",      p => p.RequireRole("Admin"));
    options.AddPolicy("ITOnly",         p => p.RequireRole("IT"));
    options.AddPolicy("EquipeAchatOnly",p => p.RequireRole("EquipeAchat"));
    options.AddPolicy("EmployeOnly",    p => p.RequireRole("Employe"));
    options.AddPolicy("ITOrAdmin",      p => p.RequireRole("IT", "Admin"));
    options.AddPolicy("AchatOrAdmin",   p => p.RequireRole("EquipeAchat", "Admin"));
    options.AddPolicy("ITOrAchat",      p => p.RequireRole("IT", "EquipeAchat"));
});

// === INJECTION DES SERVICES ===
builder.Services.AddHttpClient<IAuthService, KeycloakAuthService>();
builder.Services.AddScoped<IEmployeService,             EmployeService>();
builder.Services.AddScoped<IIncidentService,            IncidentService>();
builder.Services.AddScoped<IFournisseurService,         FournisseurService>();
builder.Services.AddScoped<IMaterielService,            MaterielService>();
builder.Services.AddScoped<ICommandeService,            CommandeService>();
builder.Services.AddScoped<IDemandeAchatService,        DemandeAchatService>();
builder.Services.AddScoped<IStatistiquesService,        StatistiquesService>();
builder.Services.AddScoped<IAffectationService,         AffectationService>();
builder.Services.AddScoped<IEmployeManagementService,   EmployeManagementService>();
builder.Services.AddScoped<IDemandeAchatITService,      DemandeAchatITService>();
builder.Services.AddScoped<IOffreAchatService,          OffreAchatService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddHttpClient<IOcrInvoiceService, OcrInvoiceService>();
builder.Services.AddScoped<IRedisOffreService, RedisOffreService>();
builder.Services.AddHttpClient<ChatOffreController>();
builder.Services.AddHttpClient<IFaceAuthService, FaceAuthService>();

// === SIGNALR ===
builder.Services.AddSignalR();

// === CORS ===
// AllowCredentials() requis par SignalR → WithOrigins() obligatoire (pas AllowAnyOrigin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
        policy
            .WithOrigins(
                "http://localhost:5001",    // Blazor HTTP (launchSettings)
                "https://localhost:7020"  // Blazor HTTPS (launchSettings)
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()); // ← requis par SignalR
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
app.UseAuthorization();          // ← UseAuthorization AVANT MapHub

app.MapControllers();
app.MapHub<ChatHub>("/chathub"); // ← après UseAuthorization

// === MIGRATION AUTOMATIQUE ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();