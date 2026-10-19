using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

public record MarketTickerPayload(
    decimal Kse100Index,
    decimal Kse100ChangePoints,
    decimal Kse100ChangePercent,
    decimal GoldTola24KPKR,
    decimal SilverTolaPKR,
    decimal UsdPkrRate,
    Dictionary<string, decimal> StockQuotes,
    DateTime TimestampUtc
);

public class MarketHub : Hub
{
    private readonly ILogger<MarketHub> _logger;

    public MarketHub(ILogger<MarketHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("📈 Market Ticker subscriber connected: {ConnectionId}", Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "MarketTickerGroup");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("📉 Market Ticker subscriber disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
