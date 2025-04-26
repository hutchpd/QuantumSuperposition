// QuantumMathComplexTests — Because reality isn't weird enough until it's imaginary.

using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;

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
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0), new Complex(0, 3) },
                complexOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.True);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsDefault_ReturnsFalse_Complex()
        {
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), Complex.Zero, new Complex(0, 3) },
                complexOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.False);
        }

        [Test]
        public void QuBit_AdditionOperator_WithScalar_Complex()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 2), new Complex(3, 4) },
                complexOps
            );
            var scalar = new Complex(1, 1);
            var result = (qubit + scalar).States.ToList();
            var expected = new List<Complex> { new Complex(2, 3), new Complex(4, 5) };

            Assert.That(result.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; i++)
                AssertComplexEqual(expected[i], result[i]);
        }

        [Test]
        public void QuBit_MultiplicationOperator_WithQuBit_Complex()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

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

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_Append_IncreasesWeight_Complex()
        {
            var weightedItems = new[]
            {
        (new Complex(1, 0), (Complex)0.5),
        (new Complex(2, 0), (Complex)1.0)
    };
            var qubit = new QuBit<Complex>(weightedItems, complexOps);
            qubit.Append(new Complex(1, 0));

            var dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            Assert.That(dict[new Complex(1, 0)].Real, Is.EqualTo(1.5).Within(1e-12));
            Assert.That(dict[new Complex(2, 0)].Real, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void QuBit_NormalizeWeights_MakesSumEqualOne_Complex()
        {
            var weightedItems = new (Complex value, Complex weight)[]
            {
        (new Complex(1, 1), 0.3),
        (new Complex(2, 2), 0.7)
            };
            var qubit = new QuBit<Complex>(weightedItems, complexOps);

            qubit.NormaliseWeights();
            var dict = qubit.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            double sumSq = dict.Values.Sum(w => w.Magnitude * w.Magnitude);
            Assert.That(sumSq, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void QuBit_Observe_WithSeed_IsDeterministic()
        {
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0) },
                complexOps
            );

            var observed1 = qubit.Observe(42);
            var observed2 = qubit.Observe(42);

            Assert.That(observed1, Is.EqualTo(observed2));
        }

        [Test]
        public void QuBit_WithMockCollapse_ReturnsForcedValue()
        {
            var forcedValue = new Complex(5, 5);
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0) },
                complexOps
            ).WithMockCollapse(forcedValue);

            var observed = qubit.Observe();
            Assert.That(observed, Is.EqualTo(forcedValue));
        }

        [Test]
        public void QuBit_ObserveInBasis_AppliesHadamardAndCollapses()
        {
            var weightedStates = new List<(Complex value, Complex weight)>
    {
        (new Complex(1, 0), new Complex(1, 0)),
        (new Complex(0, 1), new Complex(0, 1))
    };
            var qubit = new QuBit<Complex>(weightedStates, complexOps);

            var observed = qubit.ObserveInBasis(QuantumGates.Hadamard);
            var originalStates = weightedStates.Select(ws => ws.value).ToList();

            Assert.That(originalStates, Does.Contain(observed));
        }

        [Test]
        public void QuBit_ModificationAfterCollapse_ThrowsException()
        {
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0) },
                complexOps
            );
            var _ = qubit.Observe();

            Assert.That(() => qubit.Append(new Complex(1, 1)), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void QuBit_CloneMutable_AllowsModificationsAfterCollapse()
        {
            var qubit = new QuBit<Complex>(
                new List<Complex> { new Complex(1, 1), new Complex(2, 0) },
                complexOps
            );

            var mutableClone = (QuBit<Complex>)qubit.Clone();
            var _ = qubit.Observe();

            Assert.That(() => mutableClone.Append(new Complex(3, 3)), Throws.Nothing);
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

            var qubit = new QuBit<double>(
                new List<double> { 1.0, 2.0 },
                doubleOps
            );
            var scalar = 1.5;
            var result = (qubit + scalar).States.ToList();
            var expected = new List<double> { 2.5, 3.5 };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_MultiplicationOperator_WithQuBit_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var qubitA = new QuBit<double>(
                new List<double> { 2.0, 3.0 },
                doubleOps
            );
            var qubitB = new QuBit<double>(
                new List<double> { 4.0, 5.0 },
                doubleOps
            );

            var result = (qubitA * qubitB).States.ToList();
            var expected = new List<double> { 8.0, 10.0, 12.0, 15.0 };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_ModulusOperator_WithScalar_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var qubit = new QuBit<double>(
                new List<double> { 5.5, 7.0 },
                doubleOps
            );
            var scalar = 3.0;
            var result = (qubit % scalar).States.ToList();
            var expected = new List<double> { 5.5 % 3.0, 7.0 % 3.0 };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_ModulusOperator_WithQuBit_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var qubitA = new QuBit<double>(
                new List<double> { 10.5, 12.0 },
                doubleOps
            );
            var qubitB = new QuBit<double>(
                new List<double> { 2.0, 3.0 },
                doubleOps
            );

            var result = (qubitA % qubitB).States.ToList();
            var expected = new List<double>
            {
                10.5 % 2.0,
                10.5 % 3.0,
                12.0 % 2.0,
                12.0 % 3.0
            };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_DivisionOperator_WithScalar_Double()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var qubit = new QuBit<double>(
                new List<double> { 8.0, 6.0 },
                doubleOps
            );
            var scalar = 2.0;
            var result = (qubit / scalar).States.ToList();
            var expected = new List<double> { 4.0, 3.0 };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_EvaluateAll_AllPositive_ReturnsTrue_Double()
        {
            var qubit = new QuBit<double>(
                new List<double> { 1.0, 2.0, 3.0 },
                doubleOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.True);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsZero_ReturnsFalse_Double()
        {
            var qubit = new QuBit<double>(
                new List<double> { 0.0, 2.0 },
                doubleOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.False);
        }

        [Test]
        public void DoubleOperators_Modulus_Works()
        {
            var ops = new DoubleOperators();

            double a = 10.5;
            double b = 3.0;
            double result = ops.Mod(a, b);

            AssertDoubleEqual(10.5 % 3.0, result);
        }

        [Test]
        public void DoubleOperators_Divide_Works()
        {
            var ops = new DoubleOperators();
            double a = 10.0;
            double b = 2.5;
            double result = ops.Divide(a, b);

            AssertDoubleEqual(4.0, result);
        }

        #endregion
    }
}
