using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using PositronicVariables.Transactions;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class LinearizabilityModelTests
    {
        private IPositronicRuntime _rt;

        [SetUp]
        public void SetUp()
        {
            var hb = new HostBuilder().ConfigureServices(s => s.AddPositronicRuntime());
            PositronicAmbient.InitialiseWith(hb);
            _rt = PositronicAmbient.Current;
            STMTelemetry.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.IsInitialized && PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.PanicAndReset();
        }

        private enum OpKind
        {
            IncA,
            DecA,
            IncB,
            DecB,
            AssignAtoB,
            ConstrainAEq0,
        }

        private sealed class Op
        {
            public OpKind Kind { get; }
            public Op(OpKind k) { Kind = k; }
            public override string ToString() => Kind.ToString();
        }

        private static IEnumerable<Op[]> EnumerateSequences(int length)
        {
            OpKind[] kinds = (OpKind[])Enum.GetValues(typeof(OpKind));
            int K = kinds.Length;
            int[] idx = new int[length];
            while (true)
            {
                Op[] seq = new Op[length];
                for (int i = 0; i < length; i++) seq[i] = new Op(kinds[idx[i]]);
                yield return seq;
                int pos = length - 1;
                while (pos >= 0)
                {
                    idx[pos]++;
                    if (idx[pos] < K) break;
                    idx[pos] = 0; pos--;
                }
                if (pos < 0) yield break;
            }
        }

        private static (int a, int b) ApplySerial(PositronicVariable<int> a, PositronicVariable<int> b, Op[] ops)
        {
            int av = a.GetCurrentQBit().ToCollapsedValues().First();
            int bv = b.GetCurrentQBit().ToCollapsedValues().First();
            foreach (var op in ops)
            {
                switch (op.Kind)
                {
                    case OpKind.IncA: av = av + 1; break;
                    case OpKind.DecA: av = av - 1; break;
                    case OpKind.IncB: bv = bv + 1; break;
                    case OpKind.DecB: bv = bv - 1; break;
                    case OpKind.AssignAtoB: av = bv; break; // simple assign model
                    case OpKind.ConstrainAEq0: av = 0; break; // equality constraint
                }
            }
            return (av, bv);
        }

        private static void ApplyConcurrent(PositronicVariable<int> a, PositronicVariable<int> b, Op[] ops)
        {
            TransactionV2.RunWithRetry(tx =>
            {
                tx.RecordRead(a); tx.RecordRead(b);
                int av = a.GetCurrentQBit().ToCollapsedValues().First();
                int bv = b.GetCurrentQBit().ToCollapsedValues().First();
                foreach (var op in ops)
                {
                    switch (op.Kind)
                    {
                        case OpKind.IncA:
                            tx.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
                            av = av + 1; break;
                        case OpKind.DecA:
                            tx.StageWrite(a, new QuBit<int>(new[] { av - 1 }).Any());
                            av = av - 1; break;
                        case OpKind.IncB:
                            tx.StageWrite(b, new QuBit<int>(new[] { bv + 1 }).Any());
                            bv = bv + 1; break;
                        case OpKind.DecB:
                            tx.StageWrite(b, new QuBit<int>(new[] { bv - 1 }).Any());
                            bv = bv - 1; break;
                        case OpKind.AssignAtoB:
                            tx.StageWrite(a, new QuBit<int>(new[] { bv }).Any());
                            av = bv; break;
                        case OpKind.ConstrainAEq0:
                            tx.StageWrite(a, new QuBit<int>(new[] { 0 }).Any());
                            av = 0; break;
                    }
                }
            }, maxAttempts: 4, baseDelayMs: 0, maxDelayMs: 2);
        }

        [Test]
        public void Small_Model_Checker_Length2_No_Counterexamples()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            foreach (var seq1 in EnumerateSequences(2))
            {
                foreach (var seq2 in EnumerateSequences(2))
                {
                    // Reset variables to baseline for each trial
                    a.ConstrainEqual(new QuBit<int>(new[] { 0 }).Any());
                    b.ConstrainEqual(new QuBit<int>(new[] { 0 }).Any());

                    // Run two logical threads concurrently (sequentially here but under STM they act atomically)
                    ApplyConcurrent(a, b, seq1);
                    ApplyConcurrent(a, b, seq2);

                    int finalA = a.GetCurrentQBit().ToCollapsedValues().First();
                    int finalB = b.GetCurrentQBit().ToCollapsedValues().First();

                    // Enumerate serializations and check if one matches the observed outcome
                    var s1 = ApplySerial(a, b, seq1);
                    // Note: ApplySerial uses current values; snapshot baseline instead
                    (int sa0, int sb0) = (0, 0);
                    (int s1a, int s1b) = ApplySerialVals(sa0, sb0, seq1);
                    (int s2a, int s2b) = ApplySerialVals(sa0, sb0, seq2);
                    (int leftA, int leftB) = ApplySerialVals(s1a, s1b, seq2);
                    (int rightA, int rightB) = ApplySerialVals(s2a, s2b, seq1);

                    bool linearizable = (finalA == leftA && finalB == leftB) || (finalA == rightA && finalB == rightB);
                    if (!linearizable)
                    {
                        Assert.Fail($"Counterexample found. seq1=[{string.Join(",", seq1.Select(o=>o.Kind))}] seq2=[{string.Join(",", seq2.Select(o=>o.Kind))}] -> observed=({finalA},{finalB}) vs serial L=({leftA},{leftB}) R=({rightA},{rightB})");
                    }
                }
            }
        }

        private static (int a, int b) ApplySerialVals(int a0, int b0, Op[] ops)
        {
            int av = a0;
            int bv = b0;
            foreach (var op in ops)
            {
                switch (op.Kind)
                {
                    case OpKind.IncA: av = av + 1; break;
                    case OpKind.DecA: av = av - 1; break;
                    case OpKind.IncB: bv = bv + 1; break;
                    case OpKind.DecB: bv = bv - 1; break;
                    case OpKind.AssignAtoB: av = bv; break;
                    case OpKind.ConstrainAEq0: av = 0; break;
                }
            }
            return (av, bv);
        }
    }
}
