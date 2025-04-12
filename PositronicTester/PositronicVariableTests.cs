//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using QuantumSuperposition.QuantumSoup;
//using QuantumSuperposition.Core;
//using NUnit.Framework;
//using System.Collections;

//namespace PositronicVariables.Tests
//{
//    [TestFixture]
//    public class PositronicVariableTests
//    {
//        // Before each test, reset the runtime (and thus the global state).
//        [SetUp]
//        public void SetUp()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            QuantumConfig.ForbidDefaultOnCollapse = true;
//        }

//        /// <summary>
//        /// Forward pass adds new timeline slice without erasing negative timeline.
//        /// </summary>
//        [Test]
//        public void TimelineState_BeforeAndAfterForwardPass()
//        {
//            // Set to negative time and cycle through values 0->1->2 several times.
//            PositronicVariable<int>.SetEntropy(-1);
//            // Using the interface type for external operations.
//            var testVar = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            var pv = testVar as PositronicVariable<int>;

//            // Run several negative steps.
//            for (int i = 0; i < 10; i++)
//            {
//                var next = (pv + 1) % 3;
//                pv.Assign(next);
//            }

//            var timelineBefore = GetTimelineSnapshot(pv);
//            TestContext.WriteLine("Timeline BEFORE forward pass:");
//            foreach (var slice in timelineBefore)
//                TestContext.WriteLine($"Slice: [{string.Join(", ", slice.OrderBy(x => x))}]");

//            // Switch to forward time.
//            PositronicVariable<int>.SetEntropy(1);
//            var forwardVal = (pv + 1) % 3;
//            pv.Assign(forwardVal);

//            var timelineAfter = GetTimelineSnapshot(pv);
//            TestContext.WriteLine("Timeline AFTER forward pass:");
//            foreach (var slice in timelineAfter)
//                TestContext.WriteLine($"Slice: [{string.Join(", ", slice.OrderBy(x => x))}]");

//            // Assert that the forward pass added a timeline slice.
//            Assert.That(timelineAfter.Count, Is.EqualTo(timelineBefore.Count),
//                "No new slice expected post-convergence.");
//        }

//        /// <summary>
//        /// Negative loop convergence and correct unified "any(...)" output in timeline.
//        /// </summary>
//        [Test]
//        public void NegativeToForward_OutputAndTimelineValidation()
//        {
//            var output = new StringBuilder();
//            var writer = new StringWriter(output);
//            var originalOut = Console.Out;
//            Console.SetOut(writer);

//            PositronicVariable<int>.SetEntropy(-1);
//            var testVar = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            var pv = testVar as PositronicVariable<int>;

//            int convDistance = 0;
//            for (int i = 0; i < 20; i++)
//            {
//                var next = (pv + 1) % 3;
//                pv.Assign(next);
//                convDistance = pv.Converged();
//                if (convDistance > 0)
//                    break;
//            }
//            Assert.That(convDistance, Is.GreaterThan(0), "Expected convergence in negative loop.");

//            pv.UnifyAll();

//            PositronicVariable<int>.SetEntropy(1);
//            var forward = (pv + 1) % 3;
//            pv.Assign(forward);

//            Console.WriteLine($"After forward assignment, testVar: {pv}");
//            Console.SetOut(originalOut);

//            string captured = output.ToString();
//            TestContext.WriteLine("Captured Output:\n" + captured);
//            Assert.That(captured, Does.Contain("any("),
//                $"Expected output to contain 'any(...)' but got: {captured}");

//            var finalStates = pv.ToValues().Distinct().OrderBy(x => x).ToList();
//            Assert.That(finalStates, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                $"Final timeline state does not contain expected values. Got: {string.Join(", ", finalStates)}");
//        }

//        /// <summary>
//        /// Helper method using reflection to capture a snapshot of the timeline.
//        /// </summary>
//        private List<List<int>> GetTimelineSnapshot(PositronicVariable<int> variable)
//        {
//            var snapshot = new List<List<int>>();
//            FieldInfo timelineField = typeof(PositronicVariable<int>)
//                .GetField("timeline", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//            var slices = timelineField.GetValue(variable) as List<QuBit<int>>;
//            if (slices == null)
//                return snapshot;
//            foreach (var qb in slices)
//            {
//                snapshot.Add(qb.States.ToList());
//            }
//            return snapshot;
//        }

//        /// <summary>
//        /// Negative assignments and timeline unify correctly to "any(...)" state.
//        /// </summary>
//        [Test]
//        public void Debug_NegativeLoopThenManualForward_StepByStep()
//        {
//            var output = new StringBuilder();
//            var writer = new StringWriter(output);
//            var originalOut = Console.Out;
//            Console.SetOut(writer);

//            PositronicVariable<int>.SetEntropy(-1);
//            var testVar = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            var pv = testVar as PositronicVariable<int>;

//            for (int i = 0; i < 15; i++)
//            {
//                var next = (pv + 1) % 3;
//                pv.Assign(next);
//                Console.WriteLine($"After negative step {i + 1}: {pv}");
//            }

//            Console.WriteLine("Timeline in negative mode:");
//            var snapshotNeg = GetTimelineSnapshot(pv);
//            foreach (var slice in snapshotNeg)
//                Console.WriteLine($"Slice: [{string.Join(", ", slice.OrderBy(x => x))}]");

//            pv.UnifyAll();
//            Console.WriteLine("After UnifyAll in negative mode: " + pv);

//            PositronicVariable<int>.SetEntropy(1);
//            var forward = (pv + 1) % 3;
//            pv.Assign(forward);
//            Console.WriteLine($"After forward assignment, testVar: {pv}");

//            Console.SetOut(originalOut);
//            string finalOutput = output.ToString();
//            TestContext.WriteLine("Captured Debug Output:\n" + finalOutput);

//            var finalStates = pv.ToValues().Distinct().OrderBy(x => x).ToList();
//            Assert.That(finalStates, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                $"Expected final state to include 0,1,2 but got: {string.Join(", ", finalStates)}");

//            Assert.That(finalOutput, Does.Contain("any("),
//                "Expected unified output to contain 'any(', but it did not.");
//        }

