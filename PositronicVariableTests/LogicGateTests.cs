using System;
using System.Numerics;
using NUnit.Framework;
using System.Linq;
using QuantumCore; // Ensure this namespace includes your QuantumGates class

namespace QuantumMathTests
{
    [TestFixture]
    public class LogicGateTests
    {
        // Helper: Multiply two square matrices.
        private Complex[,] MultiplyMatrices(Complex[,] A, Complex[,] B)
        {
            int rows = A.GetLength(0);
            int common = A.GetLength(1);
            int cols = B.GetLength(1);
            var product = new Complex[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Complex sum = Complex.Zero;
                    for (int k = 0; k < common; k++)
                    {
                        sum += A[i, k] * B[k, j];
                    }
                    product[i, j] = sum;
                }
            }
            return product;
        }

        // Helper: Multiply a matrix by a vector.
        private Complex[] MultiplyMatrixVector(Complex[,] matrix, Complex[] vector)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            if (vector.Length != cols)
                throw new ArgumentException("Matrix column count must equal vector length.");

            var result = new Complex[rows];
            for (int i = 0; i < rows; i++)
            {
                Complex sum = Complex.Zero;
                for (int j = 0; j < cols; j++)
                {
                    sum += matrix[i, j] * vector[j];
                }
                result[i] = sum;
            }
            return result;
        }

        // Helper: Compare two matrices elementwise with a given tolerance.
        private void AssertMatricesEqual(Complex[,] expected, Complex[,] actual, double tolerance = 1e-12)
        {
            Assert.AreEqual(expected.GetLength(0), actual.GetLength(0), "Row counts differ");
            Assert.AreEqual(expected.GetLength(1), actual.GetLength(1), "Column counts differ");

            for (int i = 0; i < expected.GetLength(0); i++)
            {
                for (int j = 0; j < expected.GetLength(1); j++)
                {
                    Assert.That(actual[i, j].Real, Is.EqualTo(expected[i, j].Real).Within(tolerance),
                        $"Real parts differ at element ({i},{j})");
                    Assert.That(actual[i, j].Imaginary, Is.EqualTo(expected[i, j].Imaginary).Within(tolerance),
                        $"Imaginary parts differ at element ({i},{j})");
                }
            }
        }

        [Test]
        public void RootNot_Twice_Equals_PauliX()
        {
            // Arrange: Retrieve gates from the QuantumGates class.
            var rootNot = QuantumGates.RootNot;
            var pauliX = QuantumGates.PauliX;

            // Act: Multiply the RootNot gate with itself.
            var result = MultiplyMatrices(rootNot, rootNot);

            // Assert: The resulting matrix should equal the PauliX gate.
            AssertMatricesEqual(pauliX, result);
        }

        [Test]
        public void Hadamard_AppliedTo_BasisState0_Yields_EqualSuperposition()
        {
            // Arrange: Basis state |0> represented as [1, 0] and the Hadamard gate.
            var hadamard = QuantumGates.Hadamard;
            var inputVector = new Complex[] { 1, 0 };
            var expected = new Complex[] { 1 / Math.Sqrt(2), 1 / Math.Sqrt(2) };

            // Act: Multiply the Hadamard gate with the input vector.
            var result = MultiplyMatrixVector(hadamard, inputVector);

            // Assert: Each element in the result should equal the expected value.
            Assert.That(result[0].Real, Is.EqualTo(expected[0].Real).Within(1e-12));
            Assert.That(result[0].Imaginary, Is.EqualTo(expected[0].Imaginary).Within(1e-12));
            Assert.That(result[1].Real, Is.EqualTo(expected[1].Real).Within(1e-12));
            Assert.That(result[1].Imaginary, Is.EqualTo(expected[1].Imaginary).Within(1e-12));
        }

        [Test]
        public void Identity_AppliedTo_Vector_Yields_SameVector()
        {
            // Arrange: Create a sample vector and use the Identity gate.
            var identity = QuantumGates.Identity;
            var vector = new Complex[] { new Complex(2, 3), new Complex(4, -1) };

            // Act: Multiply the Identity matrix by the vector.
            var result = MultiplyMatrixVector(identity, vector);

            // Assert: The output should match the original vector.
            for (int i = 0; i < vector.Length; i++)
            {
                Assert.That(result[i].Real, Is.EqualTo(vector[i].Real).Within(1e-12));
                Assert.That(result[i].Imaginary, Is.EqualTo(vector[i].Imaginary).Within(1e-12));
            }
        }

        [Test]
        public void PhaseGate_AppliedTo_BasisState1_Yields_PhasedVector()
        {
            // Arrange: For a phase rotation of π/2, applying the gate to basis state |1> = [0,1]
            // should yield [0, exp(i*pi/2)] = [0, i].
            var phaseGate = QuantumGates.Phase(Math.PI / 2);
            var inputVector = new Complex[] { 0, 1 };
            var expected = new Complex[] { 0, Complex.ImaginaryOne };

            // Act: Multiply the phase gate matrix by the input vector.
            var result = MultiplyMatrixVector(phaseGate, inputVector);

            // Assert: Check the resulting vector.
            Assert.That(result[0].Real, Is.EqualTo(expected[0].Real).Within(1e-12));
            Assert.That(result[0].Imaginary, Is.EqualTo(expected[0].Imaginary).Within(1e-12));
            Assert.That(result[1].Real, Is.EqualTo(expected[1].Real).Within(1e-12));
            Assert.That(result[1].Imaginary, Is.EqualTo(expected[1].Imaginary).Within(1e-12));
        }

        [Test]
        public void Hadamard_Twice_Equals_Identity()
        {
            // Arrange: The product of two successive Hadamard operations should equal the Identity.
            var hadamard = QuantumGates.Hadamard;
            var identity = QuantumGates.Identity;

            // Act: Multiply the Hadamard matrix by itself.
            var doubleHadamard = MultiplyMatrices(hadamard, hadamard);

            // Assert: The double Hadamard transformation should equal the Identity matrix.
            AssertMatricesEqual(identity, doubleHadamard);
        }
    }
}
