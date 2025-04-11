using System.Numerics;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Core;

namespace QuantumMathTests
{
    [TestFixture]
    public class ProbabilisticFunctionalOperatorTests
    {
        [SetUp]
        public void Setup()
        {
            // Resetting the quantum cosmos. No multiverse corruption allowed between tests.
            QuantumConfig.EnableNonObservationalArithmetic = false;
        }

        [Test]
        public void QuBit_ConditionalOperation_ShouldNotCollapseAndTransformStatesCorrectly()
        {
            // Like Schrödinger but sassier — even numbers get multiplied, odds get +5 therapy
            var qubit = new QuBit<int>(new[] { 1, 2, 3 });

            var transformed = qubit.Conditional(
                (value, weight) => value % 2 == 0,
                qb => qb.Select(x => x * 10),   // The “Even Elite” treatment
                qb => qb.Select(x => x + 5)     // The “Odd Compensation Plan”
            );

            var expectedStates = new[] { 6, 20, 8 };

            // We expect the new reality to include these upgraded states
            Assert.That(transformed.States, Is.EquivalentTo(expectedStates));
            Assert.That(transformed.IsCollapsed, Is.False, "Nobody looked. Collapse would be rude.");

        }

        [Test]
        public void QuBit_Select_ShouldTransformEachStateWithoutCollapsing()
        {
            // Turning each state into its double — like a motivational poster but numeric
            var qubit = new QuBit<int>(new[] { 1, 2, 3 });
            var selected = qubit.Select(x => x * 2);

            Assert.That(selected.States, Is.EquivalentTo(new[] { 2, 4, 6 }));
            Assert.That(selected.IsCollapsed, Is.False, "Mapping ≠ spying. Superposition intact.");
        }

        [Test]
        public void QuBit_SelectMany_ShouldPreserveWeightedMultiplicationOfAmplitudes()
        {
            // Weighted decisions from the quantum council
            var weightedItems = new (int, Complex)[]
            {
                (1, new Complex(0.5, 0)),
                (2, new Complex(2.0, 0))
            };
            var qubit = new QuBit<int>(weightedItems);

            QuBit<int> InnerSelector(int x) =>
                x == 1 ? new QuBit<int>(new[] { 10, 11 }) : new QuBit<int>(new[] { 20 });

            var result = qubit.SelectMany(InnerSelector);

            Assert.That(result.States, Is.EquivalentTo(new[] { 10, 11, 20 }));
            Assert.That(result.IsCollapsed, Is.False, "Just branching, not spying. We’re good.");
        }

        [Test]
        public void QuBit_Where_ShouldFilterStatesCorrectlyWithoutCollapse()
        {
            // Delete the odds. Not from history — just from this multiverse
            var qubit = new QuBit<int>(new[] { 1, 2, 3, 4 });
            var filtered = qubit.Where(x => x % 2 == 0);

            Assert.That(filtered.States, Is.EquivalentTo(new[] { 2, 4 }));
            Assert.That(filtered.IsCollapsed, Is.False, "Filtering ≠ observing. The waveform lives.");
        }

        [Test]
        public void QuBit_NonObservationalArithmetic_ShouldNotCollapseQuBits()
        {
            // Enabling math that doesn’t stare into your soul and judge you
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var q1 = new QuBit<int>(new[] { 1, 2 });
            var q2 = new QuBit<int>(new[] { 3, 4 });

            var result = q1 * q2; // Quantum Tinder: every value matches with every other

            // use nuget 4 syntax
            Assert.That(result.States, Is.EquivalentTo(new[] { 3, 4, 6, 8 }),
                "Multiplying qubits should be like a quantum orgy — all combinations.");

            QuantumConfig.EnableNonObservationalArithmetic = false;
        }

        [Test]
        public void QuBit_ArithmeticWithScalar_NonObservational_ShouldRetainSuperposition()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var qubit = new QuBit<int>(new[] { 1, 2, 3 });
            var result = qubit + 5; // Just casually throwing +5 at quantum uncertainty

            Assert.That(result.States, Is.EquivalentTo(new[] { 6, 7, 8 }),
                "Adding a scalar should be like adding sprinkles to a cupcake — no collapse.");

            QuantumConfig.EnableNonObservationalArithmetic = false;
        }

        [Test]
        public void QuBit_CommutativeOptimization_ShouldReturnSameResultRegardlessOfOrder()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var a = new QuBit<int>(new[] { 3, 5 });
            var b = new QuBit<int>(new[] { 2, 4 });

            var result1 = a + b;
            var result2 = b + a;

            Assert.That(result1.States, Is.EquivalentTo(result2.States),
                "Commutativity isn’t just polite — it’s optimized");

            QuantumConfig.EnableNonObservationalArithmetic = false;
        }

        [Test]
        public void QuBit_MonadChainOperations_ShouldSupportLINQStyle()
        {
            // It’s a pipeline. A journey. A heroic quest through lambda land.
            var qubit = new QuBit<int>(new[] { 1, 2 });

            var result = qubit
                .Select(x => x + 1)                    // +1 to every state
                .Where(x => x > 2)                     // Keep only those brave enough to be > 2
                .SelectMany(x => new QuBit<int>(new[] { x * 3, x * 4 })); // Multiply their efforts

            Assert.That(result.States, Is.EquivalentTo(new[] { 9,12 }));
            Assert.That(result.IsCollapsed, Is.False, "The pipeline remains unsnooped.");
        }

        [Test]
        public void Eigenstates_SelectAndWhereOperations_ShouldWorkCorrectly()
        {
            // Eigenstates get their glow-up and a spa treatment
            var eigen = new Eigenstates<int>(new[] { 10, 20, 30 });

            var selected = eigen.Select(x => x / 10); // They shrink — spiritually
            var filtered = selected.Where(x => x < 3); // Only the small survive

            Assert.That(filtered.States, Is.EquivalentTo(new[] { 1, 2 }));
        }
    }
}
