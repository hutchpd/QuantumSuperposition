using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PositronicVariables.Transactions
{
    public static class STMTelemetry
    {
        private static long _totalCommits;
        private static long _totalReadOnlyCommits;
        private static long _totalAborts;
        private static long _totalRetries;
        private static long _totalValidationFailures;
        private static long _totalWritesApplied;
        private static long _totalLockHoldTicks;
        private static long _maxLockHoldTicks;

        // Track hotspots by TxId (identity-based)
        private static readonly ConcurrentDictionary<long, long> _contentionCounts = new();

        public static long TotalCommits => System.Threading.Interlocked.Read(ref _totalCommits);
        public static long TotalReadOnlyCommits => System.Threading.Interlocked.Read(ref _totalReadOnlyCommits);
        public static long TotalAborts => System.Threading.Interlocked.Read(ref _totalAborts);
        public static long TotalRetries => System.Threading.Interlocked.Read(ref _totalRetries);
        public static long TotalValidationFailures => System.Threading.Interlocked.Read(ref _totalValidationFailures);
        public static long TotalWritesApplied => System.Threading.Interlocked.Read(ref _totalWritesApplied);
        public static long TotalLockHoldTicks => System.Threading.Interlocked.Read(ref _totalLockHoldTicks);
        public static long MaxLockHoldTicks => System.Threading.Interlocked.Read(ref _maxLockHoldTicks);

        public static void RecordCommit(bool readOnly, int writesApplied, long lockHoldTicks)
        {
            System.Threading.Interlocked.Increment(ref _totalCommits);
            if (readOnly)
            {
                System.Threading.Interlocked.Increment(ref _totalReadOnlyCommits);
            }
            if (writesApplied > 0)
            {
                System.Threading.Interlocked.Add(ref _totalWritesApplied, writesApplied);
            }
            if (lockHoldTicks > 0)
            {
                System.Threading.Interlocked.Add(ref _totalLockHoldTicks, lockHoldTicks);
                long prevMax;
                long current = lockHoldTicks;
                do
                {
                    prevMax = _maxLockHoldTicks;
                    if (current <= prevMax) break;
                } while (System.Threading.Interlocked.CompareExchange(ref _maxLockHoldTicks, current, prevMax) != prevMax);
            }
        }

        public static void RecordRetry()
        {
            System.Threading.Interlocked.Increment(ref _totalRetries);
        }

        public static void RecordAbort()
        {
            System.Threading.Interlocked.Increment(ref _totalAborts);
        }

        public static void RecordValidationFailure(long txId)
        {
            System.Threading.Interlocked.Increment(ref _totalValidationFailures);
            _contentionCounts.AddOrUpdate(txId, 1, (_, v) => v + 1);
        }

        public static (long txId, long count)[] GetContentionHotspots(int top = 10)
        {
            return _contentionCounts
                .OrderByDescending(kv => kv.Value)
                .Take(top)
                .Select(kv => (kv.Key, kv.Value))
                .ToArray();
        }

        public static void Reset()
        {
            _totalCommits = 0;
            _totalReadOnlyCommits = 0;
            _totalAborts = 0;
            _totalRetries = 0;
            _totalValidationFailures = 0;
            _totalWritesApplied = 0;
            _totalLockHoldTicks = 0;
            _maxLockHoldTicks = 0;
            _contentionCounts.Clear();
        }

        public static string GetReport()
        {
            StringBuilder sb = new();
            sb.AppendLine("STM Telemetry Report");
            sb.AppendLine($"TotalCommits={TotalCommits}");
            sb.AppendLine($"ReadOnlyCommits={TotalReadOnlyCommits}");
            sb.AppendLine($"TotalRetries={TotalRetries}");
            sb.AppendLine($"TotalAborts={TotalAborts}");
            sb.AppendLine($"ValidationFailures={TotalValidationFailures}");
            sb.AppendLine($"WritesApplied={TotalWritesApplied}");
            sb.AppendLine($"LockHoldTicksTotal={TotalLockHoldTicks}");
            sb.AppendLine($"LockHoldTicksMax={MaxLockHoldTicks}");
            var hotspots = GetContentionHotspots();
            if (hotspots.Length > 0)
            {
                sb.AppendLine("Hotspots (TxId:Count):");
                foreach (var (txId, cnt) in hotspots)
                {
                    sb.AppendLine($"  {txId}:{cnt}");
                }
            }
            return sb.ToString();
        }
    }
}
