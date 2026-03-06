using QuantumSuperposition.NoiseProperties;

namespace QuantumSoupTester
{
    [TestFixture]
    public class NoiseModelTests
    {
        [Test]
        public void NoiseModel_Default_IsNoNoise()
        {
            NoiseModel model = new();

            Assert.That(model.SingleQubitErrorRate, Is.EqualTo(0.0));
            Assert.That(model.TwoQubitErrorRate, Is.EqualTo(0.0));
            Assert.That(model.ReadoutErrorMatrix, Is.EqualTo(ReadoutErrorMatrix.Identity));
            Assert.That(model.ThermalRelaxation, Is.Null);
        }

        [Test]
        public void NoiseModel_InvalidSingleQubitErrorRate_Throws()
        {
            Assert.That(
                () => new NoiseModel(singleQubitErrorRate: -0.01, twoQubitErrorRate: 0.0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NoiseModel_InvalidTwoQubitErrorRate_Throws()
        {
            Assert.That(
                () => new NoiseModel(singleQubitErrorRate: 0.0, twoQubitErrorRate: 1.01),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ReadoutErrorMatrix_FromFlipProbabilities_BuildsExpectedMatrix()
        {
            ReadoutErrorMatrix m = ReadoutErrorMatrix.FromFlipProbabilities(p01: 0.02, p10: 0.05);

            Assert.That(m.P00, Is.EqualTo(0.98).Within(1e-12));
            Assert.That(m.P01, Is.EqualTo(0.02).Within(1e-12));
            Assert.That(m.P10, Is.EqualTo(0.05).Within(1e-12));
            Assert.That(m.P11, Is.EqualTo(0.95).Within(1e-12));

            Assert.That(m[0, 1], Is.EqualTo(0.02).Within(1e-12));
            Assert.That(m[1, 0], Is.EqualTo(0.05).Within(1e-12));
        }

        [Test]
        public void ReadoutErrorMatrix_RowNotSummingToOne_Throws()
        {
            Assert.That(
                () => new ReadoutErrorMatrix(p00: 0.9, p01: 0.2, p10: 0.0, p11: 1.0),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ThermalRelaxation_NonPositive_Throws()
        {
            Assert.That(
                () => new ThermalRelaxation(TimeSpan.Zero, TimeSpan.FromMilliseconds(10)),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new ThermalRelaxation(TimeSpan.FromMilliseconds(10), TimeSpan.Zero),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NoiseModel_AllFields_Assignable()
        {
            NoiseModel model = new(
                singleQubitErrorRate: 0.001,
                twoQubitErrorRate: 0.01,
                readoutErrorMatrix: ReadoutErrorMatrix.FromFlipProbabilities(0.03, 0.04),
                thermalRelaxation: new ThermalRelaxation(TimeSpan.FromMicroseconds(30), TimeSpan.FromMicroseconds(20)));

            Assert.That(model.SingleQubitErrorRate, Is.EqualTo(0.001).Within(1e-12));
            Assert.That(model.TwoQubitErrorRate, Is.EqualTo(0.01).Within(1e-12));
            Assert.That(model.ReadoutErrorMatrix.P01, Is.EqualTo(0.03).Within(1e-12));
            Assert.That(model.ReadoutErrorMatrix.P10, Is.EqualTo(0.04).Within(1e-12));
            Assert.That(model.ThermalRelaxation.HasValue, Is.True);
        }
    }
}
