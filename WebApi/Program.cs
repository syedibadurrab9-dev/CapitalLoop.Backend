using Application.Common.Interfaces;
using FluentValidation;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Scalar.AspNetCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebApi.Hubs;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);
// 1. SERVICES REGISTRATION

bool isEfTooling = args.Any(a => a.Contains("ef") || a.Contains("migrations"));
if (isEfTooling)
{
    // Register the bare minimum needed for EF Core to reflect on the DbContext
    builder.Services.AddInfrastructureServices(builder.Configuration);
}
else
{
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ApplicationDbContextInitializer>();
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(WebApi.Controllers.AiAdvisor.AdvisorController).Assembly);

    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
        options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    });

    // 2. OPENAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    // 3. POLLY RESILIENCE
    var retryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential
        })
        .Build();
    builder.Services.AddSingleton<ResiliencePipeline>(retryPipeline);

    #region MEDIATR & CQRS VALIDATE INTERCEPTORS
    // Explicitly scope the runtime metadata footprints of both the Core Interface and Feature slices
    var abstractionAssembly = typeof(IApplicationDbContext).Assembly;
    var featureSlicesAssembly = typeof(Application.Portfolios.Commands.AuthenticateGoogleUserCommand).Assembly;

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(abstractionAssembly);
        cfg.RegisterServicesFromAssembly(featureSlicesAssembly);
    });

    // Wire up FluentValidation across both compiled domains
    builder.Services.AddValidatorsFromAssembly(abstractionAssembly);
    builder.Services.AddValidatorsFromAssembly(featureSlicesAssembly);
    #endregion

    // =============================================================================
    // 5. SECURITY MATRIX VALIDATION GATES (MULTI-AUTHORITY AUTH ENGINE) [CL-101]
    // =============================================================================
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperSecretLongBaseSecureKey6366F1!CapitalLoopMasterTokenValidationKey2026";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CapitalLoopAuthAuthority";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CapitalLoopMobileAppClients";

    var googleClientIds = builder.Configuration.GetSection("Authentication:Google:ValidClientIds").Get<string[]?>()
                          ?? Array.Empty<string>();

    var paddedSecret = jwtSecret.Length >= 32 ? jwtSecret : jwtSecret.PadRight(32, '0');
    var signingKeyBytes = Encoding.UTF8.GetBytes(paddedSecret);

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        // 🔒 PIPELINE A: Handles internal re-signed symmetric tokens (Upgrades)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
                ClockSkew = TimeSpan.FromMinutes(2),
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    string? authorizationHeader = context.Request.Headers["Authorization"];

                    if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var inboundTokenRawString = authorizationHeader.Substring(7).Trim();

                        Console.WriteLine("\n=============================================================================");
                        Console.WriteLine($"🔍 [DIAGNOSTIC]: Token String Length: {inboundTokenRawString.Length}");
                        Console.WriteLine($"🔍 [DIAGNOSTIC]: Contains Dots (Is JWT Layout?): {inboundTokenRawString.Contains(".")}");
                        Console.WriteLine($"🔍 [DIAGNOSTIC]: Token Snapshot: {(inboundTokenRawString.Length > 20 ? inboundTokenRawString.Substring(0, 20) : inboundTokenRawString)}...");
                        Console.WriteLine("=============================================================================\n");
                    }

                    var webSocketToken = context.Request.Query["access_token"];
                    var requestPath = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(webSocketToken) && requestPath.StartsWithSegments("/hubs/support"))
                    {
                        context.Token = webSocketToken;
                    }

                    return Task.CompletedTask;
                }
            };
        })
        // 🌐 PIPELINE B: Handles federated Google ID Tokens automatically via Google's discovery endpoints
        .AddJwtBearer("GoogleAuthScheme", options =>
        {
            options.Authority = "https://accounts.google.com";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[] { "https://accounts.google.com", "accounts.google.com" },
                ValidateAudience = true,
                ValidAudiences = googleClientIds,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                NameClaimType = "name",
                RoleClaimType = "role"
            };
        });

    // 🛡️ RE-ALIGNED POLICY MANAGER: Standardized across both authentication pipelines
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "GoogleAuthScheme")
            .Build();
    });

    // =============================================================================
    // 6. CROSS-ORIGIN RESOURCE SHARING FOOTPRINTS (CORS)
    // =============================================================================
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevCors", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:4200",
                    "http://localhost:8081",
                    "http://10.20.0.81:8081",
                    "http://10.20.0.97:8081",  // Office API Node
                    "http://10.20.0.97:4200",  // Office Frontend Node
                    "http://192.168.0.104:8081",
                    "http://192.168.0.104:4200", // Home Frontend Node
                    "https://capitalloop.com"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

// =============================================================================
// 🚨 FORCE CONTAINER VALIDATION ON BUILD (SMASH SILENT INJECTOR DEADLOCKS)
// =============================================================================
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = true;    // Prevents captive dependency tracking bugs
    options.ValidateOnBuild = true;   // 💥 Forces .NET to resolve all constructor trees instantly at boot up!
});

var app = builder.Build();

if (!isEfTooling)
{
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseRouting();
    app.UseCors("DevCors");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<SubscriptionGateMiddleware>();

    app.MapControllers();
    app.MapHub<SupportHub>("/hubs/support");
}

app.Run();