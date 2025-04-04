using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;

namespace QuantumMathTests
{
    [TestFixture]
    public class QuantumMathComplexTests
    {
        // Use the ComplexOperators instance for complex arithmetic.
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
            // Arrange: create a QuBit with non-zero (non-default) complex states.
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0), new Complex(0, 3) },
                complexOps
            );

            // Act
            bool result = qubit.EvaluateAll();

            // Assert: all states are non-default (non-zero)
            Assert.IsTrue(result);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsDefault_ReturnsFalse_Complex()
        {
            // Arrange: include a default (0+0i) value.
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), Complex.Zero, new Complex(0, 3) },
                complexOps
            );

            // Act
            bool result = qubit.EvaluateAll();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void QuBit_AdditionOperator_WithScalar_Complex()
        {
            // Arrange: a qubit with states (1+2i) and (3+4i)
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 2), new Complex(3, 4) },
                complexOps
            );
            var scalar = new Complex(1, 1);

            // Act: add the scalar to each state.
            var result = (qubit + scalar).States.ToList();

            // Expected:
            // (1+2i) + (1+1i) = (2+3i)
            // (3+4i) + (1+1i) = (4+5i)
            var expected = new List<Complex> { new Complex(2, 3), new Complex(4, 5) };

            // Assert: compare each complex value.
            Assert.AreEqual(expected.Count, result.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                AssertComplexEqual(expected[i], result[i]);
            }
        }

        [Test]
        public void QuBit_MultiplicationOperator_WithQuBit_Complex()
        {
            // Arrange: create two qubits.
            // qubitA: (2+0i) and (1+1i)
            var qubitA = new QuBit<Complex>(
                new List<Complex> { new Complex(2, 0), new Complex(1, 1) },
                complexOps
            );
            // qubitB: (3+0i) and (0+2i)
            var qubitB = new QuBit<Complex>(
                new List<Complex> { new Complex(3, 0), new Complex(0, 2) },
                complexOps
            );

            // Act: multiply the two qubits.
            var result = (qubitA * qubitB).States.ToList();

            // Expected Cartesian product:
            // (2+0i)*(3+0i) = (6+0i)
            // (2+0i)*(0+2i) = (0+4i)
            // (1+1i)*(3+0i) = (3+3i)
            // (1+1i)*(0+2i) = (-2+2i)
            var expected = new List<Complex>
            {
                new Complex(6, 0),
                new Complex(0, 4),
                new Complex(3, 3),
                new Complex(-2, 2)
            };

            // Assert: use collection equivalence (order-independent).
            CollectionAssert.AreEquivalent(expected, result);
        }

        [Test]
        public void QuBit_Append_IncreasesWeight_Complex()
        {
            // Arrange: create a weighted QuBit with states (1+0i) and (2+0i).
            var weightedItems = new[]
            {
                (new Complex(1, 0), (Complex)0.5),
                (new Complex(2, 0), (Complex)1.0)
            };

            var qubit = new QuBit<Complex>(weightedItems, complexOps);

            // Act: Append (1+0i) again to increase its weight.
            qubit.Append(new Complex(1, 0));

            // Assert: the weight for (1+0i) should now be 1.5 and for (2+0i) remain 1.0.
            var dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            Assert.AreEqual(1.5, dict[new Complex(1, 0)].Real, 1e-12);
            Assert.AreEqual(1.0, dict[new Complex(2, 0)].Real, 1e-12);
        }

        [Test]
        public void QuBit_NormalizeWeights_MakesSumEqualOne_Complex()
        {
            // Arrange: create a weighted qubit with states and weights.
            // (1+1i) with weight 0.3 and (2+2i) with weight 0.7.
            var weightedItems = new (Complex value, Complex weight)[]
            {
                (new Complex(1, 1), 0.3),
                (new Complex(2, 2), 0.7)
            };
            var qubit = new QuBit<Complex>(weightedItems, complexOps);

            // Act: normalize the weights.
            qubit.NormaliseWeights();
            var dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            // Assert: the sum of squared magnitudes of weights should equal 1.
            double sumSq = dict.Values.Sum(w => w.Magnitude * w.Magnitude);
            Assert.AreEqual(1.0, sumSq, 1e-12);
        }

        #endregion

        #region Eigenstates Tests for Complex

        [Test]
        public void Eigenstates_ArithmeticAddition_WithScalar_ReturnsCorrectValues_Complex()
        {
            // Arrange: create Eigenstates with keys (1+1i) and (2+2i).
            var items = new List<Complex> { new Complex(1, 1), new Complex(2, 2) };
            var eigen = new Eigenstates<Complex>(items, complexOps);
            var scalar = new Complex(1, 0);

            // Act: add scalar to each state.
            var resultEigen = eigen + scalar;

            // Expected:
            // (1+1i) + (1+0i) = (2+1i)
            // (2+2i) + (1+0i) = (3+2i)
            var expected = new List<Complex> { new Complex(2, 1), new Complex(3, 2) };
            var pairs = resultEigen.ToMappedWeightedValues().OrderBy(x => x.value.Real)
                                                      .ThenBy(x => x.value.Imaginary)
                                                      .ToList();

            // Assert
            Assert.AreEqual(expected.Count, pairs.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                AssertComplexEqual(expected[i], pairs[i].value);
            }
        }

        [Test]
        public void Eigenstates_FilteringOperators_LessThanOrEqual_ReturnsCorrectSubset_Complex()
        {
            // Arrange: create Eigenstates with keys:
            // (3+4i) has magnitude 5, (1+1i) ~1.414, (0+3i) magnitude 3.
            var items = new List<Complex> { new Complex(3, 4), new Complex(1, 1), new Complex(0, 3) };
            var eigen = new Eigenstates<Complex>(items, complexOps);
            // Use threshold (3+0i) with magnitude 3.
            var threshold = new Complex(3, 0);

            // Act: filter for states with magnitude less than or equal to 3.
            var result = (eigen <= threshold).ToValues().OrderBy(x => x.Magnitude).ToList();

            // Expected: (1+1i) and (0+3i)
            var expected = new List<Complex> { new Complex(1, 1), new Complex(0, 3) };

            // Assert
            Assert.AreEqual(expected.Count, result.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                AssertComplexEqual(expected[i], result[i]);
            }
        }

        [Test]
        public void Eigenstates_ToString_ReturnsProperFormatForConjunctive_Complex()
        {
            // Arrange: create Eigenstates with duplicate keys.
            var items = new List<Complex> { new Complex(4, 4), new Complex(4, 4), new Complex(4, 4) };
            var eigen = new Eigenstates<Complex>(items, complexOps);

            // Act: ToString should simplify to a single state representation.
            string result = eigen.ToString();

            // Assert: the result should contain "4" (as part of the key representation).
            Assert.IsTrue(result.Contains("4"));
        }

        [Test]
        public void Eigenstates_WeightedArithmetic_Multiply_Complex()
        {
            // Arrange: create two weighted Eigenstates.
            // eA: (2+0i) with weight 0.3 and (4+0i) with weight 0.7.
            var eA = new Eigenstates<Complex>(
                new (Complex, Complex)[]
                {
                    (new Complex(2, 0), 0.3),
                    (new Complex(4, 0), 0.7)
                },
                complexOps
            );
            // eB: (3+0i) with weight 0.5 and (0+2i) with weight 0.5.
            var eB = new Eigenstates<Complex>(
                new (Complex, Complex)[]
                {
                    (new Complex(3, 0), 0.5),
                    (new Complex(0, 2), 0.5)
                },
                complexOps
            );

            // Act: multiply the two eigenstates.
            var result = eA * eB;

            // Expected results:
            // (2+0i)*(3+0i) = (6+0i) with weight 0.15
            // (2+0i)*(0+2i) = (0+4i) with weight 0.15
            // (4+0i)*(3+0i) = (12+0i) with weight 0.35
            // (4+0i)*(0+2i) = (0+8i) with weight 0.35
            var weighted = result.ToMappedWeightedValues()
                                 .OrderBy(x => x.value.Real)
                                 .ThenBy(x => x.value.Imaginary)
                                 .ToList();
            var expected = new Dictionary<Complex, Complex>
            {
                { new Complex(6, 0),  0.15 },
                { new Complex(0, 4),  0.15 },
                { new Complex(12, 0), 0.35 },
                { new Complex(0, 8),  0.35 }
            };

            // Assert
            Assert.AreEqual(expected.Count, weighted.Count);
            foreach (var (val, wt) in weighted)
            {
                Assert.AreEqual(expected[val].Magnitude, wt.Magnitude, 1e-12, $"Mismatch for state {val}");
            }
        }

        [Test]
        public void Eigenstates_NormaliseWeights_SetsSumToOne_Complex()
        {
            // Arrange: weighted Eigenstates with keys (1+0i) weight 2.0, (2+0i) weight 6.0, (3+0i) weight 2.0.
            var eigen = new Eigenstates<Complex>(
                new (Complex, Complex)[]
                {
                    (new Complex(1, 0), 2.0),
                    (new Complex(2, 0), 6.0),
                    (new Complex(3, 0), 2.0)
                },
                complexOps
            );

            // Act: normalize the weights.
            eigen.NormaliseWeights();
            var dict = eigen.ToMappedWeightedValues().ToDictionary(x => x.value, x => x.weight);
            double sumSq = dict.Values.Sum(w => w.Magnitude * w.Magnitude);

            // Assert: total probability should be 1.
            Assert.AreEqual(1.0, sumSq, 1e-12);
        }

        [Test]
        public void Eigenstates_CollapseWeighted_ReturnsKeyWithHighestWeight_Complex()
        {
            // Arrange: weighted Eigenstates with keys (10+0i) weight 0.1, (20+0i) weight 0.3, (30+0i) weight 0.6.
            var eigen = new Eigenstates<Complex>(
                new (Complex, Complex)[]
                {
                    (new Complex(10, 0), 0.1),
                    (new Complex(20, 0), 0.3),
                    (new Complex(30, 0), 0.6)
                },
                complexOps
            );

            // Act
            var collapsed = eigen.CollapseWeighted();

            // Assert: expect (30+0i) since it has the highest weight.
            AssertComplexEqual(new Complex(30, 0), collapsed);
        }

        [Test]
        public void Eigenstates_TopNByWeight_ReturnsHighestWeightedKeys_Complex()
        {
            // Arrange: weighted Eigenstates with keys (10+0i) weight 0.1, (20+0i) weight 0.4, (30+0i) weight 0.4, (40+0i) weight 0.1.
            var eigen = new Eigenstates<Complex>(
                new (Complex, Complex)[]
                {
                    (new Complex(10, 0), 0.1),
                    (new Complex(20, 0), 0.4),
                    (new Complex(30, 0), 0.4),
                    (new Complex(40, 0), 0.1)
                },
                complexOps
            );

            // Act: get the top 2 keys by weight.
            var top2 = eigen.TopNByWeight(2)
                            .OrderBy(x => x.Real)
                            .ThenBy(x => x.Imaginary)
                            .ToList();

            // Expected: (20+0i) and (30+0i)
            var expected = new List<Complex> { new Complex(20, 0), new Complex(30, 0) };

            // Assert
            Assert.AreEqual(expected.Count, top2.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                AssertComplexEqual(expected[i], top2[i]);
            }
        }

        #endregion
    }
}
