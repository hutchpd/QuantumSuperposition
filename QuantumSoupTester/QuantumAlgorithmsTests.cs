using QuantumSuperposition.Core;
using QuantumSuperposition.Systems;
using System.Globalization;
using System.Reflection;

namespace QuantumSoupTester
{
    [TestFixture]
    public class QuantumFourierTransformTests
    {
        private static List<QuantumSystem.GateOperation> GetGateOperations(QuantumSystem system)
        {
            FieldInfo? queueField = typeof(QuantumSystem).GetField("_gateQueue", BindingFlags.NonPublic | BindingFlags.Instance);
            Queue<QuantumSystem.GateOperation>? queue = (Queue<QuantumSystem.GateOperation>?)queueField?.GetValue(system);
            return queue?.ToList() ?? new List<QuantumSystem.GateOperation>();
        }

        [Test]
        public void QuantumFourierTransform_AppliesExpectedGates()
        {
            QuantumSystem system = new();
            int[] qubits = new[] { 0, 1, 2 };

            QuantumAlgorithms.QuantumFourierTransform(system, qubits);
            List<QuantumSystem.GateOperation> operations = GetGateOperations(system);

            Assert.That(operations.Count, Is.EqualTo(7), "Expected 7 gate operations for a 3-qubit QFT.");

            QuantumSystem.GateOperation op0 = operations[0];
            Assert.That(op0.OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(op0.TargetQubits[0], Is.EqualTo(0));
            Assert.That(op0.GateName, Is.EqualTo("H"));

            double tolerance = 1e-12;

            QuantumSystem.GateOperation op1 = operations[1];
            Assert.That(op1.OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(op1.TargetQubits, Is.EqualTo(new[] { 1, 0 }));
            Assert.That(ExtractTheta(op1.GateName), Is.EqualTo(Math.PI / 4).Within(tolerance));

            QuantumSystem.GateOperation op2 = operations[2];
            Assert.That(op2.OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(op2.TargetQubits, Is.EqualTo(new[] { 2, 0 }));
            Assert.That(ExtractTheta(op2.GateName), Is.EqualTo(Math.PI / 8).Within(tolerance));

            QuantumSystem.GateOperation op3 = operations[3];
            Assert.That(op3.OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(op3.TargetQubits[0], Is.EqualTo(1));
            Assert.That(op3.GateName, Is.EqualTo("H"));

            QuantumSystem.GateOperation op4 = operations[4];
            Assert.That(op4.OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(op4.TargetQubits, Is.EqualTo(new[] { 2, 1 }));
            Assert.That(ExtractTheta(op4.GateName), Is.EqualTo(Math.PI / 4).Within(tolerance));

            QuantumSystem.GateOperation op5 = operations[5];
            Assert.That(op5.OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(op5.TargetQubits[0], Is.EqualTo(2));
            Assert.That(op5.GateName, Is.EqualTo("H"));

            QuantumSystem.GateOperation op6 = operations[6];
            Assert.That(op6.OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(op6.TargetQubits, Is.EqualTo(new[] { 0, 2 }));
            Assert.That(op6.GateName, Is.EqualTo("SWAP"));
        }

        private double ExtractTheta(string gateName)
        {
            int start = gateName.IndexOf('(');
            int end = gateName.IndexOf(')');
            string thetaString = gateName.Substring(start + 1, end - start - 1);
            return double.Parse(thetaString, CultureInfo.InvariantCulture);
        }
    }

    [TestFixture]
    public class GroverSearchTests
    {
        private static List<QuantumSystem.GateOperation> GetGateOperations(QuantumSystem system)
        {
            FieldInfo? queueField = typeof(QuantumSystem).GetField("_gateQueue", BindingFlags.NonPublic | BindingFlags.Instance);
            Queue<QuantumSystem.GateOperation>? queue = (Queue<QuantumSystem.GateOperation>?)queueField?.GetValue(system);
            return queue?.ToList() ?? new List<QuantumSystem.GateOperation>();
        }

        [Test]
        public void GroverSearch_AppliesExpectedIterationGates()
        {
            QuantumSystem system = new();
            int[] qubits = new[] { 0, 1 };
            static bool oracle(int[] bits) => bits.Length == 2 && bits[0] == 1 && bits[1] == 0;

            QuantumAlgorithms.GroverSearch(system, qubits, oracle);
            List<QuantumSystem.GateOperation> operations = GetGateOperations(system);

            Assert.That(operations[0].GateName, Is.EqualTo("H"));
            Assert.That(operations[1].GateName, Is.EqualTo("H"));
            Assert.That(operations[2].GateName, Is.EqualTo("Oracle"));

            QuantumSystem.GateOperation[] diffusionOps = operations.Skip(3).ToArray();
            Assert.That(diffusionOps.Any(op => op.GateName == "MCZ"), Is.True);
            int indexMCZ = Array.FindIndex(diffusionOps, op => op.GateName == "MCZ");
            Assert.That(indexMCZ, Is.GreaterThan(0));
            Assert.That(diffusionOps.Take(indexMCZ).Any(op => op.GateName == "X"), Is.True);
            Assert.That(diffusionOps.Skip(indexMCZ + 1).Any(op => op.GateName == "X"), Is.True);
        }
    }

    [TestFixture]
    public class QuantumHelperTests
    {
        [Test]
        public void IndexToBits_ReturnsCorrectBitArray()
        {
            int[] bits = QuantumAlgorithms.IndexToBits(5, 4);
            Assert.That(bits, Is.EqualTo(new[] { 0, 1, 0, 1 }));
        }

        [Test]
        public void BitsToIndex_ReturnsOriginalIndex()
        {
            int index = QuantumAlgorithms.BitsToIndex(new[] { 0, 1, 0, 1 });
            Assert.That(index, Is.EqualTo(5));
        }

        [Test]
        public void ExtractSubstate_ReturnsCorrectSubstate()
        {
            int fullIndex = 10; // 1010
            int substate = QuantumAlgorithms.ExtractSubstate(fullIndex, new[] { 1, 3 }, 4);
            Assert.That(substate, Is.EqualTo(0));
            substate = QuantumAlgorithms.ExtractSubstate(fullIndex, new[] { 0, 2 }, 4);
            Assert.That(substate, Is.EqualTo(3));
        }
    }
}
