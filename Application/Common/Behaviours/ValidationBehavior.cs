using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using ValidationException = FluentValidation.ValidationException;

namespace Application.Common.Behaviours;

/// <summary>
/// Cross-Cutting MediatR Pipeline Behavior that automatically intercepts incoming request payloads,
/// executes all corresponding FluentValidation discovery rules concurrently, and blocks execution 
/// before invalid states can reach underlying transactional domain handlers [CL-102].
/// Optimized to minimize heap allocations and garbage collection overhead under heavy transaction loads.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull // Aligned with modern MediatR constraint definitions
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly bool _hasValidators;

    /// <summary>
    /// Initializes a new instance of the ValidationBehavior pipeline interceptor.
    /// Evaluates collection presence metrics on construction to optimize subsequent runtime execution.
    /// </summary>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));

        // Cache condition tracking parameter to bypass lookup allocations inside the active hot path
        _hasValidators = _validators.Any();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        // 🚀 HOT-PATH FAST RETURN OPTIMIZATION: Bypass structural evaluation if no rules are registered
        if (!_hasValidators)
        {
            return await next();
        }

        // Instantiate the validation aggregate cell boundary explicitly now that existence is verified
        var context = new ValidationContext<TRequest>(request);

        // 🛡️ ALLOCATION HARDENING: Optimize structural checks based on validator pool depth 
        List<ValidationFailure> failures;

        // Extract list wrapper natively if available to skip LINQ enumerable iterator allocations
        if (_validators is IReadOnlyList<IValidator<TRequest>> listPool)
        {
            if (listPool.Count == 1)
            {
                // Index-based retrieval bypasses .First() iterator heap allocations entirely
                var result = await listPool[0].ValidateAsync(context, cancellationToken);
                failures = result.Errors.Count != 0
                    ? result.Errors.Where(f => f is not null).ToList()
                    : [];
            }
            else
            {
                // Process multiple pre-allocated items concurrently
                var tasks = new Task<ValidationResult>[listPool.Count];
                for (int i = 0; i < listPool.Count; i++)
                {
                    tasks[i] = listPool[i].ValidateAsync(context, cancellationToken);
                }

                var validationResults = await Task.WhenAll(tasks);
                failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f is not null)
                    .ToList();
            }
        }
        else
        {
            // Fallback strategy if data structures are provided as raw standard enumerables
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();
        }

        // 🚨 REGULATORY BUSINESS RULE BARRIER: Block pipeline execution and bubble up array trace records
        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}