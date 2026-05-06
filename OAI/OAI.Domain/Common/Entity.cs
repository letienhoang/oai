using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.Common;

public class Entity
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        Id = id;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    protected void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        return GetType() == other.GetType() && Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
