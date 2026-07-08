using System.Collections.Generic;
using System.Linq;

namespace FinTrack.Domain.Common;

/// <summary>
/// Clase base para todos los objetos de valor en el dominio que se comparan por propiedades, no por identidad.
/// </summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? one, ValueObject? two)
    {
        if (one is null && two is null)
            return true;

        if (one is null || two is null)
            return false;

        return one.Equals(two);
    }

    public static bool operator !=(ValueObject? one, ValueObject? two)
    {
        return !(one == two);
    }
}
