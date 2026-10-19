namespace Application.Common.Interfaces;

/// <summary>
/// Defines a handler for a CQRS command. Handlers are registered and injected directly by their closed generic type.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