//        /// <summary>
//        /// Convergence within 6 steps and timeline contains {0,1,2} after unify.
//        /// </summary>
//        [Test]
//        public void ShortCycle_ExpectConvergenceWithinSixSteps()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var cycleVar = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            var pv = cycleVar as PositronicVariable<int>;
//            int convergeResult = 0;

//            for (int i = 1; i <= 6; i++)
//            {
//                var next = (pv + 1) % 3;
//                pv.Assign(next);
//                convergeResult = pv.Converged();
//                if (convergeResult > 0)
//                {
//                    TestContext.WriteLine($"Detected convergence at iteration {i}, distance={convergeResult}.");
//                    pv.UnifyAll();
//                    break;
//                }
//            }
//            Assert.That(convergeResult, Is.GreaterThan(0),
//                "Expected a repeated state in 6 steps of cycling 0→1→2, but none found.");
//            DumpTimeline(pv);
//            var finalStates = pv.ToValues().OrderBy(x => x).Distinct().ToList();
//            Assert.That(finalStates, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                "After unifying all, expected final states to contain 0,1,2.");
//        }

//        /// <summary>
//        /// Partial unify correctly retains {0,1,2} in final slice.
//        /// </summary>
//        [Test]
//        public void ManualCycle_PartialUnifyAfterConvergence()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var varCycle = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            var pv = varCycle as PositronicVariable<int>;
//            int convDistance = 0;

//            for (int i = 1; i <= 15; i++)
//            {
//                var nextVal = (pv + 1) % 3;
//                pv.Assign(nextVal);
//                convDistance = pv.Converged();
//                if (convDistance > 0)
//                {
//                    TestContext.WriteLine($"Convergence detected at iteration {i}, distance={convDistance}.");
//                    break;
//                }
//            }
//            Assert.That(convDistance, Is.GreaterThan(0),
//                "Expected to detect a repeated state in the cycle but did not.");
//            pv.Unify(convDistance);
//            DumpTimeline(pv);
//            var finalValues = pv.ToValues().OrderBy(x => x).Distinct().ToList();
//            Assert.That(finalValues, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                $"After partial unify, expected final states to contain [0,1,2], got: [{string.Join(", ", finalValues)}]");
//        }

//        /// <summary>
//        /// Convergence detection and unify-all results in timeline {0,1,2}.
//        /// </summary>
//        [Test]
//        public void ManualCycle_ConvergesAndUnifiesAll()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var varCycle = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            var pv = varCycle as PositronicVariable<int>;
//            int convergenceStep = 0;

//            for (int i = 1; i <= 10; i++)
//            {
//                var nextVal = (pv + 1) % 3;
//                pv.Assign(nextVal);
//                convergenceStep = pv.Converged();
//                if (convergenceStep > 0)
//                {
//                    TestContext.WriteLine($"Convergence detected at step {i}, distance={convergenceStep}.");
//                    break;
//                }
//            }
//            Assert.That(convergenceStep, Is.GreaterThan(0), "Expected to find a repeated slice in the cycle!");
//            pv.UnifyAll();
//            DumpTimeline(pv);
//            var finalValues = pv.ToValues().Distinct().OrderBy(x => x).ToList();
//            Assert.That(finalValues, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                $"Expected final states to contain 0,1,2 but got: [{string.Join(", ", finalValues)}]");
//        }

//        /// <summary>
//        /// Addition operator correctly calculates new PositronicVariable.
//        /// </summary>
//        [Test]
//        public void AdditionOperator_AddsValueToPositronicVariable()
//        {
//            var variable = PositronicVariable<int>.GetOrCreate("testVar", 5);
//            var pv = variable as PositronicVariable<int>;
//            var result = pv + 3;
//            var expectedValues = new List<int> { 8 };
//            Assert.That(result.Value.ToValues(), Is.EquivalentTo(expectedValues));
//        }

//        /// <summary>
//        /// Modulo operator correctly calculates remainder.
//        /// </summary>
//        [Test]
//        public void ModuloOperator_CalculatesModuloOfPositronicVariable()
//        {
//            var variable = PositronicVariable<int>.GetOrCreate("testVar", 10);
//            var pv = variable as PositronicVariable<int>;
//            var result = pv % 3;
//            var expectedValues = new List<int> { 1 };
//            Assert.That(result.Value.ToValues(), Is.EquivalentTo(expectedValues));
//        }

//        /// <summary>
//        /// Correct assignment of value from another PositronicVariable.
//        /// </summary>
//        [Test]
//        public void AssignMethod_AssignsValueFromAnotherPositronicVariable()
//        {
//            PositronicVariable<int>.ResetStaticVariables();

//            PositronicVariable<int>.SetEntropy(1);
//            var variable1 = PositronicVariable<int>.GetOrCreate("testVar4", 5);
//            var variable2 = PositronicVariable<int>.GetOrCreate("testVar5", 10);
//            variable1.Assign(variable2);
//            var expectedValues = new List<int> { 10 };
//            Assert.That(variable1.Value.ToValues(), Is.EquivalentTo(expectedValues));
//            PositronicVariable<int>.SetEntropy(-1); // Reset entropy
//        }

//        /// <summary>
//        /// Converged returns zero when timeline is too short.
//        /// </summary>
//        [Test]
//        public void ConvergedMethod_ReturnsZeroWhenTimelineCountIsLessThanThree()
//        {
//            var variable = PositronicVariable<int>.GetOrCreate("testVar", 5);
//            var result = variable.Converged();
//            Assert.That(result, Is.EqualTo(0));
//        }

//        /// <summary>
//        /// No convergence detected when states differ.
//        /// </summary>
//        [Test]
//        public void ConvergedMethod_ReturnsZeroWhenNoConvergenceIsFound()
//        {
//            PositronicVariable<int>.ResetStaticVariables();

//            var variable = PositronicVariable<int>.GetOrCreate("testVar8", 5);
//            variable.Assign(PositronicVariable<int>.GetOrCreate("testVar9", 10));
//            variable.Assign(PositronicVariable<int>.GetOrCreate("testVar10", 15));
//            var result = variable.Converged();
//            Assert.That(result, Is.EqualTo(0));
//        }

