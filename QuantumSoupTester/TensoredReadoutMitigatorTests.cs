using QuantumSuperposition.NoiseProperties;

namespace QuantumSoupTester
{
    [TestFixture]
    public class TensoredReadoutMitigatorTests
    {
        [Test]
        public void Mitigate_TwoQubits_KnownPerQubitMatrices_ApproximatelyRecoversDistribution()
        {
            // Two qubits with different readout characteristics.
            ReadoutErrorMatrix q0 = ReadoutErrorMatrix.FromFlipProbabilities(p01: 0.2, p10: 0.1); // p00=0.8 p11=0.9
            ReadoutErrorMatrix q1 = ReadoutErrorMatrix.FromFlipProbabilities(p01: 0.3, p10: 0.2); // p00=0.7 p11=0.8

            int[] qubits = new[] { 0, 1 };
            var per = new Dictionary<int, ReadoutErrorMatrix> { [0] = q0, [1] = q1 };
            TensoredReadoutMitigator mitigator = new(per, qubits);

            // True counts (must be >= 0). Use a large total so rounding of simulated measurement counts is negligible.
            var trueCounts = new Dictionary<string, int>
            {
                ["00"] = 500_000,
                ["01"] = 200_000,
                ["10"] = 200_000,
                ["11"] = 100_000
            };

            Dictionary<string, int> measuredCounts = SimulateMeasuredCounts(trueCounts, q0, q1);

            MitigationResult result = mitigator.Mitigate(measuredCounts, clampNegativeToZero: false);

            // Compare frequencies; mitigation should bring us close to the original distribution.
            Assert.That(result.MitigatedFrequencies["00"], Is.EqualTo(0.5).Within(0.01));
            Assert.That(result.MitigatedFrequencies["01"], Is.EqualTo(0.2).Within(0.01));
            Assert.That(result.MitigatedFrequencies["10"], Is.EqualTo(0.2).Within(0.01));
            Assert.That(result.MitigatedFrequencies["11"], Is.EqualTo(0.1).Within(0.01));

            Assert.That(result.TotalImprovement, Is.GreaterThan(0.0));
        }

        [Test]
        public void MitigateToIntArrayFrequencies_ReturnsIntArrayKeyedDistribution()
        {
            ReadoutErrorMatrix q0 = ReadoutErrorMatrix.FromFlipProbabilities(p01: 0.2, p10: 0.1);
            ReadoutErrorMatrix q1 = ReadoutErrorMatrix.FromFlipProbabilities(p01: 0.3, p10: 0.2);

            int[] qubits = new[] { 0, 1 };
            var per = new Dictionary<int, ReadoutErrorMatrix> { [0] = q0, [1] = q1 };
            TensoredReadoutMitigator mitigator = new(per, qubits);

            var trueCounts = new Dictionary<int[], int>(new QuantumSuperposition.Utilities.IntArrayComparer())
            {
                [new[] { 0, 0 }] = 50,
                [new[] { 0, 1 }] = 20,
                [new[] { 1, 0 }] = 20,
                [new[] { 1, 1 }] = 10,
            };

            Dictionary<int[], double> freq = mitigator.MitigateToIntArrayFrequencies(trueCounts);

            Assert.That(freq.Keys.Any(k => k.SequenceEqual(new[] { 0, 0 })), Is.True);
            Assert.That(freq.Values.Sum(), Is.EqualTo(1.0).Within(1e-12));
        }

        private static Dictionary<string, int> SimulateMeasuredCounts(
            Dictionary<string, int> trueCounts,
            ReadoutErrorMatrix q0,
            ReadoutErrorMatrix q1)
        {
            // Forward model: m = (C^T)^{\otimes n} * t.
            // Do it by expanding each true outcome into all measured outcomes.
            // (This is test-only; production mitigation uses O(n*2^n) without materializing matrices.)

            var measured = new Dictionary<string, double>
            {
                ["00"] = 0,
                ["01"] = 0,
                ["10"] = 0,
                ["11"] = 0
            };

            foreach (var kv in trueCounts)
            {
                string actual = kv.Key;
                double weight = kv.Value;

                int a0 = actual[0] == '1' ? 1 : 0;
                int a1 = actual[1] == '1' ? 1 : 0;

                // P(measBit | actualBit) for each qubit.
                double p00_q0 = a0 == 0 ? q0.P00 : q0.P10;
                double p01_q0 = a0 == 0 ? q0.P01 : q0.P11;

                double p00_q1 = a1 == 0 ? q1.P00 : q1.P10;
                double p01_q1 = a1 == 0 ? q1.P01 : q1.P11;

                measured["00"] += weight * p00_q0 * p00_q1;
                measured["01"] += weight * p00_q0 * p01_q1;
                measured["10"] += weight * p01_q0 * p00_q1;
                measured["11"] += weight * p01_q0 * p01_q1;
            }

            return measured.ToDictionary(k => k.Key, v => (int)Math.Round(v.Value));
        }
    }
}
