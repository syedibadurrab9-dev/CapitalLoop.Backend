using Application.Common.DTOs;
using MediatR;

namespace Application.Portfolios.Commands;

/// <summary>
/// CQRS Command to process federated Google Single Sign-On (SSO) handshakes [CL-101].
/// Validates external identity claims and issues a unified CapitalLoop security token footprint.
/// </summary>
/// <param name="IdToken">The raw, cryptographically signed identity assertion string streamed from client endpoints.</param>
/// <param name="CodeVerifier">The optional transient string used to satisfy back-channel Proof Key for Code Exchange (PKCE) constraints.</param>
public record AuthenticateGoogleUserCommand(string IdToken, string? CodeVerifier = null) : IRequest<AuthResultDto>;