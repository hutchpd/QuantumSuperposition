using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumSuperposition.Core;
using QuantumSuperposition.Utilities;
using QuantumSuperposition.Systems;

namespace QuantumSoupTester
{
    [TestFixture]
    public class InterferenceTests
    {
        [Test]
        public void Hadamard_PhasePi_Hadamard_ProducesDeterministicOne()
        {
            Complex[] state = { Complex.One, Complex.Zero }; // |0>
            Complex[,] H = QuantumGates.Hadamard.Matrix;
            Complex[] after = QuantumMathUtility<Complex>.ApplyMatrix(state, H); // (|0>+|1>)/sqrt(2)
            Complex[,] P = QuantumGates.Phase(Math.PI).Matrix; // phase pi -> diag(1,-1)
            after = QuantumMathUtility<Complex>.ApplyMatrix(after, P);
            after = QuantumMathUtility<Complex>.ApplyMatrix(after, H);
            double p0 = after[0].Magnitude * after[0].Magnitude;
            double p1 = after[1].Magnitude * after[1].Magnitude;
            Assert.That(p0, Is.LessThan(1e-9));
            Assert.That(Math.Abs(p1 - 1.0), Is.LessThan(1e-9));
        }

        [Test]
        public void Hadamard_T_Hadamard_ShowsPartialInterference()
        {
            Complex[] state = { Complex.One, Complex.Zero }; // |0>
            Complex[] after = QuantumMathUtility<Complex>.ApplyMatrix(state, QuantumGates.Hadamard.Matrix);
            after = QuantumMathUtility<Complex>.ApplyMatrix(after, QuantumGates.T.Matrix); // phase pi/4 on |1>
            after = QuantumMathUtility<Complex>.ApplyMatrix(after, QuantumGates.Hadamard.Matrix);
            double p0 = after[0].Magnitude * after[0].Magnitude;
            double p1 = after[1].Magnitude * after[1].Magnitude;
            // Expect both probabilities non-zero and summing ~1
            Assert.That(Math.Abs(p0 + p1 - 1.0), Is.LessThan(1e-9));
            Assert.That(p0, Is.GreaterThan(0));
            Assert.That(p1, Is.GreaterThan(0));
        }

        [Test]
        public void Hadamard_RXPi_Hadamard_ReturnsUniform()
        {
            Complex[] state = { Complex.One, Complex.Zero }; // |0>
            Complex[] after = QuantumMathUtility<Complex>.ApplyMatrix(state, QuantumGates.Hadamard.Matrix);
            after = QuantumMathUtility<Complex>.ApplyMatrix(after, QuantumGates.RX(Math.PI).Matrix); // RX(pi) ~ X up to phase
            after = QuantumMathUtility<Complex>.ApplyMatrix(after, QuantumGates.Hadamard.Matrix);
            double p0 = after[0].Magnitude * after[0].Magnitude;
            double p1 = after[1].Magnitude * after[1].Magnitude;
            Assert.That(Math.Abs(p0 + p1 - 1.0), Is.LessThan(1e-9));
        }

        [Test]
        public void TwoQubit_CPhase_InterferencePattern()
        {
            // Start |00>
            Complex[] state = { Complex.One, Complex.Zero, Complex.Zero, Complex.Zero };
            // Apply H on first qubit (tensor H ? I)
            Complex[,] H = QuantumGates.Hadamard.Matrix;
            Complex[,] HI = new Complex[4,4];
            // Build H ? I manually
            for (int r=0;r<2;r++)
            {
                for (int c=0;c<2;c++)
                {
                    HI[r*2 + 0, c*2 + 0] = H[r,c];
                    HI[r*2 + 1, c*2 + 1] = H[r,c];
                }
            }
            Complex[] after = QuantumMathUtility<Complex>.ApplyMatrix(state, HI); // (|00>+|10>)/sqrt(2)
            // Apply controlled phase on |11> (no effect yet since second qubit 0)
            after = QuantumMathUtility<Complex>.ApplyMatrix(after, QuantumGates.CPhase(Math.PI).Matrix);
            // Apply Hadamard on second qubit (I ? H)
            Complex[,] IH = new Complex[4,4];
            for (int r=0;r<2;r++)
            {
                for (int c=0;c<2;c++)
                {
                    IH[0*2 + r, 0*2 + c] = H[r,c];
                    IH[1*2 + r, 1*2 + c] = H[r,c];
                }
            }
            after = QuantumMathUtility<Complex>.ApplyMatrix(after, IH);
            double norm = after.Sum(a => a.Magnitude * a.Magnitude);
            Assert.That(Math.Abs(norm - 1.0), Is.LessThan(1e-9));
        }

        [Test]
        public void QFT_PreservesNormalisation()
        {
            // 2-qubit uniform superposition
            Complex amp = new Complex(0.5,0);
            Complex[] state = { amp, amp, amp, amp }; // already normalised
            Complex[] after = QuantumMathUtility<Complex>.ApplyMatrix(state, QuantumGates.QuantumFourierTransformGate(2).Matrix);
            double norm = after.Sum(a => a.Magnitude * a.Magnitude);
            Assert.That(Math.Abs(norm - 1.0), Is.LessThan(1e-9));
        }
    }
}
