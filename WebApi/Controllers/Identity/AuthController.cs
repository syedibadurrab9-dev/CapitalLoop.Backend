using Application.Common.DTOs;
using Application.Portfolios.Commands;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers.Identity;

/// <summary>
/// 🔐 FEDERATED IDENTITY & WORKSPACE GATEWAY
/// Entry endpoint managing Google SSO handshakes and early account setup boundaries [CL-101].
/// </summary>
public class AuthController : ApiControllerBase
{
    /// <summary>
    /// Processes federated Google SSO tokens, verifies back-channel crypto assertions, and issues JWT footprints.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("google-sso")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResultDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AuthenticateGoogleUser([FromBody] AuthenticateGoogleUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var authResult = await Mediator.Send(command);
        return Ok(authResult);
    }


    //[AllowAnonymous]
    //[HttpPost("google-sso")]
    //public async Task<IActionResult> AuthenticateGoogleUser([FromBody] AuthenticateGoogleUserCommand command)
    //{
    //    Console.WriteLine("\n=============================================================");
    //    Console.WriteLine("[DIAGNOSTIC-DEEP] Step-by-Step Container Verification Flow");
    //    Console.WriteLine("=============================================================");

    //    try
    //    {
    //        using var scope = HttpContext.RequestServices.CreateScope();
    //        var services = scope.ServiceProvider;
    //        Console.WriteLine("[TRACE 1]: Scope Built.");

    //        // 🎯 DISCOVERY TEST A: Resolve the raw DbContext Options footprint
    //        Console.WriteLine("[TRACE 2A]: Resolving DbContext Options structural mapping...");
    //        var options = services.GetService<Microsoft.EntityFrameworkCore.DbContextOptions<ApplicationDbContext>>();
    //        Console.WriteLine($"[TRACE 2A - SUCCESS]: Options loaded cleanly.");

    //        // 🎯 DISCOVERY TEST B: Resolve the concrete implementation directly
    //        Console.WriteLine("[TRACE 2B]: Attempting to pull concrete ApplicationDbContext...");
    //        var concreteContext = services.GetService<Infrastructure.Persistence.ApplicationDbContext>();
    //        Console.WriteLine($"[TRACE 2B - SUCCESS]: Concrete Context instantiated: {concreteContext?.GetType().Name}");

    //        // 🎯 DISCOVERY TEST C: Resolve via interface mapping
    //        Console.WriteLine("[TRACE 2C]: Attempting to pull interface IApplicationDbContext abstraction...");
    //        var interfaceContext = services.GetService<Application.Common.Interfaces.IApplicationDbContext>();
    //        Console.WriteLine($"[TRACE 2C - SUCCESS]: Abstraction resolved cleanly.");

    //        Console.WriteLine("[TRACE 3]: Progressing to IIdentityService check...");
    //        var identityService = services.GetRequiredService<Application.Common.Interfaces.IIdentityService>();

    //        var manualHandler = new Application.Portfolios.Commands.AuthenticateGoogleUserCommandHandler(identityService);
    //        var manualResult = await manualHandler.Handle(command, HttpContext.RequestAborted);

    //        return Ok(manualResult);
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"\n🚨 [DEEP-CRASH]: {ex.Message}");
    //        Console.WriteLine($"🚨 [STACK]: {ex.StackTrace}\n");
    //        return StatusCode(500, ex.Message);
    //    }
    //}

    /// <summary>
    /// Instantiates initial multi-tenant workspace records and compliance flags post-registration.
    /// </summary>
    [Authorize]
    [HttpPost("onboarding/initialize")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitializeOnboarding([FromBody] InitializeOnboardingCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var profileId = await Mediator.Send(command);
        return Ok(profileId);
    }
}