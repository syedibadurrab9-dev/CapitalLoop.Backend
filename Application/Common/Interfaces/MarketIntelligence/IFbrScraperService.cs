using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces.MarketIntelligence;

/// <summary>
/// 🛡️ MARKET INTELLIGENCE & SCRAPING CONTRACT
/// Abstraction defining high-frequency background scraping hooks into the Federal Board of Revenue (FBR) systems.
/// Fully conforms to the specification signature mapped in your master architectural matrix.
/// </summary>
public interface IFbrScraperService
{
    /// <summary>
    /// Executes a headless background connection sweep against official FBR Active Taxpayer List (ATL) portals 
    /// to determine a customer's active filing registration status.
    /// </summary>
    /// <param name="cnic">The sanitized 13-digit Pakistani National Identity Card number string (without hyphens).</param>
    /// <param name="cancellationToken">The structural system verification cancellation token token assignment.</param>
    /// <returns>True if the target identity is an active verified Filer; otherwise, false.</returns>
    Task<bool> FetchActiveTaxFilerStatusAsync(string cnic, CancellationToken cancellationToken = default);
}