using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumSuperposition.Core;

namespace QuantumSoupTester
{
    [TestFixture]
    public class GateCatalogueTests
    {
        private static void AssertClose(Complex actual, Complex expected, double tol)
        {
            Assert.That(Math.Abs(actual.Real - expected.Real), Is.LessThanOrEqualTo(tol), $"Real parts differ: {actual.Real} vs {expected.Real}");
            Assert.That(Math.Abs(actual.Imaginary - expected.Imaginary), Is.LessThanOrEqualTo(tol), $"Imag parts differ: {actual.Imaginary} vs {expected.Imaginary}");
        }

        [Test]
        public void IdentityOfLength_ProducesCorrectDimension()
        {
            var g = QuantumGates.IdentityOfLength(3);
            Assert.That(g.Matrix.GetLength(0), Is.EqualTo(8));
            Assert.That(g.Matrix.GetLength(1), Is.EqualTo(8));
            for (int i=0;i<8;i++)
            {
                for (int j=0;j<8;j++)
                {
                    if (i==j) AssertClose(g.Matrix[i,j], Complex.One, 1e-12);
                    else AssertClose(g.Matrix[i,j], Complex.Zero, 1e-12);
                }
            }
        }

        [Test]
        public void HadamardOfLength_NormalisationHolds()
        {
            var h2 = QuantumGates.HadamardOfLength(2).Matrix; // 4x4
            double rowNorm(int r)
            {
                double sum = 0; for (int c=0;c<4;c++) sum += h2[r,c].Magnitude * h2[r,c].Magnitude; return sum;
            }
            for (int r=0;r<4;r++)
            {
                Assert.That(rowNorm(r), Is.EqualTo(1.0).Within(1e-9));
            }
        }

        [Test]
        public void PauliY_IsSkewHermitianTimesI()
        {
            var y = QuantumGates.PauliY.Matrix;
            // Y^2 = I
            Complex[,] prod = Multiply(y,y);
            for (int i=0;i<2;i++) for (int j=0;j<2;j++) AssertClose(prod[i,j], i==j? Complex.One: Complex.Zero, 1e-12);
        }

        [Test]
        public void Controlled_PauliX_Dimension()
        {
            var cx = QuantumGates.Controlled(QuantumGates.PauliX);
            Assert.That(cx.Matrix.GetLength(0), Is.EqualTo(4));
            Assert.That(cx.Matrix.GetLength(1), Is.EqualTo(4));
        }

        [Test]
        public void QFTGate_HasCorrectGlobalPhasePattern()
        {
            var qft = QuantumGates.QuantumFourierTransformGate(2).Matrix; // 4x4
            double norm = 1.0 / Math.Sqrt(4);
            Assert.That(qft[0,0].Magnitude, Is.EqualTo(norm).Within(1e-12));
            Complex expected = norm * Complex.Exp(Complex.ImaginaryOne * 2 * Math.PI * 1 * 1 / 4);
            AssertClose(qft[1,1], expected, 1e-12);
        }

        [Test]
        public void Toffoli_FlipsTargetOn11()
        {
            var t = QuantumGates.Toffoli.Matrix; // 8x8
            AssertClose(t[6,7], Complex.One, 1e-12);
            AssertClose(t[7,6], Complex.One, 1e-12);
        }

        [Test]
        public void Fredkin_SwapsWhenControlOne()
        {
            var f = QuantumGates.Fredkin.Matrix;
            AssertClose(f[5,6], Complex.One, 1e-12);
            AssertClose(f[6,5], Complex.One, 1e-12);
        }

        [Test]
        public void SquareRootSwap_CompositionGivesSwap()
        {
            var srs = QuantumGates.SquareRootSwap.Matrix;
            var comp = Multiply(srs, srs);
            var swap = QuantumGates.SWAP.Matrix;
            for (int i=0;i<4;i++) for (int j=0;j<4;j++) AssertClose(comp[i,j], swap[i,j], 1e-9);
        }

        private static Complex[,] Multiply(Complex[,] A, Complex[,] B)
        {
            int r = A.GetLength(0); int c = B.GetLength(1); int common = A.GetLength(1);
            Complex[,] R = new Complex[r,c];
            for (int i=0;i<r;i++) for (int j=0;j<c;j++) { Complex sum = Complex.Zero; for (int k=0;k<common;k++) sum += A[i,k]*B[k,j]; R[i,j] = sum; }
            return R;
        }
    }
}
