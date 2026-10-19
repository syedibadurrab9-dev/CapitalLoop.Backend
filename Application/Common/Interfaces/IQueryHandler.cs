namespace Application.Common.Interfaces;

/// <summary>
/// Defines a handler for a CQRS query. Handlers are registered and injected directly by their closed generic type.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
