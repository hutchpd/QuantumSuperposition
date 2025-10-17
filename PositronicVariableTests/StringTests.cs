using QuantumSuperposition.Core;
using QuantumSuperposition.Operators;
using QuantumSuperposition.QuantumSoup;

namespace QuantumSoupTester
{
    [TestFixture]
    public class QuantumMathStringTests
    {
        // Use our simple StringOperators instance.
        private readonly IQuantumOperators<string> stringOps = new StringOperators();

        [Test]
        public void QuBit_EvaluateAll_AllNonDefault_ReturnsTrue_String()
        {
            QuBit<string> qubit = new(
                ["Alpha", "Beta", "Gamma"],
                stringOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.True);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsDefault_ReturnsFalse_String()
        {
            QuBit<string> qubit = new(
                ["Alpha", null, "Gamma"],
                stringOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.False);
        }

        [Test]
        public void QuBit_AdditionOperator_WithScalar_String()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<string> qubit = new(
                ["Hello", "World"],
                stringOps
            );
            string scalar = "!";
            // Expect the addition operator to perform concatenation.
            List<string> result = (qubit + scalar).States.ToList();
            List<string> expected = ["Hello!", "World!"];

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_Concatenation_WithAnotherQuBit_String()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            QuBit<string> qubitA = new(
                ["Quantum", "Super"],
                stringOps
            );
            QuBit<string> qubitB = new(
                ["Math", "Test"],
                stringOps
            );

            // Expect the addition operator to produce every combination.
            List<string> result = (qubitA + qubitB).States.ToList();
            List<string> expected =
            [
                "QuantumMath", "QuantumTest", "SuperMath", "SuperTest"
            ];

            Assert.That(result, Is.EquivalentTo(expected));
        }
    }
}
