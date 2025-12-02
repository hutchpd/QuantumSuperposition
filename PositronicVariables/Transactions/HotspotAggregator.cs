using System;
using System.Collections.Generic;
using System.Linq;

namespace PositronicVariables.Transactions
{
    /// <summary>
    /// Aggregates STMTelemetry into hotspot stats and applies mitigation strategies.
    /// </summary>
    public sealed class HotspotAggregator
    {
        private readonly List<IHotspotMitigationStrategy> _strategies = new();

        public void RegisterStrategy(IHotspotMitigationStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            _strategies.Add(strategy);
        }

        public IEnumerable<HotspotStats> ComputeStats()
        {
            var perVar = STMTelemetry.GetPerVariableStats();
            long totalAborts = STMTelemetry.TotalAborts;
            long totalCommits = STMTelemetry.TotalCommits;
            double abortRateGlobal = totalCommits == 0 ? 0.0 : (double)totalAborts / totalCommits;

            foreach (var (txId, failures, writes, lockTicks) in perVar)
            {
                double avgLock = writes == 0 ? 0.0 : (double)lockTicks / writes;
                // Approximate abort rate for this var as share of failures relative to total commits
                double abortRate = totalCommits == 0 ? 0.0 : Math.Min(1.0, (double)failures / totalCommits);
                yield return new HotspotStats(txId, abortRate, avgLock, failures, writes);
            }
        }

        public IEnumerable<(long tvarId, IHotspotMitigationStrategy strategy)> DetectHotspots()
        {
            foreach (var s in ComputeStats())
            {
                foreach (var strat in _strategies)
                {
                    if (strat.ShouldMitigate(s.TxId, s))
                    {
                        yield return (s.TxId, strat);
                    }
                }
            }
        }

        public void ApplyDetectedMitigations()
        {
            foreach (var (id, strat) in DetectHotspots())
            {
                strat.ApplyMitigation(id);
            }
        }
    }
}
