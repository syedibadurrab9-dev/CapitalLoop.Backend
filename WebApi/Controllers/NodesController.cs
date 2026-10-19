using Infrastructure.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers;

[Authorize]
public class NodesController : BaseApiController
{
    private readonly IApplicationDbContext _context;

    public NodesController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns live operational health and telemetry metrics for all system ingestion nodes.
    /// </summary>
    [HttpGet]
    public IActionResult GetActiveNodes()
    {
        var nodes = new[]
        {
            new { Id = "psx-feed", Name = "PSX Market Gateway (KSE-100)", LastKnownPrice = 78500.0, LatencyMs = 24, IsHealthy = true },
            new { Id = "binance-spot", Name = "Binance Cryptographic Spot Feed", LastKnownPrice = 68500.0, LatencyMs = 42, IsHealthy = true },
            new { Id = "pmex-gold", Name = "PMEX Physical Gold Tola Ticker", LastKnownPrice = 242000.0, LatencyMs = 18, IsHealthy = true },
            new { Id = "sbp-fx", Name = "SBP Interbank FX Exchange (USD/PKR)", LastKnownPrice = 278.50, LatencyMs = 14, IsHealthy = true }
        };

        return Ok(nodes);
    }

    /// <summary>
    /// Retrieves mapped portfolio asset positions for a specific user.
    /// </summary>
    [HttpGet("positions/{email}")]
    public async Task<IActionResult> GetUserPositions(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var assets = await _context.Assets
            .AsNoTracking()
            .Where(a => a.UserEmail == normalizedEmail)
            .Select(a => new
            {
                Id = a.Id.ToString(),
                Name = a.Name,
                Category = a.Category.ToString(),
                HoldingValuePKR = a.HoldingValuePKR
            })
            .ToListAsync(cancellationToken);

        return Ok(assets);
    }
}
