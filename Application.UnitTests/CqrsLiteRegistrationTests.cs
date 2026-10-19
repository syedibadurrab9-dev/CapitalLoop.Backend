using Application;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Portfolios.Commands.CreateManualAsset;
using Application.Portfolios.Commands.GoogleSso;
using Application.Portfolios.Commands.LinkBinanceWallet;
using Application.Portfolios.Commands.SubmitKycDocuments;
using Application.Portfolios.Commands.TriggerApiSync;
using Application.Portfolios.Commands.UpdateKycPayload;
using Application.Portfolios.Queries.GetAggregatedNetWorth;
using Application.Portfolios.Queries.GetLiveChartsData;
using Application.Portfolios.Queries.GetRiskAuditSummary;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Application.UnitTests;

public class CqrsLiteRegistrationTests
{
    [Fact]
    public void AddApplicationServices_ShouldRegisterAllCqrsLiteCommandHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert - Command Handlers registered
        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<CreateManualAssetCommand, Guid>) &&
            s.ImplementationType == typeof(CreateManualAssetCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<GoogleSsoCommand, AuthResultDto>) &&
            s.ImplementationType == typeof(GoogleSsoCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<LinkBinanceWalletCommand, BinanceSyncResult>) &&
            s.ImplementationType == typeof(LinkBinanceWalletCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<TriggerApiSyncCommand, bool>) &&
            s.ImplementationType == typeof(TriggerApiSyncCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<UpdateKycPayloadCommand, bool>) &&
            s.ImplementationType == typeof(UpdateKycPayloadCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<SubmitKycDocumentsCommand, KycSubmissionResultDto>) &&
            s.ImplementationType == typeof(SubmitKycDocumentsCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<Application.Compliance.Commands.EvaluateKycAudit.EvaluateKycAuditCommand, bool>) &&
            s.ImplementationType == typeof(Application.Compliance.Commands.EvaluateKycAudit.EvaluateKycAuditCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<Application.Compliance.Commands.SyncTaxFilerStatus.SyncTaxFilerStatusCommand, Application.Compliance.Commands.SyncTaxFilerStatus.FbrAtlSyncResultDto>) &&
            s.ImplementationType == typeof(Application.Compliance.Commands.SyncTaxFilerStatus.SyncTaxFilerStatusCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<Application.Portfolios.Commands.UpdateRealEstateInstallment.UpdateRealEstateInstallmentCommand, Application.Portfolios.Commands.UpdateRealEstateInstallment.RealEstateInstallmentResultDto>) &&
            s.ImplementationType == typeof(Application.Portfolios.Commands.UpdateRealEstateInstallment.UpdateRealEstateInstallmentCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<Application.Portfolios.Commands.EvaluateSuitability.EvaluateSuitabilityCommand, Application.Portfolios.Commands.EvaluateSuitability.SuitabilityProfileResultDto>) &&
            s.ImplementationType == typeof(Application.Portfolios.Commands.EvaluateSuitability.EvaluateSuitabilityCommandHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterAllCqrsLiteQueryHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert - Query Handlers registered
        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<GetAggregatedNetWorthQuery, NetWorthDto>) &&
            s.ImplementationType == typeof(GetAggregatedNetWorthQueryHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<GetLiveChartsDataQuery, List<ChartDataPointDto>>) &&
            s.ImplementationType == typeof(GetLiveChartsDataQueryHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<GetRiskAuditSummaryQuery, RiskAuditDto>) &&
            s.ImplementationType == typeof(GetRiskAuditSummaryQueryHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<Application.Compliance.Queries.GetComplianceQueue.GetComplianceQueueQuery, List<Application.Compliance.Queries.GetComplianceQueue.UserComplianceDto>>) &&
            s.ImplementationType == typeof(Application.Compliance.Queries.GetComplianceQueue.GetComplianceQueueQueryHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<Application.Compliance.Queries.GetAccountAuditHistory.GetAccountAuditHistoryQuery, List<Application.Compliance.Queries.GetAccountAuditHistory.ComplianceAuditTrailLogDto>>) &&
            s.ImplementationType == typeof(Application.Compliance.Queries.GetAccountAuditHistory.GetAccountAuditHistoryQueryHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<Application.Portfolios.Queries.GetRealEstateTelemetry.GetRealEstateTelemetryQuery, Application.Portfolios.Queries.GetRealEstateTelemetry.RealEstateCashflowSummaryDto>) &&
            s.ImplementationType == typeof(Application.Portfolios.Queries.GetRealEstateTelemetry.GetRealEstateTelemetryQueryHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<Application.Portfolios.Queries.GetSuitabilityProfile.GetSuitabilityProfileQuery, Application.Portfolios.Commands.EvaluateSuitability.SuitabilityProfileResultDto>) &&
            s.ImplementationType == typeof(Application.Portfolios.Queries.GetSuitabilityProfile.GetSuitabilityProfileQueryHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<Application.Portfolios.Queries.GetForecastProjections.GetForecastProjectionsQuery, Application.Portfolios.Queries.GetForecastProjections.ForecastProjectionsResultDto>) &&
            s.ImplementationType == typeof(Application.Portfolios.Queries.GetForecastProjections.GetForecastProjectionsQueryHandler) &&
            s.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterFluentValidationValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert - Validators registered
        services.Should().Contain(s =>
            s.ServiceType == typeof(IValidator<CreateManualAssetCommand>) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IValidator<LinkBinanceWalletCommand>) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IValidator<UpdateKycPayloadCommand>) &&
            s.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(s =>
            s.ServiceType == typeof(IValidator<SubmitKycDocumentsCommand>) &&
            s.Lifetime == ServiceLifetime.Scoped);
    }
}
