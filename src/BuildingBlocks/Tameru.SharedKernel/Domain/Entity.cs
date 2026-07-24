namespace Tameru.SharedKernel.Domain;

/// <summary>
/// Base class for all business entities. Provides a GUID identity (CLAUDE.md rule #7) and a
/// domain-event buffer. Audit fields and soft-delete live on <see cref="AuditableEntity"/>.
/// </summary>
public abstract class Entity : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected Entity()
    {
    }

    protected Entity(Guid id) => Id = id;

    public Guid Id { get; protected set; } = Guid.NewGuid();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id && Id != Guid.Empty;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

/// <summary>
/// An <see cref="Entity"/> that carries the standard audit fields and soft-delete flags. Every
/// persisted business table inherits this (CLAUDE.md: every table has the standard audit fields).
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable, ISoftDeletable
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id) : base(id)
    {
    }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }
}
