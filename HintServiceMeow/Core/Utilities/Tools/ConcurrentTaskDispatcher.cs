namespace HintServiceMeow.Core.Utilities.Tools
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using HintServiceMeow.Core.Interface;

    internal sealed class ConcurrentTaskDispatcher : IConcurrentTaskDispatcher, IDisposable
    {
        private const int MinimumQueueCapacity = 64;
        private const int MaximumQueueCapacity = 256;
        private const int QueueSlotsPerWorker = 32;
        private const int MaximumWorkerCount = 4;

        private static readonly object InstanceLock = new();

        private static ConcurrentTaskDispatcher instance = CreateDefault();

        private readonly BlockingCollection<ITaskPatch> taskQueue;
        private readonly List<Task> workers = [];
        private readonly CancellationTokenSource shutdownTokenSource = new();
        private readonly int queueCapacity;

        private int disposed;
        private long droppedTaskCount;
        private long lastSaturationLogTicks;

        public ConcurrentTaskDispatcher(int workerCount)
        {
            workerCount = Math.Min(MaximumWorkerCount, Math.Max(1, workerCount));
            queueCapacity = Math.Min(MaximumQueueCapacity, Math.Max(MinimumQueueCapacity, workerCount * QueueSlotsPerWorker));
            taskQueue = new BlockingCollection<ITaskPatch>(queueCapacity);

            for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
            {
                // BlockingCollection waits synchronously. Dedicated workers keep those waits off
                // the shared thread pool used by the game, EXILED, and every other plugin.
                workers.Add(
                    Task.Factory.StartNew(
                        WorkerMethod,
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning,
                        System.Threading.Tasks.TaskScheduler.Default));
            }
        }

        private interface ITaskPatch
        {
            Task ExecuteAsync();

            void Cancel();
        }

        public static IConcurrentTaskDispatcher Instance => Volatile.Read(ref instance);

        private bool IsDisposed => Volatile.Read(ref disposed) != 0;

        public void Enqueue(Func<Task> task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            QueueOrCancel(new TaskPatch(task));
        }

        public Task<T> Enqueue<T>(Func<Task<T>> task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            TaskPatch<T> wrapper = new(task);
            QueueOrCancel(wrapper);
            return wrapper.Completion.Task;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;

            taskQueue.CompleteAdding();
            shutdownTokenSource.Cancel();

            while (taskQueue.TryTake(out ITaskPatch? pendingTask))
            {
                pendingTask.Cancel();
            }

            // Do not block the game thread waiting for an already-running parse. Dispose the
            // synchronization objects only after every worker has observed cancellation.
            _ = Task.WhenAll(workers).ContinueWith(
                _ =>
                {
                    taskQueue.Dispose();
                    shutdownTokenSource.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
        }

        internal static void Start()
        {
            lock (InstanceLock)
            {
                if (instance.IsDisposed)
                    instance = CreateDefault();
            }
        }

        internal static void Restart()
        {
            ConcurrentTaskDispatcher replacement = CreateDefault();
            ConcurrentTaskDispatcher previous;

            lock (InstanceLock)
            {
                previous = instance;
                instance = replacement;
            }

            previous.Dispose();
        }

        internal static void Shutdown()
        {
            lock (InstanceLock)
            {
                instance.Dispose();
            }
        }

        private static ConcurrentTaskDispatcher CreateDefault()
            => new(Math.Max(1, Environment.ProcessorCount - 1));

        private void QueueOrCancel(ITaskPatch task)
        {
            if (IsDisposed)
            {
                task.Cancel();
                return;
            }

            try
            {
                if (taskQueue.TryAdd(task))
                    return;

                // Prefer current render state over old queued state. PlayerDisplay observes the
                // cancellation and schedules one fresh parse after a short backoff.
                if (taskQueue.TryTake(out ITaskPatch? staleTask))
                {
                    staleTask.Cancel();
                    RecordSaturationDrop();
                }

                if (taskQueue.TryAdd(task))
                    return;
            }
            catch (ObjectDisposedException)
            {
                // Finished disposal raced a caller that captured the old instance.
            }
            catch (InvalidOperationException)
            {
                // CompleteAdding raced this enqueue during round reset or plugin shutdown.
            }

            task.Cancel();
            RecordSaturationDrop();
        }

        private void RecordSaturationDrop()
        {
            long dropped = Interlocked.Increment(ref droppedTaskCount);
            long now = DateTime.UtcNow.Ticks;
            long previous = Volatile.Read(ref lastSaturationLogTicks);

            if (now - previous < TimeSpan.TicksPerMinute ||
                Interlocked.CompareExchange(ref lastSaturationLogTicks, now, previous) != previous)
            {
                return;
            }

            Logger.Instance.Info($"[HSM] Parser queue saturated (capacity={queueCapacity}); dropped pending work={dropped}.");
        }

        private void WorkerMethod()
        {
            try
            {
                foreach (ITaskPatch task in taskQueue.GetConsumingEnumerable(shutdownTokenSource.Token))
                {
                    if (shutdownTokenSource.IsCancellationRequested)
                    {
                        task.Cancel();
                        break;
                    }

                    task.ExecuteAsync().GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException) when (shutdownTokenSource.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        private sealed class TaskPatch<T> : ITaskPatch
        {
            public TaskPatch(Func<Task<T>> task)
            {
                Task = task ?? throw new ArgumentNullException(nameof(task));
                Completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public Func<Task<T>> Task { get; }

            public TaskCompletionSource<T> Completion { get; }

            public void Cancel() => Completion.TrySetCanceled();

            public async Task ExecuteAsync()
            {
                try
                {
                    T result = await Task().ConfigureAwait(false);
                    Completion.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    Completion.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    Completion.TrySetException(ex);
                }
            }
        }

        private sealed class TaskPatch : ITaskPatch
        {
            public TaskPatch(Func<Task> task)
            {
                Task = task ?? throw new ArgumentNullException(nameof(task));
            }

            public Func<Task> Task { get; }

            public void Cancel()
            {
            }

            public async Task ExecuteAsync()
            {
                try
                {
                    await Task().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }
    }
}
