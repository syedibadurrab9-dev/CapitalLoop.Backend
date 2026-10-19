using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Safely extracts the user's validated identity email context from incoming JWT authentication claims.
    /// </summary>
    protected string GetUserEmailFromClaims()
    {
        var emailClaim = User.FindFirstValue(ClaimTypes.Email)
                         ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(emailClaim))
        {
            throw new UnauthorizedAccessException("The authenticated context contains an invalid or empty identity signature record.");
        }

        return emailClaim.ToLowerInvariant();
    }
}
