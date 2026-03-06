using QuantumSuperposition.Systems;

namespace QuantumSuperposition.NoiseProperties
{
    public enum ExtrapolationType
    {
        Linear,
        Polynomial
    }

    public enum NoiseScalingMethod
    {
        ScaleNoiseModel
    }

    /// <summary>
    /// A minimal ZNE circuit wrapper: contains a baseline noise model and a function that executes the circuit
    /// on a provided noisy system and returns a scalar observable.
    /// </summary>
    public sealed record ZneCircuit(NoiseModel BaseNoiseModel, Func<NoisyQuantumSystem, double> Run);

    public sealed class ZeroNoiseExtrapolation
    {
        public double Execute(
            ZneCircuit circuit,
            double[] noiseScales,
            ExtrapolationType extrapolation = ExtrapolationType.Polynomial,
            NoiseScalingMethod scalingMethod = NoiseScalingMethod.ScaleNoiseModel,
            Random? rng = null)
        {
            if (circuit is null) throw new ArgumentNullException(nameof(circuit));
            if (noiseScales is null) throw new ArgumentNullException(nameof(noiseScales));
            if (noiseScales.Length < 2) throw new ArgumentException("At least two noise scales are required for extrapolation.", nameof(noiseScales));
            if (noiseScales.Any(s => s <= 0)) throw new ArgumentOutOfRangeException(nameof(noiseScales), "Noise scales must be > 0.");

            rng ??= Random.Shared;

            double[] values = new double[noiseScales.Length];
            for (int i = 0; i < noiseScales.Length; i++)
            {
                double scale = noiseScales[i];
                NoiseModel scaled = scalingMethod switch
                {
                    NoiseScalingMethod.ScaleNoiseModel => ScaleNoiseModel(circuit.BaseNoiseModel, scale),
                    _ => throw new NotSupportedException($"Unsupported scaling method: {scalingMethod}")
                };

                // Each scale runs on a fresh system to avoid cross-run state leakage.
                NoisyQuantumSystem sys = new(scaled);
                values[i] = circuit.Run(sys);
            }

            return extrapolation switch
            {
                ExtrapolationType.Linear => ExtrapolateLinearToZero(noiseScales, values),
                ExtrapolationType.Polynomial => ExtrapolatePolynomialToZero(noiseScales, values),
                _ => throw new NotSupportedException($"Unsupported extrapolation type: {extrapolation}")
            };
        }

        private static NoiseModel ScaleNoiseModel(NoiseModel baseModel, double scale)
        {
            double single = Clamp01(baseModel.SingleQubitErrorRate * scale);
            double two = Clamp01(baseModel.TwoQubitErrorRate * scale);

            // Scale readout flip probs (off-diagonals) and reconstruct a valid stochastic matrix.
            double p01 = Clamp01(baseModel.ReadoutErrorMatrix.P01 * scale);
            double p10 = Clamp01(baseModel.ReadoutErrorMatrix.P10 * scale);
            ReadoutErrorMatrix readout = ReadoutErrorMatrix.FromFlipProbabilities(p01, p10);

            return new NoiseModel(
                singleQubitErrorRate: single,
                twoQubitErrorRate: two,
                readoutErrorMatrix: readout,
                thermalRelaxation: baseModel.ThermalRelaxation);
        }

        private static double ExtrapolateLinearToZero(double[] x, double[] y)
        {
            // Least-squares fit y = a + b x; return a.
            int n = x.Length;

            double sumX = 0, sumY = 0, sumXX = 0, sumXY = 0;
            for (int i = 0; i < n; i++)
            {
                sumX += x[i];
                sumY += y[i];
                sumXX += x[i] * x[i];
                sumXY += x[i] * y[i];
            }

            double denom = n * sumXX - sumX * sumX;
            if (Math.Abs(denom) < 1e-15)
                throw new InvalidOperationException("Cannot perform linear extrapolation: degenerate x values.");

            double b = (n * sumXY - sumX * sumY) / denom;
            double a = (sumY - b * sumX) / n;
            return a;
        }

        private static double ExtrapolatePolynomialToZero(double[] x, double[] y)
        {
            // Use Lagrange interpolation through all points and evaluate at x=0.
            // This gives an educational, deterministic curve for small point sets.
            int n = x.Length;
            double result = 0.0;

            for (int i = 0; i < n; i++)
            {
                double liAt0 = 1.0;
                for (int j = 0; j < n; j++)
                {
                    if (j == i) continue;
                    double denom = x[i] - x[j];
                    if (Math.Abs(denom) < 1e-15)
                        throw new InvalidOperationException("Cannot perform polynomial extrapolation: duplicate x values.");
                    liAt0 *= (0.0 - x[j]) / denom;
                }
                result += y[i] * liAt0;
            }

            return result;
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }
}
