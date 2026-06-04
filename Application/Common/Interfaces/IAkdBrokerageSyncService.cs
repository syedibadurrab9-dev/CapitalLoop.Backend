using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Interfaces
{
    public interface IAkdBrokerageSyncService
    {
        /// <summary>
        /// Connects to the secure email ingestion stream, parses actual shares inventories 
        /// from settled AKD trade confirmations, and evaluates their real-time PKR values.
        /// </summary>
        Task<Dictionary<string, decimal>> GetLiveSharesInventoryAsync(string email, CancellationToken cancellationToken);
    }
}
