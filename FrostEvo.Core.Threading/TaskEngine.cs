namespace FrostEvo.Core.Threading;

public class TaskEngine : TaskScheduler
{
    /// <summary>
    /// Indicates whether the current thread is processing work items.
    /// </summary>
    [ThreadStatic] private static bool _currentThreadIsProcessingItems;

    /// <summary>
    /// The list of tasks to be executed, protected by a lock.
    /// </summary>
    private readonly LinkedList<Task> _tasks = new();

    /// <summary>
    /// The maximum concurrency level allowed by this scheduler.
    /// </summary>
    private readonly int _maxDegreeOfParallelism;

    /// <summary>
    /// Indicates whether the scheduler is currently processing work items.
    /// </summary>
    private int _delegatesQueuedOrRunning = 0;

    /// <summary>
    /// Gets the maximum concurrency level supported by this scheduler.
    /// </summary>
    public sealed override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

    /// <summary>
    /// Creates a new instance with the specified degree of parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Usually the machine core count</param>
    public TaskEngine(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1)
            maxDegreeOfParallelism = 1;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <summary>
    /// Queues a task to the scheduler.
    /// </summary>
    /// <param name="task"></param>
    protected override void QueueTask(Task task)
    {
        // Add the task to the list of tasks to be processed.  If there aren't enough 
        // delegates currently queued or running to process tasks, schedule another. 
        lock (_tasks)
        {
            _tasks.AddLast(task);
            if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
            {
                ++_delegatesQueuedOrRunning;
                NotifyThreadPoolOfPendingWork();
            }
        }
    }

    /// <summary>
    /// Inform the ThreadPool that there's work to be executed for this scheduler. 
    /// </summary>
    private void NotifyThreadPoolOfPendingWork()
    {
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            // Note that the current thread is now processing work items.
            // This is necessary to enable inlining of tasks into this thread.
            _currentThreadIsProcessingItems = true;

            try
            {
                // Process all available items in the queue.
                while (true)
                {
                    Task item;
                    lock (_tasks)
                    {
                        // When there are no more items to be processed,
                        // note that we're done processing, and get out.
                        if (_tasks.Count == 0)
                        {
                            --_delegatesQueuedOrRunning;
                            break;
                        }

                        // Get the next item from the queue
                        item = _tasks.First.Value;
                        _tasks.RemoveFirst();
                    }

                    // Execute the task we pulled out of the queue
                    base.TryExecuteTask(item);
                }
            }
            // We're done processing items on the current thread
            finally
            {
                _currentThreadIsProcessingItems = false;
            }
        }, null);
    }

    /// <summary>
    /// Attemps to execute the specified task on the current thread
    /// </summary>
    /// <param name="task">task to execute</param>
    /// <param name="taskWasPreviouslyQueued"></param>
    /// <returns></returns>
    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (_currentThreadIsProcessingItems)
            return false;

        if (taskWasPreviouslyQueued)
            return TryDequeue(task) && base.TryExecuteTask(task);

        return base.TryExecuteTask(task);
    }

    /// <summary>
    /// Attempt to remove a previously scheduled task from the scheduler. 
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    protected sealed override bool TryDequeue(Task task)
    {
        lock (_tasks)
            return _tasks.Remove(task);
    }

    /// <summary>
    /// Gets an enumerable of the tasks currently scheduled on this scheduler.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    protected override IEnumerable<Task> GetScheduledTasks()
    {
        var lockTaken = false;
        try
        {
            Monitor.TryEnter(_tasks, ref lockTaken);
            if (lockTaken)
                return _tasks;
            else
                throw new NotSupportedException();
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(_tasks);
        }
    }
}