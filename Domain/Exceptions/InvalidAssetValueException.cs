using System;

namespace Domain.Exceptions;

/// <summary>
/// Thrown when an asset transaction, ledger entry, or commodity weight parameter 
/// violates core numeric invariants or evaluates into an illegal negative boundary sum.
/// </summary>
public class InvalidAssetValueException : Exception
{
    public InvalidAssetValueException()
        : base("Wealth management asset fields balance configurations cannot hold negative parameters.") { }

    public InvalidAssetValueException(string message)
        : base(message) { }

    public InvalidAssetValueException(string message, Exception innerException)
        : base(message, innerException) { }
}