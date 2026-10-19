namespace WebApi.Middleware;

/// <summary>
/// Enforces hardened HTTP security headers (CSP, HSTS, X-Frame-Options, XSS protection, anti-sniffing)
/// on every outbound response across the Capital Loop gateway.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // 1. Prevent MIME-type sniffing (CWE-79 / XSS)
            headers["X-Content-Type-Options"] = "nosniff";

            // 2. Prevent clickjacking / UI redressing (CWE-1021)
            headers["X-Frame-Options"] = "DENY";

            // 3. Legacy browser XSS filter enforcement
            headers["X-XSS-Protection"] = "1; mode=block";

            // 4. Strict referrer control across origins
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // 5. Content Security Policy (CSP) - restrict scripts, frames, and connect origins
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' https: data:; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "connect-src 'self' wss: https: http://localhost:4200 http://localhost:8081; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            // 6. Strict Transport Security (HSTS) - 1 year with subdomains
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

            // 7. Restrict unneeded browser hardware capabilities
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
