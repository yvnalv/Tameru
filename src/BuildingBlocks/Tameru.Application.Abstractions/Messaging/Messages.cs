namespace Tameru.Application.Abstractions.Messaging;

/// <summary>Marker for a use-case request (command or query) handled by a single handler.</summary>
public interface IRequest<TResponse>
{
}

/// <summary>Handles a <see cref="IRequest{TResponse}"/>.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// An integration event published in-process after a successful commit, enabling loosely coupled
/// cross-module reactions (docs/ARCHITECTURE.md → Module boundaries).
/// </summary>
public interface IIntegrationEvent
{
    DateTimeOffset OccurredAt { get; }
}

/// <summary>Handles an <see cref="IIntegrationEvent"/> in a consuming module.</summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
