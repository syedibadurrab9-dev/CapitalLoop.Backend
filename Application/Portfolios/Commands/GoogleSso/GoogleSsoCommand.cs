using Application.Common.DTOs;
using Application.Common.Interfaces;

namespace Application.Portfolios.Commands.GoogleSso;

public record GoogleSsoCommand(string Token, string? CodeVerifier, string? RedirectUri = null) : ICommand<AuthResultDto>;

public class GoogleSsoCommandHandler : ICommandHandler<GoogleSsoCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;

    public GoogleSsoCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResultDto> HandleAsync(GoogleSsoCommand command, CancellationToken cancellationToken)
    {
        return await _identityService.ValidateGoogleTokenAndGenerateJwtAsync(
            command.Token,
            command.CodeVerifier,
            command.RedirectUri,
            cancellationToken);
    }
}