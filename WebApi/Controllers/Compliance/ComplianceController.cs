using System;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Compliance.Commands.EvaluateKycAudit;
using Application.Compliance.Commands.QueueTaxAudit;
using Application.Compliance.Commands.SubmitKycDocuments;
using Application.Compliance.Commands.SyncTaxFilerStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;
using WebApi.Filters;

namespace WebApi.Controllers.Compliance;

/// <summary>
/// ⚖️ REGULATORY COMPLIANCE & LEGAL WORKFLOW CONTROLLER
/// Coordinates onboarding KYC uploads, automated CNIC OCR auditing, tax tier validations, 
/// and out-of-process automated audit queues [CL-104 / CL-501].
/// </summary>
[Authorize]
public class ComplianceController : ApiControllerBase
{
    private readonly IIdentityService _identityService;

    public ComplianceController(IIdentityService identityService)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }

    // =============================================================================
    // 🎛️ COMMAND ROUTING CHANNELS (STATE CHANGING OPERATIONS)
    // =============================================================================

    /// <summary>
    /// Accepts physical onboarding document file streams, converting binary paths into localized cloud tracks [CL-104].
    /// </summary>
    [HttpPost("kyc/submit")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitKycDocuments([FromBody] SubmitKycDocumentsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Ok(await Mediator.Send(command));
    }

    /// <summary>
    /// Triggers automated backend OCR validation algorithms against processed regulatory credentials.
    /// </summary>
    [HttpPost("kyc/evaluate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    public async Task<IActionResult> EvaluateKycAudit([FromBody] EvaluateKycAuditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Ok(await Mediator.Send(command));
    }

    /// <summary>
    /// Background task alignment endpoint that updates local storage states using FBR verification vectors.
    /// </summary>
    [HttpPost("tax/sync-filer-status")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    public async Task<IActionResult> SyncTaxFilerStatus([FromQuery] bool isFiler)
    {
        // 🎯 SECURITY ALIGNMENT: Resolves target identity properties straight from system claims principal
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);

        // 🛠️ FIXED CS7036: Correctly maps (string UserEmail, bool IsFiler) to match your record constructor definition
        var command = new SyncTaxFilerStatusCommand(
            UserEmail: currentUserEmail,
            IsFiler: isFiler
        );

        return Ok(await Mediator.Send(command));
    }

    /// <summary>
    /// Places an asynchronous multi-asset optimization audit task into thread-isolated background memory channels [CL-501].
    /// </summary>
    [HttpPost("tax/queue-audit")]
    [SubscriptionRequirement("Tax_Audit_Compliance_Review")] // 🔒 FEATURE GATE: Restricted to tax-consultant and administrative back-office layers
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QueueTaxAudit()
    {
        // 🎯 SECURITY ALIGNMENT: Resolves initiating identity properties straight from system claims principal
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);

        // 🛠️ FIXED CS1739: Realigned parameters to match (string UserEmail, bool IsTriggeredBySystem) 
        // completely removing the unmapped JobId token to satisfy your record constraint contract.
        var messageCommand = new QueueTaxAuditCommand(
            UserEmail: currentUserEmail,
            IsTriggeredBySystem: false // Flagged as false since this is manually triggered over the HTTP API portal
        );

        // The Application layer Command handler generates and returns the persistent tracking Job ID Guid
        Guid allocatedJobId = await Mediator.Send(messageCommand);

        // Returns 202 Accepted to signal that asynchronous background computation has been successfully spawned
        return Accepted(allocatedJobId);
    }
}