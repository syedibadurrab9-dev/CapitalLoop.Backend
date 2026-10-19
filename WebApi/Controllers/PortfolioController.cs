using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Portfolios.Commands.CreateManualAsset;
using Application.Portfolios.Commands.LinkBinanceWallet;
using Application.Portfolios.Commands.TriggerApiSync;
using Application.Portfolios.Commands.UpdateRealEstateInstallment;
using Application.Portfolios.Queries.GetAggregatedNetWorth;
using Application.Portfolios.Queries.GetForecastProjections;
using Application.Portfolios.Queries.GetLiveChartsData;
using Application.Portfolios.Queries.GetRealEstateTelemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize] // Protect financial metrics via JWT verification filters
public class PortfolioController : BaseApiController
{
    private readonly IQueryHandler<GetAggregatedNetWorthQuery, NetWorthDto> _netWorthHandler;
    private readonly IQueryHandler<GetLiveChartsDataQuery, List<ChartDataPointDto>> _chartsHandler;
    private readonly ICommandHandler<CreateManualAssetCommand, Guid> _createAssetHandler;
    private readonly ICommandHandler<TriggerApiSyncCommand, bool> _triggerSyncHandler;
    private readonly ICommandHandler<LinkBinanceWalletCommand, BinanceSyncResult> _binanceHandler;
    private readonly ICommandHandler<UpdateRealEstateInstallmentCommand, RealEstateInstallmentResultDto> _updateRealEstateHandler;
    private readonly IQueryHandler<GetRealEstateTelemetryQuery, RealEstateCashflowSummaryDto> _realEstateTelemetryHandler;
    private readonly IQueryHandler<GetForecastProjectionsQuery, ForecastProjectionsResultDto> _forecastHandler;

    public PortfolioController(
        IQueryHandler<GetAggregatedNetWorthQuery, NetWorthDto> netWorthHandler,
        IQueryHandler<GetLiveChartsDataQuery, List<ChartDataPointDto>> chartsHandler,
        ICommandHandler<CreateManualAssetCommand, Guid> createAssetHandler,
        ICommandHandler<TriggerApiSyncCommand, bool> triggerSyncHandler,
        ICommandHandler<LinkBinanceWalletCommand, BinanceSyncResult> binanceHandler,
        ICommandHandler<UpdateRealEstateInstallmentCommand, RealEstateInstallmentResultDto> updateRealEstateHandler,
        IQueryHandler<GetRealEstateTelemetryQuery, RealEstateCashflowSummaryDto> realEstateTelemetryHandler,
        IQueryHandler<GetForecastProjectionsQuery, ForecastProjectionsResultDto> forecastHandler)
    {
        _netWorthHandler = netWorthHandler;
        _chartsHandler = chartsHandler;
        _createAssetHandler = createAssetHandler;
        _triggerSyncHandler = triggerSyncHandler;
        _binanceHandler = binanceHandler;
        _updateRealEstateHandler = updateRealEstateHandler;
        _realEstateTelemetryHandler = realEstateTelemetryHandler;
        _forecastHandler = forecastHandler;
    }

    #region CORE DASHBOARD & MANIPULATION ENDPOINTS

    [HttpGet("net-worth")]
    public async Task<IActionResult> GetNetWorth([FromQuery] string currency, CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var result = await _netWorthHandler.HandleAsync(new GetAggregatedNetWorthQuery(userEmail, currency), cancellationToken);
        return Ok(result);
    }

    [HttpGet("charts-data")]
    public async Task<IActionResult> GetChartsData(CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var result = await _chartsHandler.HandleAsync(new GetLiveChartsDataQuery(userEmail), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Computes 36-month and 120-month net-of-tax compounding wealth projections differentiating FBR Filers vs Non-Filers.
    /// GET: api/Portfolio/forecast
    /// </summary>
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecastProjections(CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var result = await _forecastHandler.HandleAsync(new GetForecastProjectionsQuery(userEmail), cancellationToken);
        return Ok(result);
    }

    [HttpPost("manual-asset")]
    public async Task<IActionResult> CreateManualAsset([FromBody] CreateManualAssetCommand command, CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        // Ensure user email is bound to authenticated claims
        var secureCommand = command with { UserEmail = userEmail };

        var assetId = await _createAssetHandler.HandleAsync(secureCommand, cancellationToken);
        return Ok(new { AssetId = assetId, Status = "Success" });
    }

    [HttpPost("trigger-sync")]
    public async Task<IActionResult> TriggerApiSync(CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var syncSuccess = await _triggerSyncHandler.HandleAsync(new TriggerApiSyncCommand(userEmail), cancellationToken);

        if (!syncSuccess)
            return BadRequest("Automated balance aggregation tracking failed or no assets discovered.");

        return Ok(new { Message = "Portfolio matrices synchronized successfully via external nodes." });
    }

    #endregion

    #region FEATURE INTEGRATION: DIGITAL EXCHANGES (BINANCE)

    [HttpPost("sync/binance")]
    public async Task<IActionResult> LinkAndSyncBinance([FromBody] ConnectBinanceRequest request, CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();

        var command = new LinkBinanceWalletCommand
        {
            ApiKey = request.ApiKey,
            SecretKey = request.SecretKey,
            UserEmail = userEmail
        };

        var syncSummary = await _binanceHandler.HandleAsync(command, cancellationToken);
        return Ok(syncSummary);
    }

    #endregion

    #region REAL ESTATE INSTALLMENT TELEMETRY & CASHFLOW VALUATION

    /// <summary>
    /// Updates real estate installment milestone payments and recalculates paid equity and liabilities.
    /// </summary>
    [HttpPut("real-estate/{assetId:guid}/installments")]
    public async Task<IActionResult> UpdateInstallments(
        Guid assetId,
        [FromBody] UpdateInstallmentsRequest request,
        CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var command = new UpdateRealEstateInstallmentCommand(
            AssetId: assetId,
            UserEmail: userEmail,
            PaidInstallments: request.PaidInstallments,
            TotalInstallments: request.TotalInstallments,
            UpdatedValuation: request.UpdatedValuation
        );

        var result = await _updateRealEstateHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves comprehensive real estate portfolio telemetry, cashflows, and liability commitments.
    /// </summary>
    [HttpGet("real-estate/telemetry")]
    public async Task<IActionResult> GetRealEstateTelemetry(CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var result = await _realEstateTelemetryHandler.HandleAsync(
            new GetRealEstateTelemetryQuery(userEmail),
            cancellationToken);

        return Ok(result);
    }

    #endregion
}

/// <summary>
/// Explicit request payload record for secure incoming Binance keys.
/// </summary>
public record ConnectBinanceRequest(string ApiKey, string SecretKey);

/// <summary>
/// Request payload for updating real estate installment telemetry.
/// </summary>
public record UpdateInstallmentsRequest(
    int PaidInstallments,
    int TotalInstallments,
    decimal? UpdatedValuation = null
);