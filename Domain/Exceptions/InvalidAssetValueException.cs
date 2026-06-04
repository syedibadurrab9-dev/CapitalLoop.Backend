namespace Domain.Exceptions;

public class InvalidAssetValueException : Exception
{
    public InvalidAssetValueException(string message) : base(message) { }
}
