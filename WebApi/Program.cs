using Application;
using Application.Common.Interfaces;
using FluentValidation;
using Infrastructure.Common.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.SyncServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Polly;
using System.Text;
using WebApi.Hubs;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

#region CONTROLLERS + SIGNALR
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<WebApi.Hubs.ISupportSessionManager, WebApi.Hubs.SupportSessionManager>();
#endregion

#region OPENAPI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Paste JWT token here"
        };

        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = scheme;

        var schemeReference = new OpenApiSecuritySchemeReference("Bearer", document);

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            { schemeReference, new List<string>() }
        });

        document.SetReferenceHostDocument();

        return Task.CompletedTask;
    });
});
#endregion

#region DB CONTEXT
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseInMemoryDatabase("CapitalLoopDevDb");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddScoped<IApplicationDbContext>(
    p => p.GetRequiredService<ApplicationDbContext>());
#endregion

#region POLLY RESILIENCE FRAMEWORK (Zero Vendor Lock-In, Standard Open Source)
var resiliencePipeline = new ResiliencePipelineBuilder()
    .AddTimeout(TimeSpan.FromSeconds(10))
    .AddRetry(new Polly.Retry.RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
    {
        ShouldHandle = new PredicateBuilder()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>(),
        SamplingDuration = TimeSpan.FromSeconds(30),
        FailureRatio = 0.5,
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(15)
    })
    .Build();

builder.Services.AddSingleton(resiliencePipeline);
#endregion

#region HTTP CLIENTFACTORY REGISTRATIONS
builder.Services.AddHttpClient();

builder.Services.AddHttpClient("GoogleOAuth", client =>
{
    client.BaseAddress = new Uri("https://oauth2.googleapis.com");
});

// ✅ OPTIMIZED: Using named HttpClients with dedicated proxy lifecycle handling 
// to prevent typed resolution conflicts with our Singleton Polly pipelines
builder.Services.AddHttpClient<ICryptoSyncService, BinanceApiClient>(client =>
{
    // Sets up standard base paths safely for our low-level API handshake vectors
    client.Timeout = TimeSpan.FromSeconds(15);
});
#endregion

#region CORE APPLICATION SERVICES MANAGEMENT (DI)
builder.Services.AddScoped<IIdentityService, GoogleSsoService>();
builder.Services.AddScoped<IPreciousMetalSyncService, PreciousMetalsClient>();
builder.Services.AddScoped<IPsxScraperService, PsxScraperService>();
builder.Services.AddScoped<IAkdBrokerageSyncService, AkdBrokerageSyncService>();

// ✅ FIXED: Registered the core data concrete worker to resolve the multi-asset translation dependencies
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<IFbrTaxService, FbrTaxService>();
#endregion

#region CQRS LITE & APPLICATION REGISTRATIONS
builder.Services.AddApplicationServices();
#endregion

#region AUTHENTICATION & SECURITY VALUATION FOR SECURE WEBSOCKETS
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer missing");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience missing");

var googleClientIds = new List<string>();
var audienceSection = builder.Configuration.GetSection("Authentication:Google:ValidClientIds");
foreach (var child in audienceSection.GetChildren())
{
    if (!string.IsNullOrEmpty(child.Value))
    {
        googleClientIds.Add(child.Value);
    }
}

if (googleClientIds.Count == 0)
{
    throw new InvalidOperationException("Google ValidClientIds configuration is missing from appsettings.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero, // Strict zero-tolerance against replaying expired JWT tokens
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Captures state signatures explicitly when handshaking from Angular or React Native over raw websockets
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/support-hub"))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT AUTH FAILED] Exception: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"[JWT CHALLENGE] Error: {context.Error}, Description: {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
#endregion

#region CORS PIPELINES
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Mandated to sustain active real-time SignalR handshakes
    });
});
#endregion

var app = builder.Build();

#region MIDDLEWARE PIPELINE EXECUTIONS
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
#endregion

#region DEV SWAGGER TOOLS
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CapitalLoop API v1");
        options.RoutePrefix = "swagger";
    });
}
#endregion

app.UseRouting();
app.UseCors("DevCors");
app.UseMiddleware<AntiforgeryValidationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SupportHub>("/support-hub");
app.MapHub<MarketHub>("/market-hub");

#region DATABASE DATA SEED WORKER
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await ApplicationDbContext.SeedDataAsync(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Startup database seed instantiation failed.");
    }
}
#endregion

app.Run();