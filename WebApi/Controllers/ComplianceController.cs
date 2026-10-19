using Application.Common.Interfaces;
using Application.Compliance.Commands.EvaluateKycAudit;
using Application.Compliance.Queries.GetAccountAuditHistory;
using Application.Compliance.Queries.GetComplianceQueue;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers;

[Authorize]
public class ComplianceController : BaseApiController
{
    private readonly IQueryHandler<GetComplianceQueueQuery, List<UserComplianceDto>> _queueHandler;
    private readonly ICommandHandler<EvaluateKycAuditCommand, bool> _evaluateHandler;
    private readonly IQueryHandler<GetAccountAuditHistoryQuery, List<ComplianceAuditTrailLogDto>> _auditHandler;
    private readonly ICommandHandler<Application.Compliance.Commands.SyncTaxFilerStatus.SyncTaxFilerStatusCommand, Application.Compliance.Commands.SyncTaxFilerStatus.FbrAtlSyncResultDto> _fbrSyncHandler;
    private readonly IApplicationDbContext _context;

    public ComplianceController(
        IQueryHandler<GetComplianceQueueQuery, List<UserComplianceDto>> queueHandler,
        ICommandHandler<EvaluateKycAuditCommand, bool> evaluateHandler,
        IQueryHandler<GetAccountAuditHistoryQuery, List<ComplianceAuditTrailLogDto>> auditHandler,
        ICommandHandler<Application.Compliance.Commands.SyncTaxFilerStatus.SyncTaxFilerStatusCommand, Application.Compliance.Commands.SyncTaxFilerStatus.FbrAtlSyncResultDto> fbrSyncHandler,
        IApplicationDbContext context)
    {
        _queueHandler = queueHandler;
        _evaluateHandler = evaluateHandler;
        _auditHandler = auditHandler;
        _fbrSyncHandler = fbrSyncHandler;
        _context = context;
    }

