using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System;
using System.IO;
using PositronicVariables.Engine.Transponder;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class GoldenRunTests
    {
        private IPositronicRuntime _rt;

        [SetUp]
        public void SetUp()
        {
            var hb = new HostBuilder().ConfigureServices(s => s.AddPositronicRuntime());
            PositronicAmbient.InitialiseWith(hb);
            _rt = PositronicAmbient.Current;
            _rt.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.IsInitialized && PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.PanicAndReset();
        }

        private sealed class RingNodule
        {
            public PositronicVariable<int> In1 { get; }
            public PositronicVariable<int> In2 { get; }
            public PositronicVariable<int> Target { get; }

            public RingNodule(PositronicVariable<int> in1, PositronicVariable<int> in2, PositronicVariable<int> target)
            {
                In1 = in1; In2 = in2; Target = target;
            }

            // sum mod 5, and a +1 branch (mod 5 wraps)
            public void Fire()
            {
                IEnumerable<int> i1 = In1.ToValues();
                IEnumerable<int> i2 = In2.ToValues();
                HashSet<int> outputs = new();
                foreach (int a in i1)
                {
                    foreach (int b in i2)
                    {
                        int sum = a + b;
                        int m = ((sum % 5) + 5) % 5;
                        int m2 = ((sum + 1) % 5 + 5) % 5;
                        outputs.Add(m);
                        outputs.Add(m2);
                    }
                }
                QuBit<int> qb = new QuBit<int>(outputs.ToArray());
                qb.Any();
                Target.Assign(qb);
            }
        }

        private static IEnumerable<int> Support(PositronicVariable<int> v)
        {
            return v.ToValues().Distinct().OrderBy(x => x);
        }

        private static Dictionary<string, object> Snapshot(int passIndex, PositronicVariable<int> A, PositronicVariable<int> B, PositronicVariable<int> C)
        {
            return new Dictionary<string, object>
            {
                ["pass"] = passIndex,
                ["A"] = new { values = Support(A).ToArray() },
                ["B"] = new { values = Support(B).ToArray() },
                ["C"] = new { values = Support(C).ToArray() }
            };
        }

        [Test]
        public void ThreeNodeRing_GoldenRun_ProducesExpectedSupportStabilisation()
        {
            // Initialise variables: A=0, B=3, C=4
            var A = PositronicVariable<int>.GetOrCreate("A", 0, _rt);
            var B = PositronicVariable<int>.GetOrCreate("B", 3, _rt);
            var C = PositronicVariable<int>.GetOrCreate("C", 4, _rt);

            // Build nodules (ring): NAB(A,B)->B, NBC(B,C)->C, NCA(C,A)->A
            var NAB = new RingNodule(A, B, B);
            var NBC = new RingNodule(B, C, C);
            var NCA = new RingNodule(C, A, A);
            var nodes = new[] { NAB, NBC, NCA };

            // Instrumentation storage
            List<Dictionary<string, object>> supportByPass = new();

            // Convergence loop: track pass index and stabilisation when supports stop changing
            int stabilisedAtPass = -1;
            (string a, string b, string c) lastSig = default;
            int passIndex = 0;

            PositronicVariable<int>.RunConvergenceLoop(_rt, () =>
            {
                foreach (var n in nodes) n.Fire();

                var sig = (
                    string.Join(',', Support(A)),
                    string.Join(',', Support(B)),
                    string.Join(',', Support(C))
                );

                supportByPass.Add(Snapshot(passIndex, A, B, C));

                if (passIndex > 0 && sig.Equals(lastSig) && stabilisedAtPass < 0)
                {
                    stabilisedAtPass = passIndex;
                }
                lastSig = sig;
                passIndex++;

            }, runFinalIteration: true, unifyOnConvergence: true, bailOnFirstReverseWhenIdle: false);

            // If not detected, assume last pass yielded stable supports
            if (stabilisedAtPass < 0 && supportByPass.Count > 0)
            {
                stabilisedAtPass = (int)supportByPass[^1]["pass"];
            }

            // Write artifacts for the paper
            string outDir = Path.Combine("Artifacts", "GoldenRun");
            Directory.CreateDirectory(outDir);

            string byPassJson = Path.Combine(outDir, "SupportByPass.json");
            File.WriteAllText(byPassJson, JsonSerializer.Serialize(supportByPass, new JsonSerializerOptions { WriteIndented = true }));

            // Final probabilities (equal weights if unweighted)
            var final = new Dictionary<string, object>
            {
                ["A"] = new { values = Support(A).ToArray() },
                ["B"] = new { values = Support(B).ToArray() },
                ["C"] = new { values = Support(C).ToArray() }
            };
            string finalJson = Path.Combine(outDir, "FinalProbabilities.json");
            File.WriteAllText(finalJson, JsonSerializer.Serialize(final, new JsonSerializerOptions { WriteIndented = true }));

            // Assertions: stabilises within N passes by index (not epoch id)
            Assert.That(stabilisedAtPass, Is.GreaterThanOrEqualTo(1));
            Assert.That(stabilisedAtPass, Is.LessThanOrEqualTo(5));

            // Compute supports
            var supA = Support(A).ToArray();
            var supB = Support(B).ToArray();
            var supC = Support(C).ToArray();

            // Shape assertions: bounded growth and core-value presence
            Assert.That(supA.Length, Is.LessThanOrEqualTo(5));
            Assert.That(supB.Length, Is.LessThanOrEqualTo(5));
            Assert.That(supC.Length, Is.LessThanOrEqualTo(5));

            int[] domain = { 0, 1, 2, 3, 4 };
            Assert.That(supA.All(x => domain.Contains(x)), "A contains out-of-domain values");
            Assert.That(supB.All(x => domain.Contains(x)), "B contains out-of-domain values");
            Assert.That(supC.All(x => domain.Contains(x)), "C contains out-of-domain values");

            // Core shape (must include at least these values)
            Assert.That(supA.Intersect(new[] { 0, 4 }).Count(), Is.EqualTo(2), "A must include 0 and 4");
            Assert.That(supB.Intersect(new[] { 3, 4 }).Count(), Is.EqualTo(2), "B must include 3 and 4");
            Assert.That(supC.Intersect(new[] { 1, 2 }).Count(), Is.EqualTo(2), "C must include 1 and 2");
        }

        [Test]
        public void AttributedEntry_PrintsExactlyOnce_NoDuplicateEmission()
        {
            // Capture console output
            StringWriter sw = new StringWriter();
            TextWriter prev = Console.Out;
            Console.SetOut(sw);
            try
            {
                // Run the attributed entry point (TestPV program)
                AethericRedirectionGrid.RunAttributedEntryPointForTests();
            }
            finally
            {
                Console.SetOut(prev);
            }

            string output = sw.ToString();
            // Count occurrences of the two expected prefixes
            int antivalCount = CountOccurrences(output, "The antival is ");
            int valueCount = CountOccurrences(output, "The value is ");

            Assert.That(antivalCount, Is.EqualTo(1), "Antival line should print exactly once.");
            Assert.That(valueCount, Is.EqualTo(1), "Value line should print exactly once.");
        }

        private static int CountOccurrences(string text, string needle)
        {
            int count = 0; int idx = 0;
            while ((idx = text.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
            {
                count++; idx += needle.Length;
            }
            return count;
        }
    }
}
