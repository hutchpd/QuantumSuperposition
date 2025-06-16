using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using QuantumSuperposition.Core;
using QuantumSuperposition.DependencyInjection;
using QuantumSuperposition.QuantumSoup;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicVariableTests
    {
        private ServiceProvider _provider;
        private IPositronicRuntime _runtime;

        [SetUp]
        public void SetUp()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices(s => s.AddPositronicRuntime());

            PositronicAmbient.InitialiseWith(hostBuilder);
            _runtime = PositronicAmbient.Current;

            QuantumConfig.ForbidDefaultOnCollapse = true;
            OperationLog.Clear();
            if (_runtime.OracularStream is StringWriter sw)
                sw.GetStringBuilder().Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.ResetAmbient();
        }

        #region OperatorsAndAssignments

        [Test]
        public void AdditionOperator_AddsValueToPositronicVariable()
        {
            // Arrange
            var v = PositronicVariable<int>.GetOrCreate("x", 5, _runtime);

            // Act
            var result = v + 3;

            // Assert
            Assert.That(result.ToCollapsedValues(), Is.EquivalentTo(new[] { 8 }));
        }

        [Test]
        public void AssignMethod_AssignsValueFromAnotherPositronicVariable()
        {
            // Arrange
            PositronicVariable<int>.SetEntropy(_runtime, 1);
            var a = PositronicVariable<int>.GetOrCreate("a", 5, _runtime);
            var b = PositronicVariable<int>.GetOrCreate("b", 10, _runtime);

            // Act
            a.Assign(b);

            // Assert
            Assert.That(a.Value.ToValues(), Is.EquivalentTo(new[] { 10 }));
        }

        [Test]
        public void Assigning_QuBit_With_Multiple_States_Should_Not_Collapse()
        {
            // Arrange
            var v = PositronicVariable<int>.GetOrCreate("q", 0, _runtime);
            var qb = new QuBit<int>(new[] { 7, 8, 9 });
            qb.Any();

            // Act
            v.Assign(qb);

            // Assert
            Assert.That(v.ToValues().OrderBy(x => x),
                        Is.EquivalentTo(new[] { 7, 8, 9 }));
        }

        #endregion

        #region ConvergenceLoopBehavior

        [Test]
        public void ConvergenceLoop_Should_Run_More_Than_One_HalfCycle()
        {
            // Arrange
            PositronicVariable<double>.SetEntropy(_runtime, -1);
            int passes = 0;

            // Act
            PositronicVariable<double>.RunConvergenceLoop(_runtime, () =>
            {
                passes++;
                var t = PositronicVariable<double>.GetOrCreate("t", 0, _runtime);
                t.Assign(t + 1);
            },
            runFinalIteration: false,
            unifyOnConvergence: true);

            // Assert
            Assert.That(passes, Is.GreaterThan(1));
        }

        [Test]
        public void ConvergenceLoop_Should_Run_At_Least_One_Forward_Tick()
        {
            // Arrange
            bool sawForward = false;

            // Act
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                if (PositronicVariable<int>.GetEntropy(_runtime) > 0)
                    sawForward = true;
            }, runFinalIteration: false);

            // Assert
            Assert.That(sawForward, Is.True);
        }

        [Test]
        public void Convergence_Fires_On_First_Reverse_Before_Any_Forward_Ticks()
        {
            // Arrange
            var v = PositronicVariable<int>.GetOrCreate("v", 0, _runtime);
            int forwardAppends = 0;
            v.OnTimelineAppended += () => forwardAppends++;

            // Act
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                if (PositronicVariable<int>.GetEntropy(_runtime) > 0)
                    v.Assign(v + 1);
            },
            runFinalIteration: false,
            bailOnFirstReverseWhenIdle: true);

            // Assert
            Assert.That(forwardAppends, Is.EqualTo(0));
            Assert.That(v.ToValues().Single(), Is.EqualTo(0));
        }

        [Test]
        public void Loop_Should_Begin_With_Entropy_Minus_One()
        {
            // Arrange
            int firstEntropy = int.MaxValue;

            // Act
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                if (firstEntropy == int.MaxValue)
                    firstEntropy = PositronicVariable<int>.GetEntropy(_runtime);
                PositronicAmbient.Current.Converged = true;
            }, runFinalIteration: false);

            // Assert
            Assert.That(firstEntropy, Is.EqualTo(-1));
        }

        #endregion

        #region ReverseReplaySemantics

        [Test]
        public void Addition_Undoes_Itself_On_ReverseStep()
        {
            // Arrange
            var v = PositronicVariable<int>.GetOrCreate("v", 0, _runtime);

            // Act
            RunStep(() => v.Assign(v + 1), +1);
            RunStep(() => { }, -1);

            // Assert
            Assert.That(v.ToValues().Single(), Is.EqualTo(0));
        }

        [Test]
        public void ReverseReplay_Only_Rebuilds_Forward_Appends()
        {
            // Arrange
            var v = PositronicVariable<int>.GetOrCreate("h", 0, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, 1);
            v.Assign(1);
            v.Assign(2);
            var slice = v.GetCurrentQBit();

            // Act
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            v.Assign(slice);

            // Assert
            Assert.That(v.GetCurrentQBit().ToCollapsedValues().OrderBy(x => x),
                        Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public void ForwardWrite_AfterLoopUnify_AppendsRatherThanOverwrites()
        {
            var v = PositronicVariable<int>.GetOrCreate("chk", 0, _runtime);

            // force a single‑slice timeline (simulate an in‑loop Unify)
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () => {
                v.Assign(v + 1);
                v.Unify(2);   // timeline length == 1 again
            }, runFinalIteration: false);

            var before = v.TimelineLength;                 // must be 1
            PositronicVariable<int>.SetEntropy(_runtime, +1);
            v.Assign(99);                                  // second forward write

            Assert.That(v.TimelineLength, Is.EqualTo(before + 1),
                "Second in‑loop forward write should append, not overwrite");
        }

        [Test]
        public void Engine_Stops_When_UnifyDisabled()
        {
            int iterations = 0;
            PositronicVariable<int>.RunConvergenceLoop(
                _runtime,
                () => iterations++,
                runFinalIteration: false,
                unifyOnConvergence: false);

            Assert.That(iterations, Is.EqualTo(2),     // one reverse + one forward
                "Engine should bail out after one round‑trip when unification is disabled");
        }

        [Test]
        public void ReverseReplay_Collects_All_Forward_Appends()
        {
            var v = PositronicVariable<int>.GetOrCreate("rr", 0, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, +1);
            v.Assign(1);                           // append slice 1
            v.Assign(2);                           // append slice 2

            var snap = v.GetCurrentQBit();         // {2}
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            v.Assign(snap);                        // triggers reverse replay

            var final = v.GetCurrentQBit().ToCollapsedValues().OrderBy(x => x);
            Assert.That(final, Is.EquivalentTo(new[] { 1, 2 }));
        }


            [Test]
        public void ForwardHalfCycle_ShouldAppend_WhenReplaceTrue()
        {
            var v = PositronicVariable<int>.GetOrCreate("antivalProbe", 0, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, +1);   // forward, *outside* loop

            v.Assign(new QuBit<int>(new[] { 1 }));  // replace==true but should APPEND
            Assert.That(v.TimelineLength, Is.EqualTo(2), "First forward write must append, not overwrite");
        }

        [Test]
        public void FirstScalarWrite_OutsideLoop_ShouldNotRewriteBootstrap()
        {
            var v = PositronicVariable<int>.GetOrCreate("scalarProbe", 0, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, +1);   // forward, not in loop

            v.Assign(1);                                        // replace == false
            Assert.That(v.TimelineLength, Is.EqualTo(2), "Bootstrap slice must remain untouched");
        }



        [Test]
        public void ModPlusModulus_Reversal_Should_Restore_Original_Value()
        {
            // Arrange
            var v = PositronicVariable<int>.GetOrCreate("v", -1, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, 1);
            var q = (v + 1) % 3;
            v.Assign(q);
            Assert.That(v.ToValues().Single(), Is.EqualTo(0));

            // Act
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            OperationLog.ReverseLastOperations();
            v.Assign(v);

            // Assert
            Assert.That(v.ToValues().Single(), Is.EqualTo(-1));
        }

        private void RunStep(Action code, int entropy)
        {
            PositronicVariable<int>.SetEntropy(_runtime, entropy);
            code();
            if (entropy < 0)
                OperationLog.ReverseLastOperations();
        }

        #endregion

        #region TimelineStateTests

        [Test]
        public void TimelineState_BeforeAndAfterForwardPass()
        {
            // Arrange
            var pv = PositronicVariable<int>.GetOrCreate("t", 0, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            for (int i = 0; i < 5; i++)
                pv.Assign((pv + 1) % 3);
            var before = Snapshot(pv);

            // Act
            PositronicVariable<int>.SetEntropy(_runtime, 1);
            pv.Assign((pv + 1) % 3);
            var after = Snapshot(pv);

            // Assert
            Assert.That(after.Count, Is.EqualTo(before.Count + 1));
        }

        [Test]
        public void TimelineState_ForwardPassMayMerge()
        {
            // Arrange
            var pv = PositronicVariable<int>.GetOrCreate("t2", 0, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            for (int i = 0; i < 5; i++)
                pv.Assign((pv + 1) % 3);
            var before = Snapshot(pv);

            // Act
            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                PositronicVariable<int>.SetEntropy(_runtime, 1);
                pv.Assign((pv + 1) % 3);
            }, runFinalIteration: false);

            var after = Snapshot(pv);

            // Assert
            Assert.That(after.Count, Is.LessThanOrEqualTo(before.Count));
        }

        private List<List<int>> Snapshot(PositronicVariable<int> var)
        {
            var field = typeof(PositronicVariable<int>)
                .GetField("timeline", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var slices = (List<QuBit<int>>)field.GetValue(var);
            return slices.Select(q => q.States.ToList()).ToList();
        }

        #endregion

        #region DomainAndLogTests

        [Test]
        public void OperationLog_And_Domain_Are_Cleared_After_Convergence()
        {
            // Arrange
            var v = PositronicVariable<double>.GetOrCreate("d", 0, _runtime);

            // Act
            PositronicVariable<double>.RunConvergenceLoop(_runtime, () =>
            {
                v.Assign(v + 1);
            }, runFinalIteration: false);

            // Assert
            var log = (ICollection)typeof(OperationLog)
                .GetField("_log", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
            Assert.That(log.Count, Is.EqualTo(0));

            var domain = (ICollection<double>)typeof(PositronicVariable<double>)
                .GetField("_domain", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(v);
            Assert.That(domain, Is.EquivalentTo(new[] { v.ToValues().Single() }));
        }

        #endregion

        #region IOAndFormatting

        [Test]
        public void ScalarAssign_InLoop_ShouldNotTouchBootstrap()
        {
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            var v = PositronicVariable<int>.GetOrCreate("p", 0, _runtime);

            PositronicVariable<int>.RunConvergenceLoop(_runtime, () => v.Assign(v + 1),
                                                       runFinalIteration: false);

            var firstSlice = v.GetSlice(v.TimelineLength - 1)         // slice[0]
                              .ToCollapsedValues().Single();
            Assert.That(firstSlice, Is.EqualTo(0));                    // should still be 0
        }


        [Test]
        public void ConvergenceLoop_ScalarWrites_Always_AppendDistinctSlices()
        {
            // Arrange: start at negative time so we see all slices
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            var v = PositronicVariable<int>.GetOrCreate("sliceTest", 0, _runtime);

            // Act: run one full negative→positive pass, but don’t unify
            PositronicVariable<int>.RunConvergenceLoop(
                _runtime,
                () =>
                {
                    v.Assign(v + 1);   // should snapshot 1
                    v.Assign(v + 2);   // should snapshot 3 (1+2)
                    v.Assign(v + 3);   // should snapshot 6 (3+3)   
                },
                runFinalIteration: false,
                unifyOnConvergence: false,
                bailOnFirstReverseWhenIdle: false);

            // Grab the private timeline via reflection (or use your Snapshot helper)
            var timelineField = typeof(PositronicVariable<int>)
                .GetField("timeline", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            var slices = (List<QuBit<int>>)timelineField.GetValue(v);

            // Assert: we expect four slices: [0], [1], [3], [6]
            Assert.That(slices.Select(qb => qb.ToCollapsedValues().Single()),
                        Is.EqualTo(new[] { 0, 1, 3, 6 }),
                        "Every scalar write inside the convergence loop must produce its own new slice.");
        }

        [Test]
        public void SimpleProgram_BackwardsAssignments_Prints12()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            PositronicVariable<double>.RunConvergenceLoop(_runtime, () =>
            {
                var temp = PositronicVariable<double>.GetOrCreate("temp", 0, _runtime);
                Console.WriteLine($"The temperature in c is {temp}");
                temp.Assign(temp + 1);
                temp.Assign(temp + 1);
                temp.Assign(10);
            });

            // Assert
            Console.Out.Flush();
            Assert.That(output.ToString().TrimEnd(),
                        Is.EqualTo("The temperature in c is any(12)"));
        }

        [Test]
        public void FirstForwardScalar_Appends_NotMerges()
        {
            var v = PositronicVariable<int>.GetOrCreate("probe", 0, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, +1);   // forward, outside loop

            v.Assign(1);                                        // scalar write

            Assert.That(v.TimelineLength, Is.EqualTo(2));
            Assert.That(v.GetSlice(1).ToCollapsedValues().Single(), Is.EqualTo(0));
            Assert.That(v.GetCurrentQBit().ToCollapsedValues().Single(), Is.EqualTo(1));
        }


        [Test]
        public void BackwardsAssignmentOfScalarPlusAdds_YieldsCorrectAntiValue()
        {
            // Arrange
            var v = PositronicVariable<int>.GetOrCreate("x", 0, _runtime);

            // forward‐only pass: record +2, +3, then scalar assign 7
            PositronicVariable<int>.SetEntropy(_runtime, +1);
            v.Assign(v + 2);
            v.Assign(v + 3);
            v.Assign(7);

            // now go backwards
            PositronicVariable<int>.SetEntropy(_runtime, -1);
            // trigger the replay
            v.Assign(v.GetCurrentQBit());

            // Act
            var result = v.GetCurrentQBit().ToCollapsedValues().Single();

            // Assert:  7 + 3 + 2  ⇒ 12
            Assert.That(result, Is.EqualTo(12),
                "Backward‐replay should invert the +2 and +3 before the scalar assignment, giving 12");
        }


        [Test]
        public void UnifiedQuBit_ToString_ShouldDisplayFullUnion()
        {
            var qb = new QuBit<int>(new[] { 1, 0, 2 });
            qb.Any();

            var text = qb.ToString();

            Assert.That(text, Does.Contain("any("));
            Assert.That(text, Does.Contain("0"));
            Assert.That(text, Does.Contain("1"));
            Assert.That(text, Does.Contain("2"));
        }

        #endregion

        #region SpecialCases

        [Test]
        public void Antival_CyclesThrough0_1_2_AndEventuallyConvergesToAllThree()
        {
            var v = PositronicVariable<int>.GetOrCreate("a", -1, _runtime);

            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                v.Assign((v + 1) % 3);
            });

            var result = v.ToValues().OrderBy(x => x).ToList();
            Assert.That(result, Is.EquivalentTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void ZeroConvergence_NoRepeatedStates_AlwaysUnique()
        {
            var v = PositronicVariable<int>.GetOrCreate("z", 10, _runtime);
            PositronicVariable<int>.SetEntropy(_runtime, -1);

            for (int i = 0; i < 10; i++)
                v.Assign((v + 2) % 9999);

            Assert.That(v.Converged(), Is.EqualTo(0));
        }

        #endregion
    }

    [TestFixture]
    public class PositronicQuickPathBugTests
    {
        private ServiceProvider _provider;
        private IPositronicRuntime _runtime;

        [SetUp]
        public void SetUp()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices(s => s.AddPositronicRuntime());

            PositronicAmbient.InitialiseWith(hostBuilder);
            _runtime = PositronicAmbient.Current;

            QuantumConfig.ForbidDefaultOnCollapse = true;
            OperationLog.Clear();
            if (_runtime.OracularStream is StringWriter sw)
                sw.GetStringBuilder().Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.ResetAmbient();
        }


        [Test]
        public void QuickPath_SecondAssign_ShouldMergeNotOverwrite()
        {
            // forward-only context, *outside* convergence-loop
            PositronicVariable<int>.SetEntropy(_runtime, +1);

            var v = PositronicVariable<int>.GetOrCreate("q", 0, _runtime);
            v.Assign(1);          // should yield any(0,1)
            v.Assign(2);          // should yield any(0,1,2)

            var values = v.ToValues().OrderBy(x => x).ToArray();

            Assert.That(values, Is.EquivalentTo(new[] { 0, 1, 2 }),
                "Quick-path forward writes overwrite instead of merging.");
        }

        [Test]
        public void SecondForwardScalar_ShouldUnionWithExistingSlice()
        {
            PositronicVariable<int>.SetEntropy(_runtime, +1);
            var v = PositronicVariable<int>.GetOrCreate("q", 0, _runtime);
            v.Assign(1); 
            v.Assign(2); 

            Assert.That(v.GetCurrentQBit().ToCollapsedValues(),
                        Is.EquivalentTo(new[] { 0, 1, 2 }));
        }


        [Test]
        public void QuickPath_WritesAreNotRolledBackByFinalUndo()
        {
            // forward-only, single assignment
            PositronicVariable<int>.SetEntropy(_runtime, +1);
            var v = PositronicVariable<int>.GetOrCreate("x", 0, _runtime);
            v.Assign(1);

            // simulate what ConvergenceEngine does at the *very* end:
            OperationLog.ReverseLastOperations();

            // If the write had been logged, Undo() would have restored the 0.
            var final = v.ToValues().Single();


            Assert.That(final, Is.EqualTo(0),
                "The final reverse pass did not undo the quick-path overwrite—" +
                "suggests the write was never recorded.");
        }

        [Test]
        public void UnifyCycle_FailsOnlyBecauseEarlierValuesWereDropped()
        {
            //  replicate exactly the original arrange
            var v = PositronicVariable<int>.GetOrCreate("probe", 0, _runtime);
            v.Assign(1);
            v.Assign(2);               // timeline intended to be [0,1,2]

            //  sanity-check before calling Unify
            var before = v.ToValues().OrderBy(x => x).ToArray();
            Assert.That(before, Is.EquivalentTo(new[] { 0, 1, 2 }),
                "Pre-condition failed: values already missing *before* Unify().");

            //  call the API under test
            v.Unify(3);

            //  it should still be the same set
            var after = v.ToValues().OrderBy(x => x).ToArray();
            Assert.That(after, Is.EquivalentTo(new[] { 0, 1, 2 }),
                "Unify(count) must preserve the whole set.");
        }
    }
}
