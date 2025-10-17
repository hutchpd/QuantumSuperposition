using QuantumSuperposition.Core;
using QuantumSuperposition.Systems;
using System.Globalization;
using System.Reflection;

namespace QuantumSoupTester
{
    // This helper uses reflection to extract the private gate queue from a QuantumSystem.
    public static class QuantumSystemTestHelper
    {
        public static List<QuantumSystem.GateOperation> GetGateOperations(QuantumSystem system)
        {
            // Get the private field "_gateQueue" from QuantumSystem.
            FieldInfo queueField = typeof(QuantumSystem).GetField("_gateQueue", BindingFlags.NonPublic | BindingFlags.Instance);
            if (queueField == null)
            {
                throw new Exception("Cannot find _gateQueue field in QuantumSystem.");
            }

            // The queue contains the enqueued gate operations.
            Queue<QuantumSystem.GateOperation>? queue = (Queue<QuantumSystem.GateOperation>)queueField.GetValue(system);
            return queue.ToList();
        }
    }

    [TestFixture]
    public class QuantumFourierTransformTests
    {
        [Test]
        public void QuantumFourierTransform_AppliesExpectedGates()
        {
            QuantumSystem system = new();
            int[] qubits = new[] { 0, 1, 2 };

            QuantumAlgorithms.QuantumFourierTransform(system, qubits);
            List<QuantumSystem.GateOperation> operations = QuantumSystemTestHelper.GetGateOperations(system);

            Assert.That(operations.Count, Is.EqualTo(7), "Expected 7 gate operations for a 3-qubit QFT.");

            QuantumSystem.GateOperation op0 = operations[0];
            Assert.That(op0.OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(op0.TargetQubits[0], Is.EqualTo(0), "Qubit 0 should receive the Hadamard.");
            Assert.That(op0.GateName, Is.EqualTo("H"));

            double tolerance = 1e-12;

            QuantumSystem.GateOperation op1 = operations[1];
            Assert.That(op1.OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(op1.TargetQubits, Is.EqualTo(new[] { 1, 0 }), "First CPhase should be applied from qubit 1 to qubit 0.");
            Assert.That(ExtractTheta(op1.GateName), Is.EqualTo(Math.PI / 4).Within(tolerance),
                "The theta value for operation 1 CPhase gate is not within tolerance.");

            QuantumSystem.GateOperation op2 = operations[2];
            Assert.That(op2.OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(op2.TargetQubits, Is.EqualTo(new[] { 2, 0 }));
            Assert.That(ExtractTheta(op2.GateName), Is.EqualTo(Math.PI / 8).Within(tolerance),
                "The theta value for operation 2 CPhase gate is not within tolerance.");

            QuantumSystem.GateOperation op3 = operations[3];
            Assert.That(op3.OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(op3.TargetQubits[0], Is.EqualTo(1), "Qubit 1 should receive its Hadamard.");
            Assert.That(op3.GateName, Is.EqualTo("H"));

            QuantumSystem.GateOperation op4 = operations[4];
            Assert.That(op4.OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(op4.TargetQubits, Is.EqualTo(new[] { 2, 1 }));
            Assert.That(ExtractTheta(op4.GateName), Is.EqualTo(Math.PI / 4).Within(tolerance),
                "The theta value for operation 4 CPhase gate is not within tolerance.");

            QuantumSystem.GateOperation op5 = operations[5];
            Assert.That(op5.OperationType, Is.EqualTo(QuantumSystem.GateType.SingleQubit));
            Assert.That(op5.TargetQubits[0], Is.EqualTo(2), "Qubit 2 gets a solo Hadamard.");
            Assert.That(op5.GateName, Is.EqualTo("H"));

            QuantumSystem.GateOperation op6 = operations[6];
            Assert.That(op6.OperationType, Is.EqualTo(QuantumSystem.GateType.TwoQubit));
            Assert.That(op6.TargetQubits, Is.EqualTo(new[] { 0, 2 }), "Swap should occur between qubits 0 and 2.");
            Assert.That(op6.GateName, Is.EqualTo("SWAP"));
        }

        /// <summary>
        /// Extracts the numeric theta value from a CPhase gate name string.
        /// Assumes the gate name is formatted as "CPhase(<numeric value>)".
        /// </summary>
        private double ExtractTheta(string gateName)
        {
            // Find the positions of the opening and closing parentheses.
            int start = gateName.IndexOf('(');
            int end = gateName.IndexOf(')');
            if (start < 0 || end < 0 || end <= start)
            {
                throw new FormatException("Gate name is not in the expected format.");
            }
            // Extract the numeric substring.
            string thetaString = gateName.Substring(start + 1, end - start - 1);
            return double.Parse(thetaString, CultureInfo.InvariantCulture);
        }

    }

    [TestFixture]
    public class GroverSearchTests
    {
        [Test]
        public void GroverSearch_AppliesExpectedIterationGates()
        {
            QuantumSystem system = new();
            int[] qubits = new[] { 0, 1 };
            static bool oracle(int[] bits)
            {
                return bits.Length == 2 && bits[0] == 1 && bits[1] == 0;
            }

            QuantumAlgorithms.GroverSearch(system, qubits, oracle);
            List<QuantumSystem.GateOperation> operations = QuantumSystemTestHelper.GetGateOperations(system);

            Assert.That(operations[0].GateName, Is.EqualTo("H"), "Initial superposition should apply H to qubit 0.");
            Assert.That(operations[1].GateName, Is.EqualTo("H"), "Initial superposition should apply H to qubit 1.");
            Assert.That(operations[2].GateName, Is.EqualTo("Oracle"), "The Oracle should be the 3rd operation.");

            QuantumSystem.GateOperation[] diffusionOps = operations.Skip(3).ToArray();
            Assert.That(diffusionOps.Any(op => op.GateName == "MCZ"), Is.True,
                "Diffusion operator should contain an MCZ gate.");

            int indexMCZ = Array.FindIndex(diffusionOps, op => op.GateName == "MCZ");
            Assert.That(indexMCZ, Is.GreaterThan(0), "MCZ should not be the first operation in the diffusion operator.");
            Assert.That(diffusionOps.Take(indexMCZ).Any(op => op.GateName == "X"), Is.True,
                "Expected at least one X operation before MCZ in diffusion.");
            Assert.That(diffusionOps.Skip(indexMCZ + 1).Any(op => op.GateName == "X"), Is.True,
                "Expected at least one X operation after MCZ in diffusion.");
        }

    }

    [TestFixture]
    public class QuantumHelperTests
    {
        [Test]
        public void IndexToBits_ReturnsCorrectBitArray()
        {
            int index = 5;
            int length = 4;
            int[] bits = InvokePrivateStatic<int[]>("IndexToBits", index, length);
            Assert.That(bits, Is.EqualTo(new[] { 0, 1, 0, 1 }), "IndexToBits should convert 5 to [0,1,0,1].");
        }

        [Test]
        public void BitsToIndex_ReturnsOriginalIndex()
        {
            int[] bits = new[] { 0, 1, 0, 1 };
            int index = InvokePrivateStatic<int>("BitsToIndex", bits);
            Assert.That(index, Is.EqualTo(5), "BitsToIndex should reverse IndexToBits, converting [0,1,0,1] to 5.");
        }

        [Test]
        public void ExtractSubstate_ReturnsCorrectSubstate()
        {
            int fullIndex = 10;
            int[] targetQubits = new[] { 1, 3 };
            int totalQubits = 4;
            int substate = InvokePrivateStatic<int>("ExtractSubstate", fullIndex, targetQubits, totalQubits);
            Assert.That(substate, Is.EqualTo(0), "ExtractSubstate should return 0 for fullIndex 10 with target qubits [1,3].");

            targetQubits = new[] { 0, 2 };
            substate = InvokePrivateStatic<int>("ExtractSubstate", fullIndex, targetQubits, totalQubits);
            Assert.That(substate, Is.EqualTo(3), "ExtractSubstate should return 3 for fullIndex 10 with target qubits [0,2].");
        }

        private T InvokePrivateStatic<T>(string methodName, params object[] parameters)
        {
            MethodInfo method = typeof(QuantumAlgorithms).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Cannot find private static method '{methodName}' – is it hidden in quantum superposition?");
            return (T)method.Invoke(null, parameters);
        }
    }
}
