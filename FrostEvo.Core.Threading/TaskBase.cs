using System.Diagnostics;

namespace FrostEvo.Core.Threading;

public abstract class TaskBase
{
    public int Interval { get; set; }
    public string Name { get; set; }
    public long Diff { get; set; } = 0;
    public Stopwatch Watch { get; set; } = new Stopwatch();
    public Action Action { get; set; }
    public CancellationTokenSource Token { get; set; } = new();

    public abstract void Init(TaskFactory factory, int interval, string name);
    public abstract void Init(TaskFactory factory, Action action, int interval, string name);
    public abstract void Process();

    public virtual async void Tick()
    {
        while (!Token.IsCancellationRequested)
        {
            Watch.Restart();
            Action.Invoke();
            Diff = Watch.ElapsedMilliseconds;
            await Task.Delay(Interval);
        }
    }

    public virtual void Stop() => Token.Cancel();
}