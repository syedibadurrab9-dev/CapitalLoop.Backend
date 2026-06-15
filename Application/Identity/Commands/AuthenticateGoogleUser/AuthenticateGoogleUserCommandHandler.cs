using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Portfolios.Commands;

public class AuthenticateGoogleUserCommandHandler : IRequestHandler<AuthenticateGoogleUserCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;

    public AuthenticateGoogleUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }

    public async Task<AuthResultDto> Handle(AuthenticateGoogleUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.IdToken))
        {
            throw new ArgumentException("Ecosystem entry rejected: Token cannot be empty.", nameof(command));
        }

        // 📝 DEBUG TRACE 1: Confirm command entry
        Console.WriteLine($"[DEBUG] MediatR Handler reached. Token Length: {command.IdToken.Length}");

        try
        {
            Console.WriteLine("[DEBUG] Entering _identityService.ValidateGoogleTokenAndGenerateJwtAsync...");

            // 🎯 SUSPECT NODE: If the app hangs here for 15 seconds, your backend cannot reach Google's validation servers
            var authResult = await _identityService.ValidateGoogleTokenAndGenerateJwtAsync(
                command.IdToken,
                command.CodeVerifier,
                cancellationToken);

            Console.WriteLine("[DEBUG] Outbound identity validation call finished successfully.");

            if (authResult is null || string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                Console.WriteLine("[DEBUG] Validation failed: AuthResult or AccessToken is null.");
                throw new UnauthorizedAccessException("Federated Google token validation failed.");
            }

            Console.WriteLine("[DEBUG] AuthResult issued successfully. Sending back payload...");
            return authResult;
        }
        catch (Exception ex)
        {
            // 🚨 DEBUG TRACE 2: Catch hidden internal network/crypto failures
            Console.WriteLine($"[CRITICAL EXCEPTION] Identity service failed: {ex.Message}");
            Console.WriteLine($"[STACK TRACE] {ex.StackTrace}");
            throw;
        }
    }
}