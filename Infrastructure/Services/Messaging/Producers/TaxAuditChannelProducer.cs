using Application.Common.Interfaces;
using Application.Common.Interfaces.ComplianceAsyncJobs;
using Application.Portfolios.Commands.QueueTaxAudit;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Producers;

/// <summary>
/// 🚀 HIGH-PERFORMANCE BACKING WORK CHANNEL DISPATCHER
/// Concrete implementation of ITaxAuditChannelProducer managing non-blocking writes 
/// onto your bounded background memory pipeline [CL-501].
/// </summary>
public class TaxAuditChannelProducer : ITaxAuditChannelProducer
{
    private readonly ChannelWriter<TaxAuditQueueMessage> _channelWriter;

    /// <summary>
    /// Injects the strongly-typed Channel singleton instance established during host startup bootstrap routines.
    /// </summary>
    public TaxAuditChannelProducer(Channel<TaxAuditQueueMessage> channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channelWriter = channel.Writer;
    }

    /// <summary>
    /// Writes a strongly typed, pre-allocated message payload directly onto the background memory rail.
    /// </summary>
    public async ValueTask WriteMessageAsync(TaxAuditQueueMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // 🚀 HIGH-SPEED PERFORMANCE OPTIMIZATION: Attempt non-blocking synchronous placement first 
        // to avoid unneeded async state-machine allocation loops under heavy transaction throughput.
        if (_channelWriter.TryWrite(message))
        {
            return;
        }

        // Fall back gracefully to asynchronous backpressure wait state if the memory channel bounds are saturated
        await _channelWriter.WriteAsync(message, cancellationToken);
    }

    /// <summary>
    /// Overload pattern allowing handlers to stream inline parameter sequences onto the queue channel 
    /// without explicitly instantiating external wrapper records manually.
    /// </summary>
    public async ValueTask QueueAuditTaskAsync(
        Guid jobId,
        string userEmail,
        bool initiatedBySystem,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Asynchronous messaging failure: An active tracking Job ID must be supplied.", nameof(jobId));
        }

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            throw new ArgumentException("Asynchronous messaging failure: Target user email context identifier cannot be null or empty.", nameof(userEmail));
        }

        // 🎯 ENCAPSULATION UNIFICATION: Packages raw parameters cleanly into the true record type
        var messagePayload = new TaxAuditQueueMessage(
            JobId: jobId,
            UserEmail: userEmail.Trim().ToLowerInvariant(),
            InitiatedBySystem: initiatedBySystem
        );

        // Route the structured package directly to your optimized non-blocking writing thread matrix
        await WriteMessageAsync(messagePayload, cancellationToken);
    }
}