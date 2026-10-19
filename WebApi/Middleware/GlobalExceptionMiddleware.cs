using Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace WebApi.Middleware;

/// <summary>
/// Centralized RFC 7807-compliant exception handler providing deterministic error mapping,
/// preventing sensitive stack leakages, and structuring validation errors cleanly.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, errorCode, message, details) = exception switch
        {
            ValidationException valEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                "One or more validation constraints were violated.",
                (object)valEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            UnauthorizedAccessException unauthEx => (
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED_ACCESS",
                unauthEx.Message,
                (object?)null
            ),
            DomainException domainEx => (
                HttpStatusCode.UnprocessableEntity,
                "DOMAIN_RULE_VIOLATION",
                domainEx.Message,
                (object?)null
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "SERVER_FAULT",
                "An unexpected internal error occurred. Please contact support.",
                _environment.IsDevelopment() ? (object)exception.ToString() : null
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled system fault caught by GlobalExceptionMiddleware: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Controlled request rejection [{StatusCode}]: {Message}", statusCode, exception.Message);
        }

        context.Response.StatusCode = (int)statusCode;

        var problemPayload = new
        {
            type = $"https://capitalloop.pk/errors/{errorCode.ToLowerInvariant()}",
            title = errorCode,
            status = (int)statusCode,
            detail = message,
            instance = context.Request.Path.Value,
            errors = details
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemPayload, jsonOptions));
    }
}
