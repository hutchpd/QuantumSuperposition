using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumSuperposition.Core;
using QuantumSuperposition.Systems;

namespace QuantumSoupTester
{
    [TestFixture]
    public class GateRegisterSugarTests
    {
        [Test]
        public void SingleQubitGate_AppliesToRegister()
        {
            var system = new QuantumSystem();
            // Start with |0> basis
            var reg = QuantumRegister.FromInt(0, 1, system);
            // Apply Hadamard via operator
            reg = QuantumGates.Hadamard * reg;
            // After application, amplitude count should remain 2 basis states
            Assert.That(system.Amplitudes.Count, Is.EqualTo(2));
            double probSum = system.Amplitudes.Values.Sum(a => a.Magnitude * a.Magnitude);
            Assert.That(probSum, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void TwoQubitGate_CompositionMaintainsProbability()
        {
            var system = new QuantumSystem();
            var reg = QuantumRegister.GHZState(system, 2); // effectively Bell-like
            // Apply CNOT using operator (should enqueue then process)
            reg = QuantumGates.CNOT * reg;
            double probSum = system.Amplitudes.Values.Sum(a => a.Magnitude * a.Magnitude);
            Assert.That(probSum, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void MultiQubitGate_AppliesDirectly()
        {
            var system = new QuantumSystem();
            var reg = QuantumRegister.WState(system, 3);
            // Identity-like 8x8
            Complex[,] identity8 = new Complex[8,8];
            for (int i=0;i<8;i++) identity8[i,i] = Complex.One;
            var gate = new QuantumGate(identity8);
            reg = gate * reg;
            // Should preserve amplitudes count = 3 (W state basis states)
            Assert.That(system.Amplitudes.Count, Is.EqualTo(3));
        }

        [Test]
        public void GateArityMismatch_Throws()
        {
            var system = new QuantumSystem();
            var reg = QuantumRegister.GHZState(system, 3);
            // 2x2 gate on 3-qubit register should fail
            Assert.Throws<ArgumentException>(() => { reg = QuantumGates.Hadamard * reg; });
        }
    }
}
