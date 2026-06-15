using System.Collections.Generic;
using MediatR;

namespace Application.AiAdvisor.Queries.GetSuitabilityQuestions;

/// <summary>
/// CQRS Query to retrieve the standardized, SECP-compliant regulatory risk assessment questionnaire [CL-301].
/// Used to dynamically populate the user-facing suitability assessment wizard.
/// </summary>
/// <param name="LanguagePreference">Optional language identifier for localized question sets (e.g., "en", "ur").</param>
public record GetSuitabilityQuestionsQuery(string LanguagePreference = "en") : IRequest<IReadOnlyCollection<SuitabilityQuestionDto>>;