//        /// <summary>
//        /// Correctly detects and reports convergence step.
//        /// </summary>
//        [Test]
//        public void ConvergedMethod_ReturnsConvergenceStepWhenConvergenceIsFound()
//        {
//            var variable = PositronicVariable<int>.GetOrCreate("testVar7", 5);
//            int convergenceResult = 0;
//            PositronicVariable<int>.RunConvergenceLoop(() =>
//            {
//                variable.Assign(PositronicVariable<int>.GetOrCreate("testVar", 10));
//                if (PositronicVariable<int>.GetEntropy() < 0)
//                    convergenceResult = variable.Converged();
//            });
//            Assert.That(convergenceResult, Is.GreaterThan(0));
//        }

//        /// <summary>
//        /// Antival cycles converge correctly to all three values {0,1,2}.
//        /// </summary>
//        [Test]
//        public void Antival_CyclesThrough0_1_2_AndEventuallyConvergesToAllThree()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar2", -1);
//            int maxSteps = 20;
//            bool converged = false;

//            for (int i = 0; i < maxSteps; i++)
//            {
//                var nextValue = (antival + 1) % 3;
//                antival.Assign(nextValue);
//                if (antival.Converged() > 0)
//                {
//                    converged = true;
//                    break;
//                }
//            }
//            if (converged)
//                antival.UnifyAll();

//            var finalStates = antival.ToValues().OrderBy(x => x).Distinct().ToList();
//            Assert.That(finalStates, Is.EquivalentTo(new List<int> { 0, 1, 2 }),
//                $"Expected to see all [0,1,2] in final slice, but got [{string.Join(", ", finalStates)}].");
//        }

//        /// <summary>
//        /// Negative timeline builds correctly and inspects states.
//        /// </summary>
//        [Test]
//        public void NegativeTimeTimelineInspection()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);

//            for (int i = 0; i < 10; i++)
//            {
//                var nextValue = (antival + 1) % 3;
//                antival.Assign(nextValue);
//                TestContext.WriteLine($"Iteration {i}, antival timeline:");
//                DumpTimeline(antival);
//                int c = antival.Converged();
//                TestContext.WriteLine($"Converged() => {c}");
//                TestContext.WriteLine("-----");
//            }
//        }

//        /// <summary>
//        /// Helper method that uses reflection to dump the timeline.
//        /// </summary>
//        private void DumpTimeline(PositronicVariable<int> variable)
//        {
//            try
//            {
//                var timelineField = typeof(PositronicVariable<int>)
//                    .GetField("timeline", BindingFlags.NonPublic | BindingFlags.Instance);
//                if (timelineField == null)
//                {
//                    Console.WriteLine("Reflection error: 'timeline' field not found.");
//                    return;
//                }
//                var slices = timelineField.GetValue(variable) as List<QuBit<int>>;
//                if (slices == null)
//                {
//                    Console.WriteLine("Reflection error: timeline is null or not a List<QuBit<int>>.");
//                    return;
//                }
//                Console.WriteLine("=== Timeline Dump ===");
//                for (int i = 0; i < slices.Count; i++)
//                {
//                    var values = slices[i].States.OrderBy(x => x)
//                        .Select(x => x.ToString());
//                    Console.WriteLine($"Slice {i}: [{string.Join(", ", values)}]");
//                }
//                Console.WriteLine("=====================");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Reflection error while dumping timeline: {ex}");
//            }
//        }

//        /// <summary>
//        /// Partial unify correctly retains {0,1,2}.
//        /// </summary>
//        [Test]
//        public void ForcedPartialUnifyAfterConvergence()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);

//            int cVal = 0;
//            for (int i = 0; i < 10; i++)
//            {
//                var nextValue = (antival + 1) % 3;
//                antival.Assign(nextValue);
//                cVal = antival.Converged();
//                if (cVal > 0)
//                    break;
//            }

//            Assert.That(cVal, Is.GreaterThan(0), "Expected to eventually find a repeated slice!");
//            // Now unify exactly 'cVal' slices.
//            antival.Unify(cVal);

//            TestContext.WriteLine("After forced partial unify:");
//            DumpTimeline(antival);

//            var finalStates = antival.ToValues().OrderBy(x => x).ToList();
//            Assert.That(finalStates, Is.EquivalentTo(new[] { 0, 1, 2 }));
//        }

//        /// <summary>
//        /// Negative-to-forward transition correctly prints "any(...)".
//        /// </summary>
//        [Test]
//        public void NegativeLoopThenManualForward()
//        {
//            var output = new StringBuilder();
//            var writer = new StringWriter(output);

//            // 1) Temporarily discard console output.
//            Console.SetOut(TextWriter.Null);

//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);

//            // Negative runs.
//            for (int i = 0; i < 20; i++)
//            {
//                var val = (antival + 1) % 3;
//                antival.Assign(val);

//                bool allConverged = PositronicVariable<int>.AllConverged();
//                if (allConverged)
//                {
//                    // Unify only the looped portion.
//                    foreach (var v in PositronicVariable<int>.GetAllVariables())
//                    {
//                        int dist = v.Converged();
//                        if (dist > 0)
//                            ((PositronicVariable<int>)v).Unify(dist);
//                    }
//                    TestContext.WriteLine("DEBUG after unify => " + antival);
//                    TestContext.WriteLine($"DEBUG eType => {antival.GetCurrentQBit().GetCurrentType()}");
//                    break;
//                }
//            }

//            TestContext.WriteLine("DEBUG after negative loop => " + antival);

//            // 2) Now do one forward pass, capturing the output.
//            Console.SetOut(writer);
//            PositronicVariable<int>.SetEntropy(1);

//            Console.WriteLine($"The antival is {antival}");
//            var forwardVal = (antival + 1) % 3;
//            Console.WriteLine($"The value is {forwardVal}");
//            antival.Assign(forwardVal);

//            // 3) Restore console & show captured.
//            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
//            Console.WriteLine("Output was:\n" + output);

//            string final = output.ToString();
//            Assert.That(final, Does.Contain("any("),
//                $"Expected to see 'any(...)' in final pass! Output was {final}");
//        }

