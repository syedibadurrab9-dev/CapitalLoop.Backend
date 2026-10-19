using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly IHubContext<MarketHub> _hubContext;
    private static MarketTickerPayload _latestSnapshot = new(
        Kse100Index: 104_850.25m,
        Kse100ChangePoints: +542.80m,
        Kse100ChangePercent: +0.52m,
        GoldTola24KPKR: 278_500.00m,
        SilverTolaPKR: 3_250.00m,
        UsdPkrRate: 278.50m,
        StockQuotes: new Dictionary<string, decimal>
        {
            ["SYS"] = 465.80m,
            ["ENGRO"] = 325.40m,
            ["HUBC"] = 124.60m,
            ["OGDC"] = 142.10m,
            ["LUCK"] = 890.50m
        },
        TimestampUtc: DateTime.UtcNow
    );

    public MarketController(IHubContext<MarketHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Returns the latest live market ticker snapshot (KSE-100, Gold, Silver, FX).
    /// GET: api/Market/ticker
    /// </summary>
    [HttpGet("ticker")]
    public IActionResult GetLatestTicker()
    {
        return Ok(_latestSnapshot);
    }

    /// <summary>
    /// Dispatches an immediate real-time market data broadcast to all connected SignalR clients.
    /// POST: api/Market/broadcast
    /// </summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> BroadcastMarketUpdate([FromBody] MarketTickerPayload? customPayload)
    {
        if (customPayload != null)
        {
            _latestSnapshot = customPayload with { TimestampUtc = DateTime.UtcNow };
        }
        else
        {
            // Simulate slight market fluctuation
            var randomFactor = (decimal)(new Random().NextDouble() * 0.006 - 0.003);
            var updatedKse = Math.Round(_latestSnapshot.Kse100Index * (1 + randomFactor), 2);
            var pointsDelta = Math.Round(updatedKse - 104_000m, 2);

            _latestSnapshot = _latestSnapshot with
            {
                Kse100Index = updatedKse,
                Kse100ChangePoints = pointsDelta,
                Kse100ChangePercent = Math.Round((pointsDelta / 104_000m) * 100m, 2),
                TimestampUtc = DateTime.UtcNow
            };
        }

        await _hubContext.Clients.All.SendAsync("ReceiveMarketData", _latestSnapshot);
        return Ok(new { Status = "BroadcastSent", Snapshot = _latestSnapshot });
    }
}
