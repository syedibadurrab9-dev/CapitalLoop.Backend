using Application.Common.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IIdentityService
{
    /// <summary>
    /// Validates an incoming federated Google identity assertion (either a raw mobile idToken or a web authorization code), 
    /// executes back-channel server exchanges if required, and returns a unified application security token payload.
    /// </summary>
    /// <param name="incomingPayload">The raw cryptographic identity assertion (idToken) from mobile devices or the Authorization Code string from web applications.</param>
    /// <param name="codeVerifier">The optional transient plain-text cryptographic key used to complete server-side PKCE verification constraints for public web clients.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>An encapsulated <see cref="AuthResultDto"/> container containing access tokens, profile metrics, and system tiers.</returns>
    Task<AuthResultDto> ValidateGoogleTokenAndGenerateJwtAsync(
        string incomingPayload,
        string? codeVerifier,
        string? redirectUri,
        CancellationToken cancellationToken);
}