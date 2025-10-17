using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class CrossVariableReplayTests
    {
        private IPositronicRuntime _rt;

        [SetUp]
        public void SetUp()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices(s => s.AddPositronicRuntime());
            PositronicAmbient.InitialiseWith(hostBuilder);
            _rt = PositronicAmbient.Current;

            QuantumLedgerOfRegret.Clear();
            if (_rt.Babelfish is StringWriter sw)
                sw.GetStringBuilder().Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.PanicAndReset();
        }

        // Console-style two-variable program should print any(11) any(22)
        [Test]
        public void ConsoleStyle_DoubleRun_TwoVariables_Prints_11_and_22()
        {
            PositronicAmbient.PanicAndReset();

            static void ProgramBody()
            {
                var antival = PositronicVariable<int>.GetOrCreate("antival", 0);
                var antival2 = PositronicVariable<int>.GetOrCreate("antival2", 0);

                Console.WriteLine($"The antivals are {antival} {antival2}");
                antival.State = antival.State + 1;   // +1 then force 10 => print-site 11
                antival.Scalar = 10;

                // Cross-variable: antival2 from antival (+2), then force 20 => print-site 22
                antival2.State = antival.State + 2;
                antival2.Scalar = 20;
            }

            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                Console.SetOut(sw);
                ProgramBody(); // outside the loop
                PositronicVariable<int>.RunConvergenceLoop(PositronicAmbient.Current, ProgramBody);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var lines = sw.ToString()
                          .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var last = lines.Last(l => l.StartsWith("The antivals are"));
            Assert.That(last.Trim(), Is.EqualTo("The antivals are any(11) any(22)"));
        }

        // Cross-variable addition (+2)
        [Test]
        public void CrossVariable_Addition_LogsAgainst_SourceVariable_NotTarget()
        {
            var a = PositronicVariable<int>.GetOrCreate("antival", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("antival2", 0, _rt);

            PositronicVariable<int>.SetEntropy(_rt, +1);
            QuantumLedgerOfRegret.Clear();

            var expr = a.State + 2; 
            b.Assign(expr);

            var op = PeekLastOperation();
            Assert.That(op, Is.Not.Null, "Expected at least one operation to be recorded.");

            var opVar = TryGetOperationVariable(op);
            Assert.That(opVar, Is.Not.Null, "Could not locate the variable captured by the operation via reflection.");

            Assert.That(ReferenceEquals(opVar, a), Is.True,
                "Cross-variable addition should not be recorded against the source variable.");
        }

        [Test]
        public void CrossVariable_ReverseReplay_DoesNotMutate_SourceVariable()
        {
            var a = PositronicVariable<int>.GetOrCreate("antival", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("antival2", 0, _rt);

            QuantumLedgerOfRegret.Clear();

            // Forward: force end-states
            PositronicVariable<int>.SetEntropy(_rt, +1);
            a.Assign(10);
            b.Assign(20);

            var expr = a + 2;

            PositronicVariable<int>.SetEntropy(_rt, -1);

            var beforeA = a.ToValues().Single();
            b.Assign(expr); 
            var afterA = a.ToValues().Single();

            Assert.That(afterA, Is.EqualTo(beforeA),
                "Cross-variable reverse replay should not mutate the source variable.");
        }

        [Test]
        public void CrossVariable_ReverseReplay_Target_Reconstructs_To_22()
        {
            var a = PositronicVariable<int>.GetOrCreate("antival", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("antival2", 0, _rt);

            QuantumLedgerOfRegret.Clear();

            PositronicVariable<int>.SetEntropy(_rt, +1);
            a.Assign(10); 
            b.Assign(20);

            var expr = a + 2; 

            PositronicVariable<int>.SetEntropy(_rt, -1);
            b.Assign(expr); // reverse-append into b

            var bNow = b.ToValues().ToArray();
            Assert.That(bNow, Does.Contain(22),
                "Expected antival2 to include 22 during reverse reconstruction from final 20 via (antival + 2).");
        }

        [Test]
        public void PrePrintSnapshots_TwoVariables_AreScalar_AndExpected()
        {
            var antival = PositronicVariable<int>.GetOrCreate("antival", 0, _rt);
            var antival2 = PositronicVariable<int>.GetOrCreate("antival2", 0, _rt);

            var prePrintA = new System.Collections.Generic.List<int[]>();
            var prePrintB = new System.Collections.Generic.List<int[]>();

            void ProgramBody()
            {
                prePrintA.Add(antival.GetCurrentQBit().ToCollapsedValues().ToArray());
                prePrintB.Add(antival2.GetCurrentQBit().ToCollapsedValues().ToArray());

                var _ = $"The antivals are {antival} {antival2}";

                antival.State = antival.State + 1;
                antival.Assign(10);

                // Cross-variable: derive b from a (+2), then force 20
                antival2.State = antival.State + 2;
                antival2.Assign(20);
            }

            ProgramBody(); // outside loop
            PositronicVariable<int>.RunConvergenceLoop(_rt, ProgramBody, runFinalIteration: true, unifyOnConvergence: true);

            var lastA = prePrintA.Last();
            var lastB = prePrintB.Last();

            Assert.That(lastA.Length, Is.EqualTo(1), "antival pre-print slice should be scalar.");
            Assert.That(lastB.Length, Is.EqualTo(1), "antival2 pre-print slice should be scalar.");

            Assert.That(lastA[0], Is.EqualTo(11), "antival pre-print should be 11 (10 back through +1).");
            Assert.That(lastB[0], Is.EqualTo(22), "antival2 pre-print should be 22 (20 back through (antival + 2)).");
        }

        [Test]
        public void CrossVariable_ReverseWrite_ReplacesWithinEpoch_NotAppend()
        {
            var rt = PositronicAmbient.Current;
            var a = PositronicVariable<int>.GetOrCreate("a", 0, rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, rt);

            PositronicVariable<int>.SetEntropy(rt, +1);
            a.Assign(10);
            b.Assign(20);

            PositronicVariable<int>.SetEntropy(rt, -1);
            b.Assign(a + 2);
            var len1 = b.TimelineLength;
            b.Assign(a + 2);
            var len2 = b.TimelineLength;

            Assert.That(len2, Is.EqualTo(len1),
                "Cross-variable reverse assignment should replace the last slice in the same epoch, not append.");
        }

        [Test]
        public void CrossVariable_ReverseReplay_InLoop_DoesNotMutate_Source()
        {
            var rt = PositronicAmbient.Current;
            var a = PositronicVariable<int>.GetOrCreate("a", 0, rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, rt);

            PositronicVariable<int>.SetEntropy(rt, +1);
            a.Assign(10);
            b.Assign(20);

            PositronicVariable<int>.RunConvergenceLoop(rt, () =>
            {
                if (PositronicVariable<int>.GetEntropy(rt) < 0)
                {
                    var before = a.ToValues().Single();
                    b.Assign(a + 2);
                    var after = a.ToValues().Single();
                    Assert.That(after, Is.EqualTo(before), "Source variable changed during cross reverse replay.");
                    rt.Converged = true;
                }
            }, runFinalIteration: false, unifyOnConvergence: false);
        }

        [Test]
        public void CrossVariable_Forward_InLoop_DoesNotPollute_Target()
        {
            var rt = PositronicAmbient.Current;
            var a = PositronicVariable<int>.GetOrCreate("a", 0, rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, rt);

            PositronicVariable<int>.SetEntropy(rt, +1);
            a.Assign(10);
            b.Assign(20);

            PositronicVariable<int>.RunConvergenceLoop(rt, () =>
            {
                if (PositronicVariable<int>.GetEntropy(rt) > 0)
                {
                    b.Assign(a + 2);

                    var vals = b.GetCurrentQBit().ToCollapsedValues().ToArray();
                    Assert.That(vals, Does.Not.Contain(2), "Forward half-cycle polluted target with raw k-shift.");
                    rt.Converged = true;
                }
            }, runFinalIteration: false, unifyOnConvergence: false);
        }

        [Test, Explicit("Diagnostics")]
        public void Trace_Timelines_AndEpochs_TwoVariables_Dump()
        {
            var rt = PositronicAmbient.Current;
            var a = PositronicVariable<int>.GetOrCreate("antival", 0, rt);
            var b = PositronicVariable<int>.GetOrCreate("antival2", 0, rt);

            var sb = new StringBuilder();
            sb.AppendLine("=== Diagnostic trace: timelines and epoch stamps per half-cycle ===");

            void Dump(string label)
            {
                int entropy = PositronicVariable<int>.GetEntropy(rt);

                var aStates = GetTimelineStates(a);
                var bStates = GetTimelineStates(b);
                var aEpochs = GetEpochs(a);
                var bEpochs = GetEpochs(b);

                int logDepth = GetOpLogDepth();

                sb.AppendLine($"[{label}] Entropy={(entropy > 0 ? "FWD" : "REV")}, LogDepth={logDepth}");
                sb.AppendLine($"  antival.timeline      = {FormatTimeline(aStates)}");
                sb.AppendLine($"  antival._sliceEpochs  = [{string.Join(", ", aEpochs)}]");
                sb.AppendLine($"  antival2.timeline     = {FormatTimeline(bStates)}");
                sb.AppendLine($"  antival2._sliceEpochs = [{string.Join(", ", bEpochs)}]");
            }

            void ProgramBody()
            {
                Dump("pre-print");
                var _ = $"The antivals are {a} {b}";

                a.State = a.State + 1; // +1 then force 10 => print-site 11
                a.Scalar = 10;

                // Cross-variable (important for this diagnostic)
                b.State = a.State + 2;
                b.Scalar = 20;

                Dump("post-mutations");
            }

            ProgramBody(); // outside loop
            PositronicVariable<int>.RunConvergenceLoop(rt, ProgramBody, runFinalIteration: true, unifyOnConvergence: true);

            TestContext.WriteLine(sb.ToString());

            Assert.That(a.timeline.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(b.timeline.Count, Is.GreaterThanOrEqualTo(1));
        }

        private static string FormatTimeline(IReadOnlyList<int[]> slices)
        {
            // Example: [(0)] [(0, 2)] [(20)] ...
            return string.Join(" ",
                slices.Select(s => $"({string.Join(", ", s)})")
                      .Select(s => $"[{s}]"));
        }

        private static IReadOnlyList<int[]> GetTimelineStates(PositronicVariable<int> v)
        {
            // reflect public field 'timeline' then expand each QuBit into its collapsed values
            var timelineField = typeof(PositronicVariable<int>)
                .GetField("timeline", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var slices = (IEnumerable)timelineField!.GetValue(v);
            var result = new List<int[]>();
            foreach (var qb in slices)
            {
                var toVals = qb.GetType().GetMethod("ToCollapsedValues", BindingFlags.Public | BindingFlags.Instance);
                var vals = ((IEnumerable<int>)toVals!.Invoke(qb, null)).ToArray();
                result.Add(vals);
            }
            return result;
        }

        private static IReadOnlyList<int> GetEpochs(PositronicVariable<int> v)
        {
            var epochsField = typeof(PositronicVariable<int>)
                .GetField("_sliceEpochs", BindingFlags.NonPublic | BindingFlags.Instance);
            var epochs = (IEnumerable<int>)epochsField!.GetValue(v);
            return epochs.ToArray();
        }

        private static int GetOpLogDepth()
        {
            var logField = typeof(QuantumLedgerOfRegret).GetField("_log", BindingFlags.NonPublic | BindingFlags.Static);
            var log = (IEnumerable)logField!.GetValue(null);
            int count = 0;
            foreach (var _ in log) count++;
            return count;
        }


        private static object PeekLastOperation()
        {
            var logField = typeof(QuantumLedgerOfRegret).GetField("_log", BindingFlags.NonPublic | BindingFlags.Static);
            var log = (IEnumerable)logField!.GetValue(null);
            return log.Cast<object>().LastOrDefault();
        }

        private static object TryGetOperationVariable(object op)
        {
            if (op is null) return null;
            var fields = op.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                var val = f.GetValue(op);
                if (val is IPositronicVariable) return val;
            }
            return null;
        }

        [Test]
        public void CrossVariable_AddConst_OverLastScalar_ShiftsTarget()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            // forward
            PositronicVariable<int>.SetEntropy(_rt, +1);
            a.Assign(10);
            b.Assign(20);

            PositronicVariable<int>.SetEntropy(_rt, -1);
            b.Assign(a + 2);

            var vals = b.ToValues().OrderBy(x => x).ToArray();
            Assert.That(vals, Is.EquivalentTo(new[] { 22 }));
        }

        [Test, Explicit("Diagnostics")]
        public void SelfFeedback_Forward_WithFollowingScalar_DoesNotAppend()
        {
            var rt = PositronicAmbient.Current;
            var b = PositronicVariable<int>.GetOrCreate("b", 0, rt);

            PositronicVariable<int>.RunConvergenceLoop(rt, () =>
            {
                if (PositronicVariable<int>.GetEntropy(rt) > 0)
                {
                    var startLen = b.TimelineLength;
                    b.Assign(b + 2);   // self-feedback
                    b.Assign(20);      // scalar overwrite in same forward half-cycle
                    var endLen = b.TimelineLength;

                    // We expect either a replace or a no-op for the self-feedback slice in forward-pass,
                    // but no growth purely from the (+2) when followed by a scalar in the same half-cycle.
                    Assert.That(endLen - startLen, Is.LessThanOrEqualTo(1));
                    rt.Converged = true;
                }
            }, runFinalIteration: false, unifyOnConvergence: false);
        }

        // Add inside the CrossVariableReplayTests class
        [Test, Explicit("Deep diagnostics")]
        public void DeepTrace_Epochs_Baselines_And_OpLog()
        {
            var rt = PositronicAmbient.Current;
            var a = PositronicVariable<int>.GetOrCreate("antival", 0, rt);
            var b = PositronicVariable<int>.GetOrCreate("antival2", 0, rt);

            var sb = new StringBuilder();
            sb.AppendLine("=== Deep diagnostic: epochs, baselines, op-log ===");

            void Dump(string label)
            {
                int entropy = PositronicVariable<int>.GetEntropy(rt);
                int epoch = GetStaticInt(typeof(PositronicVariable<int>), "s_CurrentEpoch");
                int lastEntSeen = GetStaticInt(typeof(PositronicVariable<int>), "s_LastEntropySeenForEpoch");
                bool revStarted = GetStaticBool(typeof(PositronicVariable<int>), "_reverseReplayStarted");

                var aStates = GetTimelineStates(a);
                var bStates = GetTimelineStates(b);
                var aEpochs = GetEpochs(a);
                var bEpochs = GetEpochs(b);

                var (aHasBase, aBase) = GetForwardBaseline(a);
                var (bHasBase, bBase) = GetForwardBaseline(b);

                sb.AppendLine($"[{label}] Entropy={(entropy > 0 ? "FWD" : "REV")}, Epoch={epoch}, LastEntropySeen={lastEntSeen}, ReverseReplayStarted={revStarted}");
                sb.AppendLine($"  antival.timeline      = {FormatTimeline(aStates)}");
                sb.AppendLine($"  antival._sliceEpochs  = [{string.Join(", ", aEpochs)}]");
                sb.AppendLine($"  antival.CurrentQBit   = [{string.Join(", ", a.GetCurrentQBit().ToCollapsedValues())}]");
                sb.AppendLine($"  antival.ForwardBaseline = {(aHasBase ? aBase.ToString() : "(none)")}");

                sb.AppendLine($"  antival2.timeline     = {FormatTimeline(bStates)}");
                sb.AppendLine($"  antival2._sliceEpochs = [{string.Join(", ", bEpochs)}]");
                sb.AppendLine($"  antival2.CurrentQBit  = [{string.Join(", ", b.GetCurrentQBit().ToCollapsedValues())}]");
                sb.AppendLine($"  antival2.ForwardBaseline = {(bHasBase ? bBase.ToString() : "(none)")}");

                DumpOpLog(sb, a, b);
            }

            void ProgramBody()
            {
                Dump("pre-print");
                var _ = $"The antivals are {a} {b}";

                // a: +1 then force 10 => print-site should be 11
                a.State = a.State + 1;
                a.Scalar = 10;

                // b: derive from a (+2), then force 20 => print-site should be 22
                b.State = a.State + 2;
                b.Scalar = 20;

                Dump("post-mutations");
            }

            // Outside-the-loop run
            ProgramBody();
            // In-loop run
            PositronicVariable<int>.RunConvergenceLoop(rt, ProgramBody, runFinalIteration: true, unifyOnConvergence: true);

            TestContext.WriteLine(sb.ToString());

            // sanity: keep variables alive
            Assert.That(a.timeline.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(b.timeline.Count, Is.GreaterThanOrEqualTo(1));
        }

        // ------- helpers (re-use your existing ones; add these locally) -------

        private static (bool has, int baseline) GetForwardBaseline(PositronicVariable<int> v)
        {
            var t = typeof(PositronicVariable<int>);
            var fHas = t.GetField("_hasForwardScalarBaseline", BindingFlags.NonPublic | BindingFlags.Instance);
            var fBase = t.GetField("_forwardScalarBaseline", BindingFlags.NonPublic | BindingFlags.Instance);

            bool has = (bool)(fHas?.GetValue(v) ?? false);
            int baseline = has ? (int)(fBase!.GetValue(v) ?? 0) : 0;
            return (has, baseline);
        }

        private static int GetStaticInt(Type t, string fieldName)
        {
            var f = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return (int)(f?.GetValue(null) ?? 0);
        }

        private static bool GetStaticBool(Type t, string fieldName)
        {
            var f = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return (bool)(f?.GetValue(null) ?? false);
        }

        private static void DumpOpLog(StringBuilder sb, PositronicVariable<int> a, PositronicVariable<int> b)
        {
            var logField = typeof(QuantumLedgerOfRegret).GetField("_log", BindingFlags.NonPublic | BindingFlags.Static);
            var log = ((IEnumerable)logField!.GetValue(null)).Cast<object>().ToArray();
            sb.AppendLine($"  OpLog (depth={log.Length}):");

            for (int i = 0; i < log.Length; i++)
            {
                var entry = log[i];
                string type = entry?.GetType().Name ?? "(null)";

                string target = "";
                var opVar = TryGetOperationVariable(entry);
                if (ReferenceEquals(opVar, a)) target = " [antival]";
                else if (ReferenceEquals(opVar, b)) target = " [antival2]";

                // Try to show added/replaced slice values if available
                string extra = "";
                var addedSliceProp = entry?.GetType().GetProperty("AddedSlice", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var replacedSliceProp = entry?.GetType().GetProperty("ReplacedSlice", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (addedSliceProp != null)
                {
                    var qb = addedSliceProp.GetValue(entry);
                    var toVals = qb?.GetType().GetMethod("ToCollapsedValues", BindingFlags.Public | BindingFlags.Instance);
                    var vals = toVals != null ? string.Join(", ", ((IEnumerable<int>)toVals.Invoke(qb, null)).ToArray()) : "";
                    extra = $" Added=[{vals}]";
                }
                else if (replacedSliceProp != null)
                {
                    var qb = replacedSliceProp.GetValue(entry);
                    var toVals = qb?.GetType().GetMethod("ToCollapsedValues", BindingFlags.Public | BindingFlags.Instance);
                    var vals = toVals != null ? string.Join(", ", ((IEnumerable<int>)toVals.Invoke(qb, null)).ToArray()) : "";
                    extra = $" Replaced=[{vals}]";
                }

                sb.AppendLine($"    {i,2}: {type}{target}{extra}");
            }
        }
    }
}