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

        #endregion
    }
}
