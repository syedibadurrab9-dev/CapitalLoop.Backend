using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Common.Behaviours;
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("CapitalLoop Pipeline Execution Initialized: {RequestName}", requestName);

        var response = await next();

        _logger.LogInformation("CapitalLoop Pipeline Execution Completed Successfully: {RequestName}", requestName);
        return response;
    }
}