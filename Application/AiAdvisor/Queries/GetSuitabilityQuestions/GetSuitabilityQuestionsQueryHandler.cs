using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Application.AiAdvisor.Queries.GetSuitabilityQuestions;


public class GetSuitabilityQuestionsQueryHandler : IRequestHandler<GetSuitabilityQuestionsQuery, IReadOnlyCollection<SuitabilityQuestionDto>>
{
    public Task<IReadOnlyCollection<SuitabilityQuestionDto>> Handle(GetSuitabilityQuestionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // 🚀 EFFICIENCY OPTIMIZATION: Materialize structured compliance rules directly from memory
        // parameters to eliminate query execution latency and protect storage engine performance.
        var questionnaire = new List<SuitabilityQuestionDto>
        {
            new(
                QuestionId: 1,
                Text: "What is your primary investment objective for your capital loop allocation?",
                Category: "Objective",
                Choices: new Dictionary<string, int>
                {
                    { "Capital Preservation (Low Risk / Stable Returns)", 10 },
                    { "Balanced Wealth Building (Moderate Risk / Mixed Growth)", 25 },
                    { "Aggressive Capital Appreciation (High Risk / Maximum Growth)", 50 }
                }
            ),
            new(
                QuestionId: 2,
                Text: "What is your typical investment horizon or lock-in commitment timeframe?",
                Category: "Horizon",
                Choices: new Dictionary<string, int>
                {
                    { "Short-Term (Less than 12 months)", 5 },
                    { "Medium-Term (1 to 3 years - Active SIP Pipeline)", 20 },
                    { "Long-Term (3 to 10+ years - Multi-Asset Wealth Building)", 40 }
                }
            ),
            new(
                QuestionId: 3,
                Text: "How would you react if a high-volatility position (e.g., Crypto or Blue-Chip Equity) dropped 15% in a single month?",
                Category: "Tolerance",
                Choices: new Dictionary<string, int>
                {
                    { "Liquidate instantly to safeguard remaining capital balances", 0 },
                    { "Maintain structural positions and wait for index stabilization", 20 },
                    { "Capitalize on the variance and purchase additional units via dollar-cost averaging", 45 }
                }
            )
        };

        return Task.FromResult<IReadOnlyCollection<SuitabilityQuestionDto>>(questionnaire.AsReadOnly());
    }
}