namespace Application.Common.Interfaces;

public interface IFbrTaxService
{
    Task<FbrAtlStatusResult> CheckActiveTaxpayerStatusAsync(string cnicOrNtn, CancellationToken cancellationToken = default);
}

public record FbrAtlStatusResult(
    bool IsActiveFiler,
    string CnicOrNtn,
    string StatusDescription,
    DateTime VerifiedAtUtc
);
