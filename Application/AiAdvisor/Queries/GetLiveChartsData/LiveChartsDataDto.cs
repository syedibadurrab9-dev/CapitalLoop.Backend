using System;
using System.Collections.Generic;

namespace Application.AiAdvisor.Queries.GetLiveChartsData;

/// <summary>
/// 📦 CQRS OUTPUT CONTRACT (TResponse)
/// Immutable time-series DTO structured for seamless JSON object parsing inside UI charting arrays.
/// Aligned with multi-asset aggregation matrices to pass structured, chronologically sorted tracking points.
/// </summary>
public record LiveChartsDataDto(
    IReadOnlyCollection<ChartPointDto> ChartPoints,
    DateTime GeneratedAtUtc
);

/// <summary>
/// 📊 INLINE TELEMETRY RECORD BLOCK
/// Captures a localized historical spot valuation checkpoint for a specific market entity symbol.
/// </summary>
