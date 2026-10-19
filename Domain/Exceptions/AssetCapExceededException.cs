namespace Domain.Exceptions;

public class AssetCapExceededException : DomainException
{
    public AssetCapExceededException(string message) : base(message) { }
}
