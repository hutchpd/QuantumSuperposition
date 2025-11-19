using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumSuperposition.Core;
using QuantumSuperposition.Systems;

namespace QuantumSoupTester
{
    [TestFixture]
    public class CanonicalStatesTests
    {
        [Test]
        public void EPRPair_ConstructsBellState()
        {
            var system = new QuantumSystem();
            var reg = QuantumRegister.EPRPair(system);
            var amps = system.Amplitudes;
            Assert.That(amps.Count, Is.EqualTo(2));
            var probSum = amps.Values.Sum(a => a.Magnitude * a.Magnitude);
            Assert.That(probSum, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(amps.Keys.Any(k => k.SequenceEqual(new[] { 0, 0 })), Is.True);
            Assert.That(amps.Keys.Any(k => k.SequenceEqual(new[] { 1, 1 })), Is.True);
        }

        [Test]
        public void WState_Length3_HasThreeBasisStates()
        {
            var system = new QuantumSystem();
            var reg = QuantumRegister.WState(system, 3);
            Assert.That(system.Amplitudes.Count, Is.EqualTo(3));
            double expectedAmp = 1.0 / Math.Sqrt(3);
            foreach (var amp in system.Amplitudes.Values)
            {
                Assert.That(amp.Magnitude, Is.EqualTo(expectedAmp).Within(1e-12));
            }
        }

        [Test]
        public void GHZState_Length4_HasTwoBasisStates()
        {
            var system = new QuantumSystem();
            var reg = QuantumRegister.GHZState(system, 4);
            Assert.That(system.Amplitudes.Count, Is.EqualTo(2));
            double expectedAmp = 1.0 / Math.Sqrt(2);
            foreach (var amp in system.Amplitudes.Values)
            {
                Assert.That(amp.Magnitude, Is.EqualTo(expectedAmp).Within(1e-12));
            }
            Assert.That(system.Amplitudes.Keys.Any(k => k.All(b => b == 0)), Is.True);
            Assert.That(system.Amplitudes.Keys.Any(k => k.All(b => b == 1)), Is.True);
        }

        [Test]
        public void WState_InvalidLength_Throws()
        {
            var system = new QuantumSystem();
            Assert.Throws<ArgumentOutOfRangeException>(() => QuantumRegister.WState(system, 1));
        }

        [Test]
        public void GHZState_InvalidLength_Throws()
        {
            var system = new QuantumSystem();
            Assert.Throws<ArgumentOutOfRangeException>(() => QuantumRegister.GHZState(system, 1));
        }
    }
}
