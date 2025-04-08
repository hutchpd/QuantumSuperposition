using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;

namespace QuantumMathTests
{
    [TestFixture]
    public class ProbabilisticFunctionalOperatorTests
    {
        // Ensure a consistent starting config for each test.
        [SetUp]
        public void Setup()
        {
            QuantumConfig.EnableNonObservationalArithmetic = false;
        }

        [Test]
        public void QuBit_ConditionalOperation_ShouldNotCollapseAndTransformStatesCorrectly()
        {
            // Arrange: create a qubit with states 1, 2, and 3.
            var qubit = new QuBit<int>(new[] { 1, 2, 3 });

            // Act: apply a conditional transformation:
            // if the state is even, multiply by 10; if odd, add 5.
            var transformed = qubit.Conditional(
                (value, weight) => value % 2 == 0,
                qb => qb.Select(x => x * 10),
                qb => qb.Select(x => x + 5)
            );

            // Expected outcomes:
            // 1 (odd) -> 1 + 5 = 6
            // 2 (even) -> 2 * 10 = 20
            // 3 (odd) -> 3 + 5 = 8
            var expectedStates = new[] { 6, 20, 8 };

            // Assert: all transformed states appear (order is not enforced)
            CollectionAssert.AreEquivalent(expectedStates, transformed.States);
            Assert.IsFalse(transformed.IsCollapsed, "Conditional operation should not collapse the qubit.");
        }

        [Test]
        public void QuBit_Select_ShouldTransformEachStateWithoutCollapsing()
        {
            // Arrange: a qubit with three states.
            var qubit = new QuBit<int>(new[] { 1, 2, 3 });

            // Act: multiply each state by 2 using the Select (map) function.
            var selected = qubit.Select(x => x * 2);

            // Expected: 1->2, 2->4, 3->6
            CollectionAssert.AreEquivalent(new[] { 2, 4, 6 }, selected.States);
            Assert.IsFalse(selected.IsCollapsed);
        }

        [Test]
        public void QuBit_SelectMany_ShouldPreserveWeightedMultiplicationOfAmplitudes()
        {
            // Arrange: a qubit with weighted states.
            var weightedItems = new (int, Complex)[]
            {
                (1, new Complex(0.5, 0)),
                (2, new Complex(2.0, 0))
            };
            var qubit = new QuBit<int>(weightedItems);

            // Act: For each state, generate a new qubit:
            // For 1 return a qubit with states {10, 11}, for 2 return {20}.
            QuBit<int> InnerSelector(int x)
            {
                return x == 1
                  ? new QuBit<int>(new[] { 10, 11 })
                  : new QuBit<int>(new[] { 20 });
            }
            var result = qubit.SelectMany(InnerSelector);

            // Expected: from 1 we get 10 and 11, from 2 we get 20.
            var expectedStates = new[] { 10, 11, 20 };
            CollectionAssert.AreEquivalent(expectedStates, result.States);
            Assert.IsFalse(result.IsCollapsed);
        }

        [Test]
        public void QuBit_Where_ShouldFilterStatesCorrectlyWithoutCollapse()
        {
            // Arrange: create a qubit with states 1, 2, 3, and 4.
            var qubit = new QuBit<int>(new[] { 1, 2, 3, 4 });

            // Act: filter out odd numbers.
            var filtered = qubit.Where(x => x % 2 == 0);

            // Assert: only even states (2 and 4) remain.
            CollectionAssert.AreEquivalent(new[] { 2, 4 }, filtered.States);
            Assert.IsFalse(filtered.IsCollapsed);
        }

        [Test]
        public void QuBit_NonObservationalArithmetic_ShouldNotCollapseQuBits()
        {
            // Arrange: enable non-observational arithmetic.
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var q1 = new QuBit<int>(new[] { 1, 2 });
            var q2 = new QuBit<int>(new[] { 3, 4 });

            // Act: perform qubit multiplication; expected Cartesian product:
            // 1*3 = 3, 1*4 = 4, 2*3 = 6, 2*4 = 8.
            var result = q1 * q2;

            // Assert
            CollectionAssert.AreEquivalent(new[] { 3, 4, 6, 8 }, result.States);
            Assert.IsFalse(result.IsCollapsed, "Non-observational arithmetic should preserve the superposition.");

            // Reset configuration.
            QuantumConfig.EnableNonObservationalArithmetic = false;
        }

        [Test]
        public void QuBit_ArithmeticWithScalar_NonObservational_ShouldRetainSuperposition()
        {
            // Arrange: enable non-observational arithmetic.
            QuantumConfig.EnableNonObservationalArithmetic = true;
            var qubit = new QuBit<int>(new[] { 1, 2, 3 });

            // Act: add scalar 5 to each branch.
            var result = qubit + 5; // expected states: 6, 7, 8.

            // Assert
            CollectionAssert.AreEquivalent(new[] { 6, 7, 8 }, result.States);
            Assert.IsFalse(result.IsCollapsed);

            QuantumConfig.EnableNonObservationalArithmetic = false;
        }

        [Test]
        public void QuBit_CommutativeOptimization_ShouldReturnSameResultRegardlessOfOrder()
        {
            // Arrange: enable non-observational arithmetic (which uses caching for pure commutative ops).
            QuantumConfig.EnableNonObservationalArithmetic = true;
            var a = new QuBit<int>(new[] { 3, 5 });
            var b = new QuBit<int>(new[] { 2, 4 });

            // Act: perform addition in both orders.
            var result1 = a + b;
            var result2 = b + a;

            // Assert: addition is commutative so the superpositions should be equivalent.
            CollectionAssert.AreEquivalent(result1.States, result2.States);

            QuantumConfig.EnableNonObservationalArithmetic = false;
        }

        [Test]
        public void QuBit_MonadChainOperations_ShouldSupportLINQStyle()
        {
            // Arrange: create a qubit with two states.
            var qubit = new QuBit<int>(new[] { 1, 2 });

            // Act: chain several LINQ–style operations:
            // 1. Select: add 1 to each state.
            // 2. Where: filter out values <= 2.
            // 3. SelectMany: for each remaining value, generate a qubit with two outcomes (multiplied by 3 and 4).
            // For state 1: 1+1=2 (filtered out), for state 2: 2+1=3 resulting in { 9, 12 }.
            var result = qubit.Select(x => x + 1)
                              .Where(x => x > 2)
                              .SelectMany(x => new QuBit<int>(new[] { x * 3, x * 4 }));

            // Assert: the final states should be 9 and 12.
            CollectionAssert.AreEquivalent(new[] { 9, 12 }, result.States);
            Assert.IsFalse(result.IsCollapsed);
        }

        [Test]
        public void Eigenstates_SelectAndWhereOperations_ShouldWorkCorrectly()
        {
            // Arrange: create eigenstates from three values.
            var eigen = new Eigenstates<int>(new[] { 10, 20, 30 });

            // Act: first, transform each value by dividing by 10 (yielding 1, 2, 3), then filter out values not less than 3.
            var selected = eigen.Select(x => x / 10);
            var filtered = selected.Where(x => x < 3);

            // Assert: the final eigenstates should contain only 1 and 2.
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, filtered.States);
        }
    }
}
