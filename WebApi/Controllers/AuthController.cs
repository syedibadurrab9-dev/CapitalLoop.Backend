using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Portfolios.Commands.GoogleSso;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Controllers;

public class GoogleSsoRequest
{
    [Required]
    public string Token { get; set; } = string.Empty; // Holds either the Mobile idToken OR the Web raw Auth Code

    public string? CodeVerifier { get; set; } // Required for secure Angular Web applications using PKCE validation
    public string? RedirectUri { get; set; } // Optional redirect URI if exchanging auth code via PKCE
}

public class AuthController : BaseApiController
{
    private readonly ICommandHandler<GoogleSsoCommand, AuthResultDto> _googleSsoHandler;

    public AuthController(ICommandHandler<GoogleSsoCommand, AuthResultDto> googleSsoHandler)
    {
        _googleSsoHandler = googleSsoHandler;
    }

    [HttpPost("google-sso")]
    public async Task<IActionResult> GoogleSso([FromBody] GoogleSsoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Google identity token payload parameter (idToken/AuthCode) is required." });

        try
        {
            var redirectUri = request.RedirectUri;
            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                var origin = Request.Headers.Origin.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(origin))
                {
                    redirectUri = $"{origin.TrimEnd('/')}/login";
                }
            }

            var result = await _googleSsoHandler.HandleAsync(
                new GoogleSsoCommand(request.Token, request.CodeVerifier, redirectUri),
                cancellationToken);

            // Returns a single unified response payload that satisfies both Redux (mobile) and local storage (web) hydrations
            return Ok(new
            {
                accessToken = result.AccessToken,
                token = result.AccessToken, // Fallback property to match Angular app token mappings perfectly
                name = result.Name,
                email = result.Email,
                tier = result.Tier,
                isPremium = result.IsPremium,
                role = result.Role,
                licenseNumber = result.LicenseNumber,
                kycStatus = result.KycStatus,
                success = result.Success
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Internal federated identity pipeline orchestration failure.",
                error = ex.Message
            });
        }
    }
}