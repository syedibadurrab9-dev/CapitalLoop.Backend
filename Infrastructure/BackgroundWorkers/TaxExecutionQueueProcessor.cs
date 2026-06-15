using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Portfolios.Commands.QueueTaxAudit; // Ensure this namespace is correct
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundWorkers;

public class TaxExecutionQueueProcessor : BackgroundService
{
    private readonly Channel<TaxAuditQueueMessage> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaxExecutionQueueProcessor> _logger;
    private const string EngineOperatorIdentity = "system-tax-engine@capitalloop.internal";

    public TaxExecutionQueueProcessor(
        Channel<TaxAuditQueueMessage> channel,
        IServiceProvider serviceProvider,
        ILogger<TaxExecutionQueueProcessor> logger)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("⚡ Tax Execution Background Processor Daemon initialized.");

        // Read all items from the channel until completion
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Create a scope to resolve DbContext safely
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                _logger.LogInformation("Processing Tax Audit Job: {JobId}", message.JobId);

                var jobRecord = await dbContext.TaxAuditJobs
                    .FirstOrDefaultAsync(j => j.Id == message.JobId, stoppingToken);

                if (jobRecord == null || jobRecord.ExecutionStatus == "Completed")
                    continue;

                // Execute logic
                await Task.Delay(3000, stoppingToken); // Simulated work

                jobRecord.CompleteJob(EngineOperatorIdentity);

                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Task Completed | Job ID: {JobId}", message.JobId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Tax Processor shutting down...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job {JobId}", message.JobId);
            }
        }
    }
}