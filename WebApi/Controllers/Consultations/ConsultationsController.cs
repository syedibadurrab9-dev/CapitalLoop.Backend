using System;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Consultations.Commands.AssignConsultant;
using Application.Consultations.Commands.BookConsultationSlot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;
using WebApi.Filters;

namespace WebApi.Controllers.Consultations;

/// <summary>
/// 🏛️ ADVISOR WORKSPACE & EXPERT CONSULTATION GATEWAY
/// Coordinates early-funnel expert calendar slot bookings and administrative allocation routines [CL-502].
/// </summary>
[Authorize]
public class ConsultationsController : ApiControllerBase
{
    private readonly IIdentityService _identityService;

    public ConsultationsController(IIdentityService identityService)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }

    // =============================================================================
    // 🎛️ COMMAND ROUTING CHANNELS (STATE CHANGING OPERATIONS)
    // =============================================================================

    /// <summary>
    /// Reserves a dedicated advisory calendar slot mapping back to professional consulting ledgers [CL-502].
    /// </summary>
    [HttpPost("book-slot")]
    [SubscriptionRequirement("On_Demand_Advisor_Consultations")] // 🔒 FEATURE GATE: Handled by advisor-wealth / agent-backoffice policies
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BookConsultationSlot([FromBody] BookConsultationSlotCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 🎯 SECURITY ALIGNMENT: Intercepts active HTTP request token contexts to guarantee multi-tenant identity verification
        var authenticatedUserEmail = _identityService.GetUserEmailFromPrincipal(User);

        // 🛠️ FIXED CS0117: Realigned mutation parameter from 'UserEmail' over to 'ProspectEmail' 
        // to match your explicit positional record constructor arguments flawlessly.
        var synchronizedCommand = command with
        {
            ProspectEmail = authenticatedUserEmail
        };

        // Dispatches the mutated, authenticated command safely down to its corresponding Application layer handler
        var appointmentId = await Mediator.Send(synchronizedCommand);
        return Ok(appointmentId);
    }

    /// <summary>
    /// Administrative back-office routing channel to assign specialized investment consultants to user portfolios.
    /// </summary>
    [Authorize(Roles = "Admin, AdvisorManager")] // Locked down to specialized administrative identities
    [HttpPost("assign-consultant")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    public async Task<IActionResult> AssignConsultant([FromBody] AssignConsultantCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Ok(await Mediator.Send(command));
    }
}