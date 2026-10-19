using Application.Common.Interfaces;
using Application.Portfolios.Commands.SubmitKycDocuments;
using Application.Portfolios.Commands.UpdateKycPayload;
using Infrastructure.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers;

[Authorize]
public class KycController : BaseApiController
{
    private readonly ICommandHandler<SubmitKycDocumentsCommand, KycSubmissionResultDto> _submitKycHandler;
    private readonly ICommandHandler<UpdateKycPayloadCommand, bool> _legacyKycHandler;
    private readonly IApplicationDbContext _context;

    public KycController(
        ICommandHandler<SubmitKycDocumentsCommand, KycSubmissionResultDto> submitKycHandler,
        ICommandHandler<UpdateKycPayloadCommand, bool> legacyKycHandler,
        IApplicationDbContext context)
    {
        _submitKycHandler = submitKycHandler;
        _legacyKycHandler = legacyKycHandler;
        _context = context;
    }

    /// <summary>
    /// Returns the authenticated investor's current identity profile, SECP KYC status, and latest submitted document metadata.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetKycStatus(CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();

        var profile = await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (profile == null)
            return NotFound(new { message = "Citizen investor profile not found in registry." });

        var latestDoc = await _context.KycDocuments
            .AsNoTracking()
            .Where(d => d.UserEmail == normalizedEmail)
            .OrderByDescending(d => d.SubmittedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            email = profile.Email,
            name = profile.Name,
            kycStatus = profile.KycStatus.ToString(),
            isPremium = profile.IsPremiumUser,
            role = "Investor",
            latestDocument = latestDoc != null ? new
            {
                documentType = latestDoc.DocumentType,
                documentNumber = latestDoc.DocumentNumber,
                submittedUtc = latestDoc.SubmittedUtc,
                storagePath = latestDoc.StorageUrlPath
            } : null
        });
    }

    /// <summary>
    /// SECP & NADRA compliant KYC submission endpoint verifying 13-digit Smart CNIC and adult investor age (18+).
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitKyc([FromBody] SubmitKycRequest request, CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var command = new SubmitKycDocumentsCommand(
            userEmail,
            request.DocumentType,
            request.DocumentNumber,
            request.DateOfBirth,
            request.DocumentFileBase64
        );

        var result = await _submitKycHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Backward-compatible legacy document submission endpoint.
    /// </summary>
    [HttpPost("submit-documents")]
    public async Task<IActionResult> SubmitKycDocumentsLegacy([FromBody] UpdateKycPayloadCommand command, CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var secureCommand = command with { UserEmail = userEmail };

        var result = await _legacyKycHandler.HandleAsync(secureCommand, cancellationToken);
        if (!result) return NotFound("User identity baseline profile was not discovered.");
        return Ok(new { Status = "PendingReview", Message = "Compliance metrics filed successfully." });
    }
}

public record SubmitKycRequest(
    string DocumentType,
    string DocumentNumber,
    DateOnly DateOfBirth,
    string? DocumentFileBase64 = null
);
