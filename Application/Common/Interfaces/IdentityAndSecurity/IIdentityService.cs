using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.DTOs;

namespace Application.Common.Interfaces;

/// <summary>
/// 🔐 DOMAIN INFRASTRUCTURE SECURITY CONTRACT
/// Orchestrates federated Google Single Sign-On (SSO) assertion validation exchanges, 
/// cryptographic JWT token generation, real-time subscription privilege auditing, 
/// and stateful session management across the CapitalLoop ecosystem [CL-101 / CL-503].
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Processes federated Google SSO tokens, handles back-channel crypto validation, and issues platform JWT footprints.
    /// Natively utilized by AuthenticateGoogleUserCommand.
    /// </summary>
    /// <param name="token">The raw cryptographic identity assertion token (idToken) streamed from client nodes.</param>
    /// <returns>An encapsulated AuthResultDto container housing access tokens, profile records, and tier configurations.</returns>
    Task<AuthResultDto> AuthenticateWithGoogleAsync(string token);

    /// <summary>
    /// Back-office admin tool that updates subscriber plans, alters identity security claims, and evicts active cache maps.
    /// Natively utilized by OverrideUserPlanCommand.
    /// </summary>
    /// <param name="userId">The unique identifier anchor of the target security principal.</param>
    /// <param name="tier">The target subscription tier configuration constant to inject into the claim matrix (e.g., "Premium").</param>
    /// <returns>A comprehensive layout holding override state indicators and freshly minted claim data summaries.</returns>
    Task<SubscriptionOverrideResultDto> UpdateUserClaimsAsync(string userId, string tier);

    /// <summary>
    /// Validates an incoming federated Google identity token, executes back-channel 
    /// server verification exchanges, hydrates local user profiles, and issues a unified application security token payload [CL-101].
    /// </summary>
    Task<AuthResultDto> ValidateGoogleTokenAndGenerateJwtAsync(
        string tokenPayload,
        string? codeVerifier = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves and extracts the current user email context directly from an active claims principal collection.
    /// </summary>
    string GetUserEmailFromPrincipal(ClaimsPrincipal principal);

    /// <summary>
    /// 🏢 INFRASTRUCTURE HOOK UTILITY: Resolves the active user email directly from the ambient HTTP context.
    /// Injected directly inside your ApplicationDbContext to drive zero-argument automated database save interceptors.
    /// </summary>
    /// <returns>The lowercase email string of the active security principal; returns null if the execution trace is anonymous or system-driven.</returns>
    string? GetCurrentUserEmail();

    /// <summary>
    /// Intercepts active request context footprints to verify if the executing user 
    /// account maintains proper subscription tier access authorization [CL-503].
    /// </summary>
    Task<string> GetActiveSubscriptionTierAsync(string userEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a refreshed, cryptographically signed JWT token following a successful 
    /// manual or automated back-office workspace subscription tier change [CL-503].
    /// </summary>
    Task<string> RegenerateJwtForTierOverrideAsync(string userEmail, string targetPlanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly evicts an active identity session from cache frameworks and distributed storage blocks.
    /// </summary>
    Task InvalidateUserSessionAsync(string userEmail, CancellationToken cancellationToken = default);
}