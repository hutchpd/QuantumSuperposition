using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using NUnit.Framework;
using QuantumSuperposition.Core;
using QuantumSuperposition.Systems;

namespace QuantumAlgorithmsTests
{
    // This helper uses reflection to extract the private gate queue from a QuantumSystem.
    public static class QuantumSystemTestHelper
    {
        public static List<QuantumSystem.GateOperation> GetGateOperations(QuantumSystem system)
        {
            // Get the private field "_gateQueue" from QuantumSystem.
            FieldInfo queueField = typeof(QuantumSystem).GetField("_gateQueue", BindingFlags.NonPublic | BindingFlags.Instance);
            if (queueField == null)
                throw new Exception("Cannot find _gateQueue field in QuantumSystem.");

            // The queue contains the enqueued gate operations.
            var queue = (Queue<QuantumSystem.GateOperation>)queueField.GetValue(system);
            return queue.ToList();
        }
    }

    [TestFixture]
    public class QuantumFourierTransformTests
    {
        [Test]
        public void QuantumFourierTransform_AppliesExpectedGates()
        {
            // Create a fresh QuantumSystem.
            var system = new QuantumSystem();
            // Use qubits 0, 1, and 2. Imagine this as the quantum dance floor.
            int[] qubits = new[] { 0, 1, 2 };

            // Apply the Quantum Fourier Transform (QFT).
            QuantumAlgorithms.QuantumFourierTransform(system, qubits);

            // Instead of intercepting methods via overriding, we now peek into the gate queue.
            var operations = QuantumSystemTestHelper.GetGateOperations(system);
            // For 3 qubits, the QFT should enqueue:
            // - For i=0: 1 Hadamard, then 2 CPhase gates.
            // - For i=1: 1 Hadamard, then 1 CPhase.
            // - For i=2: 1 Hadamard.
            // - Followed by a swap (one CNOT gate with description "SWAP").
            // Expected total: 7 gate operations.
            Assert.AreEqual(7, operations.Count, "Expected 7 gate operations for a 3-qubit QFT.");

            // Operation 0: Hadamard on qubit 0.
            var op0 = operations[0];
            Assert.AreEqual(QuantumSystem.GateType.SingleQubit, op0.OperationType);
            Assert.AreEqual(0, op0.TargetQubits[0], "Qubit 0 should receive the Hadamard.");
            Assert.AreEqual("H", op0.GateName);

            // Set a tolerance for comparing doubles.
            double tolerance = 1e-12;

            // Operation 1: CPhase with theta = PI/4 from qubit 1 to 0.
            var op1 = operations[1];
            Assert.AreEqual(QuantumSystem.GateType.TwoQubit, op1.OperationType);
            CollectionAssert.AreEqual(new[] { 1, 0 }, op1.TargetQubits,
                "First CPhase should be applied from qubit 1 to qubit 0.");
            double expectedTheta = Math.PI / 4;
            double actualTheta = ExtractTheta(op1.GateName);
            Assert.AreEqual(expectedTheta, actualTheta, tolerance, "The theta value for operation 1 CPhase gate is not within tolerance.");

            // Operation 2: CPhase with theta = PI/8 from qubit 2 to 0.
            var op2 = operations[2];
            Assert.AreEqual(QuantumSystem.GateType.TwoQubit, op2.OperationType);
            CollectionAssert.AreEqual(new[] { 2, 0 }, op2.TargetQubits);
            expectedTheta = Math.PI / 8;
            actualTheta = ExtractTheta(op2.GateName);
            Assert.AreEqual(expectedTheta, actualTheta, tolerance, "The theta value for operation 2 CPhase gate is not within tolerance.");

            // Operation 3: Hadamard on qubit 1.
            var op3 = operations[3];
            Assert.AreEqual(QuantumSystem.GateType.SingleQubit, op3.OperationType);
            Assert.AreEqual(1, op3.TargetQubits[0], "Qubit 1 should receive its Hadamard.");
            Assert.AreEqual("H", op3.GateName);

            // Operation 4: CPhase with theta = PI/4 from qubit 2 to 1.
            var op4 = operations[4];
            Assert.AreEqual(QuantumSystem.GateType.TwoQubit, op4.OperationType);
            CollectionAssert.AreEqual(new[] { 2, 1 }, op4.TargetQubits);
            expectedTheta = Math.PI / 4;
            actualTheta = ExtractTheta(op4.GateName);
            Assert.AreEqual(expectedTheta, actualTheta, tolerance, "The theta value for operation 4 CPhase gate is not within tolerance.");

            // Operation 5: Hadamard on qubit 2.
            var op5 = operations[5];
            Assert.AreEqual(QuantumSystem.GateType.SingleQubit, op5.OperationType);
            Assert.AreEqual(2, op5.TargetQubits[0], "Qubit 2 gets a solo Hadamard.");
            Assert.AreEqual("H", op5.GateName);

            // Operation 6: The swap between qubits 0 and 2.
            var op6 = operations[6];
            Assert.AreEqual(QuantumSystem.GateType.TwoQubit, op6.OperationType);
            CollectionAssert.AreEqual(new[] { 0, 2 }, op6.TargetQubits, "Swap should occur between qubits 0 and 2.");
            Assert.AreEqual("SWAP", op6.GateName);
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
            // Create a new QuantumSystem for our quantum haystack.
            var system = new QuantumSystem();
            // Using 2 qubits. For Grover’s algorithm on 2 qubits, the iteration count is:
            // floor(pi/4 * sqrt(2^2)) = floor(pi/4 * 2) = floor(pi/2) = 1 iteration.
            int[] qubits = new[] { 0, 1 };

            // Define an oracle: mark the state [1, 0] (with qubit 0 as the MSB).
            Func<int[], bool> oracle = bits => bits.Length == 2 && bits[0] == 1 && bits[1] == 0;

            // Run Grover’s search algorithm.
            QuantumAlgorithms.GroverSearch(system, qubits, oracle);

            // Retrieve enqueued operations.
            var operations = QuantumSystemTestHelper.GetGateOperations(system);

            // Instead of expecting a fixed total count (which may vary due to optimizations),
            // verify key components appear in the right order:

            // 1. The initial layer: expect 2 Hadamard operations.
            Assert.AreEqual("H", operations[0].GateName, "Initial superposition should apply H to qubit 0.");
            Assert.AreEqual("H", operations[1].GateName, "Initial superposition should apply H to qubit 1.");

            // 2. Next, the Oracle operation.
            Assert.AreEqual("Oracle", operations[2].GateName, "The Oracle should be the 3rd operation.");

            // 3. The diffusion operator should follow.
            //    Here, we check that a multi-qubit MCZ gate is applied somewhere after the oracle.
            var diffusionOps = operations.Skip(3).ToArray();
            Assert.IsTrue(diffusionOps.Any(op => op.GateName == "MCZ"), "Diffusion operator should contain an MCZ gate.");

            // 4. Additionally, verify that there are Pauli-X (X) gates before and after the MCZ.
            int indexMCZ = Array.FindIndex(diffusionOps, op => op.GateName == "MCZ");
            Assert.IsTrue(indexMCZ > 0, "MCZ should not be the first operation in the diffusion operator.");
            Assert.IsTrue(diffusionOps.Take(indexMCZ).Any(op => op.GateName == "X"),
                "Expected at least one X operation before MCZ in diffusion.");
            Assert.IsTrue(diffusionOps.Skip(indexMCZ + 1).Any(op => op.GateName == "X"),
                "Expected at least one X operation after MCZ in diffusion.");

            // (Optional) If desired, you could also check for Hadamard (H) operations in the diffusion operator.
            // Do note that the exact count of operations may vary if the system merges operations.
        }
    }