//        /// <summary>
//        /// No false convergence when states always unique.
//        /// </summary>
//        [Test]
//        public void ZeroConvergence_NoRepeatedStates_AlwaysUnique()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var var1 = PositronicVariable<int>.GetOrCreate("testVar", 10);
//            for (int i = 0; i < 10; i++)
//            {
//                var nextVal = (var1 + 2) % 9999;
//                var1.Assign(nextVal);
//            }
//            Assert.That(var1.Converged(), Is.EqualTo(0));
//        }

//        /// <summary>
//        /// Convergence correctly detected after cycling.
//        /// </summary>
//        [Test]
//        public void EnoughStepsFor3Cycle_ExpectConverged()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            bool gotConverged = false;
//            for (int i = 0; i < 10; i++)
//            {
//                antival.Assign((antival + 1) % 3);
//                if (antival.Converged() > 0)
//                {
//                    gotConverged = true;
//                    break;
//                }
//            }
//            Assert.That(gotConverged, Is.True, "Never saw repeated older slice in 10 steps?!");
//        }

//        /// <summary>
//        /// Timeline slices correctly represent successive states.
//        /// </summary>
//        [Test]
//        public void TimelineInspectionTest()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var testVar = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            for (int i = 0; i < 4; i++)
//            {
//                var next = (testVar + 1) % 3;
//                testVar.Assign(next);
//            }
//            var timeline = GetTimelineSnapshot(testVar);
//            Assert.That(timeline.Count, Is.EqualTo(2));
//            Assert.That(timeline[0].OrderBy(x => x).ToList(), Is.EqualTo(new[] { 1 }));
//            Assert.That(timeline[1].OrderBy(x => x).ToList(), Is.EquivalentTo(new[] { 0, 1, 2 }));
//        }

//        /// <summary>
//        /// Direct unify results correctly in "any(...)" state.
//        /// </summary>
//        [Test]
//        public void DirectDisjunctionTest()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var testVar = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            testVar.Assign(1);
//            testVar.Assign(2);
//            testVar.Assign(0);
//            testVar.UnifyAll();
//            var finalStates = testVar.ToValues().OrderBy(x => x).ToList();
//            Assert.That(finalStates, Is.EquivalentTo(new[] { 0, 1, 2 }), "Unified state does not contain the expected values.");
//            Assert.That(testVar.ToString(), Does.Contain("any("), "Final state string does not indicate a disjunctive (any(...)) state.");
//        }

//        /// <summary>
//        /// Step-by-step convergence and unify maintains correct states {0,1,2}.
//        /// </summary>
//        [Test]
//        public void StepByStepConvergenceTest()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var testVar = PositronicVariable<int>.GetOrCreate("testVar", 0);
//            int convergence = 0;
//            for (int i = 0; i < 10; i++)
//            {
//                var next = (testVar + 1) % 3;
//                testVar.Assign(next);
//                convergence = testVar.Converged();
//                TestContext.WriteLine($"Step {i + 1}: Value: {testVar}, Convergence: {convergence}");
//                if (convergence > 0)
//                    break;
//            }
//            Assert.That(convergence, Is.GreaterThan(0), "Expected convergence did not occur within 10 steps.");
//            testVar.UnifyAll();
//            TestContext.WriteLine("After UnifyAll: " + testVar);
//            var finalStates = testVar.ToValues().OrderBy(x => x).ToList();
//            Assert.That(finalStates, Is.EquivalentTo(new[] { 0, 1, 2 }), "Final unified state does not contain all expected values.");
//        }

//        /// <summary>
//        /// Full negative cycle accumulated before forward pass.
//        /// </summary>
//        [Test]
//        public void RunConvergenceLoop_ShouldAccumulateFullCycleBeforeForwardPass()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            for (int i = 0; i < 6; i++)
//            {
//                var next = (antival + 1) % 3;
//                antival.Assign(next);
//            }
//            antival.UnifyAll();
//            var union = antival.ToValues().OrderBy(x => x).ToList();
//            Assert.That(union, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                "Expected the union of negative states to be {0, 1, 2}.");
//        }

//        /// <summary>
//        /// Forward pass uses full negative-phase union correctly.
//        /// </summary>
//        [Test]
//        public void RunConvergenceLoop_ForwardPassUsesFullUnion()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            for (int i = 0; i < 6; i++)
//            {
//                var next = (antival + 1) % 3;
//                antival.Assign(next);
//            }
//            antival.UnifyAll();
//            var negativeUnion = antival.ToValues().OrderBy(x => x).ToList();
//            Assert.That(negativeUnion, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                "Expected the negative-phase union to be {0, 1, 2}.");
//            PositronicVariable<int>.SetEntropy(1);
//            var forward = (antival + 1) % 3;
//            antival.Assign(forward);
//            var forwardUnion = antival.ToValues().OrderBy(x => x).ToList();
//            Assert.That(forwardUnion, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                "Expected the forward-phase union (after addition) to be {0, 1, 2}.");
//        }

//        /// <summary>
//        /// Merge branch assignment updates union correctly.
//        /// </summary>
//        [Test]
//        public void WhenConverged_AssignMergesOnlyLastSlice_UnionMode()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var pv = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            pv.Assign((pv + 1) % 3);
//            pv.Assign((pv + 1) % 3);
//            pv.Assign((pv + 1) % 3);
//            pv.UnifyAll();
//            var unionBefore = pv.ToValues().OrderBy(x => x).ToList();
//            Assert.That(unionBefore, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                "Expected union from negative loop to be {0, 1, 2}.");
//            pv.Assign(0);
//            var unionAfterSame = pv.ToValues().OrderBy(x => x).ToList();
//            Assert.That(unionAfterSame, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                "Union should remain unchanged when assigning an already-present value.");
//            pv.Assign(3);
//            var unionAfterNew = pv.ToValues().OrderBy(x => x).ToList();
//            Assert.That(unionAfterNew, Is.EquivalentTo(new[] { 0, 1, 2, 3 }),
//                "The merge branch should update the union when a new value is assigned.");
//        }

