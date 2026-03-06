using QuantumSuperposition.Core;
using QuantumSuperposition.NoiseProperties;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;
using System.Reflection;

namespace QuantumSoupTester
{
    [TestFixture]
    public class NoisyQuantumSystemTests
    {
        private static List<QuantumSystem.GateOperation> GetGateOperations(QuantumSystem system)
        {
            FieldInfo? queueField = typeof(QuantumSystem).GetField("_gateQueue", BindingFlags.NonPublic | BindingFlags.Instance);
            Queue<QuantumSystem.GateOperation>? queue = (Queue<QuantumSystem.GateOperation>?)queueField?.GetValue(system);
            return queue?.ToList() ?? new List<QuantumSystem.GateOperation>();
        }

        [Test]
        public void Apply_SingleQubitGate_ErrorRateZero_DoesNotInjectNoiseGate()
        {
            NoiseModel noise = new(singleQubitErrorRate: 0.0, twoQubitErrorRate: 0.0);
            NoisyQuantumSystem noisy = new(noise);
            _ = new QuBit<int>(noisy.InnerSystem, new[] { 0 });

            noisy.Apply(QuantumGates.Hadamard, qubitIndex: 0, rng: new Random(123));

            List<QuantumSystem.GateOperation> ops = GetGateOperations(noisy.InnerSystem);
            Assert.That(ops.Count, Is.EqualTo(1));
            Assert.That(ops[0].OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
        }

        [Test]
        public void Apply_SingleQubitGate_ErrorRateOne_InjectsNoiseGateAfter()
        {
            NoiseModel noise = new(singleQubitErrorRate: 1.0, twoQubitErrorRate: 0.0);
            NoisyQuantumSystem noisy = new(noise);
            _ = new QuBit<int>(noisy.InnerSystem, new[] { 0 });

            noisy.Apply(QuantumGates.Hadamard, qubitIndex: 0, rng: new Random(123));

            List<QuantumSystem.GateOperation> ops = GetGateOperations(noisy.InnerSystem);
            Assert.That(ops.Count, Is.EqualTo(2));
            Assert.That(ops[0].OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(ops[1].OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(ops[1].GateName, Is.AnyOf("X", "Z"));
            Assert.That(ops[1].TargetQubits, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Apply_TwoQubitGate_ErrorRateOne_InjectsSingleQubitNoiseGateAfter()
        {
            NoiseModel noise = new(singleQubitErrorRate: 0.0, twoQubitErrorRate: 1.0);
            NoisyQuantumSystem noisy = new(noise);
            _ = new QuBit<int>(noisy.InnerSystem, new[] { 0 });
            _ = new QuBit<int>(noisy.InnerSystem, new[] { 1 });

            noisy.Apply(QuantumGates.CNOT, qubitA: 0, qubitB: 1, rng: new Random(123));

            List<QuantumSystem.GateOperation> ops = GetGateOperations(noisy.InnerSystem);
            Assert.That(ops.Count, Is.EqualTo(2));
            Assert.That(ops[0].OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(ops[1].OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(ops[1].GateName, Is.AnyOf("X", "Z"));
            Assert.That(ops[1].TargetQubits[0], Is.AnyOf(0, 1));
        }

        [Test]
        public void ObserveGlobal_ReadoutErrorRate_CanFlipReturnedBits()
        {
            // Build a system fully collapsed to |0> and then apply readout noise that always flips 0->1.
            NoiseModel noise = new(
                singleQubitErrorRate: 0.0,
                twoQubitErrorRate: 0.0,
                readoutErrorMatrix: ReadoutErrorMatrix.FromFlipProbabilities(p01: 1.0, p10: 0.0));

            NoisyQuantumSystem noisy = new(noise);

            // Ensure wavefunction has 1 qubit in |0>.
            var amps = new Dictionary<int[], System.Numerics.Complex>(new QuantumSuperposition.Utilities.IntArrayComparer())
            {
                [new[] { 0 }] = System.Numerics.Complex.One
            };
            noisy.InnerSystem.SetAmplitudes(amps);

            int[] measured = noisy.ObserveGlobal(new[] { 0 }, rng: new Random(5));
            Assert.That(measured, Is.EqualTo(new[] { 1 }));
        }
    }
}
