namespace Tameru.SharedKernel.Domain;

/// <summary>Marker for a domain event raised by an aggregate.</summary>
public interface IDomainEvent
{
}

/// <summary>Carries the standard audit fields (stamped by the persistence layer, not by callers).</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }

    Guid CreatedBy { get; set; }

    DateTimeOffset? UpdatedAt { get; set; }

    Guid? UpdatedBy { get; set; }
}

/// <summary>Supports soft delete: rows are flagged, never physically removed (CLAUDE.md rule #6).</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedAt { get; set; }

    Guid? DeletedBy { get; set; }
}

/// <summary>Buffers domain events until they are dispatched after a successful commit.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
