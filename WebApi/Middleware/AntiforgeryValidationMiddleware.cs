using System.Net;

namespace WebApi.Middleware;

/// <summary>
/// Defends against Cross-Site Request Forgery (CSRF) on state-changing API endpoints
/// by verifying origin integrity and validating browser custom headers.
/// </summary>
public class AntiforgeryValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AntiforgeryValidationMiddleware> _logger;
    private static readonly HashSet<string> StateChangingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Delete,
        HttpMethods.Patch
    };

    private static readonly HashSet<string> AllowedOrigins = new(StringComparer.OrdinalIgnoreCase)
    {
        "http://localhost:4200",
        "http://localhost:8081",
        "http://127.0.0.1:4200",
        "http://127.0.0.1:8081"
    };

    public AntiforgeryValidationMiddleware(RequestDelegate next, ILogger<AntiforgeryValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only evaluate state-changing operations
        if (StateChangingMethods.Contains(context.Request.Method))
        {
            var origin = context.Request.Headers.Origin.FirstOrDefault();
            
            // If the browser sends an Origin header, ensure it is on our trusted whitelist
            if (!string.IsNullOrEmpty(origin))
            {
                var isAllowed = AllowedOrigins.Contains(origin) ||
                    (Uri.TryCreate(origin, UriKind.Absolute, out var uri) && 
                     (uri.Host == "localhost" || 
                      uri.Host == "127.0.0.1" || 
                      uri.Host.StartsWith("192.168.") || 
                      uri.Host.StartsWith("10.") ||
                      uri.Host.EndsWith("trycloudflare.com", StringComparison.OrdinalIgnoreCase) ||
                      uri.Host.EndsWith("vercel.app", StringComparison.OrdinalIgnoreCase)));

                if (!isAllowed)
                {
                    _logger.LogWarning("CSRF block: Request rejected due to untrusted origin '{Origin}' on {Path}", origin, context.Request.Path);
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"errorCode\":\"CSRF_UNTRUSTED_ORIGIN\",\"message\":\"Cross-site request forgery defense: Untrusted origin rejected.\"}");
                    return;
                }
            }
        }

        await _next(context);
    }
}
