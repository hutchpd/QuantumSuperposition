using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;

namespace QuantumMathTests
{
    [TestFixture]
    public class QuantumMathStringTests
    {
        // Use our simple StringOperators instance.
        private readonly IQuantumOperators<string> stringOps = new StringOperators();

        [Test]
        public void QuBit_EvaluateAll_AllNonDefault_ReturnsTrue_String()
        {
            var qubit = new QuBit<string>(
                new List<string> { "Alpha", "Beta", "Gamma" },
                stringOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.True);
        }

        [Test]
        public void QuBit_EvaluateAll_ContainsDefault_ReturnsFalse_String()
        {
            var qubit = new QuBit<string>(
                new List<string> { "Alpha", null, "Gamma" },
                stringOps
            );

            bool result = qubit.EvaluateAll();
            Assert.That(result, Is.False);
        }

        [Test]
        public void QuBit_AdditionOperator_WithScalar_String()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var qubit = new QuBit<string>(
                new List<string> { "Hello", "World" },
                stringOps
            );
            var scalar = "!";
            // Expect the addition operator to perform concatenation.
            var result = (qubit + scalar).States.ToList();
            var expected = new List<string> { "Hello!", "World!" };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void QuBit_Concatenation_WithAnotherQuBit_String()
        {
            QuantumConfig.EnableNonObservationalArithmetic = true;

            var qubitA = new QuBit<string>(
                new List<string> { "Quantum", "Super" },
                stringOps
            );
            var qubitB = new QuBit<string>(
                new List<string> { "Math", "Test" },
                stringOps
            );

            // Expect the addition operator to produce every combination.
            var result = (qubitA + qubitB).States.ToList();
            var expected = new List<string>
            {
                "QuantumMath", "QuantumTest", "SuperMath", "SuperTest"
            };

            Assert.That(result, Is.EquivalentTo(expected));
        }
    }
}
