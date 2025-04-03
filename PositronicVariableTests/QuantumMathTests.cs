using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace QuantumMathTests
{
    [TestFixture]
    public class QuantumMathTests
    {
        private readonly IQuantumOperators<int> intOps = new IntOperators();

        #region QuBit Tests

        [Test]
        public void QuBit_EvaluateAll_AllNonDefault_ReturnsTrue()
        {
            // Arrange: Create a QuBit with only non-default (non-zero) elements.
            var qubit = new QuBit<int>(new List<int> { 1, 2, 3 }, intOps);

            // Act: EvaluateAll should return true if none of the states equals default(int) (0).
            bool result = qubit.EvaluateAll();

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsDefault_ReturnsFalse()
        {
            // Arrange: Create a QuBit that contains a default value (0 for int).
            var qubit = new QuBit<int>(new List<int> { 1, 0, 3 }, intOps);

            // Act
            bool result = qubit.EvaluateAll();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void QuBit_ModuloOperator_WithScalarFirst_ReturnsCorrectRemainders()
        {
            // Arrange: Using operator %(T, QuBit<T>) to calculate 10 % each element.
            var qubit = new QuBit<int>(new List<int> { 3, 4 }, intOps);

            // Act: 10 % qubit should yield [10 % 3, 10 % 4] = [1, 2].
            var result = (10 % qubit).States.ToList();

            // Assert
            CollectionAssert.AreEqual(new List<int> { 1, 2 }, result);
        }

        [Test]
        public void QuBit_ModuloOperator_WithScalarSecond_ReturnsCorrectRemainders()
        {
            // Arrange: Using operator %(QuBit<T>, T) to calculate each element % 10.
            var qubit = new QuBit<int>(new List<int> { 3, 4 }, intOps);

            // Act: qubit % 10 should yield [3 % 10, 4 % 10] = [3, 4].
            var result = (qubit % 10).States.ToList();

            // Assert
            CollectionAssert.AreEqual(new List<int> { 3, 4 }, result);
        }

        [Test]
        public void QuBit_Append_AddsElementToStates()
        {
            // Arrange: Create a QuBit and then append an element.
            var qubit = new QuBit<int>(new List<int> { 1, 2 }, intOps);

            // Act
            qubit.Append(3);
            var states = qubit.States.ToList();

            // Assert: Verify that the state now includes the appended element.
            CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, states);
        }

        #endregion

        #region Eigenstates Tests

        [Test]
        public void Eigenstates_FactorsProjection_ReturnsCorrectFactors()
        {
            // Arrange: Using the projection constructor to compute 10 % x for each x in 1..10.
            // Then, filtering with "== 0" should select the factors of 10.
            var eigen = new Eigenstates<int>(Enumerable.Range(1, 10), x => 10 % x, intOps);

            // Act: Filter for keys where the projection equals 0.
            var factors = (eigen == 0).ToValues().OrderBy(x => x).ToList();

            // Expected factors for 10: 1, 2, 5, and 10.
            CollectionAssert.AreEqual(new List<int> { 1, 2, 5, 10 }, factors);
        }

        [Test]
        public void Eigenstates_ArithmeticAddition_WithScalar_ReturnsCorrectValues()
        {
            // Arrange: Create Eigenstates with keys 1, 2, and 3.
            var eigen = new Eigenstates<int>(new List<int> { 1, 2, 3 }, intOps);

            // Act: Add 5 to each projected value.
            var resultEigen = eigen + 5;
            string debugString = resultEigen.ToDebugString();

            // Assert: Check that each key's projected value has been correctly updated.
            Assert.IsTrue(debugString.Contains("1 => 6"));
            Assert.IsTrue(debugString.Contains("2 => 7"));
            Assert.IsTrue(debugString.Contains("3 => 8"));
        }

        [Test]
        public void Eigenstates_FilteringOperators_LessThanOrEqual_ReturnsCorrectSubset()
        {
            // Arrange: Create Eigenstates with keys 1 through 5.
            var eigen = new Eigenstates<int>(new List<int> { 1, 2, 3, 4, 5 }, intOps);

            // Act: Filter keys where the projected value is less than or equal to 3.
            var result = (eigen <= 3).ToValues().OrderBy(x => x).ToList();

            // Assert: Expect keys 1, 2, and 3.
            CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, result);
        }

        [Test]
        public void Eigenstates_ToString_ReturnsProperFormatForConjunctive()
        {
            // Arrange: Create Eigenstates with multiple identical entries.
            var eigen = new Eigenstates<int>(new List<int> { 4, 4, 4 }, intOps);

            // Act: ToString should return a single value (since all keys are the same).
            string result = eigen.ToString();

            // Assert: Expected to just return "4" because of the unique state.
            Assert.AreEqual("4", result);
        }

        [Test]
        public void Eigenstates_ToString_ReturnsProperFormatForDisjunctive()
        {
            // Arrange: Create Eigenstates with distinct keys and switch to disjunctive mode.
            var eigen = new Eigenstates<int>(new List<int> { 1, 2, 3 }, intOps);
            eigen.Any();

            // Act: ToString should return a string in the format "any(...)".
            string result = eigen.ToString();

            // Assert: Check that the result starts with "any(" and contains each expected key.
            Assert.IsTrue(result.StartsWith("any("));
            Assert.IsTrue(result.Contains("1"));
            Assert.IsTrue(result.Contains("2"));
            Assert.IsTrue(result.Contains("3"));
        }

        [Test]
        public void QuBit_WeightedConstructor_ProducesExpectedStatesAndWeights()
        {
            // Arrange
            var weightedItems = new (int value, double weight)[]
            {
                (2, 0.2), (2, 0.1), // duplicate state 2 with more weight
                (5, 0.7)
            };

            // Act
            var qubit = new QuBit<int>(weightedItems, intOps);

            // Assert
            // States should be distinct keys: 2, 5
            CollectionAssert.AreEquivalent(new[] { 2, 5 }, qubit.States);

            // The combined weight for '2' should be 0.2 + 0.1 = 0.3
            // The weight for '5' is 0.7
            var dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            Assert.AreEqual(0.3, dict[2], 1e-14);
            Assert.AreEqual(0.7, dict[5], 1e-14);
            Assert.IsTrue(qubit.IsWeighted);
        }

        [Test]
        public void QuBit_WeightedArithmeticOperator_Scalar_Add()
        {
            // Arrange
            // Weighted QuBit: states = [2, 5] with weights [0.3, 0.7]
            var qubit = new QuBit<int>(new[] { (2, 0.3), (5, 0.7) }, intOps);

            // Act
            // + 1 => [3, 6] with the same weights [0.3, 0.7]
            var result = qubit + 1;

            // Assert
            var weightedValues = result.ToWeightedValues().OrderBy(x => x.value).ToList();
            // The new states:
            CollectionAssert.AreEqual(new[] { 3, 6 }, weightedValues.Select(x => x.value));
            // Weights remain the same
            Assert.AreEqual(0.3, weightedValues[0].weight, 1e-14);
            Assert.AreEqual(0.7, weightedValues[1].weight, 1e-14);
        }

        [Test]
        public void QuBit_WeightedArithmeticOperator_QuBit_Multiply()
        {
            // Arrange
            // QuBit A => (2 -> 0.3, 4 -> 0.7)
            var qubitA = new QuBit<int>(new[] { (2, 0.3), (4, 0.7) }, intOps);
            // QuBit B => (3 -> 0.5, 5 -> 0.5)
            var qubitB = new QuBit<int>(new[] { (3, 0.5), (5, 0.5) }, intOps);

            // Act
            var result = qubitA * qubitB;
            // The Cartesian product: 
            //   A:2 w=0.3  * B:3 w=0.5 => state=6   combined weight=0.3*0.5=0.15
            //   A:2 w=0.3  * B:5 w=0.5 => state=10  combined weight=0.3*0.5=0.15
            //   A:4 w=0.7  * B:3 w=0.5 => state=12  combined weight=0.7*0.5=0.35
            //   A:4 w=0.7  * B:5 w=0.5 => state=20  combined weight=0.7*0.5=0.35

            var items = result.ToWeightedValues().OrderBy(x => x.value).ToList();

            // Assert
            var expected = new Dictionary<int, double> {
                { 6,  0.15 },
                { 10, 0.15 },
                { 12, 0.35 },
                { 20, 0.35 }
            };

            Assert.AreEqual(4, items.Count);

            foreach (var (val, wt) in items)
            {
                Assert.AreEqual(expected[val], wt, 1e-14,
                    $"Mismatch for state {val}; expected {expected[val]}, got {wt}");
            }
        }

        [Test]
        public void QuBit_Weighted_Append_IncrementsWeightOfExistingState()
        {
            // Arrange
            var qubit = new QuBit<int>(new[] { (3, 0.5), (5, 1.0) }, intOps);

            // Act
            // Append(3) => should increment the weight of 3 by +1.0
            qubit.Append(3);
            var weighted = qubit.ToWeightedValues().ToList();

            // Assert
            // The new weight for 3 should be 1.5. The weight for 5 remains 1.0
            var (val3, w3) = weighted.First(x => x.value == 3);
            var (val5, w5) = weighted.First(x => x.value == 5);

            Assert.AreEqual(3, val3);
            Assert.AreEqual(1.5, w3, 1e-14);
            Assert.AreEqual(5, val5);
            Assert.AreEqual(1.0, w5, 1e-14);
        }

        [Test]
        public void QuBit_NormalizeWeights_MakesSumEqualOne()
        {
            // Arrange
            // Weights sum to 2.5 => (2 -> 0.5, 4 -> 2.0)
            var qubit = new QuBit<int>(new[] { (2, 0.5), (4, 2.0) }, intOps);

            // Act
            qubit.NormalizeWeights();
            // new weights => 
            //   2 => 0.5/2.5 = 0.2
            //   4 => 2.0/2.5 = 0.8
            var dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            // Assert
            Assert.AreEqual(1.0, dict.Values.Sum(), 1e-14);
            Assert.AreEqual(0.2, dict[2], 1e-14);
            Assert.AreEqual(0.8, dict[4], 1e-14);
        }

        [Test]
        public void QuBit_MostProbable_ReturnsHighestWeightedState()
        {
            // Arrange
            // (10 -> 0.1, 20 -> 0.7, 30 -> 0.2)
            var qubit = new QuBit<int>(new[] { (10, 0.1), (20, 0.7), (30, 0.2) }, intOps);

            // Act
            var mp = qubit.MostProbable();

            // Assert
            Assert.AreEqual(20, mp,
                "MostProbable should return the state with the largest weight");
        }

        [Test]
        public void QuBit_WeightedEquals_ReturnsTrueIfWeightsAreWithinTolerance()
        {
            // Arrange
            var q1 = new QuBit<int>(new[] { (2, 0.25), (4, 0.75) }, intOps);
            // Slight difference in weights but within 1e-12 tolerance
            var q2 = new QuBit<int>(new[] { (2, 0.250000000001), (4, 0.749999999999) }, intOps);

            // Act / Assert
            Assert.IsTrue(q1.Equals(q2),
                "QuBit should consider these equal within default tolerance.");
        }

        #endregion

        #region Eigenstates Weighted Tests

        [Test]
        public void Eigenstates_WeightedConstructor_StoresDistinctKeysAndWeights()
        {
            // Arrange
            var data = new (int value, double weight)[]
            {
                (3, 1.5), (3, 0.2),
                (4, 0.8),
                (5, 0.0)
            };

            // Act
            var eigen = new Eigenstates<int>(data, intOps);

            // Assert
            // Distinct keys => 3,4,5
            var keys = eigen.States.OrderBy(x => x).ToList();
            CollectionAssert.AreEqual(new[] { 3, 4, 5 }, keys);

            // Check combined weight for 3 => 1.7
            var wDict = eigen.ToMappedWeightedValues().ToDictionary(x => x.value, x => x.weight);
            Assert.AreEqual(1.7, wDict[3], 1e-14);
            Assert.AreEqual(0.8, wDict[4], 1e-14);
            Assert.AreEqual(0.0, wDict[5], 1e-14);
        }

        [Test]
        public void Eigenstates_WeightedArithmetic_AddScalar()
        {
            // Arrange
            var eigen = new Eigenstates<int>(new[] { (1, 0.4), (2, 0.6) }, intOps);

            // Act
            // Add 10 => new mapped values: 11,12
            // Weights remain 0.4, 0.6 at each original key
            var result = eigen + 10;
            var pairs = result.ToMappedWeightedValues().OrderBy(x => x.value).ToList();

            // Assert
            CollectionAssert.AreEqual(new[] { 11, 12 }, pairs.Select(x => x.value));
            Assert.AreEqual(0.4, pairs[0].weight, 1e-14);
            Assert.AreEqual(0.6, pairs[1].weight, 1e-14);
        }

        [Test]
        public void Eigenstates_WeightedArithmetic_Eigenstates_Multiply()
        {
            // Arrange
            var eA = new Eigenstates<int>(new[] { (2, 0.3), (4, 0.7) }, intOps);
            var eB = new Eigenstates<int>(new[] { (3, 0.5), (5, 0.5) }, intOps);

            // Act
            var result = eA * eB;

            // Assert
            // Expect states: 6, 10, 12, 20 
            //   2 * 3 => 6   with weight=0.3*0.5=0.15
            //   2 * 5 => 10  with weight=0.3*0.5=0.15
            //   4 * 3 => 12  with weight=0.7*0.5=0.35
            //   4 * 5 => 20  with weight=0.7*0.5=0.35
            var weighted = result.ToMappedWeightedValues().OrderBy(x => x.value).ToList();
            var expected = new Dictionary<int, double> {
                { 6,  0.15 },
                { 10, 0.15 },
                { 12, 0.35 },
                { 20, 0.35 }
            };
            Assert.AreEqual(expected.Count, weighted.Count);

            foreach (var (val, wt) in weighted)
            {
                Assert.AreEqual(expected[val], wt, 1e-14,
                    $"Mismatch at state {val}; expected {expected[val]}, got {wt}");
            }
        }

        [Test]
        public void Eigenstates_NormalizeWeights_SetsSumToOne()
        {
            // Arrange
            var e = new Eigenstates<int>(
                new[] { (3, 2.0), (4, 6.0), (5, 2.0) }, intOps
            );
            // sum of weights = 10.0

            // Act
            e.NormalizeWeights();
            var dict = e.ToMappedWeightedValues().ToDictionary(x => x.value, x => x.weight);

            // Assert
            Assert.AreEqual(1.0, dict.Values.Sum(), 1e-14);
            Assert.AreEqual(0.2, dict[3], 1e-14);
            Assert.AreEqual(0.6, dict[4], 1e-14);
            Assert.AreEqual(0.2, dict[5], 1e-14);
        }

        [Test]
        public void Eigenstates_CollapseWeighted_ReturnsKeyWithHighestWeight()
        {
            // Arrange
            var e = new Eigenstates<int>(
                new[] { (10, 0.1), (20, 0.3), (30, 0.6) }, intOps
            );

            // Act
            int collapsed = e.CollapseWeighted();

            // Assert
            Assert.AreEqual(30, collapsed, "Should pick the key with the largest weight");
        }

        [Test]
        public void Eigenstates_FilterByWeight_ExcludesLowerWeights()
        {
            // Arrange
            var e = new Eigenstates<int>(
                new[] { (2, 0.1), (3, 0.8), (4, 0.05), (5, 0.05) }, intOps
            );

            // Act
            // Keep only those with weight >= 0.1
            var filtered = e.FilterByWeight(w => w >= 0.1);
            var keys = filtered.States.OrderBy(x => x).ToList();

            // Assert
            CollectionAssert.AreEqual(new[] { 2, 3 }, keys,
                "Should drop states 4 and 5 that have weight=0.05");
        }

        [Test]
        public void Eigenstates_WeightedEqualityChecks_WithinTolerance()
        {
            // Arrange
            var e1 = new Eigenstates<int>(new[] { (5, 0.3333333333), (7, 0.6666666667) }, intOps);
            var e2 = new Eigenstates<int>(new[] { (5, 0.333333334), (7, 0.666666666) }, intOps);

            // Act / Assert
            Assert.IsTrue(e1.Equals(e2),
                "Eigenstates should be equal within the default tolerance for double comparisons");
        }

        [Test]
        public void Eigenstates_TopNByWeight_ReturnsHighestWeightedKeys()
        {
            // Arrange
            var e = new Eigenstates<int>(
                new[] { (10, 0.1), (20, 0.4), (30, 0.4), (40, 0.1) }, intOps
            );

            // Act
            // Top 2 by weight => keys 20, 30 (both 0.4)
            var top2 = e.TopNByWeight(2).OrderBy(x => x).ToList();

            // Assert
            CollectionAssert.AreEqual(new[] { 20, 30 }, top2);
        }

        #endregion
    }
}