//        /// <summary>
//        /// Forward arithmetic applies elementwise over unified negative union.
//        /// </summary>
//        [Test]
//        public void ForwardPassUsesFullNegativeUnion_UnionMode()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            for (int i = 0; i < 8; i++)
//            {
//                var next = (antival + 1) % 3;
//                antival.Assign(next);
//            }
//            antival.UnifyAll();
//            var negUnion = antival.ToValues().OrderBy(x => x).ToList();
//            TestContext.WriteLine("Negative union: " + string.Join(", ", negUnion));
//            Assert.That(negUnion, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                "Expected negative union to be {0, 1, 2}.");
//            PositronicVariable<int>.SetEntropy(1);
//            var forward = (antival + 1) % 3;
//            antival.Assign(forward);
//            string final = antival.ToString();
//            TestContext.WriteLine("Final state: " + final);
//            Assert.That(final, Does.Contain("any(").And.Contain("0").And.Contain("1").And.Contain("2"),
//                "Final state does not reflect elementwise forward arithmetic over the full union.");
//        }

//        /// <summary>
//        /// Variable correctly converges to expected single state.
//        /// </summary>
//        [Test]
//        public void RunConvergenceLoop_VariableConvergesToExpectedState()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            var pv = PositronicVariable<int>.GetOrCreate("testVar", 1);
//            void Code()
//            {
//                pv.Assign(1);
//            }
//            PositronicVariable<int>.RunConvergenceLoop(Code);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1));
//            Assert.That(pv.Converged(), Is.GreaterThan(0));
//        }

