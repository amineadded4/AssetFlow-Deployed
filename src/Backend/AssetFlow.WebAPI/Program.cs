// ============================================================
// AssetFlow.WebAPI / Program.cs — MISE À JOUR
// Ajout de CommandeService
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
            ValidateIssuer   = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero,
            // Mapper le claim "roles" Keycloak → ClaimTypes.Role ASP.NET
            RoleClaimType    = System.Security.Claims.ClaimTypes.Role
        };
        // Transformer les realm_access.roles de Keycloak
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var identity = ctx.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity == null) return Task.CompletedTask;

                // Keycloak met les rôles dans realm_access.roles
                var realmAccess = ctx.Principal?.FindFirst("realm_access");
                if (realmAccess != null)
                {
                    var doc = System.Text.Json.JsonDocument.Parse(realmAccess.Value);
                    if (doc.RootElement.TryGetProperty("roles", out var roles))
                    {
                        foreach (var role in roles.EnumerateArray())
                            identity.AddClaim(new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.Role, role.GetString()!));
                    }
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
});

// === INJECTION DES SERVICES ===
builder.Services.AddHttpClient<IAuthService, KeycloakAuthService>();
builder.Services.AddScoped<IEmployeService,   EmployeService>();
builder.Services.AddScoped<IIncidentService,  IncidentService>();
builder.Services.AddScoped<IFournisseurService, FournisseurService>();
builder.Services.AddScoped<IMaterielService,  MaterielService>();
builder.Services.AddScoped<ICommandeService,  CommandeService>(); // ← NOUVEAU
builder.Services.AddScoped<IDemandeAchatService, DemandeAchatService>();

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