using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ProcureLive.Application;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Infrastructure;
using ProcureLive.Infrastructure.Settings;
using ProcureLive.WebApi.HostedServices;
using ProcureLive.WebApi.Hubs;
using ProcureLive.WebApi.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ProcureLive API",
        Version = "v1",
        Description = "B2B E-Tedarik ve Anlık İhale Sistemi - Faz 1"
    });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header. Örnek: \"Bearer {token}\""
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

JwtSettings jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings yapılandırması eksik.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Phase 2: AuctionHub okur access_token'ı query string üzerinden (SignalR WebSocket bağlantıları
        // Authorization header taşıyamaz), bu yüzden hub yollarında token query'den de kabul edilir.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                string? accessToken = context.Request.Query["access_token"];
                PathString path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole(Roles.Admin))
    .AddPolicy(Policies.RequireBuyer, policy => policy.RequireRole(Roles.Buyer))
    .AddPolicy(Policies.RequireSupplier, policy => policy.RequireRole(Roles.Supplier))
    .AddPolicy(Policies.RequireInternalUser, policy => policy.RequireRole(Roles.Admin, Roles.Buyer))
    .AddPolicy(Policies.CompanyAccess, policy => policy.Requirements.Add(new CompanyAccessRequirement()))
    .AddPolicy(Policies.AuctionAccess, policy => policy.Requirements.Add(new AuctionAccessRequirement()));

builder.Services.AddScoped<IAuthorizationHandler, CompanyAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AuctionAccessHandler>();

const string CorsPolicyName = "ProcureLiveCors";
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection eksik."))
    .AddRedis(builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("ConnectionStrings:Redis eksik."));

string redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis eksik.");

// Faz 2: canlı ihale altyapısı — SignalR (Redis backplane ile çoklu instance desteği), hub bildirim
// servisi, ve ihale yaşam döngüsünü (Scheduled->Live->Ended) otomatik ilerleten arka plan servisi.
builder.Services
    .AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("ProcureLive");
    });

builder.Services.AddScoped<IAuctionNotificationService, SignalRAuctionNotificationService>();
builder.Services.AddHostedService<AuctionLifecycleHostedService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<AuctionHub>("/hubs/auction");

using (IServiceScope scope = app.Services.CreateScope())
{
    await ProcureLive.Infrastructure.Persistence.Seed.InitialAdminSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program
{
}
