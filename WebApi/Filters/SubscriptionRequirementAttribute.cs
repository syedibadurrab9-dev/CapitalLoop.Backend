using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Filters;

/// <summary>
/// ⚖️ FINE-GRAINED DB-DRIVEN SUBSCRIPTION FEATURE GATEWAY FILTER
/// Intercepts inbound execution pipelines to validate active profile tier contexts 
/// against targeted feature capabilities before permitting controller code invocation [CL-503].
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class SubscriptionRequirementAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _requiredFeatureGate;

    public SubscriptionRequirementAttribute(string requiredFeatureGate)
    {
        _requiredFeatureGate = requiredFeatureGate ?? throw new ArgumentNullException(nameof(requiredFeatureGate));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var httpContext = context.HttpContext;
        var requestPath = httpContext.Request.Path.Value ?? string.Empty;

        // 🚀 CRITICAL OVERRIDE: If the user is hitting the override path to upgrade, skip verification rules entirely
        if (requestPath.Contains("subscriptions/override", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // 🚀 FIXED CLAIM EXTRACTOR: Evaluates shorthand and token claim variables to properly decode identities
        var userEmail = httpContext.User?.FindFirstValue(ClaimTypes.Email)
                        ?? httpContext.User?.FindFirstValue("email")
                        ?? httpContext.User?.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var dbContext = httpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
        var sanitizedEmail = userEmail.Trim().ToLowerInvariant();

        // 🧠 FIXED PERFORMANCE BLOCKED: Swapped synchronous FirstOrDefault for non-blocking database evaluation
        var userProfile = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(u => u.Email == sanitizedEmail)
            .Select(u => new { u.SubscriptionTier })
            .FirstOrDefaultAsync(httpContext.RequestAborted);

        if (userProfile == null || string.IsNullOrWhiteSpace(userProfile.SubscriptionTier))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var normalizedTier = userProfile.SubscriptionTier.Trim().ToLowerInvariant();

        // =============================================================================
        // RE-ALIGNED MULTI-TIER MONETIZATION SECURITY MATRICES [CL-503]
        // =============================================================================
        // Aligned securely to match centralized SubscriptionTiers constants mapping definitions
        bool hasAccess = normalizedTier switch
        {
            // Back-office agents bypass standard feature restriction parameters globally
            var t when string.Equals(t, SubscriptionTiers.AgentBackoffice, StringComparison.OrdinalIgnoreCase)
                => true,

            // Wealth advisors have access to all analytical advisory tools, but skip accounting audits
            var t when string.Equals(t, SubscriptionTiers.AdvisorWealth, StringComparison.OrdinalIgnoreCase)
                => !string.Equals(_requiredFeatureGate, "Tax_Audit_Compliance_Review", StringComparison.OrdinalIgnoreCase),

            // Tax consultants have dedicated access to fiscal audit modules and baseline inputs
            var t when string.Equals(t, SubscriptionTiers.TaxConsultantTier, StringComparison.OrdinalIgnoreCase)
                => string.Equals(_requiredFeatureGate, "Tax_Audit_Compliance_Review", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(_requiredFeatureGate, "Manual_Ledger_Tracking", StringComparison.OrdinalIgnoreCase),

            // Premium tier users leverage domain tier lookup helpers to safely authorize access to advanced indices
            var t when string.Equals(t, SubscriptionTiers.Premium, StringComparison.OrdinalIgnoreCase)
                => SubscriptionTiers.IsFeatureAuthorized(SubscriptionTiers.Premium, _requiredFeatureGate),

            // Standard free tier remains restricted to entry-level manual tracking inputs
            var t when string.Equals(t, SubscriptionTiers.StandardFree, StringComparison.OrdinalIgnoreCase)
                => string.Equals(_requiredFeatureGate, "Manual_Ledger_Tracking", StringComparison.OrdinalIgnoreCase),

            _ => false
        };

        if (!hasAccess)
        {
            // Block endpoint invocation cleanly with a 403 Forbidden status message response payload
            context.Result = new ObjectResult(new
            {
                Code = "FEATURE_GATE_LOCKED",
                Message = $"Access Denied. Executing this workflow requires an upgrade to unlock the feature gate: '{_requiredFeatureGate}'.",
                RequiredTier = GetMinimumRequiredTierForFeature(_requiredFeatureGate)
            })
            { StatusCode = 403 };

            return;
        }

        // Pass validation checks cleanly — advance execution token further into the api controller channel
        await next();
    }

    private static string GetMinimumRequiredTierForFeature(string featureGate)
    {
        if (string.Equals(featureGate, "Tax_Audit_Compliance_Review", StringComparison.OrdinalIgnoreCase))
            return SubscriptionTiers.TaxConsultantTier;

        if (string.Equals(featureGate, "On_Demand_Advisor_Consultations", StringComparison.OrdinalIgnoreCase))
            return SubscriptionTiers.AdvisorWealth;

        if (string.Equals(featureGate, "Automated_Exchange_Sync", StringComparison.OrdinalIgnoreCase))
            return SubscriptionTiers.Premium;

        return SubscriptionTiers.StandardFree;
    }
}