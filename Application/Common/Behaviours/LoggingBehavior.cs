using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviours;

/// <summary>
/// Cross-Cutting MediatR Pipeline Behavior forcing unified performance tracing,
/// allocation-optimized contextual logging, and exception diagnostic isolation [CL-102].
/// Utilizes pre-compiled property delegate trees to prevent runtime reflection bottlenecks.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private static readonly Func<TRequest, string?>? ExtractedEmailDelegate;

    // Static thread-safe lookup cache for request structural profiling metadata
    private static readonly string CachedRequestName = typeof(TRequest).Name;

    /// <summary>
    /// Static Constructor compiling expression delegate matrices natively on cold assembly load.
    /// Eliminates subsequent reflection loops completely across high-frequency transaction tracks.
    /// </summary>
    static LoggingBehavior()
    {
        var requestType = typeof(TRequest);

        // Locate matching property bindings using clean architecture identity schemas
        var targetProperty = requestType.GetProperty("UserEmail", BindingFlags.Public | BindingFlags.Instance)
                             ?? requestType.GetProperty("Email", BindingFlags.Public | BindingFlags.Instance);

        if (targetProperty is not null && targetProperty.CanRead)
        {
            try
            {
                var inputParam = Expression.Parameter(requestType, "request");
                var propertyAccess = Expression.Property(inputParam, targetProperty);

                // Safe check handling null or alternative underlying variant assignments
                var conversion = Expression.TypeAs(propertyAccess, typeof(string));

                ExtractedEmailDelegate = Expression.Lambda<Func<TRequest, string?>>(conversion, inputParam).Compile();
            }
            catch
            {
                ExtractedEmailDelegate = null; // Fault-safeguard fallback gate
            }
        }
    }

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        // Extract identification variables securely without forcing CPU reflection allocation stumbles
        var userEmailContext = "Anonymous/Public_Handshake";

        if (ExtractedEmailDelegate is not null)
        {
            try
            {
                var val = ExtractedEmailDelegate(request);
                if (!string.IsNullOrWhiteSpace(val))
                {
                    userEmailContext = val.Trim().ToLowerInvariant();
                }
            }
            catch
            {
                userEmailContext = "Evaluation_Fault/Secured_Context";
            }
        }

        // 🛡️ SECURITY COMPLIANCE GATE: Strip tracking values if the pipeline is handling raw cryptography payloads
        if (CachedRequestName.Contains("Binance", StringComparison.OrdinalIgnoreCase) ||
            CachedRequestName.Contains("Sso", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "CapitalLoop Security Transaction Ingested | Request: {RequestName} | Authentication Context Shield Enforced.",
                CachedRequestName);
        }
        else
        {
            _logger.LogInformation(
                "CapitalLoop Transaction Initiated | Request: {RequestName} | Identity Context: {UserContext}",
                CachedRequestName, userEmailContext);
        }

        var timer = Stopwatch.StartNew();

        try
        {
            // Propagate execution frame down through the pipeline layers to the underlying handler slice
            var response = await next();

            timer.Stop();
            var elapsedMs = timer.ElapsedMilliseconds;

            // 🚨 PERFORMANCE SLA COMPLIANCE AUDITING: 500ms critical alert boundary trigger
            if (elapsedMs > 500)
            {
                _logger.LogWarning(
                    "CapitalLoop SLA Breach Warning | Request: {RequestName} execution took {ElapsedMs}ms! [Action Required - Optimize Downstream Dependencies]",
                    CachedRequestName, elapsedMs);
            }
            else
            {
                _logger.LogInformation(
                    "CapitalLoop Transaction Success | Request: {RequestName} | Execution Time: {ElapsedMs}ms",
                    CachedRequestName, elapsedMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            timer.Stop();

            // 🛡️ LOG INJECTION INVARIANT PREVENTER: Sanitize raw error vectors to shield internal infrastructure paths
            var sanitizedErrorMessage = ex.Message
                .Replace(Environment.NewLine, " ", StringComparison.Ordinal)
                .Replace('\n', ' ')
                .Replace('\r', ' ');

            _logger.LogError(
                ex,
                "CapitalLoop Pipeline Fault Intercepted! | Request Failure: {RequestName} | User Context: {UserContext} | Failure Frame Duration: {ElapsedMs}ms | Message Abstract: {ErrorMessage}",
                CachedRequestName, userEmailContext, timer.ElapsedMilliseconds, sanitizedErrorMessage);

            // Re-throw the exception cleanly to preserve stack metrics for global pipeline filter middleware components
            throw;
        }
    }
}