namespace FrostEvo.Core.Math;

public class FastRandom
{
    private const double RealUnitInt = 1.0 / (int.MaxValue + 1.0);
    private const double RealUnitUint = 1.0 / (uint.MaxValue + 1.0);
    private const uint Y = 842502087, Z = 3579807591, W = 273326509;
    private readonly object _syncRoot;

    private uint _x, _y, _z, _w;

    public static FastRandom Instance { get; set; } = new FastRandom();

    public FastRandom()
        : this(Environment.TickCount)
    {
    }

    public FastRandom(int seed)
    {
        _syncRoot = new object();
        Reinitialise(seed);
    }

    public bool Chance(int chance)
    {
        return Chance(chance, 100);
    }

    public bool Chance(int chance, int max)
    {
        return Next(0, max) < chance;
    }

    public void Reinitialise(int seed)
    {
        lock (_syncRoot)
        {
            _x = (uint) seed;
            _y = Y;
            _z = Z;
            _w = W;
        }
    }

    public int Next()
    {
        lock (_syncRoot)
        {
            var t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            _w = _w ^ (_w >> 19) ^ t ^ (t >> 8);

            var rtn = _w & 0x7FFFFFFF;
            if (rtn == 0x7FFFFFFF) return Next();
            return (int) rtn;
        }
    }

    public int Next(int upperBound)
    {
        lock (_syncRoot)
        {
            if (upperBound < 0) upperBound = 0;

            var t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            return (int) (RealUnitInt * (int) (0x7FFFFFFF & (_w = _w ^ (_w >> 19) ^ t ^ (t >> 8))) * upperBound);
        }
    }

    public int Sign()
    {
        var next = Next(0, 2);
        if (next == 0) return -1;
        return 1;
    }

    public int Next(int lowerBound, int upperBound)
    {
        lock (_syncRoot)
        {
            if (lowerBound > upperBound)
            {
                (lowerBound, upperBound) = (upperBound, lowerBound);
            }

            var t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            var range = upperBound - lowerBound;
            if (range < 0)
                return lowerBound + (int) (RealUnitUint * (_w = _w ^ (_w >> 19) ^ t ^ (t >> 8)) *
                                           (upperBound - (long) lowerBound));
            return lowerBound + (int) (RealUnitInt * (int) (0x7FFFFFFF & (_w = _w ^ (_w >> 19) ^ t ^ (t >> 8))) *
                                       range);
        }
    }

    public double NextDouble()
    {
        lock (_syncRoot)
        {
            var t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            return RealUnitInt * (int) (0x7FFFFFFF & (_w = _w ^ (_w >> 19) ^ t ^ (t >> 8)));
        }
    }

    public unsafe void NextBytes(byte[] buffer)
    {
        lock (_syncRoot)
        {
            if (buffer.Length % 8 != 0)
            {
                new Random().NextBytes(buffer);
                return;
            }

            uint x = this._x, y = this._y, z = this._z, w = this._w;

            fixed (byte* pByte0 = buffer)
            {
                var pDWord = (uint*) pByte0;
                for (int i = 0, len = buffer.Length >> 2; i < len; i += 2)
                {
                    var t = x ^ (x << 11);
                    x = y;
                    y = z;
                    z = w;
                    pDWord[i] = w = w ^ (w >> 19) ^ t ^ (t >> 8);

                    t = x ^ (x << 11);
                    x = y;
                    y = z;
                    z = w;
                    pDWord[i + 1] = w = w ^ (w >> 19) ^ t ^ (t >> 8);
                }
            }
        }
    }

    public uint NextUInt()
    {
        lock (_syncRoot)
        {
            var t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            return _w = _w ^ (_w >> 19) ^ t ^ (t >> 8);
        }
    }

    public int NextInt()
    {
        lock (_syncRoot)
        {
            var t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            return (int) (0x7FFFFFFF & (_w = _w ^ (_w >> 19) ^ t ^ (t >> 8)));
        }
    }
}