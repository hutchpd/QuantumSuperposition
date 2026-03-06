using QuantumSuperposition.NoiseProperties;

namespace QuantumSoupTester
{
    [TestFixture]
    public class ZeroNoiseExtrapolationTests
    {
        [Test]
        public void Execute_LinearExtrapolation_RecoversZeroNoiseIntercept_ForLinearNoiseModelScaling()
        {
            // Observable is linear in the (scaled) single-qubit error rate.
            NoiseModel baseNoise = new(singleQubitErrorRate: 0.1, twoQubitErrorRate: 0.0);
            ZneCircuit circuit = new(baseNoise, sys => 1.0 - sys.NoiseModel.SingleQubitErrorRate);

            ZeroNoiseExtrapolation zne = new();
            double est = zne.Execute(
                circuit,
                noiseScales: new[] { 1.0, 2.0, 3.0 },
                extrapolation: ExtrapolationType.Linear);

            Assert.That(est, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void Execute_PolynomialExtrapolation_RecoversZeroNoiseIntercept_ForQuadraticObservable()
        {
            // Observable is quadratic in the (scaled) single-qubit error rate.
            NoiseModel baseNoise = new(singleQubitErrorRate: 0.2, twoQubitErrorRate: 0.0);
            ZneCircuit circuit = new(baseNoise, sys => 2.0 + Math.Pow(sys.NoiseModel.SingleQubitErrorRate, 2));

            ZeroNoiseExtrapolation zne = new();
            double est = zne.Execute(
                circuit,
                noiseScales: new[] { 1.0, 2.0, 3.0 },
                extrapolation: ExtrapolationType.Polynomial);

            Assert.That(est, Is.EqualTo(2.0).Within(1e-10));
        }

        [Test]
        public void Execute_Scaling_ClampsProbabilitiesToOne()
        {
            NoiseModel baseNoise = new(singleQubitErrorRate: 0.9, twoQubitErrorRate: 0.0);
            ZneCircuit circuit = new(baseNoise, sys => sys.NoiseModel.SingleQubitErrorRate);

            ZeroNoiseExtrapolation zne = new();
            double est = zne.Execute(
                circuit,
                noiseScales: new[] { 1.0, 2.0 },
                extrapolation: ExtrapolationType.Linear);

            // At scale 2, single-qubit error rate would be 1.8, but we clamp it to 1.0.
            // y(1)=0.9, y(2)=1.0 -> linear extrapolation gives 0.8.
            Assert.That(est, Is.EqualTo(0.8).Within(1e-12));
        }
    }
}
