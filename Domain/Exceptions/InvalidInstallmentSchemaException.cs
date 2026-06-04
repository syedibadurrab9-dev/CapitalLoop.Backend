namespace Domain.Exceptions;

public class InvalidInstallmentSchemaException : Exception
{
    public InvalidInstallmentSchemaException(string message) : base(message) { }
}
