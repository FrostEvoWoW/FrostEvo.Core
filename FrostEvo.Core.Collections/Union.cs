namespace FrostEvo.Core.Collections;

public class Union
{
    public Union()
    {
    }

    public Union(ushort x, ushort y)
    {
        Value = (ulong) ((y << 16) | (x & 0xffff));
    }

    public Union(uint v1, uint v2)
    {
        Value = (ulong) (((ulong) v1 << 32) | (ulong) v2);
    }

    public ulong Value { get; set; }
    public ushort Y => (ushort) (Value >> 16);
    public ushort X => (ushort) (Value & 0xffff);


    public static implicit operator Union(int value)
    {
        return new Union {Value = (ulong) value};
    }

    public static implicit operator Union(uint value)
    {
        return new Union {Value = (ulong) value};
    }

    public static implicit operator Union(long value)
    {
        return new Union {Value = (ulong) value};
    }

    public override string ToString()
    {
        return $"{Value}:{X},{Y}";
    }

    public static implicit operator int(Union value)
    {
        return (int) value.Value;
    }

    public static implicit operator long(Union value)
    {
        return (long) value.Value;
    }

    public static implicit operator Union(ulong value)
    {
        return new Union {Value = (ulong) value};
    }

    public static implicit operator ulong(Union value)
    {
        return (ulong) value.Value;
    }
}