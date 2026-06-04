namespace Domain.Exceptions;

public class AssetCapExceededException : Exception
{
    public AssetCapExceededException(string message) : base(message) { }
}
