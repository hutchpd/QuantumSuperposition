using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PositronicVariables.Transactions;

namespace PositronicVariables.Engine.Coordinator
{
    /// <summary>
    /// The very patient queue runner that convinces time to behave.
    /// Single reader, many hopeful writers. Tea is implied, towels recommended.
    /// </summary>
    public sealed class ConvergenceCoordinator : IDisposable
    {
        private readonly Channel<IConvergenceWorkItem> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _worker;
        private long _processed;
        private long _enqueueCount;
        private long _maxDepth;
        private long _totalLatencyTicks;
        private readonly ConcurrentQueue<long> _latencySamples = new();
        private readonly object _flushLock = new();

        public ConvergenceCoordinator(int capacity = 1024)
        {
            // Single reader so the engine feels like a proper British queue.
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<IConvergenceWorkItem>(options);
            _worker = Task.Run(WorkerLoop);
        }

        /// <summary>
        /// Take a number, join the queue, bring snacks. The coordinator will see you shortly.
        /// </summary>
        public ValueTask EnqueueAsync(IConvergenceWorkItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            long depth = _channel.Reader.Count;
            UpdateMaxDepth(depth + 1);
            Interlocked.Increment(ref _enqueueCount);
            return _channel.Writer.WriteAsync(item, _cts.Token);
        }

        private async Task WorkerLoop()
        {
            // One worker to rule them all, and in the darkness converge them.
            while (await _channel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var item))
                {
                    var swLatency = Stopwatch.StartNew();
                    ProcessItem(item);
                    swLatency.Stop();
                    _latencySamples.Enqueue(swLatency.ElapsedTicks);
                    Interlocked.Add(ref _totalLatencyTicks, swLatency.ElapsedTicks);
                    Interlocked.Increment(ref _processed);
                }
            }
        }

        private static void ProcessItem(IConvergenceWorkItem item)
        {
            try
            {
                // Declare loudly that this thread is the coordinator; the universe listens (mostly).
                ConcurrencyGuard.RegisterCoordinatorThread();
                ConcurrencyGuard.RegisterEngineThread();

                TransactionV2.Run(tx =>
                {
                    item.BuildWrites(tx);
                });

                foreach (var hook in item.BuildCommitHooks())
                {
                    try { hook(); } catch (Exception exHook) { Console.Error.WriteLine($"[ConvergenceCoordinator] Commit hook error: {exHook.Message}"); }
                }
                _ = item.GetResultAfterCommit(); // Ignore result unless tests need a prophecy.
            }
            catch (Exception ex)
            {
                // Surface exception immediately so tests fail fast instead of hanging in FlushAsync.
                Console.Error.WriteLine($"[ConvergenceCoordinator] Work item failed: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                // No rethrow: the queue must carry on, like a determined penguin.
            }
        }

        private void UpdateMaxDepth(long newDepth)
        {
            long current;
            while (newDepth > (current = Interlocked.Read(ref _maxDepth)))
            {
                Interlocked.CompareExchange(ref _maxDepth, newDepth, current);
            }
        }

        public long Processed => Interlocked.Read(ref _processed);
        public long Enqueued => Interlocked.Read(ref _enqueueCount);
        public long MaxDepth => Interlocked.Read(ref _maxDepth);
        public double AverageLatencyMs
        {
            get
            {
                long processed = Interlocked.Read(ref _processed);
                if (processed == 0) return 0;
                double ticks = Interlocked.Read(ref _totalLatencyTicks) / (double)processed;
                return ticks * 1000.0 / Stopwatch.Frequency;
            }
        }

        /// <summary>
        /// Waits until the queue is empty, the kettle has boiled, and all improbabilities have settled.
        /// </summary>
        public async Task FlushAsync()
        {
            // Marker: enqueue a no-op item and wait until processed count catches up.
            long target = Interlocked.Read(ref _enqueueCount);
            while (Interlocked.Read(ref _processed) < target)
            {
                await Task.Delay(5).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            // optional unregister; multiple work items may run on same thread, like buses.
            ConcurrencyGuard.UnregisterCoordinatorThread();
            ConcurrencyGuard.UnregisterEngineThread();
            _cts.Cancel();
            try { _worker.Wait(250); } catch { }
            _cts.Dispose();
        }
    }
}
