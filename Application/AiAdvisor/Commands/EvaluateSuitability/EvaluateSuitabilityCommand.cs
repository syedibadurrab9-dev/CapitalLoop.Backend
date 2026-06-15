using MediatR;

namespace Application.Portfolios.Commands;

/// <summary>
/// CQRS Command used to submit questionnaire choices and calculate systemic risk classification profiles [CL-301].
/// </summary>
/// <param name="UserEmail">The authenticated user email address targeted for suitability profiling.</param>
/// <param name="QuestionResponses">A map linking specific sequential Question IDs directly to their chosen numeric answer score values.</param>
public record EvaluateSuitabilityCommand(
    string UserEmail,
    Dictionary<int, int> QuestionResponses) : IRequest<SuitabilityResultDto>;