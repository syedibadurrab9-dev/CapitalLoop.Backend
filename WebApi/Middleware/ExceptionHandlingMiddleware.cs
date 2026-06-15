using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApi.Middleware;

/// <summary>
/// 🛡️ GLOBAL APPLICATION EXCEPTION INTERCEPTOR
/// Catches, standardizes, and serializes structural validations and domain compliance faults globally.
/// Generates standardized RFC 7807 ProblemDetails payloads to secure back-channel logging vectors.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GLOBAL-EXCEPTION-HANDLER] An unhandled exception escaped request execution pipeline.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails();

        // Dynamic Exception Feature Branch Matrix Evaluation Pattern
        switch (exception)
        {
            // 🧱 CASE 1: Fluent Validation Failures (Bad Incoming Request Contract Models)
            case FluentValidation.ValidationException valEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation Mismatch Validation Errors";
                problemDetails.Detail = "One or more incoming fields violated structural schema constraints rules.";

                // 🛠️ FIXED: Group errors cleanly by PropertyName to prevent masking multiple errors on the same field
                var validationErrors = valEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                problemDetails.Extensions["errors"] = validationErrors;
                break;

            // ⚖️ CASE 2: Domain Layer Compliance Rule Infractions (Shariah Restrictions, Overdrafts, Stale Slices)
            case InvalidOperationException domainEx when domainEx.Message.Contains("DomainException", StringComparison.OrdinalIgnoreCase)
                                                      || domainEx.Message.Contains("Invariant", StringComparison.OrdinalIgnoreCase):
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                problemDetails.Title = "Structural System Invariant Infraction";
                problemDetails.Detail = exception.Message;
                break;

            // 🔒 CASE 3: Security & Access Right Breaches
            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                problemDetails.Status = StatusCodes.Status401Unauthorized;
                problemDetails.Title = "Authentication Credential Failure";
                problemDetails.Detail = "The requested operating boundary demands secure validation parameters.";
                break;

            // 💥 CASE 4: Standard Global Unhandled Internal Errors Fallback Handler
            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Processing Interruption";
                problemDetails.Detail = _env.IsDevelopment()
                    ? exception.Message
                    : "An unhandled exception blocked thread execution parameters inside CapitalLoop core frameworks.";
                break;
        }

        // Add instance context routing trail
        problemDetails.Instance = context.Request.Path;

        // Serialize output into standardized json format stream profiles
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var jsonResult = JsonSerializer.Serialize(problemDetails, options);

        return context.Response.WriteAsync(jsonResult);
    }
}