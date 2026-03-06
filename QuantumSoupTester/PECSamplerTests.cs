using QuantumSuperposition.NoiseProperties;

namespace QuantumSoupTester
{
    [TestFixture]
    public class PECSamplerTests
    {
        [Test]
        public void Execute_EstimateConvergesToLinearCombination()
        {
            // Ideal value is Σ η_k * f_k. Here it is exactly 0.
            // η1=0.75, f1=2  ; η2=-0.25, f2=6  => 0.75*2 - 0.25*6 = 0
            NoiseModel baseNoise = NoiseModel.None;

            PecTerm t1 = new("t1", Coefficient: 0.75, Run: _ => 2.0);
            PecTerm t2 = new("t2", Coefficient: -0.25, Run: _ => 6.0);

            PecDecomposition d = new(baseNoise, new[] { t1, t2 });
            PECSampler sampler = new();

            PecResult r = sampler.Execute(d, samples: 50_000, rng: new Random(123));

            Assert.That(r.Estimate, Is.EqualTo(0.0).Within(0.05));
            Assert.That(r.Overhead, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(r.Samples, Is.EqualTo(50_000));
        }

        [Test]
        public void Decomposition_Overhead_IsSumAbsCoefficients()
        {
            NoiseModel baseNoise = NoiseModel.None;
            PecDecomposition d = new(
                baseNoise,
                new[]
                {
                    new PecTerm("a", 0.5, _ => 1.0),
                    new PecTerm("b", -2.0, _ => 1.0),
                    new PecTerm("c", 0.25, _ => 1.0)
                });

            Assert.That(d.Overhead, Is.EqualTo(2.75).Within(1e-12));
        }
    }
}
