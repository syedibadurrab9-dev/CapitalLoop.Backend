namespace Application.Common.Interfaces;

/// <summary>
/// Marker interface for a CQRS command that produces a result of type <typeparamref name="TResult"/>.
/// </summary>
public interface ICommand<out TResult>;