    [TestFixture]
    public class QuantumHelperTests
    {
        // Helper method to invoke private static methods in QuantumAlgorithms via reflection.
        private T InvokePrivateStatic<T>(string methodName, params object[] parameters)
        {
            MethodInfo method = typeof(QuantumAlgorithms).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, $"Cannot find private static method '{methodName}' – is it hidden in quantum superposition?");
            return (T)method.Invoke(null, parameters);
        }

        [Test]
        public void IndexToBits_ReturnsCorrectBitArray()
        {
            // Index 5 with length 4 should yield [0, 1, 0, 1] (MSB first).
            int index = 5;
            int length = 4;
            int[] bits = InvokePrivateStatic<int[]>("IndexToBits", index, length);
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 1 }, bits, "IndexToBits should convert 5 to [0,1,0,1].");
        }

        [Test]
        public void BitsToIndex_ReturnsOriginalIndex()
        {
            // Converting bits [0, 1, 0, 1] back should yield 5.
            int[] bits = new[] { 0, 1, 0, 1 };
            int index = InvokePrivateStatic<int>("BitsToIndex", bits);
            Assert.AreEqual(5, index, "BitsToIndex should reverse IndexToBits, converting [0,1,0,1] to 5.");
        }

        [Test]
        public void ExtractSubstate_ReturnsCorrectSubstate()
        {
            // For a 4-qubit full state index (say 10, binary "1010"):
            // If we extract target qubits at positions [1, 3] (with 0 as MSB),
            // then the bits from "1010" are: position 0 → 1, 1 → 0, 2 → 1, 3 → 0.
            // For target qubits [1, 3] we expect [0, 0] which yields integer 0.
            int fullIndex = 10;
            int[] targetQubits = new[] { 1, 3 };
            int totalQubits = 4;
            int substate = InvokePrivateStatic<int>("ExtractSubstate", fullIndex, targetQubits, totalQubits);
            Assert.AreEqual(0, substate, "ExtractSubstate should return 0 for fullIndex 10 with target qubits [1,3].");

            // Test a different selection: target qubits [0, 2] should yield bits [1,1] → 3.
            targetQubits = new[] { 0, 2 };
            substate = InvokePrivateStatic<int>("ExtractSubstate", fullIndex, targetQubits, totalQubits);
            Assert.AreEqual(3, substate, "ExtractSubstate should return 3 for fullIndex 10 with target qubits [0,2].");
        }
    }
}
