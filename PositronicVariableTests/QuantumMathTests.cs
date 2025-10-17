// QuantumMathComplexTests — Because reality isn't weird enough until it's imaginary.

using QuantumSuperposition.Core;
using QuantumSuperposition.Operators;
using QuantumSuperposition.QuantumSoup;
using System.Numerics;

namespace QuantumSoupTester
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
            QuBit<Complex> qubit = new(
                [new Complex(1, 1), new Complex(2, 0), new Complex(0, 3)],
                complexOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.True);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsDefault_ReturnsFalse_Complex()
        {
            QuBit<Complex> qubit = new(
                [new Complex(1, 1), Complex.Zero, new Complex(0, 3)],
                complexOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.False);
        }

        [Test]
        public void QuBit_AdditionOperator_WithScalar_Complex()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<Complex> qubit = new(
                [new Complex(1, 2), new Complex(3, 4)],
                complexOps
            );
            Complex scalar = new(1, 1);
            List<Complex> result = (qubit + scalar).States.ToList();
            List<Complex> expected = [new Complex(2, 3), new Complex(4, 5)];

            Assert.That(result.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; i++)
            {
                AssertComplexEqual(expected[i], result[i]);
            }
        }

        [Test]
        public void QuBit_MultiplicationOperator_WithQuBit_Complex()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<Complex> qubitA = new(
                [new Complex(2, 0), new Complex(1, 1)],
                complexOps
            );
            QuBit<Complex> qubitB = new(
                [new Complex(3, 0), new Complex(0, 2)],
                complexOps
            );

            List<Complex> result = (qubitA * qubitB).States.ToList();
            List<Complex> expected =
            [
        new Complex(6, 0),
        new Complex(0, 4),
        new Complex(3, 3),
        new Complex(-2, 2)
    ];

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_Append_IncreasesWeight_Complex()
        {
            (Complex, Complex)[] weightedItems = new[]
            {
        (new Complex(1, 0), (Complex)0.5),
        (new Complex(2, 0), (Complex)1.0)
    };
            QuBit<Complex> qubit = new(weightedItems, complexOps);
            _ = qubit.Append(new Complex(1, 0));

            Dictionary<Complex, Complex> dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            Assert.That(dict[new Complex(1, 0)].Real, Is.EqualTo(1.5).Within(1e-12));
            Assert.That(dict[new Complex(2, 0)].Real, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void QuBit_NormalizeWeights_MakesSumEqualOne_Complex()
        {
            (Complex value, Complex weight)[] weightedItems = new (Complex value, Complex weight)[]
            {
        (new Complex(1, 1), 0.3),
        (new Complex(2, 2), 0.7)
            };
            QuBit<Complex> qubit = new(weightedItems, complexOps);

            qubit.NormaliseWeights();
            Dictionary<Complex, Complex> dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            double sumSq = dict.Values.Sum(w => w.Magnitude * w.Magnitude);
            Assert.That(sumSq, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void QuBit_Observe_WithSeed_IsDeterministic()
        {
            QuBit<Complex> qubit = new(
                [new Complex(1, 1), new Complex(2, 0)],
                complexOps
            );

            Complex observed1 = qubit.Observe(42);
            Complex observed2 = qubit.Observe(42);

            Assert.That(observed1, Is.EqualTo(observed2));
        }

        [Test]
        public void QuBit_WithMockCollapse_ReturnsForcedValue()
        {
            Complex forcedValue = new(5, 5);
            QuBit<Complex> qubit = new QuBit<Complex>(
                [new Complex(1, 1), new Complex(2, 0)],
                complexOps
            ).WithMockCollapse(forcedValue);

            Complex observed = qubit.Observe();
            Assert.That(observed, Is.EqualTo(forcedValue));
        }

        [Test]
        public void QuBit_ObserveInBasis_AppliesHadamardAndCollapses()
        {
            List<(Complex value, Complex weight)> weightedStates =
            [
        (new Complex(1, 0), new Complex(1, 0)),
        (new Complex(0, 1), new Complex(0, 1))
    ];
            QuBit<Complex> qubit = new(weightedStates, complexOps);

            Complex observed = qubit.ObserveInBasis(QuantumGates.Hadamard);
            List<Complex> originalStates = weightedStates.Select(ws => ws.value).ToList();

            Assert.That(originalStates, Does.Contain(observed));
        }

        [Test]
        public void QuBit_ModificationAfterCollapse_ThrowsException()
        {
            QuBit<Complex> qubit = new(
                [new Complex(1, 1), new Complex(2, 0)],
                complexOps
            );
            Complex _ = qubit.Observe();

            Assert.That(() => qubit.Append(new Complex(1, 1)), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void QuBit_CloneMutable_AllowsModificationsAfterCollapse()
        {
            QuBit<Complex> qubit = new(
                [new Complex(1, 1), new Complex(2, 0)],
                complexOps
            );

            QuBit<Complex> mutableClone = (QuBit<Complex>)qubit.Clone();
            Complex _ = qubit.Observe();

            Assert.That(() => mutableClone.Append(new Complex(3, 3)), Throws.Nothing);
        }

        [Test]
        public void Eigenstates_Equality_Checks()
        {
            (Complex, Complex)[] weightedItems = new (Complex, Complex)[]
            {
        (new Complex(10, 0), 0.1),
        (new Complex(20, 0), 0.3)
            };
            Eigenstates<Complex> eigenA = new(weightedItems, complexOps);
            Eigenstates<Complex> eigenB = new(weightedItems, complexOps);

            Assert.That(eigenA.Equals(eigenB), Is.True);
            Assert.That(eigenA.StrictlyEquals(eigenB), Is.True);
        }


        #endregion
    }

    [TestFixture]
    public class QuantumMathDoubleTests
    {
        // A sensible calculator for doubles. Less imaginary than Complex.
        private readonly IQuantumOperators<double> doubleOps = new DoubleOperators();

        private void AssertDoubleEqual(double expected, double actual, double tolerance = 1e-12)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance));
        }

        #region QuBit Tests for double

        [Test]
        public void QuBit_AdditionOperator_WithScalar_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<double> qubit = new(
                [1.0, 2.0],
                doubleOps
            );
            double scalar = 1.5;
            List<double> result = (qubit + scalar).States.ToList();
            List<double> expected = [2.5, 3.5];

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_MultiplicationOperator_WithQuBit_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<double> qubitA = new(
                [2.0, 3.0],
                doubleOps
            );
            QuBit<double> qubitB = new(
                [4.0, 5.0],
                doubleOps
            );

            List<double> result = (qubitA * qubitB).States.ToList();
            List<double> expected = [8.0, 10.0, 12.0, 15.0];

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_ModulusOperator_WithScalar_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<double> qubit = new(
                [5.5, 7.0],
                doubleOps
            );
            double scalar = 3.0;
            List<double> result = (qubit % scalar).States.ToList();
            List<double> expected = [5.5 % 3.0, 7.0 % 3.0];

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_ModulusOperator_WithQuBit_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<double> qubitA = new(
                [10.5, 12.0],
                doubleOps
            );
            QuBit<double> qubitB = new(
                [2.0, 3.0],
                doubleOps
            );

            List<double> result = (qubitA % qubitB).States.ToList();
            List<double> expected =
            [
                10.5 % 2.0,
                10.5 % 3.0,
                12.0 % 2.0,
                12.0 % 3.0
            ];

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_DivisionOperator_WithScalar_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<double> qubit = new(
                [8.0, 6.0],
                doubleOps
            );
            double scalar = 2.0;
            List<double> result = (qubit / scalar).States.ToList();
            List<double> expected = [4.0, 3.0];

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_EvaluateAll_AllPositive_ReturnsTrue_Double()
        {
            QuBit<double> qubit = new(
                [1.0, 2.0, 3.0],
                doubleOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.True);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsZero_ReturnsFalse_Double()
        {
            QuBit<double> qubit = new(
                [0.0, 2.0],
                doubleOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.False);
        }

        [Test]
        public void DoubleOperators_Modulus_Works()
        {
            DoubleOperators ops = new();

            double a = 10.5;
            double b = 3.0;
            double result = ops.Mod(a, b);

            AssertDoubleEqual(10.5 % 3.0, result);
        }

        [Test]
        public void DoubleOperators_Divide_Works()
        {
            DoubleOperators ops = new();
            double a = 10.0;
            double b = 2.5;
            double result = ops.Divide(a, b);

            AssertDoubleEqual(4.0, result);
        }

        #endregion
    }
}
