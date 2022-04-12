namespace FrostEvo.Core.Math;

public class Counter
{
    private readonly uint _counterStartValue;
    private readonly uint _counterMaxValue;
    private object _syncRoot;
    private uint _counter;

    private object SyncRoot
    {
        get
        {
            if (_syncRoot == null)
                Interlocked.CompareExchange(ref _syncRoot, new object(), null);
            return _syncRoot;
        }
    }

    public Counter(uint start, uint end)
    {
        _counterStartValue = start;
        _counterMaxValue = end;
        _counter = _counterStartValue;
    }

    public Counter()
    {
        _counterStartValue = 0;
        _counterMaxValue = uint.MaxValue;
        _counter = _counterStartValue;
    }

    public uint Next
    {
        get
        {
            lock (SyncRoot)
            {
                _counter++;
                if (_counter > _counterMaxValue)
                    _counter = _counterStartValue;
                return _counter;
            }
        }
    }

    public uint Value => _counter;
    public override string ToString() => $"Min: {_counterStartValue} Max: {_counterMaxValue} Next: {Next}";
    public static implicit operator uint(Counter counter) => counter.Next;
}