using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Middleware;

/// <summary>
/// 🔒 REAL-TIME CLAIMS SUBSCRIPTION INTERCEPTOR
/// Restricts access to high-concurrency robo-advisory execution nodes based on account tier claims [CL-101 / CL-503].
/// Fast-circuits illegitimate request streams prior to hitting resource-heavy query handlers.
/// </summary>
public class SubscriptionGateMiddleware
{
    private readonly RequestDelegate _next;

    // 🧱 CONSTANTS ALIGNED WITH CORE IDENTITY ROLES
    private const string SubscriptionClaimType = "SubscriptionTier";
    private const string PremiumTierName = "premium";
    private const string AdvisorWealthTierName = "advisor-wealth";
    private const string TaxConsultantTierName = "tax-consultant-tier";
    private const string AdministrativeRoleName = "Admin";
    private const string BackofficeAgentClaimName = "agent-backoffice";

    // Restricted endpoint path markers that require premium tier verification flags
    private static readonly string[] PremiumFeaturePathPrefixes =
    {
        "/api/advisor/allocation/deficiencies",
        "/api/advisor/portfolio/projections",
        "/api/advisor/recommendations/target",
        "/api/compliance/tax/queue-audit"
    };

    public SubscriptionGateMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestPath = context.Request.Path.Value ?? string.Empty;

        // 🎯 STEP 1: ROUTING MATCH INTERCEPTION
        // Evaluate if the incoming request path targets a premium-tier analytical dashboard component
        bool isPremiumFeatureRequested = PremiumFeaturePathPrefixes.Any(path =>
            requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase));

        if (isPremiumFeatureRequested)
        {
            // 🎯 STEP 2: SECURITY PRINCIPAL EVALUATION
            // Verify if the current user identity claims possess the required access level signatures
            var userPrincipal = context.User;

            if (userPrincipal?.Identity is null || !userPrincipal.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteForbiddenProblemDetailsAsync(context,
                    "Unauthenticated Access Detour",
                    "The requested analytical matrix demands a cryptographically valid security identity signature.",
                    StatusCodes.Status401Unauthorized);
                return;
            }

            // Extract the active subscription token hash claim assigned during token signing rounds
            var activeTierClaim = userPrincipal.FindFirst(SubscriptionClaimType)?.Value?.Trim().ToLowerInvariant();

            // 🛠️ RE-ALIGNED ACCESS MATRICES: Case-insensitive evaluation matching the shared 5-Tier architecture
            bool isAccessGranted = activeTierClaim switch
            {
                var tier when string.Equals(tier, PremiumTierName, StringComparison.OrdinalIgnoreCase)
                    => !requestPath.StartsWith("/api/compliance/tax/queue-audit", StringComparison.OrdinalIgnoreCase), // Premium covers analytics but excludes tax audits

                var tier when string.Equals(tier, AdvisorWealthTierName, StringComparison.OrdinalIgnoreCase)
                    => true,

                var tier when string.Equals(tier, TaxConsultantTierName, StringComparison.OrdinalIgnoreCase)
                    => true,

                var tier when string.Equals(tier, BackofficeAgentClaimName, StringComparison.OrdinalIgnoreCase)
                    => true,

                _ => userPrincipal.IsInRole(AdministrativeRoleName)
            };

            if (!isAccessGranted)
            {
                // 🚀 FAST-CIRCUIT SHORT PIPELINE TERMINATION
                // Restricts the pipeline and stops the thread before it drops down to heavy DB or Redis cache reads
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await WriteForbiddenProblemDetailsAsync(context,
                    "Elevated Subscription Plan Tier Required",
                    $"Access to advanced analytics path '{requestPath}' is restricted for your account tier configuration.",
                    StatusCodes.Status403Forbidden);
                return;
            }
        }

        // Move securely down to the next operational middleware block in sequence
        await _next(context);
    }

    private static Task WriteForbiddenProblemDetailsAsync(HttpContext context, string title, string descriptiveDetails, int statusCode)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = descriptiveDetails,
            Instance = context.Request.Path
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var jsonResult = JsonSerializer.Serialize(problemDetails, options);

        return context.Response.WriteAsync(jsonResult);
    }
}