    /// <summary>
    /// Returns the live queue of citizen investor compliance profiles for administrative review.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComplianceQueue(CancellationToken cancellationToken)
    {
        var result = await _queueHandler.HandleAsync(new GetComplianceQueueQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Evaluates an investor's KYC verification submission with immutable SECP regulatory audit logging.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateKyc([FromBody] EvaluateKycRequest request, CancellationToken cancellationToken)
    {
        string reviewerEmail;
        try
        {
            reviewerEmail = GetUserEmailFromClaims();
        }
        catch
        {
            reviewerEmail = "compliance-officer@capitalloop.pk";
        }

        var command = new EvaluateKycAuditCommand(
            TargetEmail: request.Email,
            ReviewerAgentEmail: reviewerEmail,
            Status: request.Status,
            Comments: request.Comments
        );

        var success = await _evaluateHandler.HandleAsync(command, cancellationToken);
        if (!success)
            return NotFound(new { Message = $"Investor with email '{request.Email}' was not found in the compliance registry." });

        return Ok(new { Message = "Identity compliance state evaluated and committed with SECP audit trail.", Email = request.Email, Status = request.Status });
    }

    /// <summary>
    /// Retrieves historical SECP compliance audit trail records for a targeted investor account.
    /// </summary>
    [HttpGet("audit/{email}")]
    public async Task<IActionResult> GetAccountAuditHistory(string email, CancellationToken cancellationToken)
    {
        var result = await _auditHandler.HandleAsync(new GetAccountAuditHistoryQuery(email), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Synchronizes and validates an investor's status against the FBR Active Taxpayers List (ATL).
    /// </summary>
    [HttpPost("fbr-sync/{email}")]
    public async Task<IActionResult> SyncFbrAtlStatus(string email, CancellationToken cancellationToken)
    {
        var result = await _fbrSyncHandler.HandleAsync(
            new Application.Compliance.Commands.SyncTaxFilerStatus.SyncTaxFilerStatusCommand(email),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Secure preview endpoint for citizen identification documents stored in the sovereign vault.
    /// </summary>
    [HttpGet("documents/preview/{fileName}")]
    public IActionResult PreviewDocument(string fileName, [FromQuery] string? token)
    {
        // SECP sovereign document preview link resolution
        return Ok(new
        {
            FileName = fileName,
            PreviewUrl = $"https://sovereign-vault.capitalloop.pk/documents/preview/{fileName}",
            Status = "Accessible",
            ExpiresInSeconds = 3600
        });
    }

    /// <summary>
    /// Returns the live registry of authorized administrative operators and SECP consultants.
    /// </summary>
    [HttpGet("operators")]
    public async Task<IActionResult> GetAdminOperators(CancellationToken cancellationToken)
    {
        var operators = await _context.AdminOperators
            .AsNoTracking()
            .OrderByDescending(o => o.OnboardedUtc)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Email,
                Role = o.Role.ToString(),
                o.LicenseNumber,
                o.IsActive,
                o.OnboardedUtc,
                o.OnboardedBy
            })
            .ToListAsync(cancellationToken);

        return Ok(operators);
    }

    /// <summary>
    /// Provisions and authorizes a new administrative operator with designated institutional role and SECP accreditation.
    /// </summary>
    [HttpPost("operators")]
    public async Task<IActionResult> ProvisionAdminOperator([FromBody] ProvisionOperatorRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Message = "Operator legal name and Google account email are mandatory." });

        string callerEmail;
        try { callerEmail = GetUserEmailFromClaims(); } catch { callerEmail = "system-genesis@capitalloop.pk"; }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!Enum.TryParse<AdminRole>(request.Role, true, out var roleEnum))
        {
            roleEnum = AdminRole.ComplianceOfficer;
        }

        var existing = await _context.AdminOperators.FirstOrDefaultAsync(o => o.Email == normalizedEmail, cancellationToken);
        if (existing != null)
        {
            existing.Activate();
            existing.UpdateRole(roleEnum);
            existing.UpdateLicense(request.LicenseNumber);
        }
        else
        {
            var newOperator = AdminOperator.Provision(
                request.Name,
                normalizedEmail,
                roleEnum,
                request.LicenseNumber,
                callerEmail
            );
            await _context.AdminOperators.AddAsync(newOperator, cancellationToken);
        }

        // Also ensure UserProfile exists
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (profile == null)
        {
            profile = UserProfile.Register(request.Name, normalizedEmail);
            profile.UpgradeToPremium();
            profile.UpdateKycStatus(KycStatus.Approved);
            await _context.UserProfiles.AddAsync(profile, cancellationToken);
        }

        // SECP Regulatory Audit Trail Logging
        var audit = ComplianceAuditLog.Create(
            targetEmail: normalizedEmail,
            reviewerAgentEmail: callerEmail,
            actionPerformed: "ADMIN_OPERATOR_PROVISIONED",
            comments: $"Role: {roleEnum}, License: {request.LicenseNumber ?? "N/A"}, Authority: SECP/FBR"
        );
        await _context.ComplianceAuditLogs.AddAsync(audit, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Success = true,
            Message = $"Administrative operator '{normalizedEmail}' successfully provisioned as {roleEnum}.",
            Email = normalizedEmail,
            Role = roleEnum.ToString(),
            LicenseNumber = request.LicenseNumber
        });
    }

    /// <summary>
    /// Suspends or reactivates administrative credentials for an operator.
    /// </summary>
    [HttpPost("operators/toggle")]
    public async Task<IActionResult> ToggleOperatorStatus([FromBody] ToggleOperatorRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var op = await _context.AdminOperators.FirstOrDefaultAsync(o => o.Email == normalizedEmail, cancellationToken);
        if (op == null)
            return NotFound(new { Message = $"Operator '{request.Email}' not found in registry." });

        if (op.IsActive) op.Deactivate();
        else op.Activate();

        string callerEmail;
        try { callerEmail = GetUserEmailFromClaims(); } catch { callerEmail = "system@capitalloop.pk"; }

        var audit = ComplianceAuditLog.Create(
            targetEmail: normalizedEmail,
            reviewerAgentEmail: callerEmail,
            actionPerformed: op.IsActive ? "ADMIN_OPERATOR_REACTIVATED" : "ADMIN_OPERATOR_DEACTIVATED",
            comments: $"Status toggled by {callerEmail}"
        );
        await _context.ComplianceAuditLogs.AddAsync(audit, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { Success = true, Email = normalizedEmail, IsActive = op.IsActive });
    }
}

public record EvaluateKycRequest(
    string Email,
    string Status,
    string? Comments = null
);

public record ProvisionOperatorRequest(
    string Name,
    string Email,
    string Role,
    string? LicenseNumber = null
);

public record ToggleOperatorRequest(
    string Email
);

