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

        /// <summary>
        /// Test that inspects the timeline before switching from negative to forward mode.
        /// This helps verify that the timeline built during negative time is as expected.
        /// </summary>
        [Test]
        public void TimelineState_BeforeAndAfterForwardPass()
        {
            // Set to negative time and cycle through values 0->1->2 several times.
            PositronicVariable<int>.SetEntropy(-1);
            var testVar = new PositronicVariable<int>(0);

            // Run several negative steps.
            for (int i = 0; i < 10; i++)
            {
                var next = (testVar + 1) % 3;
                testVar.Assign(next);
            }

            // Dump the timeline built during negative time.
            var timelineBefore = GetTimelineSnapshot(testVar);
            TestContext.WriteLine("Timeline BEFORE forward pass:");
            foreach (var slice in timelineBefore)
                TestContext.WriteLine($"Slice: [{string.Join(", ", slice.OrderBy(x => x))}]");

            // Now switch to forward time and do one assignment.
            PositronicVariable<int>.SetEntropy(1);
            var forwardVal = (testVar + 1) % 3;
            testVar.Assign(forwardVal);

            // Dump timeline after forward pass.
            var timelineAfter = GetTimelineSnapshot(testVar);
            TestContext.WriteLine("Timeline AFTER forward pass:");
            foreach (var slice in timelineAfter)
                TestContext.WriteLine($"Slice: [{string.Join(", ", slice.OrderBy(x => x))}]");

            // Assert that the forward pass did not erase the negative timeline.
            Assert.That(timelineAfter.Count, Is.GreaterThan(timelineBefore.Count),
                "Expected additional timeline slice after forward pass.");
        }

        /// <summary>
        /// Test that simulates the NegativeLoopThenManualForward scenario, captures console output,
        /// and inspects the timeline to see if the final state prints out the unified "any(...)" string.
        /// </summary>
        [Test]
        public void NegativeToForward_OutputAndTimelineValidation()
        {
            // Capture console output.
            var output = new StringBuilder();
            var writer = new StringWriter(output);
            var originalOut = Console.Out;
            Console.SetOut(writer);

            // Simulate negative time loop.
            PositronicVariable<int>.SetEntropy(-1);
            var testVar = new PositronicVariable<int>(0);

            // Do negative assignments until convergence is detected or a max is reached.
            int convDistance = 0;
            for (int i = 0; i < 20; i++)
            {
                var next = (testVar + 1) % 3;
                testVar.Assign(next);
                convDistance = testVar.Converged();
                if (convDistance > 0)
                    break;
            }
            // Assert we reached convergence.
            Assert.That(convDistance, Is.GreaterThan(0), "Expected convergence in negative loop.");

            // Unify all slices.
            testVar.UnifyAll();

            // Switch to forward time and do one more assignment.
            PositronicVariable<int>.SetEntropy(1);
            var forward = (testVar + 1) % 3;
            testVar.Assign(forward);

            Console.WriteLine($"After forward assignment, testVar: {testVar}");

            // Restore original console output.
            Console.SetOut(originalOut);

            // Inspect the console output.
            string captured = output.ToString();
            TestContext.WriteLine("Captured Output:\n" + captured);

            // Check that the output includes the expected 'any(...)' output.
            Assert.IsTrue(captured.Contains("any("),
                $"Expected output to contain 'any(...)' but got: {captured}");

            // Also dump timeline and assert that the final slice contains 0, 1, and 2.
            var finalStates = testVar.ToValues().Distinct().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, finalStates,
                $"Final timeline state does not contain expected values. Got: {string.Join(", ", finalStates)}");
        }

        /// <summary>
        /// Helper method using reflection to capture a snapshot of the timeline as a list of int lists.
        /// </summary>
        private List<List<int>> GetTimelineSnapshot(PositronicVariable<int> variable)
        {
            var snapshot = new List<List<int>>();
            FieldInfo timelineField = typeof(PositronicVariable<int>)
                .GetField("timeline", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timelineField == null)
                return snapshot;

            var slices = timelineField.GetValue(variable) as List<QuBit<int>>;
            if (slices == null)
                return snapshot;

            foreach (var qb in slices)
            {
                snapshot.Add(qb.ToValues().ToList());
            }
            return snapshot;
        }

        /// <summary>
        /// A targeted test that mimics the steps of NegativeLoopThenManualForward.
        /// It verifies that negative assignments produce the expected timeline,
        /// and that after switching to forward time the expected forward pass output occurs.
        /// </summary>
        [Test]
        public void Debug_NegativeLoopThenManualForward_StepByStep()
        {
            // Capture console output.
            var output = new StringBuilder();
            var writer = new StringWriter(output);
            var originalOut = Console.Out;
            Console.SetOut(writer);

            // Set to negative time and simulate a cycle.
            PositronicVariable<int>.SetEntropy(-1);
            var testVar = new PositronicVariable<int>(0);

            // Do 15 negative steps.
            for (int i = 0; i < 15; i++)
            {
                var next = (testVar + 1) % 3;
                testVar.Assign(next);
                Console.WriteLine($"After negative step {i + 1}: {testVar}");
            }

            // Dump timeline state in negative mode.
            Console.WriteLine("Timeline in negative mode:");
            var snapshotNeg = GetTimelineSnapshot(testVar);
            foreach (var slice in snapshotNeg)
                Console.WriteLine($"Slice: [{string.Join(", ", slice.OrderBy(x => x))}]");

            // Now unify all negative slices.
            testVar.UnifyAll();
            Console.WriteLine("After UnifyAll in negative mode: " + testVar);

            // Switch to forward time.
            PositronicVariable<int>.SetEntropy(1);
            var forward = (testVar + 1) % 3;
            testVar.Assign(forward);
            Console.WriteLine($"After forward assignment, testVar: {testVar}");

            // Restore original console output.
            Console.SetOut(originalOut);

            // Display captured output.
            string finalOutput = output.ToString();
            TestContext.WriteLine("Captured Debug Output:\n" + finalOutput);

            // Verify the final timeline state includes expected unified values.
            var finalStates = testVar.ToValues().Distinct().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, finalStates,
                $"Expected final state to include 0,1,2 but got: {string.Join(", ", finalStates)}");

            // Additionally, check that the debug output contains the expected 'any(' notation.
            Assert.IsTrue(finalOutput.Contains("any("),
                "Expected unified output to contain 'any(', but it did not.");
        }


        /// <summary>
        /// Shows a shorter cycle test that runs exactly 6 steps (0→1→2→0→1→2).
        /// If no convergence is found by step 6, fails. Otherwise unifies, then 
        /// dumps timeline to confirm correct repeated slices are present.
        /// </summary>
        [Test]
        public void ShortCycle_ExpectConvergenceWithinSixSteps()
        {
            PositronicVariable<int>.SetEntropy(-1);

            var cycleVar = new PositronicVariable<int>(0);
            int convergeResult = 0;

            for (int i = 1; i <= 6; i++)
            {
                var next = (cycleVar + 1) % 3;
                cycleVar.Assign(next);
                convergeResult = cycleVar.Converged();

                if (convergeResult > 0)
                {
                    TestContext.WriteLine($"Detected convergence at iteration {i}, distance={convergeResult}.");
                    cycleVar.UnifyAll();
                    break;
                }
            }

            // Expect that in 6 steps of 0→1→2→0→1→2, we do converge
            Assert.That(convergeResult, Is.GreaterThan(0),
                "Expected a repeated state in 6 steps of cycling 0→1→2, but none found.");

            DumpTimeline(cycleVar);

            var finalStates = cycleVar.ToValues().OrderBy(x => x).Distinct().ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, finalStates,
                "After unifying all, expected final states to contain 0,1,2.");
        }

        /// <summary>
        /// 1) Manually cycles 0→1→2→0…,
        /// 2) After convergence, unifies only the last 3 slices (Unify(3)) to see if partial unify is correct,
        /// 3) Checks that final slice still includes {0,1,2}.
        /// 4) Dumps the timeline for debugging.
        /// </summary>
        [Test]
        public void ManualCycle_PartialUnifyAfterConvergence()
        {
            PositronicVariable<int>.SetEntropy(-1);

            var varCycle = new PositronicVariable<int>(0);
            int convDistance = 0;

            for (int i = 1; i <= 15; i++)
            {
                var nextVal = (varCycle + 1) % 3;
                varCycle.Assign(nextVal);

                convDistance = varCycle.Converged();
                if (convDistance > 0)
                {
                    TestContext.WriteLine($"Convergence detected at iteration {i}, distance={convDistance}.");
                    break;
                }
            }

            Assert.That(convDistance, Is.GreaterThan(0),
                "Expected to detect a repeated state in the cycle but did not.");

            // Unify only the last 3 slices, not everything
            varCycle.Unify(convDistance);

            // Dump the timeline for debugging
            DumpTimeline(varCycle);

            // Now see what the final slice holds
            var finalValues = varCycle.ToValues().OrderBy(x => x).Distinct().ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, finalValues,
                $"After partial unify, expected final states to contain [0,1,2], got: [{string.Join(", ", finalValues)}]");
        }

        /// <summary>
        /// 1) Cycles a PositronicVariable<int> through 0→1→2→0… until it converges.
        /// 2) Asserts that Converged() is nonzero at the correct iteration.
        /// 3) Calls UnifyAll() and checks that the final timeline state has {0,1,2}.
        /// 4) Uses reflection to dump timeline slices for debugging.
        /// </summary>
        [Test]
        public void ManualCycle_ConvergesAndUnifiesAll()
        {
            // Force negative time so that slices accumulate
            PositronicVariable<int>.SetEntropy(-1);

            var varCycle = new PositronicVariable<int>(0);
            int convergenceStep = 0;

            // We'll do up to 10 assignments
            for (int i = 1; i <= 10; i++)
            {
                // cycle: varCycle = (varCycle + 1) % 3
                var nextVal = (varCycle + 1) % 3;
                varCycle.Assign(nextVal);

                // Check for convergence
                convergenceStep = varCycle.Converged();
                if (convergenceStep > 0)
                {
                    TestContext.WriteLine($"Convergence detected at step {i}, distance={convergenceStep}.");
                    break;
                }
            }

            Assert.That(convergenceStep, Is.GreaterThan(0), "Expected to find a repeated slice in the cycle!");

            // Unify all slices into a single multi-value slice
            varCycle.UnifyAll();

            // Inspect timeline via reflection to see what's stored
            DumpTimeline(varCycle);

            // The final slice should have {0,1,2} because we cycled them
            var finalValues = varCycle.ToValues().Distinct().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, finalValues,
                $"Expected final states to contain 0,1,2 but got: [{string.Join(", ", finalValues)}]");
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

            // Confirm we got 0,1,2
            CollectionAssert.AreEquivalent(new List<int> { 0, 1, 2 }, finalStates,
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
                TestContext.WriteLine($"Iteration {i}, antival timeline:");
                DumpTimeline(antival);

                // Now see if 'antival' says it converged
                int c = antival.Converged();
                TestContext.WriteLine($"Converged() => {c}");
                TestContext.WriteLine("-----");
            }
        }

        /// <summary>
        /// Simple helper method that uses reflection to print out
        /// all timeline slices for debugging each test.
        /// </summary>
        private void DumpTimeline(PositronicVariable<int> variable)
        {
            try
            {
                var timelineField = typeof(PositronicVariable<int>)
                    .GetField("timeline", BindingFlags.NonPublic | BindingFlags.Instance);

                if (timelineField == null)
                {
                    Console.WriteLine("Reflection error: 'timeline' field not found.");
                    return;
                }

                var slices = timelineField.GetValue(variable) as List<QuBit<int>>;
                if (slices == null)
                {
                    Console.WriteLine("Reflection error: timeline is null or not a List<QuBit<int>>.");
                    return;
                }

                Console.WriteLine("=== Timeline Dump ===");
                for (int i = 0; i < slices.Count; i++)
                {
                    var values = slices[i].ToValues().OrderBy(x => x).Select(x => x.ToString());
                    Console.WriteLine($"Slice {i}: [{string.Join(", ", values)}]");
                }
                Console.WriteLine("=====================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reflection error while dumping timeline: {ex}");
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
            TestContext.WriteLine("After forced partial unify:");
            // DumpTimeline(antival);

            var finalStates = antival.ToValues().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] {  0, 1, 2 }, finalStates);
        }

        [Test]
        public void NegativeLoopThenManualForward()
        {
            var output = new StringBuilder();
            var writer = new StringWriter(output);

            // 1) Temporarily discard console output
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
                    {
                        int dist = v.Converged();
                        if (dist > 0)
                        {
                            v.Unify(dist); // ✅ unify only the looped portion
                        }
                    }

                    TestContext.WriteLine("DEBUG after unify => " + antival);
                    TestContext.WriteLine($"DEBUG eType => {antival.GetCurrentQBit().GetCurrentType()}");
                    break;
                }
            }

            TestContext.WriteLine("DEBUG after negative loop => " + antival);

            // 2) Now do one forward pass, capturing the output
            Console.SetOut(writer);
            PositronicVariable<int>.SetEntropy(1);

            // forward pass
            Console.WriteLine($"The antival is {antival}");
            var forwardVal = (antival + 1) % 3;
            Console.WriteLine($"The value is {forwardVal}");
            antival.Assign(forwardVal);

            // 3) restore console & show captured
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.WriteLine("Output was:\n" + output);

            // 4) Now see if final includes "any(...)"
            string final = output.ToString();
            Assert.That(final.Contains("any("),
                $"Expected to see 'any(...)' in final pass! Output was {final}");
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


        /// <summary>
        /// Timeline Inspection Test:
        /// Performs several negative assignments and then uses reflection
        /// to inspect the timeline slices. This test expects that the initial
        /// slice is overwritten on the first assignment and subsequent assignments
        /// are appended.
        /// </summary>
        [Test]
        public void TimelineInspectionTest()
        {
            // Set negative time.
            PositronicVariable<int>.SetEntropy(-1);
            var testVar = new PositronicVariable<int>(0);

            // Perform 4 negative assignments.
            for (int i = 0; i < 4; i++)
            {
                var next = (testVar + 1) % 3;
                testVar.Assign(next);
            }

            // Capture the timeline snapshot via reflection.
            var timeline = GetTimelineSnapshot(testVar);
            // Expect timeline count = 1 (first slice replaced) + (4 - 1) = 4 slices.
            Assert.AreEqual(4, timeline.Count, "Unexpected number of timeline slices.");

            // Verify the content of each slice:
            // After first assignment: timeline[0] should be [1].
            CollectionAssert.AreEqual(new[] { 1 }, timeline[0].OrderBy(x => x).ToList(), "First timeline slice is incorrect.");
            // Second assignment: timeline[1] should be [2].
            CollectionAssert.AreEqual(new[] { 2 }, timeline[1].OrderBy(x => x).ToList(), "Second timeline slice is incorrect.");
            // Third assignment: timeline[2] should be [0].
            CollectionAssert.AreEqual(new[] { 0 }, timeline[2].OrderBy(x => x).ToList(), "Third timeline slice is incorrect.");
            // Fourth assignment: timeline[3] should be [1].
            CollectionAssert.AreEqual(new[] { 1 }, timeline[3].OrderBy(x => x).ToList(), "Fourth timeline slice is incorrect.");
        }

        /// <summary>
        /// Direct Disjunction Test:
        /// Manually builds a timeline with a series of assignments and then calls
        /// UnifyAll() to merge all slices. Verifies that the unified state contains all
        /// distinct values (0, 1, and 2) and that its string representation indicates a
        /// disjunctive (any(...)) state.
        /// </summary>
        [Test]
        public void DirectDisjunctionTest()
        {
            // Set negative time.
            PositronicVariable<int>.SetEntropy(-1);
            var testVar = new PositronicVariable<int>(0);

            // Build timeline slices:
            // First assignment overwrites initial slice to become [1].
            testVar.Assign(1);
            // Append new slices.
            testVar.Assign(2);
            testVar.Assign(0);

            // Unify all slices.
            testVar.UnifyAll();

            // The unified state should contain all distinct values: {0, 1, 2}.
            var finalStates = testVar.ToValues().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, finalStates, "Unified state does not contain the expected values.");

            // Check that the ToString() representation indicates a disjunctive state.
            StringAssert.Contains("any(", testVar.ToString(), "Final state string does not indicate a disjunctive (any(...)) state.");
        }

        /// <summary>
        /// Step-by-Step Convergence Test:
        /// Runs a loop of negative assignments while logging each step.
        /// Once convergence is detected (via Converged()), the timeline is unified
        /// and the final state is verified to include all values 0, 1, and 2.
        /// </summary>
        [Test]
        public void StepByStepConvergenceTest()
        {
            // Set negative time.
            PositronicVariable<int>.SetEntropy(-1);
            var testVar = new PositronicVariable<int>(0);
            int convergence = 0;

            // Execute up to 10 negative assignments.
            for (int i = 0; i < 10; i++)
            {
                var next = (testVar + 1) % 3;
                testVar.Assign(next);
                convergence = testVar.Converged();
                TestContext.WriteLine($"Step {i + 1}: Value: {testVar}, Convergence: {convergence}");
                if (convergence > 0)
                    break;
            }
            Assert.That(convergence, Is.GreaterThan(0), "Expected convergence did not occur within 10 steps.");

            // Unify the timeline slices after convergence.
            testVar.UnifyAll();
            TestContext.WriteLine("After UnifyAll: " + testVar);

            // Verify that the final unified state includes {0, 1, 2}.
            var finalStates = testVar.ToValues().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, finalStates, "Final unified state does not contain all expected values.");
        }

        [Test]
        public void RunConvergenceLoop_ShouldAccumulateFullCycleBeforeForwardPass()
        {
            // Arrange: Reset state and force negative time.
            PositronicVariable<int>.ResetStaticVariables();
            PositronicVariable<int>.SetEntropy(-1);
            var antival = new PositronicVariable<int>(-1);

            // Act: Run a fixed number of negative iterations (e.g. 6) 
            // so that we manually accumulate a full cycle.
            for (int i = 0; i < 6; i++)
            {
                var next = (antival + 1) % 3;
                antival.Assign(next);
            }

            // Manually unify without relying on the early convergence detection.
            antival.UnifyAll();
            var union = antival.ToValues().OrderBy(x => x).ToList();

            // Assert: The unified negative state should be {1, 0, 2}.
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, union,
                "Expected the union of negative states to be {1, 0, 2}.");
        }

        [Test]
        public void RunConvergenceLoop_ForwardPassUsesFullUnion()
        {
            // Arrange: Reset state and run a manual negative loop long enough to get the full cycle.
            PositronicVariable<int>.ResetStaticVariables();
            PositronicVariable<int>.SetEntropy(-1);
            var antival = new PositronicVariable<int>(-1);

            // Run enough iterations so that all three states appear.
            for (int i = 0; i < 6; i++)
            {
                var next = (antival + 1) % 3;
                antival.Assign(next);
            }
            antival.UnifyAll();

            // Confirm the negative-phase union is complete.
            var negativeUnion = antival.ToValues().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, negativeUnion,
                "Expected the negative-phase union to be {0, 1, 2}.");

            // Act: Switch to forward time and do one assignment.
            PositronicVariable<int>.SetEntropy(1);
            var forward = (antival + 1) % 3; // operator+ should apply elementwise.
            antival.Assign(forward);

            var forwardUnion = antival.ToValues().OrderBy(x => x).ToList();
            // Since {0,1,2} + 1 (elementwise) gives {1,2,0} (modulo 3), which is equivalent to {0,1,2}.
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, forwardUnion,
                "Expected the forward-phase union (after addition) to be {0, 1, 2}.");
        }

        [Test]
        public void WhenConverged_AssignMergesOnlyLastSlice()
        {
            // Arrange: Run a negative loop to accumulate a union.
            PositronicVariable<int>.ResetStaticVariables();
            PositronicVariable<int>.SetEntropy(-1);
            var pv = new PositronicVariable<int>(-1);

            // Simulate a negative loop that cycles through 0,1,2:
            pv.Assign((pv + 1) % 3); // overwrites seed: now state is 0
            pv.Assign((pv + 1) % 3); // now state is 1 (and timeline now has [0] replaced by 1)
            pv.Assign((pv + 1) % 3); // now state is 2
                                     // Manually unify so that the union is built:
            pv.UnifyAll();

            // At this point, we expect the union to be {0,1,2} (if all had been accumulated)
            var unionBefore = pv.ToValues().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, unionBefore,
                "Expected union from negative loop to be {0,1,2}.");

            // Now, simulate that convergence was detected:
            // (s_converged would be set by the convergence loop; here we mimic that)
            // Subsequent assignments now use the merge branch.
            pv.Assign(0); // new assignment is 0; union of {0,1,2} ∪ {0} is still {0,1,2}
            var unionAfterSame = pv.ToValues().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, unionAfterSame,
                "Union should remain unchanged when assigning an already-present value.");

            // Now assign a new value not in the union:
            pv.Assign(3);
            // Because the merge branch unions with only the last slice, if the last slice had been "collapsed"
            // to just {0} then the result might be {0,3} rather than {0,1,2,3}.
            var unionAfterNew = pv.ToValues().OrderBy(x => x).ToList();
            // Depending on design, you might expect the new value to join the full union, or not.
            // For example, if the intention is that once convergence happens the union is fixed,
            // then unionAfterNew might be expected to remain {0,1,2}. If the expectation is to update,
            // then you’d expect {0,1,2,3}.
            // Here we check that it is not preserving the full union:
            CollectionAssert.AreNotEquivalent(new[] { 0, 1, 2, 3 }, unionAfterNew,
                "The merge branch appears to be preserving the full union instead of merging only with the last slice.");
        }

        [Test]
        public void ForwardPassUsesOnlyLastUnifiedSlice()
        {
            // Arrange: Run a negative loop until convergence.
            PositronicVariable<int>.ResetStaticVariables();
            PositronicVariable<int>.SetEntropy(-1);
            var antival = new PositronicVariable<int>(-1);

            // Run iterations so that the negative loop cycles.
            // (Our other tests show that manually running 6 iterations yields a full cycle,
            // but here we simulate the convergence loop behavior.)
            for (int i = 0; i < 8; i++)
            {
                var next = (antival + 1) % 3;
                antival.Assign(next);
            }
            // At some point convergence is detected and UnifyAll is called,
            // which collapses the timeline to a single slice.
            antival.UnifyAll();
            antival.CollapseToLastSlice();
            // Check negative-phase union (for example, it might be {0,1,2} if we had run long enough)
            var negUnion = antival.ToValues().OrderBy(x => x).ToList();
            TestContext.WriteLine("Negative union: " + string.Join(", ", negUnion));

            // Act: Now switch to forward time.
            PositronicVariable<int>.SetEntropy(1);
            var forward = (antival + 1) % 3;
            // The operator + creates a new variable based solely on the current (unified) QBit.
            // If that QBit only has one value (say, 0), then forward becomes any(1).
            // And when we do antival.Assign(forward), we merge forward into antival.
            antival.Assign(forward);

            // Assert: Now antival.ToString() should show a state that reflects only the last slice,
            // not the full union of the negative loop.
            // For example, if the negative unified state was any(0) then the forward pass yields any(1).
            string final = antival.ToString();
            TestContext.WriteLine("Final state: " + final);

            // In our failing test, the expected output was a disjunction of three values,
            // but here we see that the final state is just any(0) followed by a forward update to any(1).
            // This confirms that the final forward pass is operating on only the last unified slice.
            Assert.IsTrue(final.Contains("any(1)"), "Final state does not reflect forward addition on the unified slice.");
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

                TestContext.WriteLine($"DEBUG: antival={antival}");
            });

            // Restore console output
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            // For debugging, let's show what we captured:
            TestContext.WriteLine("Captured final output:\n" + output);

            // The test expects a VERY specific string, something like:
            // "The antival is any(1, 0, 2)\nany(The value is 1\n, The value is 2\n, The value is 0\n)"
            // Exactly matching your original test approach:
            var expectedOutput = "The antival is any(1, 0, 2)\nany(The value is 1\n, The value is 2\n, The value is 0\n)";

            // Because your negative time loop prints each possible state, you'd get that "any(...)" effect in your output.
            // Let's do the line normalization:
            var actual = output.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
            var expected = expectedOutput.Replace("\r\n", "\n").Replace("\r", "\n");

            Assert.That(actual, Is.EqualTo(expected),
                $"Unexpected final output.  Actual:\n{actual}\nExpected:\n{expected}\n");
        }

        [Test]
        public void ConvergenceLoop_IterationCountTest()
        {
            int iterationCount = 0;
            PositronicVariable<int>.ResetStaticVariables();
            PositronicVariable<int>.SetEntropy(-1);
            var antival = new PositronicVariable<int>(-1);

            PositronicVariable<int>.RunConvergenceLoop(() =>
            {
                iterationCount++;
                // For debugging, log the current timeline length.
                TestContext.WriteLine($"Iteration {iterationCount}: antival = {antival}, timeline count = {GetTimelineSnapshot(antival).Count}");
                var val = (antival + 1) % 3;
                antival.Assign(val);
            });

            TestContext.WriteLine("Total negative iterations: " + iterationCount);
            // Verify that the negative loop iterated more than one time
            Assert.That(iterationCount, Is.GreaterThan(1), "Convergence loop did not iterate as expected.");

            // Check whether, after the loop, the variable has detected convergence.
            Assert.IsTrue(antival.Converged() > 0, "The variable did not converge as expected.");
        }

        [Test]
        public void ArithmeticOperators_ShouldApplyElementwiseToUnifiedState()
        {
            // Arrange: Force negative time to allow timeline accumulation.
            PositronicVariable<int>.SetEntropy(-1);

            // Start with an initial value of 1.
            var varUnified = new PositronicVariable<int>(1);

            // NOTE: The first assignment below will overwrite the initial value of 1
            // because the internal logic treats the initial slice as a placeholder seed
            // and replaces it on the first assignment in negative time.
            // So, the timeline will contain [2], [0] — not [1, 2, 0].

            varUnified.Assign(2);
            varUnified.Assign(0);

            // Unify all slices so that varUnified represents any(2, 0).
            varUnified.UnifyAll();

            // Act: Apply the addition operator.
            // If applied elementwise, (any(2,0)) + 1 should yield any(3, 1).
            var result = varUnified + 1;

            // Assert: Verify that the result contains 2+1 and 0+1, but NOT 1+1
            // because the original 1 was overwritten.
            var expected = new List<int> { 3, 1 };
            CollectionAssert.AreEquivalent(expected, result.Value.ToValues(),
                $"Expected elementwise addition to produce any({string.Join(", ", expected)}) " +
                $"but got any({string.Join(", ", result.Value.ToValues())}).");
        }



    }
}
