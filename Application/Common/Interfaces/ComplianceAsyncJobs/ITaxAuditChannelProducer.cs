using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Portfolios.Commands.QueueTaxAudit;

namespace Application.Common.Interfaces.ComplianceAsyncJobs;

/// <summary>
/// ⚡ BACKGROUND QUEUE PRODUCER CONTRACT
/// Interface exposing thread-safe ingestion paths into native high-concurrency memory channels [CL-501].
/// </summary>
public interface ITaxAuditChannelProducer
{
    /// <summary>
    /// Writes a strongly typed, pre-allocated message payload directly onto the background memory rail.
    /// </summary>
    /// <param name="message">The pre-allocated background task tracking message payload.</param>
    /// <param name="cancellationToken">The cancellation token lifecycle constraint monitor.</param>
    /// <returns>A ValueTask representing the asynchronous write operation.</returns>
    ValueTask WriteMessageAsync(TaxAuditQueueMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overload pattern allowing handlers to stream inline parameter sequences onto the queue channel 
    /// without explicitly instantiating external wrapper records manually.
    /// </summary>
    /// <param name="jobId">The unique tracking job identifier created in persistent storage tables.</param>
    /// <param name="userEmail">The target investor's email identifier coordinate.</param>
    /// <param name="initiatedBySystem">True if triggered automatically by routine background checks; false if manually pushed by the user.</param>
    /// <param name="cancellationToken">The cancellation token lifecycle constraint monitor.</param>
    /// <returns>A ValueTask representing the asynchronous write operation.</returns>
    ValueTask QueueAuditTaskAsync(Guid jobId, string userEmail, bool initiatedBySystem, CancellationToken cancellationToken = default);
}