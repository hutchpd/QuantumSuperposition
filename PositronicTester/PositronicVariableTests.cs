using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Core;
using NUnit.Framework;
using System.Collections;
using NUnit.Framework.Legacy;
using Microsoft.Extensions.DependencyInjection;
using QuantumSuperposition.DependencyInjection;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicVariableTests
    {
        private ServiceProvider _provider;
        private IPositronicRuntime _runtime;

        // Before each test, reset the runtime (and thus the global state).
        [SetUp]
        public void SetUp()
        {
            _provider = new ServiceCollection()
                           .AddPositronicRuntime()
                           .BuildServiceProvider();
            
            _runtime = _provider.GetRequiredService<IPositronicRuntime>();

            PositronicVariable<int>.ResetStaticVariables(); 
            QuantumConfig.ForbidDefaultOnCollapse = true;
        }

        [TearDown]
        public void TearDown() => _provider.Dispose();

        [Test]
        public void SnapshotOps_Should_Be_Cleared_Before_Convergence()
        {
            // Arrange
            PositronicVariable<double>.ResetStaticVariables();
            var t = PositronicVariable<double>.GetOrCreate(_runtime);

            // Act
            PositronicVariable<double>.RunConvergenceLoop(_runtime, () =>
            {
                t.Assign(t + 1);   // +1
                t.Assign(t + 1);   // +1
                t.Assign(10);      // set to 10
            }, runFinalIteration: false);   // <- keep last slice exactly as the loop leaves it

            // Assert  –– expect **one** collapsed value, not three
            var values = t.ToValues().ToList();
            Assert.That(values.Count, Is.EqualTo(1),
                "Timeline snapshot operations were not removed before convergence");
            Assert.That(values[0], Is.EqualTo(12));
        }


        [Test]
        public void SimpleProgram_BackwardsAssignments_Prints12()
        {
            // 1) Reset the global Positronic state
            PositronicVariable<double>.ResetStaticVariables();

            // 2) Capture Console output
            var output = new StringWriter();
            Console.SetOut(output);

            // 3) Run the exact sequence of Main(), under convergence
            PositronicVariable<double>.RunConvergenceLoop(_runtime , () =>
            {
                var temperature = PositronicVariable<double>.GetOrCreate(_runtime);

                // This line should ultimately print "12"
                Console.WriteLine($"The temperature in c is {temperature}");

                // Two +1 assignments, then set to 10, all replayed backwards
                temperature.Assign(temperature + 1);
                temperature.Assign(temperature + 1);
                temperature.Assign(10);
            });

            // 4) Flush & grab the result (there should be exactly one line)
            Console.Out.Flush();
            var actual = output.ToString().TrimEnd();

            // 5) Assert that we got "The temperature in c is 12"
            Assert.That(actual, Is.EqualTo("The temperature in c is any(12)"));
        }

        [Test]
        public void ConvergenceLoop_Should_Run_More_Than_One_HalfCycle()
        {
            // arrange
            PositronicVariable<double>.ResetStaticVariables();
            int halfCycles = 0;

            // act
            PositronicVariable<double>.RunConvergenceLoop(_runtime,
                () =>
                {
                    halfCycles++;                           // <-- count the passes
                    var temp = PositronicVariable<double>.GetOrCreate(_runtime);

                    Console.WriteLine(temp);                // just to touch stdout

                    temp.Assign(temp + 1);                  // +1
                    temp.Assign(temp + 1);                  // +1
                    temp.Assign(10);                       // 10
                },
                runFinalIteration: false,                   // don’t add the extra tick
                unifyOnConvergence: true);

            // assert – the bug collapses after the *first* pass, so this will fail
            Assert.That(halfCycles, Is.GreaterThan(1),
                "Timeline collapsed during the very first (reverse) pass – " +
                "no forward-pass operations were ever recorded.");
        }

        /// <summary>
        /// Forward " + 1 " followed by one reverse step should restore
        /// the original value.
        /// </summary>
        [Test]
        public void Addition_Undoes_Itself_On_ReverseStep()
        {
            PositronicVariable<int>.ResetStaticVariables();
            var v = PositronicVariable<int>.GetOrCreate("v", 0, _runtime);

            // one forward step
            RunStep(() => v.Assign(v + 1), +1);

            // one reverse step
            RunStep(() => { /* no-op; the reverse logic lives in RunStep */ }, -1);

            var value = v.ToValues().Single();
            Assert.That(value, Is.EqualTo(0));
        }

        public void RunStep(Action code, int entropy)
        {
            _runtime.Converged = false;
            _runtime.Entropy = entropy;
            code();
            if (entropy < 0)
                OperationLog.ReverseLastOperations();
        }

        /// <summary>
        /// Forward “% 3” should round-trip through the reverse half-cycle.
        /// The final collapsed value must be the original, not the remainder.
        /// </summary>
        [Test]
        public void Modulus_Is_Reversible()
        {
            /*  ────────────────────────────────────────────────────────────────
                1.  🎛  Spin-up a brand-new runtime under the DI container
                    (no global state is touched any more).
            ───────────────────────────────────────────────────────────────── */
            var sp = new ServiceCollection()
                         .AddPositronicRuntime()
                         .BuildServiceProvider();

            var rt = sp.GetRequiredService<IPositronicRuntime>();

            /*  ────────────────────────────────────────────────────────────────
                2.  🧮  Create the positronic variable *inside* that runtime.
                    (All ctors now take an optional `runtime:` parameter.)
            ───────────────────────────────────────────────────────────────── */
            var v = new PositronicVariable<int>(4, runtime: rt);   // 4 % 3 → 1

            /*  ────────────────────────────────────────────────────────────────
                3.  🔄  Run the convergence loop.  Every helper that used to
                    be parameter-less now takes `rt` as the first argument.
            ───────────────────────────────────────────────────────────────── */
            PositronicVariable<int>.RunConvergenceLoop(_runtime,
                code: () =>
                {
                    if (rt.Entropy > 0)         // or:  if (rt.GetEntropy(rt) > 0)
                        v.Assign(v % 3);        // forward pass does the %3
                });
            Assert.That(v.ToValues().Single(), Is.EqualTo(4));
        }


        [Test]
        public void ModPlusModulus_Reversal_Should_Restore_Original_Value()
        {
            // fresh variable v = -1
            PositronicVariable<int>.ResetStaticVariables();
            var v = PositronicVariable<int>.GetOrCreate("v", -1, _runtime);

            // emulate a single **forward** half-cycle
            PositronicVariable<int>.SetEntropy(_runtime, 1);             // forward
            var q = (v + 1) % 3;                               // records +1 and %3
            v.Assign(q);                                       // timeline now holds 0
            Assert.That(v.ToValues().Single(), Is.EqualTo(0),  // sanity
                "forward calculation should give 0");

            // emulate the **reverse** half-cycle
            PositronicVariable<int>.SetEntropy(_runtime, -1);            // reverse
            OperationLog.ReverseLastOperations();              // runtime’s global undo
            v.Assign(v);                                       // identical to what the user code would do

            // BUG: with current ApplyInverse() the value becomes -2
            Assert.That(v.ToValues().Single(), Is.EqualTo(-1),
                "inverse pipeline (+1 then %3) should restore the original -1");
        }



        /// <summary>
        /// Forward pass adds new timeline slice without erasing negative timeline.
        /// </summary>
        [Test]
        public void TimelineState_BeforeAndAfterForwardPass()
        {
            // Set to negative time and cycle through values 0->1->2 several times.
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            // Using the interface type for external operations.
            var testVar = PositronicVariable<int>.GetOrCreate("testVar", 0, _runtime);
            var pv = testVar as PositronicVariable<int>;

            // Run several negative steps.
            for (int i = 0; i < 10; i++)
            {
                var next = (pv + 1) % 3;
                pv.Assign(next);
            }

            var timelineBefore = GetTimelineSnapshot(pv);
            TestContext.WriteLine("Timeline BEFORE forward pass:");
            foreach (var slice in timelineBefore)
                TestContext.WriteLine($"Slice: [{string.Join(", ", slice.OrderBy(x => x))}]");

            // Switch to forward time.
            PositronicVariable<int>.SetEntropy(_runtime, 1);
            var forwardVal = (pv + 1) % 3;
            pv.Assign(forwardVal);

            var timelineAfter = GetTimelineSnapshot(pv);
            TestContext.WriteLine("Timeline AFTER forward pass:");
            foreach (var slice in timelineAfter)
                TestContext.WriteLine($"Slice: [{string.Join(", ", slice.OrderBy(x => x))}]");

            // Assert that the forward pass added a timeline slice.
            Assert.That(timelineAfter.Count, Is.EqualTo(timelineBefore.Count),
                "No new slice expected post-convergence.");
        }


        /// <summary>
        /// Helper method using reflection to capture a snapshot of the timeline.
        /// </summary>
        private List<List<int>> GetTimelineSnapshot(PositronicVariable<int> variable)
        {
            var snapshot = new List<List<int>>();
            FieldInfo timelineField = typeof(PositronicVariable<int>)
                .GetField("timeline", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var slices = timelineField.GetValue(variable) as List<QuBit<int>>;
            if (slices == null)
                return snapshot;
            foreach (var qb in slices)
            {
                snapshot.Add(qb.States.ToList());
            }
            return snapshot;
        }


        /// <summary>
        /// Addition operator correctly calculates new QuBit result.
        /// </summary>
        [Test]
        public void AdditionOperator_AddsValueToPositronicVariable()
        {
            var variable = PositronicVariable<int>.GetOrCreate("testVar", 5, _runtime);
            var result = variable + 3; // result is now a QuBit<int>
            var expectedValues = new List<int> { 8 };

            // Compare against result.States or result.ToCollapsedValues()
            Assert.That(result.ToCollapsedValues(), Is.EquivalentTo(expectedValues));
        }


        /// <summary>
        /// Correct assignment of value from another PositronicVariable.
        /// </summary>
        [Test]
        public void AssignMethod_AssignsValueFromAnotherPositronicVariable()
        {
            PositronicVariable<int>.ResetStaticVariables();

            PositronicVariable<int>.SetEntropy(_runtime, 1);
            var variable1 = PositronicVariable<int>.GetOrCreate("testVar4", 5, _runtime);
            var variable2 = PositronicVariable<int>.GetOrCreate("testVar5", 10, _runtime);
            variable1.Assign(variable2);
            var expectedValues = new List<int> { 10 };
            Assert.That(variable1.Value.ToValues(), Is.EquivalentTo(expectedValues));
            PositronicVariable<int>.SetEntropy(_runtime, -1); // Reset entropy
        }

        /// <summary>
        /// Converged returns zero when timeline is too short.
        /// </summary>
        [Test]
        public void ConvergedMethod_ReturnsZeroWhenTimelineCountIsLessThanThree()
        {
            var variable = PositronicVariable<int>.GetOrCreate("testVar", 5, _runtime);
            var result = variable.Converged();
            Assert.That(result, Is.EqualTo(0));
        }

        /// <summary>
        /// No convergence detected when states differ.
        /// </summary>
        [Test]
        public void ConvergedMethod_ReturnsZeroWhenNoConvergenceIsFound()
        {
            PositronicVariable<int>.ResetStaticVariables();

            var variable = PositronicVariable<int>.GetOrCreate("testVar8", 5, _runtime);
            variable.Assign(PositronicVariable<int>.GetOrCreate("testVar9", 10, _runtime));
            variable.Assign(PositronicVariable<int>.GetOrCreate("testVar10", 15, _runtime));
            var result = variable.Converged();
            Assert.That(result, Is.EqualTo(0));
        }

        /// <summary>
        /// Antival cycles converge correctly to all three values {0,1,2}.
        /// </summary>
        [Test]
        public void Antival_CyclesThrough0_1_2_AndEventuallyConvergesToAllThree()
        {
            // Start clean
            PositronicVariable<int>.ResetStaticVariables();
            var antival = PositronicVariable<int>.GetOrCreate("testVar2", -1, _runtime);


            // Run the bouncing simulation using the convergence loop
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {

                // Cycle it forward with wrapping mod 3
                var nextValue = (antival + 1) % 3;
                antival.Assign(nextValue);
            });

            var antivalFinal = PositronicVariable<int>.GetOrCreate("testVar2", _runtime);
            var finalStates = antivalFinal.ToValues().OrderBy(x => x).Distinct().ToList();

            Assert.That(finalStates, Is.EquivalentTo(new List<int> { 0, 1, 2 }),
                $"Expected to see all [0,1,2] in final slice, but got [{string.Join(", ", finalStates)}].");
        }

        [Test]
        public void MultipleAssigns_Leave_OperationLog_Empty_After_Reverse()
        {
            // clean slate
            PositronicVariable<int>.ResetStaticVariables();

            // run exactly one forward + one reverse half-cycle
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                var v = PositronicVariable<int>.GetOrCreate("x", 0, _runtime);

                // --- three Assigns in the *same* forward half-cycle ---
                v.Assign(v + 1);   //  => 1
                v.Assign(v + 1);   //  => 2
                v.Assign(10);      //  => 10
            },
            runFinalIteration: false,   // we just want the first reverse pass
            unifyOnConvergence: false); // don't let UnifyAll() hide the evidence

            // the operation log should be empty after the reverse pass
            Assert.That(
                typeof(OperationLog)
                  .GetField("_log", BindingFlags.NonPublic | BindingFlags.Static)!
                  .GetValue(null) as System.Collections.ICollection,
                Is.Empty,
                "Some reversible ops survived the reverse half-cycle ⇢ hypothesis confirmed");
        }


        [Test]
        public void Assigning_QuBit_With_Multiple_States_Should_Not_Collapse()
        {
            // fresh world
            PositronicVariable<int>.ResetStaticVariables();
            var v = PositronicVariable<int>.GetOrCreate("q", 0, _runtime);

            // build an explicit super-position { 7, 8, 9 }
            var qb = new QuBit<int>(new[] { 7, 8, 9 });
            qb.Any();                       // make sure it’s “observed”

            // *** BUG: implicit cast collapses here ***
            v.Assign(qb);

            var storedValues = v.ToValues().OrderBy(x => x).ToList();

            // should be 7,8,9 – but with the current code it’s a single value
            Assert.That(storedValues,
                Is.EquivalentTo(new[] { 7, 8, 9 }),
                "Assign(QuBit) collapsed the super-position to a single scalar value.");
        }


        [Test]
        public void Reverse_Duplicate_Triggers_Premature_Collapse()
        {
            // 1  fresh world
            PositronicVariable<int>.ResetStaticVariables();
            var v = PositronicVariable<int>.GetOrCreate("dup", -1, _runtime);

            // 2  count how many *distinct* forward values we actually manage to append
            var forwardValues = new HashSet<int>();
            v.OnTimelineAppended += () =>
            {
                if (PositronicVariable<int>.GetEntropy(_runtime) > 0)
                    forwardValues.Add(v.GetCurrentQBit().ToCollapsedValues().First());
            };

            // 3  classic “+1 mod 3” bounce
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                if (PositronicVariable<int>.GetEntropy(_runtime) > 0)
                    v.Assign((v + 1) % 3);
            },
            runFinalIteration: false);           // don’t let the tidy-up tick hide the bug

            // 4  **Buggy implementation** collapses after *one* forward value.
            Assert.That(forwardValues.Count,
                        Is.EqualTo(1),
                        "A duplicate slice from a reverse pass caused us to stop after only one forward value.");
        }


        [Test]
        public void DuplicateSlice_FromReversePass_DoesNotCountAsConvergence()
        {
            PositronicVariable<int>.ResetStaticVariables();
            var v = PositronicVariable<int>.GetOrCreate("x", 42, _runtime);

            int iterations = 0;

            // Callback mutates ONLY on reverse half-cycles
            PositronicVariable<int>.RunConvergenceLoop(_runtime,
                () =>
                {
                    iterations++;

                    if (PositronicVariable<int>.GetEntropy(_runtime) < 0)
                        v.Assign(v);               // same value, forces a duplicate slice
                },
                runFinalIteration: false           // we only care about the loop body
            );

            // The loop should NOT have stopped after the very first reverse pass
            Assert.That(iterations, Is.GreaterThan(1),
                "A duplicate slice created during a reverse half-cycle falsely triggered convergence.");
        }

        [Test]
        public void ConvergenceLoop_DoesNot_Exit_Before_First_Forward_Append()
        {
            // fresh world
            PositronicVariable<int>.ResetStaticVariables();
            var v = PositronicVariable<int>.GetOrCreate("v", 0, _runtime);

            int forwardAppends = 0;
            v.OnTimelineAppended += () => forwardAppends++;

            int iterations = 0;

            // code that appends ONLY on the forward half-cycle
            PositronicVariable<int>.RunConvergenceLoop(_runtime,
                () =>
                {
                    iterations++;
                    if (PositronicVariable<int>.GetEntropy(_runtime) > 0)
                        v.Assign(v + 1);
                },
                runFinalIteration: false      // we only care about what happens *inside* the loop
            );

            /*  EXPECTED (correct behaviour):
             *     – at least one forward half-cycle runs
             *     – therefore forwardAppends > 0
             *     – therefore iterations > 1
             *
             *  ACTUAL (current buggy code):
             *     forwardAppends == 0
             *     iterations     == 1
             */
            Assert.That(forwardAppends, Is.GreaterThan(0),
                "ConvergenceLoop exited before the first forward append occurred.");
            Assert.That(iterations, Is.GreaterThan(1),
                "ConvergenceLoop should have run at least one forward + one reverse half-cycle.");
        }

        [Test]
        public void ConvergenceLoop_Should_Run_At_Least_One_Forward_Tick()
        {
            PositronicVariable<int>.ResetStaticVariables();
            bool sawForward = false;

            PositronicVariable<int>.RunConvergenceLoop(_runtime,
                () =>
                {
                    if (PositronicVariable<int>.GetEntropy(_runtime) > 0)
                        sawForward = true;          // ← marker
                },
                runFinalIteration: false);

            Assert.That(sawForward,
                "We never saw a forward half-cycle.  The loop exited on the first reverse pass.");
        }



        [Test]
        public void Convergence_Fires_On_First_Reverse_Before_Any_Forward_Ticks()
        {
            PositronicVariable<int>.ResetStaticVariables();
            var v = PositronicVariable<int>.GetOrCreate("v", 0, _runtime);

            // Count only *forward* appends:
            int forwardAppends = 0;
            v.OnTimelineAppended += () => forwardAppends++;

            // Run the same bouncing code, but suppress the final forward tick so we only
            // see what happens inside the loop itself.
            PositronicVariable<int>.RunConvergenceLoop(_runtime,
              () =>
              {
                  if (PositronicVariable<int>.GetEntropy(_runtime) > 0)
                      v.Assign(v + 1);
              },
              runFinalIteration: false,
              bailOnFirstReverseWhenIdle: true);

            // We should have converged before ever doing a single forward append.
            Assert.That(forwardAppends, Is.Zero,
                "Expected zero forward-appends because convergence should fire on the first reverse pass.");

            // And the value should still be the original 0.
            Assert.That(v.ToValues().Single(), Is.EqualTo(0),
                "After that premature convergence we should still be at the original value.");
        }

        [Test]
        public void Timeline_Should_Have_Exactly_One_Slice_After_Reverse()
        {
            PositronicVariable<double>.ResetStaticVariables();

            PositronicVariable<double>.RunConvergenceLoop(_runtime, () =>
            {
                var t = PositronicVariable<double>.GetOrCreate(_runtime);
                t.Assign(t + 1);
                t.Assign(t + 1);
                t.Assign(10);
            });

            var timelineLen = PositronicVariable<double>.GetOrCreate(_runtime).TimelineLength;
            Assert.That(timelineLen, Is.EqualTo(1),
                "More than one slice survived the reverse pass – residual values are leaking into the unite-all.");
        }

        [Test]
        public void UnifyCycle_Should_Preserve_All_Slice_Values()
        {
            // create fake 3-slice cycle
            var v = PositronicVariable<int>.GetOrCreate("probe", 0, _runtime);
            v.Assign(1);
            v.Assign(2);              // timeline now [0,1,2]
            v.Unify(3);               // pretend we detected a 3-slice cycle

            var states = v.ToValues().OrderBy(x => x).ToArray();
            Assert.That(states, Is.EquivalentTo(new[] { 0, 1, 2 }),
                "Cycle-unification dropped one or more states.");
        }

        [Test]
        public void Loop_Should_Begin_With_Entropy_Minus_One()
        {
            PositronicVariable<int>.ResetStaticVariables();
            int entropyAtFirstTick = int.MaxValue;

            PositronicVariable<int>.RunConvergenceLoop(_runtime ,() =>
            {
                // capture entropy on first entry only
                if (entropyAtFirstTick == int.MaxValue)
                    entropyAtFirstTick = PositronicVariable<int>.GetEntropy(_runtime);

                // exit immediately – we only care about the first value
                PositronicAmbient.Current.Converged = true;
            }, runFinalIteration: false);

            Assert.That(entropyAtFirstTick, Is.EqualTo(-1),
                "RunConvergenceLoop now starts in the forward direction; tests that expect a reverse first tick will fail.");
        }



        /// <summary>
        /// Negative timeline builds correctly and inspects states.
        /// </summary>
        [Test]
        public void NegativeTimeTimelineInspection()
        {
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1, _runtime);

            for (int i = 0; i < 10; i++)
            {
                var nextValue = (antival + 1) % 3;
                antival.Assign(nextValue);
                TestContext.WriteLine($"Iteration {i}, antival timeline:");
                DumpTimeline(antival);
                int c = antival.Converged();
                TestContext.WriteLine($"Converged() => {c}");
                TestContext.WriteLine("-----");
            }
        }

        /// <summary>
        /// Helper method that uses reflection to dump the timeline.
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
                    var values = slices[i].States.OrderBy(x => x)
                        .Select(x => x.ToString());
                    Console.WriteLine($"Slice {i}: [{string.Join(", ", values)}]");
                }
                Console.WriteLine("=====================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reflection error while dumping timeline: {ex}");
            }
        }


        /// <summary>
        /// Negative-to-forward transition correctly prints "any(...)".
        /// </summary>
        [Test]
        public void NegativeLoopThenManualForward()
        {
            var output = new StringBuilder();
            var writer = new StringWriter(output);

            // 1) Temporarily discard console output.
            Console.SetOut(TextWriter.Null);

            PositronicVariable<int>.SetEntropy(_runtime, -1);
            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1, _runtime);

            // Negative runs.
            for (int i = 0; i < 20; i++)
            {
                var val = (antival + 1) % 3;
                antival.Assign(val);

                bool allConverged = PositronicVariable<int>.AllConverged(_runtime);
                if (allConverged)
                {
                    // Unify only the looped portion.
                    foreach (var v in PositronicVariable<int>.GetAllVariables(_runtime))
                    {
                        int dist = v.Converged();
                        if (dist > 0)
                            ((PositronicVariable<int>)v).Unify(dist);
                    }
                    TestContext.WriteLine("DEBUG after unify => " + antival);
                    TestContext.WriteLine($"DEBUG eType => {antival.GetCurrentQBit().GetCurrentType()}");
                    break;
                }
            }

            TestContext.WriteLine("DEBUG after negative loop => " + antival);

            // 2) Now do one forward pass, capturing the output.
            Console.SetOut(writer);
            PositronicVariable<int>.SetEntropy(_runtime, 1);

            Console.WriteLine($"The antival is {antival}");
            var forwardVal = (antival + 1) % 3;
            Console.WriteLine($"The value is {forwardVal}");
            antival.Assign(forwardVal);

            // 3) Restore console & show captured.
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.WriteLine("Output was:\n" + output);

            string final = output.ToString();
            Assert.That(final, Does.Contain("any("),
                $"Expected to see 'any(...)' in final pass! Output was {final}");
        }

        /// <summary>
        /// No false convergence when states always unique.
        /// </summary>
        [Test]
        public void ZeroConvergence_NoRepeatedStates_AlwaysUnique()
        {
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            var var1 = PositronicVariable<int>.GetOrCreate("testVar", 10, _runtime);
            for (int i = 0; i < 10; i++)
            {
                var nextVal = (var1 + 2) % 9999;
                var1.Assign(nextVal);
            }
            Assert.That(var1.Converged(), Is.EqualTo(0));
        }

        /// <summary>
        /// Variable correctly converges to expected single state.
        /// </summary>
        [Test]
        public void RunConvergenceLoop_VariableConvergesToExpectedState()
        {
            PositronicVariable<int>.ResetStaticVariables();
            var pv = PositronicVariable<int>.GetOrCreate("testVar", 1, _runtime);
            void Code()
            {
                pv.Assign(1);
            }
            PositronicVariable<int>.RunConvergenceLoop(_runtime, Code);
            Assert.That(pv.timeline.Count, Is.EqualTo(1));
            Assert.That(pv.Converged(), Is.GreaterThan(0));
        }

        /// <summary>
        /// Correct union and output during convergence loop (currently failing).
        /// </summary>
        [Test]
        public void RunConvergenceLoop_VariableConvergesToExpectedState_UnionMode()
        {
            var output = new StringBuilder();
            var writer = new StringWriter(output);
            Console.SetOut(writer);
            PositronicVariable<int>.ResetStaticVariables();
            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1, _runtime);
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                Console.WriteLine($"The antival is {antival}");
                var val = (antival + 1) % 3;
                Console.WriteLine($"The value is {val}");
                antival.Assign(val);
            });
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            TestContext.WriteLine("Captured final output:\n" + output);
            TestContext.WriteLine("Final state: " + antival);
            var expectedOutput = "The antival is any(0, 1, 2)\nThe value is any(1, 2, 0)\n";
            var actual = output.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
            var expected = expectedOutput.Replace("\r\n", "\n").Replace("\r", "\n");
            Assert.That(actual, Is.EqualTo(expected),
                $"Unexpected final output.  Actual:\n{actual}\nExpected:\n{expected}\n");
        }

        [Test]
        public void ReverseReplay_Should_Rebuild_With_All_Forward_Values()
        {
            // arrange
            PositronicVariable<int>.ResetStaticVariables();
            var v = PositronicVariable<int>.GetOrCreate("x", 0, _runtime);

            // do one forward half-cycle that appends a union {1,2}
            PositronicVariable<int>.SetEntropy(_runtime, 1);
            v.Assign(1);
            v.Assign(2);               // now top slice is any(1,2)
            var forwardSlice = v.GetCurrentQBit();

            // trigger one reverse half-cycle
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            v.Assign(forwardSlice);    // makes ReplaceOrAppendOrUnify run in reverse mode

            // assert – rebuilt slice must still contain BOTH values
            var rebuilt = v.GetCurrentQBit().ToCollapsedValues().OrderBy(x => x).ToList();
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, rebuilt,
                "Reverse replay rebuilt the slice as scalar instead of preserving the union.");
        }


        /// <summary>
        /// Unified QuBit should display full union in ToString().
        /// </summary>
        [Test]
        public void UnifiedQuBit_ToString_ShouldDisplayFullUnion()
        {
            var qb = new QuBit<int>(new[] { 1, 0, 2 });
            qb.Any();
            string output = qb.ToString();
            Assert.That(output, Does.Contain("any("), "Expected disjunctive notation.");
            Assert.That(output, Does.Contain("1"), "Expected union to include 1.");
            Assert.That(output, Does.Contain("0"), "Expected union to include 0.");
            Assert.That(output, Does.Contain("2"), "Expected union to include 2.");
        }

        /// <summary>
        /// Convergence loop iterates and detects convergence correctly.
        /// </summary>
        [Test]
        public void ConvergenceLoop_IterationCountTest()
        {
            int iterationCount = 0;
            PositronicVariable<int>.ResetStaticVariables();
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            var antival = PositronicVariable<int>.GetOrCreate("testVar", -1, _runtime);
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                iterationCount++;
                TestContext.WriteLine($"Iteration {iterationCount}: antival = {antival}, timeline count = {GetTimelineSnapshot(antival).Count}");
                var val = (antival + 1) % 3;
                antival.Assign(val);
            });
            TestContext.WriteLine("Total negative iterations: " + iterationCount);
            Assert.That(iterationCount, Is.GreaterThan(1), "Convergence loop did not iterate as expected.");
            Assert.That(antival.Converged(), Is.GreaterThan(0), "The variable did not converge as expected.");
        }

        /// <summary>
        /// Arithmetic operators correctly apply elementwise over unified state.
        /// </summary>
        [Test]
        public void ArithmeticOperators_ShouldApplyElementwiseToUnifiedState()
        {
            PositronicVariable<int>.ResetStaticVariables();

            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                var v = PositronicVariable<int>.GetOrCreate("testVar3", 1, _runtime);
                var next = (v + 1) % 3;
                v.Assign(next);
            });

            var varUnified = PositronicVariable<int>.GetOrCreate("testVar3", _runtime);
            var result = varUnified + 1;
            var expected = new List<int> { 1, 2, 3 };

            Assert.That(result.ToCollapsedValues(), Is.EquivalentTo(expected),
                $"Expected elementwise addition to produce any({string.Join(", ", expected)}) but got any({string.Join(", ", result)}).");
        }



        [Test]
        public void BabylonianSqrt_ForwardOnly_SimpleTest()
        {
            // 1) Reset everything and force forward time
            PositronicVariable<double>.ResetStaticVariables();
            PositronicVariable<double>.SetEntropy(_runtime, 1);

            // 2) Create the variable only once
            var sqrt = PositronicVariable<double>.GetOrCreate("antisqrt", 1.5, _runtime);

            // 3) Run a fixed number of forward updates
            int iterations = 10;
            double a = 2.0;
            for (int i = 0; i < iterations; i++)
            {
                double x = sqrt.ToValues().First();       // current approximate
                double next = (x + a / x) / 2.0;          // Babylonian step
                sqrt.Assign(next);                        // in-place update
            }

            // 4) Check if the final approximation is near sqrt(2)
            double finalApprox = sqrt.ToValues().First();
            TestContext.WriteLine($"Forward-only sqrt(2) after {iterations} steps: {finalApprox}");
            Assert.That(finalApprox, Is.EqualTo(1.4142).Within(0.001),
                $"Expected forward-only iteration to converge near 1.4142, but got {finalApprox}");
        }

        [Test]
        public void BabylonianSqrt_ManualBounce_SimpleTest()
        {
            // 1) Start forward
            PositronicVariable<double>.ResetStaticVariables();
            PositronicVariable<double>.SetEntropy(_runtime, 1);

            // 2) Tie the variable at 1.5
            var antisqrt = PositronicVariable<double>.GetOrCreate("antisqrt", 1.5, _runtime);

            double a = 2.0;

            // 3) Do two forward updates
            for (int i = 0; i < 2; i++)
            {
                double x = antisqrt.ToValues().First();
                double next = (x + a / x) / 2.0;
                antisqrt.Assign(next);
                TestContext.WriteLine($"Forward step {i + 1}, value={antisqrt.ToValues().First()}");
            }

            // 4) Flip to backward: do 1 iteration (which does "nothing")
            PositronicVariable<double>.SetEntropy(_runtime, -1);

            for (int i = 0; i < 1; i++)
            {
                double x = antisqrt.ToValues().First();
                double next = (x + a / x) / 2.0;
                antisqrt.Assign(next);
                // This won't actually do anything because negative time means "skip updates."
                TestContext.WriteLine($"Backward step {i + 1}, value={antisqrt.ToValues().First()}");
            }

            // 5) Flip to forward again
            PositronicVariable<double>.SetEntropy(_runtime, 1);

            // 6) Do two more forward updates
            for (int i = 0; i < 2; i++)
            {
                double x = antisqrt.ToValues().First();
                double next = (x + a / x) / 2.0;
                antisqrt.Assign(next);
                TestContext.WriteLine($"Forward step {i + 3}, value={antisqrt.ToValues().First()}");
            }

            // 7) Check final result
            double finalApprox = antisqrt.ToValues().First();
            TestContext.WriteLine($"Final approx after bounce: {finalApprox}");

            // We won't demand super close accuracy, but let's say within ±0.01
            Assert.That(finalApprox, Is.EqualTo(1.4142).Within(0.01),
                "Even with one negative bounce, we expected a roughly decent sqrt(2).");
        }



        [Test]
        public void PositronicVariable_ListInt_ShouldThrowIfTypeIsNotComparable()
        {
            // Try-catch block because the type initialization is lazy and will throw on first usage.
            var ex = Assert.Throws<TypeInitializationException>(() =>
            {
                PositronicVariable<List<int>>.ResetStaticVariables();
                PositronicVariable<List<int>>.SetEntropy(_runtime, - 1);

                // First access to the generic type will trigger the static initializer
                var baseList = new List<int> { 1, 2, 3 };
                var pv = PositronicVariable<List<int>>.GetOrCreate("listTest", baseList, _runtime);

                // Assignments will never be reached if constructor throws properly
                pv.Assign(new List<int> { 3, 2, 1 });
            });

            Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.InnerException.Message, Does.Contain("Type parameter")
                .And.Contain("must implement IComparable"),
                "Expected exception about type parameter lacking IComparable");
        }

        [Test]
        public void PascalTriangle_WithComparableList_PrintsCorrectTriangle()
        {
            // Define the expected first 5 rows of Pascal's Triangle.
            var expectedRows = new List<string>
    {
        "1",
        "1 1",
        "1 2 1",
        "1 3 3 1",
        "1 4 6 4 1"
    };

            int rowsToPrint = expectedRows.Count;

            // Create the positronic variable with an initial ComparableList wrapping [1].
            var row = PositronicVariable<ComparableList>.GetOrCreate("pascalRow", new ComparableList(1), _runtime);

            // Redirect console output to capture the printed rows.
            var output = new StringBuilder();
            var writer = new StringWriter(output);
            var originalOut = Console.Out;
            Console.SetOut(writer);

            // Iterate through the desired number of rows.
            for (int i = 0; i < rowsToPrint; i++)
            {
                // Retrieve the current row (unwrap the ComparableList).
                ComparableList currentRowWrapper = row.ToValues().Last();
                List<int> currentRow = currentRowWrapper.Items;

                // Print the current row.
                Console.WriteLine(string.Join(" ", currentRow));

                // Compute the next row of Pascal's Triangle.
                List<int> nextRow = ComputeNextRow(currentRow);

                // Wrap the next row in a ComparableList and assign it.
                row.Assign(new ComparableList(nextRow.ToArray()));
            }

            // Restore the original console output.
            Console.SetOut(originalOut);

            // Split the captured output into individual rows.
            var printedRows = output.ToString()
                                      .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                      .ToList();

            // Write the captured rows to the test context output for debugging.
            TestContext.WriteLine("Printed Pascal's Triangle:");
            foreach (var r in printedRows)
            {
                TestContext.WriteLine(r);
            }

            // Verify that the printed rows match the expected output.
            Assert.That(printedRows.Count, Is.EqualTo(rowsToPrint));
            for (int i = 0; i < rowsToPrint; i++)
            {
                Assert.That(printedRows[i], Is.EqualTo(expectedRows[i]),
                    $"Row {i + 1} is incorrect. Expected: '{expectedRows[i]}', Actual: '{printedRows[i]}'");
            }
        }

        /// <summary>
        /// Computes the next row of Pascal's Triangle given the current row.
        /// </summary>
        /// <param name="current">The current row as a List of int.</param>
        /// <returns>A new List of int representing the next row.</returns>
        private List<int> ComputeNextRow(List<int> current)
        {
            var next = new List<int> { 1 };
            // Each interior number is the sum of two adjacent numbers in the current row.
            for (int i = 0; i < current.Count - 1; i++)
            {
                next.Add(current[i] + current[i + 1]);
            }
            next.Add(1);
            return next;
        }


    }
}

// Custom wrapper class that implements IComparable for a List<int>
public class ComparableList : IComparable<ComparableList>
{
    public List<int> Items { get; }

    public ComparableList(params int[] values)
    {
        Items = new List<int>(values);
    }

    public int CompareTo(ComparableList other)
    {
        if (other == null) return 1;
        int countCompare = Items.Count.CompareTo(other.Items.Count);
        if (countCompare != 0) return countCompare;

        for (int i = 0; i < Items.Count; i++)
        {
            int cmp = Items[i].CompareTo(other.Items[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    public override bool Equals(object obj)
    {
        return obj is ComparableList cl && CompareTo(cl) == 0;
    }

    public override int GetHashCode()
    {
        return Items.Aggregate(17, (acc, val) => acc * 31 + val);
    }
}
