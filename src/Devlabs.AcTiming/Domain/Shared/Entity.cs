namespace Devlabs.AcTiming.Domain.Shared;

public abstract class Entity
{
    protected Entity() { }

    protected Entity(int id)
        : this()
    {
        Id = id;
    }

    public int Id { get; set; }

    public override bool Equals(object? obj)
    {
        if (!(obj is Entity other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (this.GetType() != other.GetType())
        {
            return false;
        }

        if (Id.Equals(default) || other.Id.Equals(default))
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => (this.GetType().ToString() + Id).GetHashCode();
}
