using System;
using MediatR;

namespace Application.Consultations.Commands.BookConsultationSlot;

/// <summary>
/// CQRS Command to reserve an incoming pre-registration wealth management discovery slot [CL-502].
/// Synchronizes inbound sales funnel configurations from the calendar wizard node.
/// </summary>
/// <param name="ProspectName">The full name or designation moniker provided by the prospect investor.</param>
/// <param name="ProspectEmail">The primary contact communication address tracking the incoming lead.</param>
/// <param name="TargetedAssetClass">The specific asset category classification requested (e.g., RealEstate, PsxEquity).</param>
/// <param name="RequestedCallSlotUtc">The designated date and time coordinates for the calendar appointment.</param>
public record BookConsultationSlotCommand(
    string ProspectName,
    string ProspectEmail,
    string TargetedAssetClass,
    DateTime RequestedCallSlotUtc) : IRequest<Guid>;