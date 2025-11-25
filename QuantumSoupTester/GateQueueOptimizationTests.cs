using NUnit.Framework;
using QuantumSuperposition.Systems;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Utilities;
using QuantumSuperposition.Core;
using System.Numerics;
using System.Linq;

namespace QuantumSoupTester
{
    [TestFixture]
    public class GateQueueOptimizationTests
    {
        [Test]
        public void ProcessGateQueue_CancelsInverses_TAndTDagger()
        {
            var system = new QuantumSystem();
            var q0 = new QuBit<int>(system, new[] { 0 }).WithWeights(new Dictionary<int, Complex>{{0,1.0},{1,1.0}}, true);
            system.SetFromTensorProduct(false, q0);
            // T then T-dagger should cancel (net identity)
            system.ApplySingleQubitGate(0, QuantumGates.T, "T");
            system.ApplySingleQubitGate(0, QuantumGates.T_Dagger, "T†");
            system.ProcessGateQueue();
            // After processing, schedule should show no operations
            var visual = system.VisualiseGateSchedule(1);
            Assert.That(visual, Is.EqualTo("no operations"));
        }

        [Test]
        public void ProcessGateQueue_DoubleHadamard_Cancels()
        {
            var system = new QuantumSystem();
            var q0 = new QuBit<int>(system, new[] { 0 }).WithWeights(new Dictionary<int, Complex>{{0,1.0},{1,1.0}}, true);
            system.SetFromTensorProduct(false, q0);
            system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "H");
            system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "H");
            system.ProcessGateQueue();
            var visual = system.VisualiseGateSchedule(1);
            Assert.That(visual, Is.EqualTo("no operations"));
        }
    }
}
