using MediatR;

namespace Application.Portfolios.Queries;

/// <summary>
/// CQRS Query to generate a predictive asset growth trajectory projection over a designated timeline [CL-302].
/// </summary>
/// <param name="UserEmail">The authenticated user email address targeted for wealth forecasting calculations.</param>
/// <param name="ProjectionMonths">The duration timeline for the forecast projection array (e.g., 36 or 120 months).</param>
public record GetForecastProjectionsQuery(string UserEmail, int ProjectionMonths = 36) : IRequest<ForecastProjectionsDto>;