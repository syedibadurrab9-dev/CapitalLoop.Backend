using MediatR;

namespace Application.Portfolios.Commands;

/// <summary>
/// CQRS Command to bootstrap and initialize a retail user's profile using CapitalLoop factory patterns [CL-101].
/// </summary>
/// <param name="ApplicationUserId">The unique database primary key string matching the identity user security table.</param>
/// <param name="UserEmail">The verified email address captured during the federated authentication step.</param>
/// <param name="FullName">The consolidated single name string parameter assigned to the user profile.</param>
public record InitializeOnboardingCommand(
    Guid ApplicationUserId,
    string UserEmail,
    string FullName) : IRequest<bool>;