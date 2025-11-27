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
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<IConvergenceWorkItem>(options);
            ConcurrencyGuard.RegisterCoordinatorThread();
            _worker = Task.Run(WorkerLoop);
        }

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
            ConcurrencyGuard.RegisterEngineThread();
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
            using var tx = TransactionV2.Begin();
            item.BuildWrites(tx);
            tx.Commit();
            foreach (var hook in item.BuildCommitHooks())
            {
                try { hook(); } catch { }
            }
            _ = item.GetResultAfterCommit(); // ignore result unless test uses it
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
            _cts.Cancel();
            try { _worker.Wait(250); } catch { }
            _cts.Dispose();
        }
    }
}
