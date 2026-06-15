using MediatR;

namespace Application.AiAdvisor.Queries.GetLiveChartsData;

/// <summary>
/// 📝 CQRS INPUT CONTRACT (TRequest)
/// Query engineered to retrieve localized, time-series data streams for financial charting engine rendering [CL-305].
/// Aligned cleanly with multi-asset aggregation matrices to resolve user-scoped portfolio histories.
/// </summary>
/// <param name="UserEmail">The unique destination email identifier tracking the investor workspace context.</param>
public record GetLiveChartsDataQuery(string UserEmail) : IRequest<LiveChartsDataDto>;