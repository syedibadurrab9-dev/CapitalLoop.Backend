using MediatR;

namespace Application.Compliance.Commands.SubmitKycDocuments;

/// <summary>
/// CQRS Command to upload extraction properties and transition a profile into the pending audit phase [CL-104].
/// </summary>
/// <param name="UserEmail">The authenticated email address of the client submitting identity verification data.</param>
/// <param name="Cnic">The 13-digit Pakistani National Identity Card number extracted via onshore OCR steps.</param>
/// <param name="Age">The verified chronological age of the account operator.</param>
public record SubmitKycDocumentsCommand(
    string UserEmail,
    string Cnic,
    int Age,
    string FrontImagePath,
    string BackImagePath) : IRequest<bool>;