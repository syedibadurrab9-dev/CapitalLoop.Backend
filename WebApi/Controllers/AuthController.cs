using Application.Portfolios.Commands.GoogleSso;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Controllers;

public class GoogleSsoRequest
{
    [Required]
    public string Token { get; set; } = string.Empty; // Holds either the Mobile idToken OR the Web raw Auth Code

    public string? CodeVerifier { get; set; } // 🚀 Required for secure Angular Web applications using PKCE validation
}

public class AuthController : BaseApiController
{
    // MediatR sender instance is automatically provided by inheriting from BaseApiController

    [HttpPost("google-sso")]
    public async Task<IActionResult> GoogleSso([FromBody] GoogleSsoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Google identity token payload parameter (idToken/AuthCode) is required." });

        try
        {
            // 🚀 FIXED: Now mapping both request parameters into the updated CQRS Command constructor
            var result = await Mediator.Send(new GoogleSsoCommand(request.Token, request.CodeVerifier), cancellationToken);

            // Returns a single unified response payload that satisfies both Redux (mobile) and local storage (web) hydrations
            return Ok(new
            {
                accessToken = result.AccessToken,
                token = result.AccessToken, // Fallback property to match Angular app token mappings perfectly
                name = result.Name,
                email = result.Email,
                tier = result.Tier,
                isPremium = result.IsPremium,
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