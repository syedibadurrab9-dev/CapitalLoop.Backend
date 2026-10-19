namespace Domain.Exceptions;

/// <summary>
/// Thrown when an onboarding investor violates SECP/SBP underage restrictions (under 18 years old).
/// </summary>
public class UnderageInvestorException : DomainException
{
    public UnderageInvestorException(string message) : base(message) { }
}
