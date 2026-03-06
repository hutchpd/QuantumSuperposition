using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;
using System.Numerics;

namespace QuantumSuperposition.NoiseProperties
{
    /// <summary>
    /// Readout error mitigation via a calibrated 2x2 confusion matrix.
    /// Calibrate measures prepared |0> and |1> states to estimate the matrix;
    /// Mitigate applies the inverse to measured outcome counts.
    /// </summary>
    public sealed class ReadoutMitigator
    {
        public ReadoutErrorMatrix ConfusionMatrix { get; }

        /// <summary>
        /// Inverse of the confusion matrix (not necessarily stochastic; entries may be negative).
        /// Layout matches <see cref="ReadoutErrorMatrix"/>: row=actual, col=measured.
        /// </summary>
        public double[,] InverseMatrix { get; }

        public ReadoutMitigator(ReadoutErrorMatrix confusionMatrix)
        {
            ConfusionMatrix = confusionMatrix;
            InverseMatrix = Invert(confusionMatrix);
        }

        public static ReadoutMitigator Calibrate(QuantumSystem system) => Calibrate(system, qubitIndex: 0, shots: 1024, rng: null);

        public static ReadoutMitigator Calibrate(QuantumSystem system, int qubitIndex, int shots = 1024, Random? rng = null)
        {
            if (system is null) throw new ArgumentNullException(nameof(system));
            if (qubitIndex < 0) throw new ArgumentOutOfRangeException(nameof(qubitIndex));
            if (shots <= 0) throw new ArgumentOutOfRangeException(nameof(shots));

            rng ??= Random.Shared;

            (int meas0When0, int meas1When0) = RunPrepareMeasure(system, qubitIndex, preparedBit: 0, shots, rng);
            (int meas0When1, int meas1When1) = RunPrepareMeasure(system, qubitIndex, preparedBit: 1, shots, rng);

            double p00 = (double)meas0When0 / shots;
            double p01 = 1.0 - p00;

            double p10 = (double)meas0When1 / shots;
            double p11 = 1.0 - p10;

            return new ReadoutMitigator(new ReadoutErrorMatrix(p00, p01, p10, p11));
        }

        public static ReadoutMitigator Calibrate(NoisyQuantumSystem system) => Calibrate(system, qubitIndex: 0, shots: 1024, rng: null);

        public static ReadoutMitigator Calibrate(NoisyQuantumSystem system, int qubitIndex, int shots = 1024, Random? rng = null)
        {
            if (system is null) throw new ArgumentNullException(nameof(system));
            if (qubitIndex < 0) throw new ArgumentOutOfRangeException(nameof(qubitIndex));
            if (shots <= 0) throw new ArgumentOutOfRangeException(nameof(shots));

            rng ??= Random.Shared;

            (int meas0When0, int meas1When0) = RunPrepareMeasure(system, qubitIndex, preparedBit: 0, shots, rng);
            (int meas0When1, int meas1When1) = RunPrepareMeasure(system, qubitIndex, preparedBit: 1, shots, rng);

            double p00 = (double)meas0When0 / shots;
            double p01 = 1.0 - p00;

            double p10 = (double)meas0When1 / shots;
            double p11 = 1.0 - p10;

            return new ReadoutMitigator(new ReadoutErrorMatrix(p00, p01, p10, p11));
        }

        public Dictionary<string, double> Mitigate(Dictionary<string, int> counts, bool clampNegativeToZero = true)
            => Mitigate(counts, ConfusionMatrix, clampNegativeToZero);

        public static Dictionary<string, double> Mitigate(Dictionary<string, int> counts, ReadoutErrorMatrix confusionMatrix, bool clampNegativeToZero = true)
        {
            if (counts is null) throw new ArgumentNullException(nameof(counts));

            if (counts.Keys.Any(k => k is not "0" and not "1"))
                throw new NotSupportedException("Readout mitigation currently supports single-qubit counts with keys '0' and '1'.");

            double m0 = counts.TryGetValue("0", out int c0) ? c0 : 0;
            double m1 = counts.TryGetValue("1", out int c1) ? c1 : 0;

            double[,] inv = Invert(confusionMatrix);

            // t = inv * m
            double t0 = inv[0, 0] * m0 + inv[0, 1] * m1;
            double t1 = inv[1, 0] * m0 + inv[1, 1] * m1;

            if (clampNegativeToZero)
            {
                if (t0 < 0) t0 = 0;
                if (t1 < 0) t1 = 0;
            }

            return new Dictionary<string, double>
            {
                ["0"] = t0,
                ["1"] = t1
            };
        }

        private static double[,] Invert(ReadoutErrorMatrix m)
        {
            double det = m.P00 * m.P11 - m.P01 * m.P10;
            if (Math.Abs(det) < 1e-15)
                throw new InvalidOperationException("Readout confusion matrix is singular (non-invertible). Cannot mitigate.");

            double invDet = 1.0 / det;

            // m is defined as P(measured | actual) with rows=actual, cols=measured.
            // For mitigation we need (m^T)^{-1} so that: true = (m^T)^{-1} * measured.
            return new[,]
            {
                {  m.P11 * invDet, -m.P10 * invDet },
                { -m.P01 * invDet,  m.P00 * invDet }
            };
        }

        private static (int measured0, int measured1) RunPrepareMeasure(QuantumSystem system, int qubitIndex, int preparedBit, int shots, Random rng)
        {
            int measured0 = 0;
            int measured1 = 0;

            for (int i = 0; i < shots; i++)
            {
                system.SetAmplitudes(MakeBasisStateAmps(qubitIndex, preparedBit));
                int[] result = system.ObserveGlobal(new[] { qubitIndex }, rng);
                if (result.Length != 1) throw new InvalidOperationException("Expected single-bit measurement.");

                if (result[0] == 0) measured0++;
                else if (result[0] == 1) measured1++;
            }

            return (measured0, measured1);
        }

        private static (int measured0, int measured1) RunPrepareMeasure(NoisyQuantumSystem system, int qubitIndex, int preparedBit, int shots, Random rng)
        {
            int measured0 = 0;
            int measured1 = 0;

            for (int i = 0; i < shots; i++)
            {
                system.InnerSystem.SetAmplitudes(MakeBasisStateAmps(qubitIndex, preparedBit));
                int[] result = system.ObserveGlobal(new[] { qubitIndex }, rng);
                if (result.Length != 1) throw new InvalidOperationException("Expected single-bit measurement.");

                if (result[0] == 0) measured0++;
                else if (result[0] == 1) measured1++;
            }

            return (measured0, measured1);
        }

        private static Dictionary<int[], Complex> MakeBasisStateAmps(int qubitIndex, int bit)
        {
            int[] state = new int[qubitIndex + 1];
            state[qubitIndex] = bit;
            return new Dictionary<int[], Complex>(new IntArrayComparer())
            {
                [state] = Complex.One
            };
        }
    }
}
