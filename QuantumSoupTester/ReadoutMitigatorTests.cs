using QuantumSuperposition.NoiseProperties;
using QuantumSuperposition.Systems;

namespace QuantumSoupTester
{
    [TestFixture]
    public class ReadoutMitigatorTests
    {
        [Test]
        public void Mitigate_KnownMatrix_RecoversOriginalCounts()
        {
            // Confusion matrix from flip probs:
            // actual 0 -> measured 1 with p01=0.2, actual 1 -> measured 0 with p10=0.1
            ReadoutErrorMatrix cm = ReadoutErrorMatrix.FromFlipProbabilities(p01: 0.2, p10: 0.1);
            ReadoutMitigator mitigator = new(cm);

            // True counts: t0=70, t1=30
            // Measured counts m = C * t:
            // m0 = 0.8*70 + 0.1*30 = 59
            // m1 = 0.2*70 + 0.9*30 = 41
            var measured = new Dictionary<string, int>
            {
                ["0"] = 59,
                ["1"] = 41
            };

            Dictionary<string, double> corrected = mitigator.Mitigate(measured, clampNegativeToZero: false);

            Assert.That(corrected["0"], Is.EqualTo(70).Within(1e-9));
            Assert.That(corrected["1"], Is.EqualTo(30).Within(1e-9));
        }

        [Test]
        public void Calibrate_NoiseFreeSystem_ProducesIdentityMatrix()
        {
            QuantumSystem system = new();
            ReadoutMitigator mitigator = ReadoutMitigator.Calibrate(system, qubitIndex: 0, shots: 32, rng: new Random(1));

            Assert.That(mitigator.ConfusionMatrix.P01, Is.EqualTo(0.0).Within(1e-12));
            Assert.That(mitigator.ConfusionMatrix.P10, Is.EqualTo(0.0).Within(1e-12));
            Assert.That(mitigator.ConfusionMatrix.P00, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(mitigator.ConfusionMatrix.P11, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void Calibrate_WithNoisySystem_EstimatesFlipProbabilities()
        {
            NoiseModel noise = new(
                singleQubitErrorRate: 0.0,
                twoQubitErrorRate: 0.0,
                readoutErrorMatrix: ReadoutErrorMatrix.FromFlipProbabilities(p01: 0.2, p10: 0.1));

            NoisyQuantumSystem noisy = new(noise);

            // Use enough shots to make this stable, but still fast.
            ReadoutMitigator mitigator = ReadoutMitigator.Calibrate(noisy, qubitIndex: 0, shots: 2000, rng: new Random(2));

            Assert.That(mitigator.ConfusionMatrix.P01, Is.EqualTo(0.2).Within(0.03));
            Assert.That(mitigator.ConfusionMatrix.P10, Is.EqualTo(0.1).Within(0.03));
        }
    }
}
