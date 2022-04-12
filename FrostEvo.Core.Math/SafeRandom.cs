namespace FrostEvo.Core.Math;

public class SafeRandom
{
    private static int _seed = Environment.TickCount;
    private static readonly ThreadLocal<Random> ThreadLocal = new(() => new Random(Interlocked.Increment(ref _seed)));
    public static Random Instance => ThreadLocal.Value;

    public static int Next(int nMin, int nMax) => nMin > nMax ? Instance.Next(nMax, nMin) : Instance.Next(nMin, nMax);
    public static int Next(int nMax) => Instance.Next(nMax);
    public static int Next() => Instance.Next();
    public static bool Next(double rate) => Instance.NextDouble() * 100 >= (100 - rate);
}