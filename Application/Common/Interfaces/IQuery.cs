namespace Application.Common.Interfaces;

/// <summary>
/// Marker interface for a CQRS query that produces a result of type <typeparamref name="TResult"/>.
/// </summary>
public interface IQuery<out TResult>;
