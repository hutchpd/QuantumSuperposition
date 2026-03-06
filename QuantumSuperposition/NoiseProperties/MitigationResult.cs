namespace QuantumSuperposition.NoiseProperties
{
    public sealed class MitigationResult
    {
        public required Dictionary<string, double> RawFrequencies { get; init; }
        public required Dictionary<string, double> MitigatedFrequencies { get; init; }

        /// <summary>
        /// Total variation distance between raw and mitigated distributions:
        /// 0.5 * sum_x |p_raw(x) - p_mitigated(x)|.
        /// </summary>
        public double TotalImprovement { get; init; }
    }
}
