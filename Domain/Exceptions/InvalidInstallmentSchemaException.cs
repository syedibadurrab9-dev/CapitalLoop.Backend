using System;

namespace Domain.Exceptions;

/// <summary>
/// Thrown when an outstanding liability schema, real estate payment tracking plan, 
/// or automotive installment calculation violates core structural bounds or parameters.
/// </summary>
public class InvalidInstallmentSchemaException : Exception
{
    public InvalidInstallmentSchemaException()
        : base("Outstanding liability schemas or installment configurations cannot accept zero evaluation pricing metrics.") { }

    public InvalidInstallmentSchemaException(string message)
        : base(message) { }

    public InvalidInstallmentSchemaException(string message, Exception innerException)
        : base(message, innerException) { }
}