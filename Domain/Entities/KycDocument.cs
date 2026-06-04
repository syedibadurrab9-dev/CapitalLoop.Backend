using Domain.Common;

namespace Domain.Entities;

public class KycDocument : Entity
{
    public string UserEmail { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty; // e.g., "Smart_CNIC" or "Passport"
    public string DocumentNumber { get; private set; } = string.Empty; // National registry values
    public string StorageUrlPath { get; private set; } = string.Empty; // Secure cloud bucket path
    public DateTime SubmittedUtc { get; private set; }

    private KycDocument() { }

    public static KycDocument RecordSubmission(string email, string type, string docNum, string path)
    {
        return new KycDocument
        {
            Id = Guid.NewGuid(),
            UserEmail = email.ToLowerInvariant(),
            DocumentType = type,
            DocumentNumber = docNum,
            StorageUrlPath = path,
            SubmittedUtc = DateTime.UtcNow
        };
    }
}