//        /// <summary>
//        /// Correct union and output during convergence loop (currently failing).
//        /// </summary>
//        [Test]
//        public void RunConvergenceLoop_VariableConvergesToExpectedState_UnionMode()
//        {
//            var output = new StringBuilder();
//            var writer = new StringWriter(output);
//            Console.SetOut(writer);
//            PositronicVariable<int>.ResetStaticVariables();
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            PositronicVariable<int>.RunConvergenceLoop(() =>
//            {
//                Console.WriteLine($"The antival is {antival}");
//                var val = (antival + 1) % 3;
//                Console.WriteLine($"The value is {val}");
//                antival.Assign(val);
//            });
//            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
//            TestContext.WriteLine("Captured final output:\n" + output);
//            TestContext.WriteLine("Final state: " + antival);
//            var expectedOutput = "The antival is any(0, 1, 2)\nThe value is any(1, 2, 0)\n";
//            var actual = output.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
//            var expected = expectedOutput.Replace("\r\n", "\n").Replace("\r", "\n");
//            Assert.That(actual, Is.EqualTo(expected),
//                $"Unexpected final output.  Actual:\n{actual}\nExpected:\n{expected}\n");
//        }

//        /// <summary>
//        /// Unified QuBit should display full union in ToString().
//        /// </summary>
//        [Test]
//        public void UnifiedQuBit_ToString_ShouldDisplayFullUnion()
//        {
//            var qb = new QuBit<int>(new[] { 1, 0, 2 });
//            qb.Any();
//            string output = qb.ToString();
//            Assert.That(output, Does.Contain("any("), "Expected disjunctive notation.");
//            Assert.That(output, Does.Contain("1"), "Expected union to include 1.");
//            Assert.That(output, Does.Contain("0"), "Expected union to include 0.");
//            Assert.That(output, Does.Contain("2"), "Expected union to include 2.");
//        }

//        /// <summary>
//        /// Convergence loop iterates and detects convergence correctly.
//        /// </summary>
//        [Test]
//        public void ConvergenceLoop_IterationCountTest()
//        {
//            int iterationCount = 0;
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            PositronicVariable<int>.RunConvergenceLoop(() =>
//            {
//                iterationCount++;
//                TestContext.WriteLine($"Iteration {iterationCount}: antival = {antival}, timeline count = {GetTimelineSnapshot(antival).Count}");
//                var val = (antival + 1) % 3;
//                antival.Assign(val);
//            });
//            TestContext.WriteLine("Total negative iterations: " + iterationCount);
//            Assert.That(iterationCount, Is.GreaterThan(1), "Convergence loop did not iterate as expected.");
//            Assert.That(antival.Converged(), Is.GreaterThan(0), "The variable did not converge as expected.");
//        }

//        /// <summary>
//        /// Arithmetic operators correctly apply elementwise over unified state.
//        /// </summary>
//        [Test]
//        public void ArithmeticOperators_ShouldApplyElementwiseToUnifiedState()
//        {
//            PositronicVariable<int>.ResetStaticVariables();

//            PositronicVariable<int>.SetEntropy(-1);
//            var varUnified = PositronicVariable<int>.GetOrCreate("testVar3", 1);
//            varUnified.Assign(2);
//            varUnified.Assign(0);
//            varUnified.UnifyAll();
//            var result = varUnified + 1;
//            var expected = new List<int> { 3, 1 };
//            Assert.That(result.Value.ToValues(), Is.EquivalentTo(expected),
//                $"Expected elementwise addition to produce any({string.Join(", ", expected)}) but got any({string.Join(", ", result.Value.ToValues())}).");
//        }

//        /// <summary>
//        /// Initial slice replacement affects accumulation as expected.
//        /// </summary>
//        [Test]
//        public void ReplacedInitialSlice_AccumulatesIncorrectly()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var pv = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1), "Expected one initial timeline slice.");
//            pv.Assign((pv + 1) % 3);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1), "Expected the first assignment to replace the initial slice.");
//            pv.Assign((pv + 1) % 3);
//            Assert.That(pv.timeline.Count, Is.EqualTo(2), "Expected a new slice to be appended after the initial replacement.");
//            pv.UnifyAll();
//            var finalStates = pv.ToValues().OrderBy(x => x).ToList();
//            Assert.That(finalStates, Is.Not.EquivalentTo(new[] { 0, 1, 2 }),
//                "The union is not accumulating all expected values because the initial slice was overwritten.");
//        }

//        /// <summary>
//        /// Elementwise arithmetic applied correctly in forward time.
//        /// </summary>
//        [Test]
//        public void ForwardArithmetic_AppliesElementwiseOverUnion()
//        {
//            PositronicVariable<int>.SetEntropy(-1);
//            var varUnion = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            varUnion.Assign((varUnion + 1) % 3);
//            varUnion.Assign((varUnion + 1) % 3);
//            varUnion.Assign((varUnion + 1) % 3);
//            varUnion.UnifyAll();
//            PositronicVariable<int>.SetEntropy(1);
//            var forwardResult = (varUnion + 1) % 3;
//            var expectedValues = new List<int> { 2, 1, 0 };
//            var actualValues = forwardResult.ToValues().OrderBy(x => x).ToList();
//            Assert.That(actualValues, Is.EquivalentTo(expectedValues),
//                "Forward arithmetic did not apply elementwise over the unified negative state as expected.");
//        }

//        /// <summary>
//        /// Initial slice replacement behavior correct.
//        /// </summary>
//        [Test]
//        public void FirstAssignment_ReplacesInitialSlice_InUnionMode()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var pv = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1), "Expected one initial timeline slice.");
//            PositronicVariable<int> nextValue = (pv + 1) % 3;
//            pv.Assign(nextValue);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1),
//                "Expected the first assignment to replace the initial slice rather than append, indicating no accumulation for union mode.");
//            Assert.That(pv.timeline[0].States.OrderBy(x => x).ToList(), Is.EqualTo(new[] { 0 }),
//                "Expected the timeline slice to contain the new state 0.");
//        }

//        /// <summary>
//        /// Negative phase timeline accumulation test.
//        /// </summary>
//        [Test]
//        public void NegativePhaseTimeline_AccumulationTest()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            Assert.That(antival.timeline.Count, Is.EqualTo(1), "Expected one initial timeline slice.");
//            antival.Assign((antival + 1) % 3);
//            Assert.That(antival.timeline.Count, Is.EqualTo(1), "Expected the first assignment to replace the initial slice.");
//            antival.Assign((antival + 1) % 3);
//            Assert.That(antival.timeline.Count, Is.EqualTo(2), "Expected a new slice to be appended after the initial replacement.");
//            antival.UnifyAll();
//            var union = antival.ToValues().OrderBy(x => x).ToList();
//            Assert.That(union, Is.Not.EquivalentTo(new[] { 0, 1, 2 }),
//                $"The union is incomplete. It contains: {string.Join(", ", union)}");
//        }

//        /// <summary>
//        /// First assignment replaces seed and prevents full union accumulation.
//        /// </summary>
//        [Test]
//        public void FirstAssignment_ReplacesSeed_AndPreventsFullUnionAccumulation()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var pv = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1), "Expected one initial timeline slice.");
//            pv.Assign((pv + 1) % 3);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1), "Expected the first assignment to replace the initial slice.");
//            pv.Assign((pv + 1) % 3);
//            Assert.That(pv.timeline.Count, Is.EqualTo(2), "Expected a new slice to be appended after the initial replacement.");
//            pv.UnifyAll();
//            var union = pv.ToValues().OrderBy(x => x).ToList();
//            Assert.That(union, Is.Not.EquivalentTo(new[] { 0, 1, 2 }),
//                $"The union is incomplete. It contains: {string.Join(", ", union)}");
//        }

//        /// <summary>
//        /// Initial slice replacement prevents union accumulation.
//        /// </summary>
//        [Test]
//        public void InitialSliceReplacementPreventsUnionAccumulation()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var pv = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1), "Expected one initial timeline slice.");
//            pv.Assign((pv + 1) % 3);
//            Assert.That(pv.timeline.Count, Is.EqualTo(1), "Expected first assignment to replace the initial slice.");
//            pv.Assign((pv + 1) % 3);
//            Assert.That(pv.timeline.Count, Is.EqualTo(2), "Expected a new slice to be appended after the initial replacement.");
//            pv.UnifyAll();
//            var union = pv.ToValues().OrderBy(x => x).ToList();
//            Assert.That(union, Is.Not.EquivalentTo(new[] { 0, 1, 2 }),
//                $"The union is incomplete as expected. It contains: {string.Join(", ", union)}");
//        }

//        /// <summary>
//        /// Debug timeline slice accumulation.
//        /// </summary>
//        [Test]
//        public void DebugTimelineSliceAccumulation()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            for (int i = 0; i < 6; i++)
//            {
//                var nextVal = (antival + 1) % 3;
//                antival.Assign(nextVal);
//                TestContext.WriteLine($"After assignment {i + 1}, antival = {antival}");
//                DumpTimeline(antival);
//            }
//            Assert.That(antival.timeline.Count, Is.GreaterThanOrEqualTo(1));
//            Assert.That(PositronicRuntime.Instance.Converged, Is.True);
//            antival.UnifyAll();
//            var finalStates = antival.ToValues().OrderBy(x => x).Distinct().ToList();
//            Assert.That(finalStates, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                $"After unification, expected final states to contain [0,1,2], but got [{string.Join(", ", finalStates)}].");
//        }

//        [Test]
//        public void QuBit_Addition_ElementwiseOnUnifiedState_ForwardMode()
//        {
//            var qb = new QuBit<int>(new[] { 1, 0, 2 });
//            qb.Any();
//            var resultQB = qb + 1;
//            var expectedValues = new List<int> { 2, 1, 3 };
//            var actualValues = resultQB.States.OrderBy(x => x).ToList();
//            Assert.That(actualValues, Is.EquivalentTo(expectedValues),
//                "Expected elementwise addition to produce any(2, 1, 3).");
//        }

//        [Test]
//        public void ForwardArithmetic_OnUnifiedVariable_AppliesElementwiseModulo()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var pv = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            pv.Assign((pv + 1) % 3);
//            pv.Assign((pv + 1) % 3);
//            pv.Assign((pv + 1) % 3);
//            pv.UnifyAll();
//            PositronicVariable<int>.SetEntropy(1);
//            var forward = (pv + 1) % 3;
//            var resultUnion = forward.ToValues().OrderBy(x => x).ToList();
//            Assert.That(resultUnion, Is.EquivalentTo(new[] { 0, 1, 2 }),
//                $"Expected forward arithmetic to preserve the full union, but got any({string.Join(", ", resultUnion)}).");
//        }

//        [Test]
//        public void RunConvergenceLoop_DoesNotCollapseNegativeHistoryInUnionMode()
//        {
//            PositronicVariable<int>.ResetStaticVariables();
//            PositronicVariable<int>.SetEntropy(-1);
//            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1);
//            int negativeIterations = 0;
//            while (antival.Converged() == 0 && negativeIterations < 1000)
//            {
//                var next = (antival + 1) % 3;
//                antival.Assign(next);
//                negativeIterations++;
//            }
//            Assert.That(antival.timeline.Count, Is.GreaterThan(1),
//                $"Expected negative timeline to have more than one slice in union mode, but got {antival.timeline.Count}");
//        }

//        /// <summary>
//        /// Uses the Babylonian method to approximate √2.
//        /// The iterative update is applied until convergence is detected,
//        /// then the timeline is unified and checked for a single value.
//        /// </summary>
//        /// <summary>
//        /// Uses the Babylonian method to approximate √2.
//        /// We explicitly call Converged() after each iteration (and again before unification)
//        /// since our test is not marked with [PositronicEntry] and the library will not auto-run convergence.
//        /// </summary>
//        [Test]
//        public void BabylonianSqrt_ConvergesToSquareRoot2()
//        {
//            double a = 2.0;
//            // We expect the approximation to round to 1.4142 after convergence.
//            double expectedSqrt = 1.4142;

//            // Define an initial guess (rounded to 9 decimal places).
//            double initialGuess = Math.Round(1.5, 9);
//            var sqrtApprox = PositronicVariable<double>.GetOrCreate("sqrt", initialGuess);

//            // Set negative-time mode to let the simulation iterate backward.
//            PositronicVariable<double>.SetEntropy(-1);

//            int maxIterations = 50;
//            int convergenceStep = 0;
//            for (int i = 0; i < maxIterations; i++)
//            {
//                // Retrieve the current approximation.
//                double currentApprox = sqrtApprox.ToValues().First();
//                // Compute next approximation using the Babylonian update rule and round to 4 decimals.
//                double nextApprox = Math.Round((currentApprox + a / currentApprox) / 2.0, 4);
//                // Update the positronic variable.
//                sqrtApprox.Assign(nextApprox);

//                // Call Converged explicitly and check if a repeated state has been detected.
//                convergenceStep = sqrtApprox.Converged();
//                if (convergenceStep > 0)
//                {
//                    TestContext.WriteLine($"Convergence detected on iteration {i + 1} (step: {convergenceStep}).");
//                    break;
//                }
//            }

//            Assert.That(sqrtApprox.Converged(), Is.GreaterThan(0),
//                "Convergence was not detected within the maximum number of iterations.");

//            // Collapse the timeline into the converged state.
//            sqrtApprox.UnifyAll();

//            // Verify via reflection that only one timeline slice remains.
//            FieldInfo timelineField = typeof(PositronicVariable<double>)
//                .GetField("timeline", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//            Assert.That(timelineField, Is.Not.Null, "Reflection failed: 'timeline' field not found.");
//            var rawTimeline = timelineField.GetValue(sqrtApprox) as IList;
//            Assert.That(rawTimeline, Is.Not.Null, "Failed to extract timeline via reflection.");
//            Assert.That(rawTimeline.Count, Is.EqualTo(1), "Expected timeline to collapse to a single slice after convergence.");

//            // Inspect internal qubit state
//            var qubit = rawTimeline[0];
//            var statesProp = qubit.GetType().GetProperty("States", BindingFlags.Instance | BindingFlags.Public);
//            var states = (IEnumerable<double>)statesProp.GetValue(qubit);
//            var allValues = states.ToList(); // Preserve the ordering (do not sort)
//            TestContext.WriteLine("Final internal qubit states: " + string.Join(", ", allValues));
//            Assert.That(allValues.Count, Is.GreaterThanOrEqualTo(1),
//                "Expected at least one value in the final slice.");

//            // Get the last entry from the list and verify it equals the expected value.
//            double finalResult = allValues.Last();
//            TestContext.WriteLine("Final result in the internal qubit (last entry): " + finalResult);
//            Assert.That(finalResult, Is.EqualTo(expectedSqrt).Within(0.0001),
//                $"Expected the last result to be approximately {expectedSqrt}, but got {finalResult}.");

//            // Switch to forward time.
//            PositronicVariable<double>.SetEntropy(1);
//        }
//        [Test]
//        public void PositronicVariable_DateTimeOffset_HandlesConvergenceCorrectly()
//        {
//            PositronicVariable<DateTimeOffset>.ResetStaticVariables();
//            PositronicVariable<DateTimeOffset>.SetEntropy(-1);
//            var baseTime = DateTimeOffset.UtcNow;
//            var pv = PositronicVariable<DateTimeOffset>.GetOrCreate("dtoTest", baseTime);

//            for (int i = 0; i < 3; i++)
//                pv.Assign(baseTime.AddHours(i));

//            Assert.That(pv.Converged(), Is.EqualTo(0));
//            pv.Assign(baseTime);
//            Assert.That(pv.Converged(), Is.GreaterThan(0));
//            pv.UnifyAll();

//            var values = pv.ToValues().ToList();
//            // Updated expectation: 3 distinct states, not 4
//            Assert.That(values.Count, Is.EqualTo(3));
//        }
//        [Test]
//        public void PositronicVariable_ListInt_ShouldThrowIfTypeIsNotComparable()
//        {
//            // Try-catch block because the type initialization is lazy and will throw on first usage.
//            var ex = Assert.Throws<TypeInitializationException>(() =>
//            {
//                PositronicVariable<List<int>>.ResetStaticVariables();
//                PositronicVariable<List<int>>.SetEntropy(-1);

//                // First access to the generic type will trigger the static initializer
//                var baseList = new List<int> { 1, 2, 3 };
//                var pv = PositronicVariable<List<int>>.GetOrCreate("listTest", baseList);

//                // Assignments will never be reached if constructor throws properly
//                pv.Assign(new List<int> { 3, 2, 1 });
//            });

//            Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
//            Assert.That(ex.InnerException.Message, Does.Contain("Type parameter")
//                .And.Contain("must implement IComparable"),
//                "Expected exception about type parameter lacking IComparable");
//        }

//        [Test]
//        public void PascalTriangle_WithComparableList_PrintsCorrectTriangle()
//        {
//            // Define the expected first 5 rows of Pascal's Triangle.
//            var expectedRows = new List<string>
//    {
//        "1",
//        "1 1",
//        "1 2 1",
//        "1 3 3 1",
//        "1 4 6 4 1"
//    };

//            int rowsToPrint = expectedRows.Count;

//            // Create the positronic variable with an initial ComparableList wrapping [1].
//            var row = PositronicVariable<ComparableList>.GetOrCreate("pascalRow", new ComparableList(1));

//            // Redirect console output to capture the printed rows.
//            var output = new StringBuilder();
//            var writer = new StringWriter(output);
//            var originalOut = Console.Out;
//            Console.SetOut(writer);

//            // Iterate through the desired number of rows.
//            for (int i = 0; i < rowsToPrint; i++)
//            {
//                // Retrieve the current row (unwrap the ComparableList).
//                ComparableList currentRowWrapper = row.ToValues().Last();
//                List<int> currentRow = currentRowWrapper.Items;

//                // Print the current row.
//                Console.WriteLine(string.Join(" ", currentRow));

//                // Compute the next row of Pascal's Triangle.
//                List<int> nextRow = ComputeNextRow(currentRow);

//                // Wrap the next row in a ComparableList and assign it.
//                row.Assign(new ComparableList(nextRow.ToArray()));
//            }

//            // Restore the original console output.
//            Console.SetOut(originalOut);

//            // Split the captured output into individual rows.
//            var printedRows = output.ToString()
//                                      .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
//                                      .ToList();

//            // Write the captured rows to the test context output for debugging.
//            TestContext.WriteLine("Printed Pascal's Triangle:");
//            foreach (var r in printedRows)
//            {
//                TestContext.WriteLine(r);
//            }

//            // Verify that the printed rows match the expected output.
//            Assert.That(printedRows.Count, Is.EqualTo(rowsToPrint));
//            for (int i = 0; i < rowsToPrint; i++)
//            {
//                Assert.That(printedRows[i], Is.EqualTo(expectedRows[i]),
//                    $"Row {i + 1} is incorrect. Expected: '{expectedRows[i]}', Actual: '{printedRows[i]}'");
//            }
//        }

//        /// <summary>
//        /// Computes the next row of Pascal's Triangle given the current row.
//        /// </summary>
//        /// <param name="current">The current row as a List of int.</param>
//        /// <returns>A new List of int representing the next row.</returns>
//        private List<int> ComputeNextRow(List<int> current)
//        {
//            var next = new List<int> { 1 };
//            // Each interior number is the sum of two adjacent numbers in the current row.
//            for (int i = 0; i < current.Count - 1; i++)
//            {
//                next.Add(current[i] + current[i + 1]);
//            }
//            next.Add(1);
//            return next;
//        }
//        [Test]
//        public void RunConvergenceLoop_SimpleTemperatureConversion_PrintsExpectedOutput()
//        {
//            // Capture console output into a StringBuilder.
//            var output = new StringBuilder();
//            var writer = new StringWriter(output);
//            var originalOut = Console.Out;
//            Console.SetOut(writer);

//            // Reset the global runtime and set negative-time mode.
//            PositronicVariable<double>.ResetStaticVariables();
//            PositronicVariable<double>.SetEntropy(-1);

//            // Use a unique key (e.g., "tempTest") so that this PositronicVariable is isolated from other tests.
//            string key = "tempTest";

//            // Run the convergence loop with our temperature conversion code.
//            // Note: The arithmetic order is adjusted so that the conversion is executed as:
//            //       Celsius = (Fahrenheit - 32) * 5 / 9.
//            PositronicVariable<double>.RunConvergenceLoop(() =>
//            {
//                var temp = PositronicVariable<double>.GetOrCreate(key, 0);

//                // Assign the Fahrenheit value.
//                temp.Assign(98.6);
//                // Convert to Celsius.
//                temp = temp - 32;  // (98.6 - 32) = 66.6
//                temp = temp * 5;   // 66.6 * 5 = 333
//                temp = temp / 9;   // 333 / 9 ≈ 37.0

//                // Output the converted temperature.
//                Console.WriteLine("Temperature is :" + temp);
//            });

//            // Restore the original console output.
//            Console.SetOut(originalOut);
//            writer.Flush();

//            // Normalize output (convert CRLF to LF for consistency).
//            string finalOutput = output.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
//            TestContext.WriteLine("Captured output from temperature conversion test:\n" + finalOutput);

//            // Verify that the output contains the expected conversion result.
//            // We check for "Temperature is :37" to allow for formatting differences (e.g., "37" vs. "37.0").
//            Assert.That(finalOutput, Does.Contain("Temperature is :37"),
//                "Final output did not contain the expected converted temperature value.");

//            // Also validate that the final state of the variable is approximately 37.0.
//            double finalValue = PositronicVariable<double>.GetOrCreate(key, 0).ToValues().First();
//            Assert.That(finalValue, Is.EqualTo(37.0).Within(0.0001),
//                $"Expected final temperature to be approximately 37.0, but got {finalValue}.");
//        }

//    }
//}

//// Custom wrapper class that implements IComparable for a List<int>
//public class ComparableList : IComparable<ComparableList>
//{
//    public List<int> Items { get; }

//    public ComparableList(params int[] values)
//    {
//        Items = new List<int>(values);
//    }

//    public int CompareTo(ComparableList other)
//    {
//        if (other == null) return 1;
//        int countCompare = Items.Count.CompareTo(other.Items.Count);
//        if (countCompare != 0) return countCompare;

//        for (int i = 0; i < Items.Count; i++)
//        {
//            int cmp = Items[i].CompareTo(other.Items[i]);
//            if (cmp != 0) return cmp;
//        }
//        return 0;
//    }

//    public override bool Equals(object obj)
//    {
//        return obj is ComparableList cl && CompareTo(cl) == 0;
//    }

//    public override int GetHashCode()
//    {
//        return Items.Aggregate(17, (acc, val) => acc * 31 + val);
//    }
//}
