using QuantumSuperposition.Core;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;
using System.Numerics;

namespace QuantumSuperposition.NoiseProperties
{
    /// <summary>
    /// Multi-qubit readout mitigation assuming independent (per-qubit) assignment errors.
    /// Calibrates a 2x2 confusion matrix per qubit and applies the tensored inverse to measurement counts.
    /// </summary>
    public sealed class TensoredReadoutMitigator
    {
        private readonly QuantumSystem? _system;
        private readonly NoisyQuantumSystem? _noisySystem;

        private int[] _qubitIndices = Array.Empty<int>();

        private readonly Dictionary<int, ReadoutErrorMatrix> _perQubitConfusion = new();
        private readonly Dictionary<int, double[,]> _perQubitInverse = new();

        public IReadOnlyList<int> QubitIndices => _qubitIndices;

        public IReadOnlyDictionary<int, ReadoutErrorMatrix> PerQubitConfusionMatrices => _perQubitConfusion;

        public TensoredReadoutMitigator(QuantumSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public TensoredReadoutMitigator(NoisyQuantumSystem system)
        {
            _noisySystem = system ?? throw new ArgumentNullException(nameof(system));
        }

        public TensoredReadoutMitigator(IReadOnlyDictionary<int, ReadoutErrorMatrix> perQubitConfusionMatrices, int[] qubitIndices)
        {
            if (perQubitConfusionMatrices is null) throw new ArgumentNullException(nameof(perQubitConfusionMatrices));
            if (qubitIndices is null) throw new ArgumentNullException(nameof(qubitIndices));
            if (qubitIndices.Length == 0) throw new ArgumentException("At least one qubit index is required.", nameof(qubitIndices));

            _qubitIndices = (int[])qubitIndices.Clone();

            foreach (int q in _qubitIndices)
            {
                if (!perQubitConfusionMatrices.TryGetValue(q, out ReadoutErrorMatrix m))
                    throw new ArgumentException($"Missing confusion matrix for qubit index {q}.", nameof(perQubitConfusionMatrices));

                _perQubitConfusion[q] = m;
                _perQubitInverse[q] = InvertForMitigation(m);
            }
        }

        public void Calibrate(int[] qubitIndices) => Calibrate(qubitIndices, shots: 1024, rng: null);

        public void Calibrate(int[] qubitIndices, int shots = 1024, Random? rng = null)
        {
            if (qubitIndices is null) throw new ArgumentNullException(nameof(qubitIndices));
            if (qubitIndices.Length == 0) throw new ArgumentException("At least one qubit index is required.", nameof(qubitIndices));
            if (shots <= 0) throw new ArgumentOutOfRangeException(nameof(shots));

            rng ??= Random.Shared;

            _qubitIndices = (int[])qubitIndices.Clone();
            _perQubitConfusion.Clear();
            _perQubitInverse.Clear();

            foreach (int qi in _qubitIndices)
            {
                ReadoutErrorMatrix m = CalibrateSingleQubit(qi, shots, rng);
                _perQubitConfusion[qi] = m;
                _perQubitInverse[qi] = InvertForMitigation(m);
            }
        }

        public MitigationResult Mitigate(Dictionary<string, int> counts, bool clampNegativeToZero = true)
        {
            if (_qubitIndices.Length == 0) throw new InvalidOperationException("Call Calibrate(qubitIndices) before Mitigate(...).");

            int n = _qubitIndices.Length;
            if (counts is null) throw new ArgumentNullException(nameof(counts));
            if (counts.Keys.Any(k => k.Length != n))
                throw new ArgumentException($"All count keys must be bitstrings of length {n}.", nameof(counts));

            double[] measured = new double[1 << n];
            foreach (var kv in counts)
            {
                int idx = BitStringToIndex(kv.Key);
                measured[idx] += kv.Value;
            }

            return MitigateVector(measured, clampNegativeToZero);
        }

        /// <summary>
        /// Mitigates a multi-qubit histogram keyed by <c>int[]</c> bit arrays and returns a probability distribution.
        /// The input bit ordering must match the ordering used when calling <see cref="Calibrate(int[],int,Random?)"/>.
        /// </summary>
        public Dictionary<int[], double> MitigateToIntArrayFrequencies(Dictionary<int[], int> counts, bool clampNegativeToZero = true)
        {
            if (_qubitIndices.Length == 0) throw new InvalidOperationException("Call Calibrate(qubitIndices) before Mitigate(...).");

            int n = _qubitIndices.Length;
            if (counts is null) throw new ArgumentNullException(nameof(counts));

            double[] measured = new double[1 << n];
            foreach (var kv in counts)
            {
                if (kv.Key.Length != n)
                    throw new ArgumentException($"All count keys must have length {n}.", nameof(counts));

                int idx = QuantumAlgorithms.BitsToIndex(kv.Key);
                measured[idx] += kv.Value;
            }

            double[] mitigatedCounts = (double[])measured.Clone();
            ApplyTensoredInverseInPlace(mitigatedCounts);

            if (clampNegativeToZero)
            {
                for (int i = 0; i < mitigatedCounts.Length; i++)
                    if (mitigatedCounts[i] < 0) mitigatedCounts[i] = 0;
            }

            double total = mitigatedCounts.Sum();
            if (total <= 0) total = 1.0;

            Dictionary<int[], double> result = new(new IntArrayComparer());
            for (int i = 0; i < mitigatedCounts.Length; i++)
            {
                int[] bits = QuantumAlgorithms.IndexToBits(i, n);
                result[bits] = mitigatedCounts[i] / total;
            }

            return result;
        }

        public MitigationResult Mitigate(Dictionary<int[], int> counts, bool clampNegativeToZero = true)
        {
            if (_qubitIndices.Length == 0) throw new InvalidOperationException("Call Calibrate(qubitIndices) before Mitigate(...).");

            int n = _qubitIndices.Length;
            if (counts is null) throw new ArgumentNullException(nameof(counts));

            double[] measured = new double[1 << n];
            foreach (var kv in counts)
            {
                if (kv.Key.Length != n)
                    throw new ArgumentException($"All count keys must have length {n}.", nameof(counts));

                int idx = QuantumAlgorithms.BitsToIndex(kv.Key);
                measured[idx] += kv.Value;
            }

            return MitigateVector(measured, clampNegativeToZero);
        }

        private MitigationResult MitigateVector(double[] measuredCounts, bool clampNegativeToZero)
        {
            int n = _qubitIndices.Length;

            Dictionary<string, double> rawFreq = ToFrequencies(measuredCounts);

            double[] mitigatedCounts = (double[])measuredCounts.Clone();
            ApplyTensoredInverseInPlace(mitigatedCounts);

            if (clampNegativeToZero)
            {
                for (int i = 0; i < mitigatedCounts.Length; i++)
                    if (mitigatedCounts[i] < 0) mitigatedCounts[i] = 0;
            }

            Dictionary<string, double> mitigatedFreq = ToFrequencies(mitigatedCounts);

            double tvd = 0.0;
            foreach (var kv in rawFreq)
            {
                double p = kv.Value;
                double q = mitigatedFreq.TryGetValue(kv.Key, out double v) ? v : 0.0;
                tvd += Math.Abs(p - q);
            }

            tvd *= 0.5;

            return new MitigationResult
            {
                RawFrequencies = rawFreq,
                MitigatedFrequencies = mitigatedFreq,
                TotalImprovement = tvd
            };
        }

        private void ApplyTensoredInverseInPlace(double[] vector)
        {
            int n = _qubitIndices.Length;
            int dim = 1 << n;
            if (vector.Length != dim) throw new ArgumentException("Vector length does not match qubit count.", nameof(vector));

            for (int pos = 0; pos < n; pos++)
            {
                int qubitIndex = _qubitIndices[pos];
                if (!_perQubitInverse.TryGetValue(qubitIndex, out double[,] inv))
                    throw new InvalidOperationException($"No inverse confusion matrix found for qubit index {qubitIndex}. Did you calibrate?");

                double a00 = inv[0, 0];
                double a01 = inv[0, 1];
                double a10 = inv[1, 0];
                double a11 = inv[1, 1];

                int stride = 1 << (n - 1 - pos);
                int blockSize = stride * 2;

                for (int block = 0; block < dim; block += blockSize)
                {
                    for (int offset = 0; offset < stride; offset++)
                    {
                        int idx0 = block + offset;
                        int idx1 = idx0 + stride;

                        double m0 = vector[idx0];
                        double m1 = vector[idx1];

                        vector[idx0] = a00 * m0 + a01 * m1;
                        vector[idx1] = a10 * m0 + a11 * m1;
                    }
                }
            }
        }

        private ReadoutErrorMatrix CalibrateSingleQubit(int qubitIndex, int shots, Random rng)
        {
            (int meas0When0, _) = RunPrepareMeasure(qubitIndex, preparedBit: 0, shots, rng);
            (int meas0When1, _) = RunPrepareMeasure(qubitIndex, preparedBit: 1, shots, rng);

            double p00 = (double)meas0When0 / shots;
            double p01 = 1.0 - p00;

            double p10 = (double)meas0When1 / shots;
            double p11 = 1.0 - p10;

            return new ReadoutErrorMatrix(p00, p01, p10, p11);
        }

        private (int measured0, int measured1) RunPrepareMeasure(int qubitIndex, int preparedBit, int shots, Random rng)
        {
            QuantumSystem? system = _noisySystem?.InnerSystem ?? _system;
            if (system is null) throw new InvalidOperationException("No system instance was provided.");

            int measured0 = 0;
            int measured1 = 0;

            for (int i = 0; i < shots; i++)
            {
                system.SetAmplitudes(MakeBasisStateAmps(qubitIndex, preparedBit));

                int[] result = _noisySystem != null
                    ? _noisySystem.ObserveGlobal(new[] { qubitIndex }, rng)
                    : system.ObserveGlobal(new[] { qubitIndex }, rng);

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

        private static int BitStringToIndex(string bits)
        {
            int idx = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                idx <<= 1;
                char c = bits[i];
                if (c == '1') idx |= 1;
                else if (c != '0') throw new ArgumentException("Bitstrings must contain only '0' or '1'.", nameof(bits));
            }
            return idx;
        }

        private static Dictionary<string, double> ToFrequencies(double[] counts)
        {
            int n = (int)Math.Log2(counts.Length);
            double total = counts.Sum();
            if (total <= 0) total = 1.0;

            Dictionary<string, double> result = new(counts.Length);
            for (int i = 0; i < counts.Length; i++)
            {
                int[] bits = QuantumAlgorithms.IndexToBits(i, n);
                string key = string.Concat(bits.Select(b => b == 0 ? '0' : '1'));
                result[key] = counts[i] / total;
            }

            return result;
        }

        private static double[,] InvertForMitigation(ReadoutErrorMatrix m)
        {
            double det = m.P00 * m.P11 - m.P01 * m.P10;
            if (Math.Abs(det) < 1e-15)
                throw new InvalidOperationException("Readout confusion matrix is singular (non-invertible). Cannot mitigate.");

            double invDet = 1.0 / det;

            // m is P(measured | actual) with rows=actual, cols=measured.
            // We use (m^T)^{-1} so that: true = (m^T)^{-1} * measured.
            return new[,]
            {
                {  m.P11 * invDet, -m.P10 * invDet },
                { -m.P01 * invDet,  m.P00 * invDet }
            };
        }
    }
}
