using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Linq;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class BitwiseOperationTests
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
        }

        [TearDown]
        public void TearDown()
        {
            PositronicAmbient.PanicAndReset();
        }

        // ---------- Basic scalar semantics (int) ----------

        [Test]
        public void BitwiseOr_QExpr_Scalar_CollapsesToExpected()
        {
            var v = PositronicVariable<int>.GetOrCreate("bor", 0b_0101, _rt);
            var q = v | 0b_0011;

            Assert.That(q.ToCollapsedValues().Single(), Is.EqualTo(0b_0111));
        }

        [Test]
        public void BitwiseAnd_QExpr_Scalar_CollapsesToExpected()
        {
            var v = PositronicVariable<int>.GetOrCreate("band", 0b_1101, _rt);
            var q = v & 0b_0110;

            Assert.That(q.ToCollapsedValues().Single(), Is.EqualTo(0b_0100));
        }

        [Test]
        public void BitwiseXor_QExpr_Scalar_CollapsesToExpected()
        {
            var v = PositronicVariable<int>.GetOrCreate("bxor", 0b_1100, _rt);
            var q = v ^ 0b_1010;

            Assert.That(q.ToCollapsedValues().Single(), Is.EqualTo(0b_0110));
        }

        [Test]
        public void BitwiseNot_QExpr_Scalar_CollapsesToExpected()
        {
            var v = PositronicVariable<int>.GetOrCreate("bnot", 0b_0000_1111, _rt);
            var q = ~v;

            // For 32-bit int, ~0x0F == 0xFFFFFFF0 == -16
            Assert.That(q.ToCollapsedValues().Single(), Is.EqualTo(~0b_0000_1111));
        }

        [Test]
        public void ShiftLeft_QExpr_Scalar_CollapsesToExpected()
        {
            var v = PositronicVariable<int>.GetOrCreate("shl", 0b_0001, _rt);
            var q = v << 3;

            Assert.That(q.ToCollapsedValues().Single(), Is.EqualTo(0b_1000));
        }

        [Test]
        public void ShiftRight_QExpr_Scalar_CollapsesToExpected_SignedArithmeticShift()
        {
            var v = PositronicVariable<int>.GetOrCreate("shr", -8, _rt);
            var q = v >> 1;

            Assert.That(q.ToCollapsedValues().Single(), Is.EqualTo(-4));
        }

        [Test]
        public void ShiftRight_QExpr_Scalar_CollapsesToExpected_UnsignedLogicalShift()
        {
            var v = PositronicVariable<uint>.GetOrCreate("ushr", 0x8000_0000u, _rt);
            var q = v >> 1;

            Assert.That(q.ToCollapsedValues().Single(), Is.EqualTo(0x4000_0000u));
        }

        // ---------- Superposition mapping (no collapse) ----------

        [Test]
        public void Superposition_And_MapsEachBranchWithoutCollapse()
        {
            var v = PositronicVariable<int>.GetOrCreate("qand", 0, _rt);
            var qb = new QuBit<int>(new[] { 1, 2, 3 });
            qb.Any();
            v.Assign(qb);

            var q = v & 0b_0110; // 1&6=0, 2&6=2, 3&6=2 -> union {0,2}
            var states = q.ToCollapsedValues().OrderBy(x => x).ToArray();

            Assert.That(states, Is.EquivalentTo(new[] { 0, 2 }));
        }

        [Test]
        public void Superposition_Or_MapsEachBranchWithoutCollapse()
        {
            var v = PositronicVariable<int>.GetOrCreate("qor", 0, _rt);
            var qb = new QuBit<int>(new[] { 0b_0001, 0b_0100 });
            qb.Any();
            v.Assign(qb);

            var q = v | 0b_0011; // {1|3=3, 4|3=7} -> {3,7}
            var states = q.ToCollapsedValues().OrderBy(x => x).ToArray();

            Assert.That(states, Is.EquivalentTo(new[] { 3, 7 }));
        }

        // ---------- Reverse replay semantics ----------

        [Test]
        public void Xor_Undoes_Itself_On_ReverseStep()
        {
            var v = PositronicVariable<int>.GetOrCreate("vxor_rev", 0b_0011, _rt);

            // Forward: apply XOR
            RunStep(() => v.Assign(v ^ 0b_0101), +1);
            // Reverse: undo
            RunStep(() => { }, -1);

            Assert.That(v.ToValues().Single(), Is.EqualTo(0b_0011));
        }

        [Test]
        public void Not_Undoes_Itself_On_ReverseStep()
        {
            var v = PositronicVariable<int>.GetOrCreate("vnot_rev", 0b_1010, _rt);

            RunStep(() => v.Assign(~v), +1);
            RunStep(() => { }, -1);

            Assert.That(v.ToValues().Single(), Is.EqualTo(0b_1010));
        }

        [Test]
        public void Or_Restores_Original_On_ReverseStep_ViaSnapshot()
        {
            var v = PositronicVariable<int>.GetOrCreate("vor_rev", 0b_0101, _rt);

            RunStep(() => v.Assign(v | 0b_0011), +1);
            RunStep(() => { }, -1);

            Assert.That(v.ToValues().Single(), Is.EqualTo(0b_0101));
        }

        [Test]
        public void ShiftLeft_Restores_Original_On_ReverseStep_ViaSnapshot()
        {
            var v = PositronicVariable<int>.GetOrCreate("vshl_rev", 3, _rt);

            RunStep(() => v.Assign(v << 2), +1);
            RunStep(() => { }, -1);

            Assert.That(v.ToValues().Single(), Is.EqualTo(3));
        }

        [Test]
        public void ShiftRight_Restores_Original_On_ReverseStep_ViaSnapshot()
        {
            var v = PositronicVariable<int>.GetOrCreate("vshr_rev", 64, _rt);

            RunStep(() => v.Assign(v >> 3), +1);
            RunStep(() => { }, -1);

            Assert.That(v.ToValues().Single(), Is.EqualTo(64));
        }

        // ---------- Cross-variable safety ----------

        [Test]
        public void CrossVariable_BitwiseOr_Reverse_DoesNotMutate_Source()
        {
            var a = PositronicVariable<int>.GetOrCreate("a_bor", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b_bor", 0, _rt);

            QuantumLedgerOfRegret.Clear();

            // Force final observed end-states
            PositronicVariable<int>.SetEntropy(_rt, +1);
            a.Assign(0b_0101);
            b.Assign(0b_0000);

            var expr = a | 0b_0011;

            // Reverse pass: assign expr to b; a must remain unchanged
            PositronicVariable<int>.SetEntropy(_rt, -1);
            var beforeA = a.ToValues().Single();
            b.Assign(expr);
            var afterA = a.ToValues().Single();

            Assert.That(afterA, Is.EqualTo(beforeA));
        }

        // ---------- Guard rails ----------

        [Test]
        public void NonIntegralType_Bitwise_Not_Throws_NotSupported()
        {
            var d = PositronicVariable<double>.GetOrCreate("dnot", 1.0, _rt);

            Assert.Throws<NotSupportedException>(() =>
            {
                var _ = ~d; // EnsureIntegralBitwise() should throw
            });
        }

        [Test]
        public void NonIntegralType_Bitwise_Or_Throws_NotSupported()
        {
            var d = PositronicVariable<double>.GetOrCreate("dor", 2.5, _rt);

            Assert.Throws<NotSupportedException>(() =>
            {
                var _ = d | 1.0;
            });
        }

        // ---------- Helpers ----------

        private static void RunStep(Action code, int entropy)
        {
            PositronicVariable<int>.SetEntropy(PositronicAmbient.Current, entropy);
            code();
            if (entropy < 0)
                QuantumLedgerOfRegret.ReverseLastOperations();
        }
    }
}