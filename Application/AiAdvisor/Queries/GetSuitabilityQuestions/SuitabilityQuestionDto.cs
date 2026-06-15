using System.Collections.Generic;

namespace Application.AiAdvisor.Queries.GetSuitabilityQuestions;

/// <summary>
/// Immutable data transfer wire record outlining individual suitability assessment question nodes.
/// </summary>
/// <param name="QuestionId">The unique sequential identifier tracking the specific question block.</param>
/// <param name="Text">The localized phrasing of the compliance or risk tolerance question.</param>
/// <param name="Category">The category segment tracking the intention of the query (e.g., Tolerance, Horizon).</param>
/// <param name="Choices">Dictionary mapping descriptive user choice labels directly to their structural numeric scoring weights.</param>
public record SuitabilityQuestionDto(
    int QuestionId,
    string Text,
    string Category,
    IReadOnlyDictionary<string, int> Choices
);