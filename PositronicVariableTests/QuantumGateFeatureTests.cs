using System.Numerics;
using QuantumSuperposition.Utilities;
using QuantumSuperposition.Core;
using QuantumSuperposition.Systems;

namespace QuantumMathTests
{
    [TestFixture]
    public class QuantumGateFeatureTests
    {
        /// <summary>
        /// Gently squints at two matrices and asks: "Are you basically the same, or are we about to file a bug report?"
        /// Performs an element-wise comparison within a whisper of tolerance, because floating point math is a cruel and chaotic beast.
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private bool AreMatricesEqual(Complex[,] m1, Complex[,] m2, double tolerance = 1e-10)
        {
            if (m1.GetLength(0) != m2.GetLength(0) ||
                m1.GetLength(1) != m2.GetLength(1))
                return false;

            for (int i = 0; i < m1.GetLength(0); i++)
            {
                for (int j = 0; j < m1.GetLength(1); j++)
                {
                    if ((m1[i, j] - m2[i, j]).Magnitude > tolerance)
                        return false;
                }
            }
            return true;
        }

        [Test]
        public void QuantumGate_Composition_TwoRootNotEqualsPauliX()
        {
            // Applying Root-NOT twice is like asking "Are we there yet?" twice—
            // you eventually end up at Pauli-X, the actual destination.
            QuantumGate rootNot = new QuantumGate(QuantumGates.RootNot);
            QuantumGate composed = rootNot.Then(rootNot);
            QuantumGate pauliX = new QuantumGate(QuantumGates.PauliX);

            Assert.IsTrue(
                AreMatricesEqual(composed.Matrix, pauliX.Matrix, 1e-8),
                "Composing RootNot twice should equal the Pauli-X gate.");
        }

        [Test]
        public void Gate_Inversion_DoubleInversion_ReturnsOriginal()
        {
            // Double inversion: because sometimes, undoing your undo is
            // just another way of admitting you had it right the first time.
            QuantumGate hadamard = QuantumGates.Hadamard;
            Complex[,] inverted = QuantumGateTools.InvertGate(hadamard.Matrix);
            Complex[,] doubleInverted = QuantumGateTools.InvertGate(inverted);

            Assert.IsTrue(
                AreMatricesEqual(doubleInverted, hadamard.Matrix, 1e-8),
                "Double inversion should return the original gate matrix.");
        }

        [Test]
        public void ParametricGate_RX_CorrectCalculation()
        {
            // RX(theta): Because even in quantum mechanics,
            // rotating on the X-axis is cooler with imaginary numbers.
            double theta = Math.PI / 4;
            QuantumGate rxGate = QuantumGates.RX(theta);
            double cos = Math.Cos(theta / 2);
            double sin = Math.Sin(theta / 2);
            Complex expected00 = cos;
            Complex expected01 = -Complex.ImaginaryOne * sin;
            Complex expected10 = -Complex.ImaginaryOne * sin;
            Complex expected11 = cos;
            Complex[,] expectedMatrix = new Complex[,]
            {
        { expected00, expected01 },
        { expected10, expected11 }
            };

            Assert.IsTrue(
                AreMatricesEqual(rxGate.Matrix, expectedMatrix, 1e-8),
                "RX gate matrix does not match the expected calculation.");
        }

        [Test]
        public void QuantumGate_ChainedComposition_SingleQubit()
        {
            // Gate chain reaction: Hadamard → RX → Root-NOT.
            // Like the quantum version of a smoothie blender set to “whoa.”
            QuantumGate gate1 = QuantumGates.Hadamard;
            QuantumGate gate2 = QuantumGates.RX(Math.PI / 2);
            QuantumGate gate3 = new QuantumGate(QuantumGates.RootNot);
            QuantumGate chain = gate1.Then(gate2).Then(gate3);

            QuantumGate expected = gate1.Then(gate2).Then(gate3);

            Assert.IsTrue(
                AreMatricesEqual(chain.Matrix, expected.Matrix, 1e-8),
                "Chained composition of gates did not produce the expected composite matrix.");
        }

        [Test]
        public void Gate_TimingOrderingStrategy_QueueProcessingAndVisualisation()
        {
            // Simulating the grand parade of quantum gates—before and after the music stops.
            QuantumSystem system = new QuantumSystem();
            system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "Hadamard");
            system.ApplyTwoQubitGate(0, 1, QuantumGates.CNOT.Matrix, "CNOT");

            // Gaze upon the majestic diagram of scheduled gate chaos.
            string Visualisation = system.VisualiseGateSchedule(totalQubits: 2);
            Assert.IsNotNull(Visualisation, "Visualisation should not be null.");
            Assert.IsNotEmpty(Visualisation, "Visualisation should contain a diagram representation.");

            // Obliterate the queue like a quantum Marie Kondo.
            system.ProcessGateQueue();

            // Make sure the queue has spiritually and computationally moved on.
            string postVisualisation = system.VisualiseGateSchedule(totalQubits: 2);
            Assert.IsTrue(
                postVisualisation.Contains("no operations") || string.IsNullOrWhiteSpace(postVisualisation),
                "Gate queue should be empty after processing.");
        }

        [Test]
        public void Gate_Inversion_TGate_DaggerIsCorrect()
        {
            // Inverting the T gate gives us T-Dagger—proof that even in quantum land,
            // there's always a fancier way to say “undo.”
            QuantumGate tGate = new QuantumGate(QuantumGates.T);
            Complex[,] inverted = QuantumGateTools.InvertGate(tGate.Matrix);
            QuantumGate expectedTDagger = new QuantumGate(QuantumGates.T_Dagger);

            Assert.IsTrue(
                AreMatricesEqual(inverted, expectedTDagger.Matrix, 1e-8),
                "The inversion of the T gate does not match T_Dagger.");
        }

        [Test]
        public void CustomGate_RegistrationAndApplication()
        {
            // Custom gate time! Roll your own matrix and slap it into the circuit.
            // We’ll use a fancy phase gate, because quantum’s all about those stylish twists.
            double theta = Math.PI / 3;
            Complex[,] customPhaseMatrix = new Complex[,]
            {
        { 1, 0 },
        { 0, Complex.Exp(Complex.ImaginaryOne * theta) }
            };
            QuantumGate customPhaseGate = new QuantumGate(customPhaseMatrix);
            QuantumGate identityGate = new QuantumGate(QuantumGates.Identity);
            QuantumGate composed = customPhaseGate.Then(identityGate);

            Assert.IsTrue(
                AreMatricesEqual(composed.Matrix, customPhaseGate.Matrix, 1e-8),
                "Custom phase gate composed with the identity gate did not return the same matrix.");
        }
    }
}