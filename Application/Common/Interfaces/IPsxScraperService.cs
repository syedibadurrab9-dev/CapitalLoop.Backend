using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Interfaces
{
    public interface IPsxScraperService
    {
        // Returns e.g., ["SYS" -> 45000, "ENGRO" -> 120000] based on market scrapers
        Task<Dictionary<string, decimal>> GetLiveStockValuationsAsync(string email, CancellationToken cancellationToken);
    }
}
