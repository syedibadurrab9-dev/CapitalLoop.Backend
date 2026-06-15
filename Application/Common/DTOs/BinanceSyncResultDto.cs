using System;

namespace Application.Common.DTOs;

/// <summary>
/// 📦 SHARED APPLICATION DATA CONTRACT (DTO)
/// 🛠️ FIXED: Converted to a positional record definition to auto-generate constructor matching the Infrastructure layer.
/// Encapsulates the aggregated, currency-normalized balance telemetry extracted during cross-exchange sweeps [CL-201].
/// </summary>
public record BinanceSyncResultDto(
    bool IsSyncSuccessful,
    decimal SyncedTotalHoldingValueUsd,
    decimal ComputedHoldingValuePkr,
    int ActiveTokenBalancesCapturedCount,
    string DiagnosticMessage,
    DateTime ExecutedAtUtc
);