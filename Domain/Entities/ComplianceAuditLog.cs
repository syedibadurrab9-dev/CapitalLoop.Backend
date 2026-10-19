using Domain.Common;

namespace Domain.Entities;

public class ComplianceAuditLog : Entity
{
    public string TargetEmail { get; private set; } = string.Empty;
    public string ReviewerAgentEmail { get; private set; } = string.Empty;
    public string ActionPerformed { get; private set; } = string.Empty; // "KYC_APPROVE", "KYC_REJECT", "DOCUMENT_RE-REQUEST"
    public string? Comments { get; private set; }
    public DateTime Timestamp { get; private set; }

    private ComplianceAuditLog() { }

    public static ComplianceAuditLog Create(
        string targetEmail,
        string reviewerAgentEmail,
        string actionPerformed,
        string? comments = null)
    {
        return new ComplianceAuditLog
        {
            Id = Guid.NewGuid(),
            TargetEmail = targetEmail.ToLowerInvariant(),
            ReviewerAgentEmail = reviewerAgentEmail.ToLowerInvariant(),
            ActionPerformed = actionPerformed,
            Comments = comments,
            Timestamp = DateTime.UtcNow
        };
    }
}
