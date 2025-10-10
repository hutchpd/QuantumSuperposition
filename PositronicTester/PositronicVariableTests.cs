using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NUnit.Framework;
using QuantumSuperposition.Core;
using QuantumSuperposition.DependencyInjection;
using QuantumSuperposition.QuantumSoup;

namespace PositronicVariables.Tests
{

    [SetUpFixture]
    public sealed class EnableAttributedExitHook
    {
        [OneTimeSetUp]
        public void Init()
        {
            PositronicAttributedEntryRunner.EnableProcessExitRunner();
        }
    }

    /// <summary>
    /// Test-only, opt-in reflection runner for a single [PositronicEntry].
    /// Nothing runs unless you call EnableProcessExitRunner() or RunOnce().
    /// </summary>
    public static class PositronicAttributedEntryRunner
    {
        /// <summary>
        /// Opt-in: attach a ProcessExit hook that runs the attributed entry point
        /// and then flushes converged output to the original Console.Out.
        /// Call this from a [SetUpFixture] OneTimeSetUp or similar.
        /// </summary>
        public static void EnableProcessExitRunner()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, __) =>
            {
                EmitOnceIfNeeded();
            };
        }

        /// <summary>
        /// Exposed for tests: perform the ProcessExit emission once,
        /// but only if the engine hasn't already flushed.
        /// </summary>
        public static void EmitOnceIfNeeded()
        {
            // If the engine already flushed to the real console, don’t print again.
            if (AethericRedirectionGrid.SuppressEndOfUniverseReading)
                return;

            RunOnce();

            // restore and flush captured content if tests redirected it
            var real = AethericRedirectionGrid.ReferenceUniverse;
            Console.SetOut(real);
            real.Write(AethericRedirectionGrid.ImprobabilityDrive.ToString());
            real.Flush();
        }


        /// <summary>
        /// Run the single [PositronicEntry] method inside the convergence loop,
        /// writing converged output into AethericRedirectionGrid.OutputBuffer.
        /// </summary>
        public static void RunOnce()
        {
            EnsureRuntime();

            var entryPoints = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(IsCandidateAssembly) // avoid VS TestPlatform & friends
                .SelectMany(asm =>
                {
                    try { return asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
                    catch (TypeLoadException) { return Array.Empty<Type>(); }
                })
                .SelectMany(t =>
                {
                    try { return t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); }
                    catch { return Array.Empty<MethodInfo>(); }
                })
                .Where(SafeHasPositronicEntryAttribute)
                .ToList();

            if (entryPoints.Count == 0)
                throw new InvalidOperationException("No method marked with [PositronicEntry] was found.");
            if (entryPoints.Count > 1)
            {
                var names = string.Join(Environment.NewLine, entryPoints.Select(m => $" - {m.DeclaringType!.FullName}.{m.Name}"));
                throw new InvalidOperationException($"Multiple [PositronicEntry] methods found:{Environment.NewLine}{names}");
            }

            var entryPoint = entryPoints[0];
            var rt = PositronicAmbient.Current;

            // Probe once (reverse) so the entry can register a variable and we can infer T.
            if (!rt.Registry.Any())
            {
                var saved = rt.Entropy;
                rt.Entropy = -1;
                // Mute console during the probe so non-converged output doesn't leak.
                var originalOut = Console.Out;
                try
                {
                    Console.SetOut(TextWriter.Null);
                    var p = entryPoint.GetParameters();
                    object?[]? args = p.Length == 0 ? null : new object?[] { Array.Empty<string>() };
                    entryPoint.Invoke(null, args);
                }
                finally
                {
                    Console.SetOut(originalOut);
                    rt.Entropy = saved;
                }
            }
            if (!rt.Registry.Any())
                throw new InvalidOperationException("No PositronicVariable was registered by the entry point.");

            var lastVar = rt.Registry.Last();
            var genericArg = lastVar.GetType().GetGenericArguments()[0];

            var runMethod = typeof(PositronicVariable<>)
                .MakeGenericType(genericArg)
                .GetMethod("RunConvergenceLoop", BindingFlags.Public | BindingFlags.Static);

            runMethod!.Invoke(
                null,
                new object[]
                {
                    rt,
                    (Action)(() =>
                    {
                        var p = entryPoint.GetParameters();
                        object?[]? args = p.Length == 0 ? null : new object?[] { Array.Empty<string>() };
                        entryPoint.Invoke(null, args);
                    }),
                    true,   // runFinalIteration
                    true,   // unifyOnConvergence
                    false   // bailOnFirstReverseWhenIdle
                });
        }
        private static bool IsCandidateAssembly(Assembly asm)
        {
            try
            {
                var name = asm.GetName().Name ?? string.Empty;
                // Limit scan to product assemblies; skip test containers & test hosts.
                return (name.StartsWith("QuantumSuperposition", StringComparison.OrdinalIgnoreCase)
                        || name.StartsWith("Positronic", StringComparison.OrdinalIgnoreCase)
                        || name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
                       && !name.Contains("TestPlatform", StringComparison.OrdinalIgnoreCase)
                       && !name.Contains("TestHost", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static bool SafeHasPositronicEntryAttribute(MethodInfo m)
        {
            try
            {
                // use metadata view to avoid instantiating attributes
                foreach (var cad in CustomAttributeData.GetCustomAttributes(m))
                {
                    var at = cad.AttributeType;
                    if (at != null && at == typeof(DontPanicAttribute))
                        return true;
                }
                return false;
            }
            catch (ReflectionTypeLoadException) { return false; }
            catch (TypeLoadException) { return false; }
            catch (FileNotFoundException) { return false; }
        }


        private static void EnsureRuntime()
        {
            if (PositronicAmbient.IsInitialized)
                return;

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(s => s.AddPositronicRuntime());
            PositronicAmbient.InitialiseWith(hostBuilder);
        }
    }

    // Small attributed program we can "run" via the test hook:
    public static class AttributedProgramForTests
    {
        [DontPanic]
        public static void EntryPointForTests()
        {
            var antival = AntiVal.GetOrCreate<double>();
            Console.WriteLine($"The antival is {antival}");
            var val = (antival + 1) % 3;
            Console.WriteLine($"The value is {val}");
            antival.Assign(val);
        }
    }



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
            QuantumLedgerOfRegret.Clear();
            if (_runtime.Babelfish is StringWriter sw)
                sw.GetStringBuilder().Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.PanicAndReset();
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
        public void OutputRedirector_Restores_And_Flushes()
        {
            // arrange: create a fresh runtime & redirector
            var rt = new DefaultPositronicRuntime(
                new ScopedPositronicVariableFactory(
                    new ServiceCollection().BuildServiceProvider()),
                new ScopedPositronicVariableFactory(
                    new ServiceCollection().BuildServiceProvider()));
            var redirector = new SubEthaOutputTransponder(rt);

            // arrange: capture anything written to Console.Out
            var output = new StringWriter();
            Console.SetOut(output);

            // act: redirect into the grid buffer, write, then restore
            redirector.Redirect();
            Console.WriteLine("hello");
            redirector.Restore();

            // assert: our StringWriter got the “hello” line
            var text = output.ToString();
            Assert.That(text, Does.Contain("hello"));
        }


        [Test]
        public void Program_Main_Integration_CapturesAllAntivalStates()
        {
            // fresh world + empty buffer
            PositronicAmbient.PanicAndReset();
            AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();

            // ensure writes go to our buffer (NUnit may reset Console.Out)
            Console.SetOut(AethericRedirectionGrid.ImprobabilityDrive);

            // run the attributed entry once (test-only, opt-in)
            PositronicAttributedEntryRunner.RunOnce();

            var full = AethericRedirectionGrid.ImprobabilityDrive
                .ToString()
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            Assert.That(full.ElementAtOrDefault(0), Does.Contain("The antival is any(0, 1, 2)"));
            Assert.That(full.ElementAtOrDefault(1), Does.Contain("The value is any(1, 2, 0)"));
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
            QuantumLedgerOfRegret.ReverseLastOperations();
            v.Assign(v);

            // Assert
            Assert.That(v.ToValues().Single(), Is.EqualTo(-1));
        }

        private void RunStep(Action code, int entropy)
        {
            PositronicVariable<int>.SetEntropy(_runtime, entropy);
            code();
            if (entropy < 0)
                QuantumLedgerOfRegret.ReverseLastOperations();
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
            var log = (ICollection)typeof(QuantumLedgerOfRegret)
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

            // Assert:  7 + 3 + 2  => 12
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
        public void Antival_CyclesThrough0_1_AndEventuallyConvergesToBoth()
        {
            var v = PositronicVariable<int>.GetOrCreate("a", -1, _runtime);

            PositronicVariable<int>.RunConvergenceLoop(_runtime, () =>
            {
                v.Assign((v + 1) % 2);
            });

            var result = v.ToValues().OrderBy(x => x).ToList();
            Assert.That(result, Is.EquivalentTo(new[] { 0, 1 }));
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
            QuantumLedgerOfRegret.Clear();
            if (_runtime.Babelfish is StringWriter sw)
                sw.GetStringBuilder().Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.PanicAndReset();
        }


        [Test]
        public void QuickPath_SecondAssign_ShouldMergeNotOverwrite()
        {
            // forward-only context, *outside* convergence-loop
            PositronicVariable<int>.SetEntropy(_runtime, +1);

            var v = PositronicVariable<int>.GetOrCreate("q", 0, _runtime);
            v.Assign(1);
            v.Assign(2);

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
            QuantumLedgerOfRegret.ReverseLastOperations();

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

        [Test]
        public void AttributedEntry_PrintsExactlyOnce_WithExitHookEnabled()
        {
            // Fresh world + clean buffer + capture console
            PositronicAmbient.PanicAndReset();
            AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();

            var output = new StringWriter();
            Console.SetOut(output);

            // Run the attributed entry once via the normal runner (engine will flush + set SuppressProcessExitEmission)
            PositronicAttributedEntryRunner.RunOnce();

            // Simulate what our test ProcessExit hook would do
            //    (after the patch, this helper checks the suppression flag)
            PositronicAttributedEntryRunner.EmitOnceIfNeeded();

            var lines = output.ToString()
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Exactly two lines, not doubled
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0], Does.StartWith("The antival is any("));
            Assert.That(lines[1], Does.StartWith("The value is any("));
        }


        [Test]
        public void Simple_Backwards_Assignments_Print12()
        {
            var output = new StringWriter();
            Console.SetOut(output);

            PositronicVariable<double>.RunConvergenceLoop(PositronicAmbient.Current, () =>
            {
                var val = AntiVal.GetOrCreate<double>(); // or PositronicVariable<double>.GetOrCreate("val", 0, rt);
                Console.WriteLine($"The result will be {val}");
                val.Assign(val + 2);
                val.Assign(val + 2);
                val.Assign(10);
            });

            Assert.That(output.ToString().Trim(), Is.EqualTo("The result will be any(14)"));
        }

    }
}
