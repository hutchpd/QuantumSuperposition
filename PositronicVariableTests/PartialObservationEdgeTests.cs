using NUnit.Framework;
using QuantumSuperposition.Systems;
using QuantumSuperposition.QuantumSoup;
using System.Numerics;
using System.Collections.Generic;

namespace QuantumSoupTester
{
    [TestFixture]
    public class PartialObservationEdgeTests
    {
        [Test]
        public void PartialObserve_AllZeroAmplitudes_ThrowsDiagnostic()
        {
            var system = new QuantumSystem();
            // Use SetAmplitudes with zero amplitudes
            var amps = new Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer())
            {
                { new[]{0,0}, Complex.Zero },
                { new[]{0,1}, Complex.Zero }
            };
            system.SetAmplitudes(amps);
            Assert.Throws<InvalidOperationException>(() => system.PartialObserve(new[] { 0 }));
        }

        [Test]
        public void PartialObserve_SingleState_NoError()
        {
            var system = new QuantumSystem();
            var q0 = new QuBit<int>(system, new[] { 0 }).WithWeights(new Dictionary<int, Complex>{{0,1.0}}, true);
            system.SetFromTensorProduct(false, q0);
            var outcome = system.PartialObserve(new[] { 0 });
            Assert.That(outcome.Length, Is.EqualTo(1));
        }
    }
}
