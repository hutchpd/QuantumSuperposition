using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PositronicVariables.Tests
{
    /// <summary>
    /// Utilities to run concurrent tasks with deterministic, synchronized start.
    /// </summary>
    internal static class ConcurrencyTestHelpers
    {
        /// <summary>
        /// Runs n tasks which all wait on a shared start signal, then execute the provided action.
        /// Returns all tasks to allow awaiting and error aggregation.
        /// </summary>
        public static IReadOnlyList<Task> RunConcurrentTasks(int n, Action action, TimeSpan? timeout = null)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var start = new ManualResetEventSlim(false);
            var ready = new CountdownEvent(n);
            var tasks = new List<Task>(n);

            for (int i = 0; i < n; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // signal that this worker is ready to start
                        ready.Signal();
                        // wait for the shared start gate
                        start.Wait(timeout ?? TimeSpan.FromSeconds(10));
                        action();
                    }
                    catch (Exception ex)
                    {
                        // bubble up exceptions to caller via task fault
                        throw new InvalidOperationException($"Worker failed: {ex.Message}", ex);
                    }
                }));
            }

            // wait until all workers are ready, then release the gate
            if (!ready.Wait(timeout ?? TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Workers did not become ready in time.");

            start.Set();
            return tasks;
        }
    }
}
