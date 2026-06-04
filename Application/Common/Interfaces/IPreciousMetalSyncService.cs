using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Interfaces
{
    public interface IPreciousMetalSyncService
    {
        // Returns e.g., ["Silver_Tolas" -> 152500, "Gold_Tolas" -> 275000]
        Task<Dictionary<string, decimal>> GetLiveRatesAsync(string email, CancellationToken cancellationToken);
    }
}
