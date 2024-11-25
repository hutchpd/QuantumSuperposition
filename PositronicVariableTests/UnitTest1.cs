using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicVariableTests
    {
        [SetUp]
        public void SetUp()
        {
            PositronicVariable<int>.ResetStaticVariables();
        }


        [Test]
        public void AdditionOperator_AddsValueToPositronicVariable()
        {
            // Arrange
            var variable = new PositronicVariable<int>(5);

            // Act
            var result = variable + 3;

            // Assert
            var expectedValues = new List<int> { 8 };
            CollectionAssert.AreEquivalent(expectedValues, result.Value.ToValues());
        }

        [Test]
        public void ModuloOperator_CalculatesModuloOfPositronicVariable()
        {
            // Arrange
            var variable = new PositronicVariable<int>(10);

            // Act
            var result = variable % 3;

            // Assert
            var expectedValues = new List<int> { 1 };
            CollectionAssert.AreEquivalent(expectedValues, result.Value.ToValues());
        }

        [Test]
        public void AssignMethod_AssignsValueFromAnotherPositronicVariable()
        {
            // Arrange
            PositronicVariable<int>.SetEntropy(1);
            var variable1 = new PositronicVariable<int>(5);
            var variable2 = new PositronicVariable<int>(10);

            // Act
            variable1.Assign(variable2);

            // Assert
            var expectedValues = new List<int> { 10 };
            CollectionAssert.AreEquivalent(expectedValues, variable1.Value.ToValues());

            PositronicVariable<int>.SetEntropy(-1); // Reset entropy to default
        }

        [Test]
        public void ConvergedMethod_ReturnsZeroWhenTimelineCountIsLessThanThree()
        {
            // Arrange
            var variable = new PositronicVariable<int>(5);

            // Act
            var result = variable.Converged();

            // Assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ConvergedMethod_ReturnsZeroWhenNoConvergenceIsFound()
        {
            // Arrange
            var variable = new PositronicVariable<int>(5);
            variable.Assign(new PositronicVariable<int>(10));
            variable.Assign(new PositronicVariable<int>(15));

            // Act
            var result = variable.Converged();

            // Assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ConvergedMethod_ReturnsConvergenceStepWhenConvergenceIsFound()
        {
            // Arrange
            // Create the variable outside the convergence loop to persist across iterations
            var variable = new PositronicVariable<int>(5);
            int convergenceResult = 0;

            // Act
            PositronicVariable<int>.RunConvergenceLoop(() =>
            {
                // Simulate variable assignment that leads to convergence
                variable.Assign(new PositronicVariable<int>(10));

                // Capture convergence result during time reversal
                if (PositronicVariable<int>.GetEntropy() < 0)
                {
                    convergenceResult = variable.Converged();
                }
            });

            // Assert
            Assert.IsTrue(convergenceResult > 0);
        }


        [Test]
        public void RunConvergenceLoop_VariableConvergesToExpectedState()
        {
            // Arrange
            var output = new StringBuilder();
            var writer = new StringWriter(output);
            Console.SetOut(writer);

            // Reset static variables before the test
            PositronicVariable<int>.ResetStaticVariables();

            // Declare 'antival' outside the lambda to persist across iterations
            var antival = new PositronicVariable<int>(-1);

            // Act
            PositronicVariable<int>.RunConvergenceLoop(() =>
            {
                // Display initial state
                Console.WriteLine($"The antival is {antival}");

                // Manipulate the value in a positronic way
                var val = (antival + 1) % 3;

                Console.WriteLine($"The value is {val}");

                // Assign the new value to 'antival'
                antival.Assign(val);
            });

            // Restore console output
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            // Assert
            var expectedOutput = "The antival is any(1, 0, 2)\nany(The value is 1\n, The value is 2\n, The value is 0\n)";
            Assert.That(
                output.ToString().Replace("\r\n", "\n").Replace("\r", "\n"), Is.EqualTo(expectedOutput.Replace("\r\n", "\n").Replace("\r", "\n")));
        }
    }
}
