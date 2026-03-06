using QuantumSuperposition.Systems;

namespace QuantumSuperposition.NoiseProperties
{
    public sealed record PecTerm(string Name, double Coefficient, Func<NoisyQuantumSystem, double> Run);

    public sealed class PecDecomposition
    {
        public NoiseModel BaseNoiseModel { get; }
        public IReadOnlyList<PecTerm> Terms { get; }

        /// <summary>
        /// Sampling overhead γ = Σ_k |η_k|. Larger overhead => higher variance.
        /// </summary>
        public double Overhead { get; }

        public PecDecomposition(NoiseModel baseNoiseModel, IReadOnlyList<PecTerm> terms)
        {
            BaseNoiseModel = baseNoiseModel ?? throw new ArgumentNullException(nameof(baseNoiseModel));
            Terms = terms ?? throw new ArgumentNullException(nameof(terms));
            if (Terms.Count == 0) throw new ArgumentException("At least one PEC term is required.", nameof(terms));

            double overhead = 0.0;
            foreach (var t in Terms)
            {
                if (t is null) throw new ArgumentException("PEC terms must not be null.", nameof(terms));
                if (t.Coefficient == 0) continue;
                overhead += Math.Abs(t.Coefficient);
            }

            if (overhead <= 0.0)
                throw new ArgumentException("PEC decomposition overhead must be > 0 (at least one coefficient must be non-zero).", nameof(terms));

            Overhead = overhead;
        }
    }

    public sealed class PecResult
    {
        public required double Estimate { get; init; }
        public required int Samples { get; init; }
        public required double Overhead { get; init; }

        public required double SampleMean { get; init; }
        public required double SampleVariance { get; init; }
    }

    /// <summary>
    /// Probabilistic Error Cancellation (PEC) sampler.
    /// Given a quasi-probability decomposition Σ_k η_k * f_k, sample terms proportional to |η_k|
    /// and re-weight results so the Monte Carlo average converges to the ideal value.
    /// </summary>
    public sealed class PECSampler
    {
        public PecResult Execute(PecDecomposition decomposition, int samples, Random? rng = null)
        {
            if (decomposition is null) throw new ArgumentNullException(nameof(decomposition));
            if (samples <= 0) throw new ArgumentOutOfRangeException(nameof(samples));

            rng ??= Random.Shared;

            // Build cumulative distribution over terms proportional to |η_k|.
            var terms = decomposition.Terms;
            double overhead = decomposition.Overhead;

            double[] cdf = new double[terms.Count];
            double cumulative = 0.0;
            for (int i = 0; i < terms.Count; i++)
            {
                double w = Math.Abs(terms[i].Coefficient);
                cumulative += w;
                cdf[i] = cumulative;
            }

            // Numerical safety: force last bucket to equal overhead.
            cdf[^1] = overhead;

            double sum = 0.0;
            double sumSq = 0.0;

            for (int s = 0; s < samples; s++)
            {
                int idx = SampleIndex(cdf, overhead, rng);
                PecTerm term = terms[idx];

                // Fresh system per sample to prevent cross-sample state leakage.
                NoisyQuantumSystem sys = new(decomposition.BaseNoiseModel);
                double value = term.Run(sys);

                double signed = term.Coefficient >= 0 ? 1.0 : -1.0;
                double weighted = overhead * signed * value;

                sum += weighted;
                sumSq += weighted * weighted;
            }

            double mean = sum / samples;
            double var = (sumSq / samples) - (mean * mean);
            if (var < 0 && var > -1e-12) var = 0; // numerical noise

            return new PecResult
            {
                Estimate = mean,
                Samples = samples,
                Overhead = overhead,
                SampleMean = mean,
                SampleVariance = var
            };
        }

        private static int SampleIndex(double[] cdf, double total, Random rng)
        {
            double r = rng.NextDouble() * total;
            int lo = 0;
            int hi = cdf.Length - 1;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) / 2);
                if (r <= cdf[mid]) hi = mid;
                else lo = mid + 1;
            }
            return lo;
        }
    }
}
