namespace Domain.Exceptions;

public class InvalidAssetValueException : DomainException
{
    public InvalidAssetValueException(string message) : base(message) { }
}
