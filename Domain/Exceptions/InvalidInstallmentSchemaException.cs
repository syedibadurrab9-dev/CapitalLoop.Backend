namespace Domain.Exceptions;

public class InvalidInstallmentSchemaException : DomainException
{
    public InvalidInstallmentSchemaException(string message) : base(message) { }
}
