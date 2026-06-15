using System;

namespace Application.Common.DTOs;

/// <summary>
/// Represents the real-time telemetry state of an active infrastructure ingestion node or broker pipeline.
/// Maps directly to the front-end cluster monitoring dashboards and APIIntegrationNode tracking interfaces.
/// </summary>
public record AssetNodeTelemetryDto
{
    public Guid NodeId { get; init; }
    public string AssetName { get; init; } = string.Empty;
    public string AssetCategory { get; init; } = string.Empty;
    public decimal ValuationPkr { get; init; }
    public decimal AllocationPercentage { get; init; }
    public bool IsAutomatedSyncActive { get; init; }
    public DateTime LastSynchronizedAtUtc { get; init; }

    public AssetNodeTelemetryDto() { }

    public AssetNodeTelemetryDto(Guid nodeId, string assetName, string assetCategory, decimal valuationPkr, decimal allocationPercentage, bool isAutomatedSyncActive, DateTime lastSynchronizedAtUtc)
    {
        NodeId = nodeId;
        AssetName = string.IsNullOrWhiteSpace(assetName) ? "Unassigned Position" : assetName.Trim();
        AssetCategory = assetCategory;
        ValuationPkr = valuationPkr < 0 ? 0 : valuationPkr;
        AllocationPercentage = Math.Clamp(allocationPercentage, 0, 100);
        IsAutomatedSyncActive = isAutomatedSyncActive;
        LastSynchronizedAtUtc = lastSynchronizedAtUtc;
    }
}
///// <summary>
///// Defines operational health tracking bounds for the real-time pipeline connection layer.
///// </summary>
//public enum PipelineHealthState
//{
//    /// <summary>
//    /// Node is uninitialized or explicitly disabled within the cluster layout.
//    /// </summary>
//    Offline = 0,

//    /// <summary>
//    /// Operational, low latency, streaming valid telemetry data frames cleanly.
//    /// </summary>
//    Healthy = 1,

//    /// <summary>
//    /// Node is experiencing heightened response latency or transient connection timeouts, but data ingestion remains partially active.
//    /// </summary>
//    Degraded = 2,

//    /// <summary>
//    /// Data transmission block failure caught. Indicates invalid API tokens or downstream DOM selector structural shifts [CL-206].
//    /// </summary>
//    Faulted = 3
//}