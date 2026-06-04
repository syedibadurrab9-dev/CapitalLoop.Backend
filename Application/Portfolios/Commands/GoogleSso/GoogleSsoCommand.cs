using MediatR;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Portfolios.Commands.GoogleSso;

// 🚀 UPDATED: Record structural blueprint now securely tracks both the Token and optional CodeVerifier
public record GoogleSsoCommand(string Token, string? CodeVerifier) : IRequest<AuthResultDto>;

public class GoogleSsoCommandHandler : IRequestHandler<GoogleSsoCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;

    public GoogleSsoCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResultDto> Handle(GoogleSsoCommand request, CancellationToken cancellationToken)
    {
        // 🚀 UPDATED: Passes both state parameters cleanly across layer boundaries into the identity engine
        return await _identityService.ValidateGoogleTokenAndGenerateJwtAsync(
            request.Token,
            request.CodeVerifier,
            cancellationToken);
    }
}