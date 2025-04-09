// QuantumMathComplexTests — Because reality isn't weird enough until it's imaginary.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumCore;

namespace QuantumMathTests
{
    [TestFixture]
    public class QuantumMathComplexTests
    {
        // Use the ComplexOperators instance for complex arithmetic. Like a calculator, but it judges you.
        private readonly IQuantumOperators<Complex> complexOps = new ComplexOperators();

        // Helper to compare Complex numbers with tolerance.
        private void AssertComplexEqual(Complex expected, Complex actual, double tolerance = 1e-12)
        {
            Assert.That(actual.Real, Is.EqualTo(expected.Real).Within(tolerance), "Real parts differ");
            Assert.That(actual.Imaginary, Is.EqualTo(expected.Imaginary).Within(tolerance), "Imaginary parts differ");
        }

        #region QuBit Tests for Complex

        [Test]
        public void QuBit_EvaluateAll_AllNonDefault_ReturnsTrue_Complex()
        {
            // We throw three complex states into a QuBit and check if they’re all proudly non-zero.
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0), new Complex(0, 3) },
                complexOps
            );

            bool result = qubit.EvaluateAll();

            // Assert: all are still confidently non-default.
            Assert.IsTrue(result);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsDefault_ReturnsFalse_Complex()
        {
            // One zero slips into the state pool like a ghost at a dinner party.
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), Complex.Zero, new Complex(0, 3) },
                complexOps
            );

            bool result = qubit.EvaluateAll();

            // Assert: not all guests are real.
            Assert.IsFalse(result);
        }

        [Test]
        public void QuBit_AdditionOperator_WithScalar_Complex()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            // A QuBit strolls into a scalar and gets all its states politely bumped.
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 2), new Complex(3, 4) },
                complexOps
            );
            var scalar = new Complex(1, 1);

            var result = (qubit + scalar).States.ToList();

            var expected = new List<Complex> { new Complex(2, 3), new Complex(4, 5) };

            Assert.AreEqual(expected.Count, result.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                AssertComplexEqual(expected[i], result[i]);
            }
        }

        [Test]
        public void QuBit_MultiplicationOperator_WithQuBit_Complex()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            // Quantum double trouble: two qubits get multiplied into glorious cartesian confusion.
            var qubitA = new QuBit<Complex>(
                new List<Complex> { new Complex(2, 0), new Complex(1, 1) },
                complexOps
            );
            var qubitB = new QuBit<Complex>(
                new List<Complex> { new Complex(3, 0), new Complex(0, 2) },
                complexOps
            );

            var result = (qubitA * qubitB).States.ToList();

            var expected = new List<Complex>
            {
                new Complex(6, 0),
                new Complex(0, 4),
                new Complex(3, 3),
                new Complex(-2, 2)
            };

            CollectionAssert.AreEquivalent(expected, result);
        }

        [Test]
        public void QuBit_Append_IncreasesWeight_Complex()
        {
            // Watch a lonely complex state get invited back to the party and become more influential.
            var weightedItems = new[]
            {
                (new Complex(1, 0), (Complex)0.5),
                (new Complex(2, 0), (Complex)1.0)
            };

            var qubit = new QuBit<Complex>(weightedItems, complexOps);

            qubit.Append(new Complex(1, 0));

            var dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            Assert.AreEqual(1.5, dict[new Complex(1, 0)].Real, 1e-12);
            Assert.AreEqual(1.0, dict[new Complex(2, 0)].Real, 1e-12);
        }

        [Test]
        public void QuBit_NormalizeWeights_MakesSumEqualOne_Complex()
        {
            // Create imbalance. Normalize. Achieve spiritual quantum unity.
            var weightedItems = new (Complex value, Complex weight)[]
            {
                (new Complex(1, 1), 0.3),
                (new Complex(2, 2), 0.7)
            };
            var qubit = new QuBit<Complex>(weightedItems, complexOps);

            qubit.NormaliseWeights();
            var dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            double sumSq = dict.Values.Sum(w => w.Magnitude * w.Magnitude);
            Assert.AreEqual(1.0, sumSq, 1e-12);
        }

        #endregion

        #region SchrödingerKnowsNow

        [Test]
        public void QuBit_Observe_WithSeed_IsDeterministic()
        {
            // Observe once. Observe again. Hope for déjà vu.
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0) },
                complexOps
            );

            var observed1 = qubit.Observe(42);
            var observed2 = qubit.Observe(42);

            Assert.AreEqual(observed1, observed2);
        }

        [Test]
        public void QuBit_WithMockCollapse_ReturnsForcedValue()
        {
            // Schrodinger’s cat gets replaced by a stage actor who always says the same line.
            var forcedValue = new Complex(5, 5);
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0) },
                complexOps
            ).WithMockCollapse(forcedValue);

            var observed = qubit.Observe();
            Assert.AreEqual(forcedValue, observed);
        }

        [Test]
        public void QuBit_ObserveInBasis_AppliesHadamardAndCollapses()
        {
            // We throw a Hadamard at it and see what falls out.
            var weightedStates = new List<(Complex value, Complex weight)>
            {
                (new Complex(1, 0), new Complex(1, 0)),
                (new Complex(0, 1), new Complex(0, 1))
            };

            var qubit = new QuBit<Complex>(weightedStates, complexOps);

            var observed = qubit.ObserveInBasis(QuantumGates.Hadamard);
            var originalStates = weightedStates.Select(ws => ws.value).ToList();
            CollectionAssert.Contains(originalStates, observed);
        }

        [Test]
        public void QuBit_ModificationAfterCollapse_ThrowsException()
        {
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0) },
                complexOps
            );
            var _ = qubit.Observe();

            Assert.Throws<InvalidOperationException>(() => qubit.Append(new Complex(1, 1)));
        }

        [Test]
        public void QuBit_CloneMutable_AllowsModificationsAfterCollapse()
        {
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0) },
                complexOps
            );

            var mutableClone = (QuBit<Complex>)qubit.Clone();
            var collapsedValue = qubit.Observe();

            Assert.DoesNotThrow(() => mutableClone.Append(new Complex(3, 3)));
        }

        [Test]
        public void Eigenstates_Equality_Checks()
        {
            var weightedItems = new (Complex, Complex)[]
            {
                (new Complex(10, 0), 0.1),
                (new Complex(20, 0), 0.3)
            };
            var eigenA = new Eigenstates<Complex>(weightedItems, complexOps);
            var eigenB = new Eigenstates<Complex>(weightedItems, complexOps);

            Assert.IsTrue(eigenA.Equals(eigenB));
            Assert.IsTrue(eigenA.StrictlyEquals(eigenB));
        }

        #endregion
    }
}
