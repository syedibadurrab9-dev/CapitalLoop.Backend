using System.Collections.Generic;
using Application.Common.DTOs;
using MediatR;

namespace Application.Portfolios.Queries.GetSubscriptionPlans;

/// <summary>
/// CQRS Query retrieving active multi-tier metadata rulesets configured across the platform ecosystem [CL-503].
/// </summary>
public record GetSubscriptionPlansQuery() : IRequest<IReadOnlyCollection<SubscriptionPlanDto>>;