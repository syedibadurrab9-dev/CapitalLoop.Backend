using Application.Common.DTOs;
using MediatR;

namespace Application.Portfolios.Commands.OverrideUserPlan;

/// <summary>
/// CQRS Administrative Command used to manually elevate or downgrade user ecosystem privilege parameters [CL-503].
/// </summary>
public record OverrideUserPlanCommand(SubscriptionOverrideRequest Payload) : IRequest<SubscriptionOverrideResultDto>;