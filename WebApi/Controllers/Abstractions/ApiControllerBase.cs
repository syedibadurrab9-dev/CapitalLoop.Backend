using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Controllers.Abstractions;

/// <summary>
/// 🏛️ CENTRALIZED PRESENTATION ROUTING BASE
/// Abstract base API controller implementing automated service locating for MediatR [CL-413].
/// Eliminates parameter constructor bloat across vertical slice feature endpoints.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
// 🎯 GLOBAL SECURITY DOCUMENTATION: Automatically maps uniform error schemas across all inheritance nodes
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    /// <summary>
    /// 🚀 LAZY-LOADED MEDIATR DISPATCHER RAIL
    /// Resolves the scoped ISender pipeline out of request services context dynamically upon first execution call.
    /// </summary>
    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}