namespace FrostEvo.Core.Services;

/// <summary>
/// Manager Base class, to be inherited to create a new manager.
/// </summary>
public abstract class Manager
{
    /// <summary>
    /// The order at which this manager will be loaded.
    /// </summary>
    public abstract int LoadOrder { get; }

    public abstract bool WorldSpecific { get; set; }
    
    /// <summary>
    /// Called ONCE when the manager starts.
    /// </summary>
    public abstract Task Start();

    /// <summary>
    /// Called ONCE when the manager stops.
    /// </summary>
    public abstract Task Stop();

    /// <summary>
    /// This can be used to process time based actions.
    /// </summary>
    public abstract Task Tick();
}