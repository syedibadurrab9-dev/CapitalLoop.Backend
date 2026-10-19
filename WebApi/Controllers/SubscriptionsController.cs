using Infrastructure.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers;

[Authorize]
public class SubscriptionsController : BaseApiController
{
    private readonly IApplicationDbContext _context;

    public SubscriptionsController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the active subscription tier packages configured on the platform.
    /// </summary>
    [HttpGet("plans")]
    public IActionResult GetAllAvailablePlans()
    {
        var plans = new[]
        {
            new
            {
                PlanId = "standard-free",
                Name = "Standard Investor (Free)",
                PricePKR = 0,
                BillingInterval = "MONTHLY",
                Features = new[] { "Manual Asset Tracking", "Standard PSX Ticker", "Basic SECP Compliance" }
            },
            new
            {
                PlanId = "premium-monthly",
                Name = "Institutional Wealth (Premium)",
                PricePKR = 4999,
                BillingInterval = "MONTHLY",
                Features = new[] { "Real-Time Exchange WebSocket", "Automated Binance API Sync", "AI Risk Advisor & Concentration Stress Testing", "Priority SECP Regulatory Ledger" }
            }
        };

        return Ok(plans);
    }

    /// <summary>
    /// Overrides a user's subscription tier status.
    /// </summary>
    [HttpPost("override")]
    public async Task<IActionResult> OverrideUserSubscription([FromBody] OverrideSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
            return NotFound(new { Message = $"User '{request.Email}' was not found." });

        if (request.PlanId.Contains("premium", StringComparison.OrdinalIgnoreCase))
        {
            user.UpgradeToPremium();
        }
        else
        {
            user.RevokePremium();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { Message = "User subscription tier updated successfully.", Email = request.Email, PlanId = request.PlanId });
    }

    /// <summary>
    /// Upgrades the currently authenticated user to Institutional Wealth (Premium).
    /// POST: api/Subscriptions/upgrade
    /// </summary>
    [HttpPost("upgrade")]
    public async Task<IActionResult> UpgradeAuthenticatedUser(CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var user = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == userEmail, cancellationToken);

        if (user == null)
            return NotFound(new { Message = $"User profile '{userEmail}' was not found." });

        user.UpgradeToPremium();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { Success = true, Message = "Upgraded to Institutional Wealth Premium tier.", Email = userEmail, IsPremiumUser = true });
    }
}

public record OverrideSubscriptionRequest(string Email, string PlanId);
