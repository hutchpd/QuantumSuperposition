using System.Collections.Concurrent;

namespace PositronicVariables.Transactions
{
    /// <summary>
    /// Runtime configuration for paranoia diagnostics and global fallback.
    /// Disabled by default; enable in CI or stress runs.
    /// </summary>
    public static class ParanoiaConfig
    {
        /// <summary>
        /// Enable additional assertions and lock acquisition diagnostics.
        /// </summary>
        public static volatile bool EnableParanoia;

        /// <summary>
        /// Enable global serialization fallback when contention is extreme.
        /// </summary>
        public static volatile bool EnableGlobalFallback;

        /// <summary>
        /// Threshold for (Retries + Aborts) to trigger fallback.
        /// </summary>
        public static long AbortRetryThreshold = long.MaxValue;

        /// <summary>
        /// Indicates if fallback is currently active.
        /// </summary>
        public static volatile bool FallbackActive;

        /// <summary>
        /// Optional stack traces recorded per TxId on lock acquisition (paranoia mode).
        /// </summary>
        public static readonly ConcurrentDictionary<long, string> LockStacks = new();

        internal static void Reset()
        {
            FallbackActive = false;
            LockStacks.Clear();
        }
    }
}
