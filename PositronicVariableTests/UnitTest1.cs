using Microsoft.VisualStudio.TestPlatform.TestHost;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        /// <summary>
        /// Manually runs negative-time passes: starts with antival = -1,
        /// each pass does antival = (antival + 1) % 3,
        /// checks if it converged. If so, unify. Finally checks if 
        /// the last timeline slice includes the states 0,1,2.
        /// 
        /// This test is purely about convergence & stored states,
        /// ignoring console output / interpolation.
        /// </summary>
        [Test]
        public void Antival_CyclesThrough0_1_2_AndEventuallyConvergesToAllThree()
        {
            // Force negative time
            PositronicVariable<int>.SetEntropy(-1);

            // We'll start with antival = -1, just like your main test
            var antival = new PositronicVariable<int>(-1);

            // We'll do up to 20 negative steps
            int maxSteps = 20;
            bool converged = false;

            for (int i = 0; i < maxSteps; i++)
            {
                // Each negative step: antival = (antival + 1) % 3
                var nextValue = (antival + 1) % 3;
                antival.Assign(nextValue);

                // After assigning, check if this variable has converged
                if (antival.Converged() > 0)
                {
                    converged = true;
                    break;
                }
            }

            // If we detected a match in the timeline, unify a bunch of slices
            if (converged)
            {
                // Try unifying 
                antival.UnifyAll();
            }

            // Now check the final states in the *last* timeline slice
            // We want them to contain {0,1,2} in some order
            var finalStates = antival.ToValues().OrderBy(x => x).Distinct().ToList();

            // Confirm we got 0,1,2 (n.b. we should expect -1 as well for the first pass)
            CollectionAssert.AreEquivalent(new List<int> { -1, 0, 1, 2 }, finalStates,
                $"Expected to see all [0,1,2] in final slice, but got [{string.Join(", ", finalStates)}].");
        }

        [Test]
        public void NegativeTimeTimelineInspection()
        {
            // Force negative time
            PositronicVariable<int>.SetEntropy(-1);

            // Start antival at -1
            var antival = new PositronicVariable<int>(-1);

            // We'll do up to 10 negative steps
            for (int i = 0; i < 10; i++)
            {
                // Each iteration: antival = (antival + 1) % 3
                var nextValue = (antival + 1) % 3;
                antival.Assign(nextValue);

                // After each assignment, let's see what's in the timeline
                // We'll do a local method that enumerates slices
                Console.WriteLine($"Iteration {i}, antival timeline:");
                DumpTimeline(antival);

                // Now see if 'antival' says it converged
                int c = antival.Converged();
                Console.WriteLine($"Converged() => {c}");
                Console.WriteLine("-----");
            }
        }

        private void DumpTimeline(PositronicVariable<int> variable)
        {
            // Use reflection to access the private "timeline" field.
            FieldInfo timelineField = typeof(PositronicVariable<int>)
                .GetField("timeline", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timelineField == null)
            {
                Console.WriteLine("Could not find the 'timeline' field.");
                return;
            }

            // Cast the field value to the expected type.
            var slices = timelineField.GetValue(variable) as List<QuBit<int>>;
            if (slices == null)
            {
                Console.WriteLine("Timeline is null or not of the expected type.");
                return;
            }

            // Loop through each timeline slice and print its states.
            for (int j = 0; j < slices.Count; j++)
            {
                var states = slices[j].ToValues().OrderBy(x => x);
                Console.WriteLine($"Slice {j}: [{string.Join(", ", states)}]");
            }
        }

        [Test]
        public void ForcedPartialUnifyAfterConvergence()
        {
            PositronicVariable<int>.SetEntropy(-1);
            var antival = new PositronicVariable<int>(-1);

            // We'll track any convergence distance
            int cVal = 0;
            for (int i = 0; i < 10; i++)
            {
                var nextValue = (antival + 1) % 3;
                antival.Assign(nextValue);

                cVal = antival.Converged();
                if (cVal > 0)
                    break;
            }

            Assert.That(cVal, Is.GreaterThan(0), "Expected to eventually find a repeated slice!");
            // Now unify exactly 'cVal' slices
            antival.Unify(cVal);

            // Dump the final timeline
            // Expect the last slice to contain all states
            // e.g. [0,1,2]
            Console.WriteLine("After forced partial unify:");
            // DumpTimeline(antival);

            var finalStates = antival.ToValues().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { -1, 0, 1, 2 }, finalStates);
        }

        [Test]
        public void NegativeLoopThenManualForward()
        {
            var output = new StringBuilder();
            var writer = new StringWriter(output);

            Console.SetOut(TextWriter.Null);
            PositronicVariable<int>.SetEntropy(-1);
            var antival = new PositronicVariable<int>(-1);

            // negative runs
            for (int i = 0; i < 20; i++)
            {
                // user code
                var val = (antival + 1) % 3;
                antival.Assign(val);

                bool allConverged = PositronicVariable<int>.AllConverged();
                if (allConverged)
                {
                    // unify all
                    foreach (var v in PositronicVariable<int>.GetAllVariables())
                        v.UnifyAll();
                    break;
                }
            }

            // now do one forward pass, capturing
            Console.SetOut(writer);
            PositronicVariable<int>.SetEntropy(1);

            // forward pass
            Console.WriteLine($"The antival is {antival}");
            var forwardVal = (antival + 1) % 3;
            Console.WriteLine($"The value is {forwardVal}");
            antival.Assign(forwardVal);

            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.WriteLine("Output was:\n" + output);

            // Now see if we got "any(1, 0, 2)" etc.
            string final = output.ToString();
            Assert.That(final.Contains("any("), "Expected to see 'any(...)' in final pass!");
        }

        [Test]
        public void ZeroConvergence_NoRepeatedStates_AlwaysUnique()
        {
            PositronicVariable<int>.SetEntropy(-1);
            var var1 = new PositronicVariable<int>(10);

            // We'll do 10 steps, each time +2 % 9999 => effectively no cycle in 10 steps
            for (int i = 0; i < 10; i++)
            {
                var nextVal = (var1 + 2) % 9999;
                var1.Assign(nextVal);
            }

            // Should never have repeated older slice
            Assert.That(var1.Converged(), Is.EqualTo(0));
        }

        [Test]
        public void EnoughStepsFor3Cycle_ExpectConverged()
        {
            PositronicVariable<int>.SetEntropy(-1);
            var antival = new PositronicVariable<int>(-1);

            bool gotConverged = false;
            for (int i = 0; i < 10; i++)
            {
                antival.Assign((antival + 1) % 3);
                if (antival.Converged() > 0)
                {
                    gotConverged = true;
                    break;
                }
            }
            Assert.That(gotConverged, Is.True, "Never saw repeated older slice in 10 steps?!");
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
