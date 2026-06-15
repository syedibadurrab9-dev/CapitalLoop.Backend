using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WebApi.Extensions;

/// <summary>
/// 🔌 MINIMAL API ROUTE REGISTRATION MATRIX
/// Maintained as an optional registration hook to map high-speed infrastructure micro-endpoints
/// or diagnostic check sweeps outside of the classic MVC controller loop [CL-415].
/// </summary>
public static class EndpointMappingExtensions
{
    /// <summary>
    /// Registers lightweight infrastructure route mappings cleanly into the main web application pipeline.
    /// </summary>
    /// <param name="endpoints">The target endpoint route builder map node container.</param>
    /// <returns>The modified route builder loop instance for fluent call chaining.</returns>
    public static IEndpointRouteBuilder MapInfrastructureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 🛠️ HEALTH CHECK DIAGNOSTIC RAIL
        // Provides an un-authenticated, zero-allocation endpoint for container live-checks or load-balancers.
        endpoints.MapGet("api/infra/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Environment = "Production",
            Ecosystem = "CapitalLoop FinTech Platform"
        }))
        .WithName("GetPlatformHealthState")
        .WithTags("InfrastructureDiagnostics");

        return endpoints;
    }
}