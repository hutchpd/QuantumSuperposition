using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Transactions;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class ParanoiaModeTests
    {
        private IPositronicRuntime _rt;

        [SetUp]
        public void SetUp()
        {
            var hb = new HostBuilder().ConfigureServices(s => s.AddPositronicRuntime());
            PositronicAmbient.InitialiseWith(hb);
            _rt = PositronicAmbient.Current;
            STMTelemetry.Reset();
            ParanoiaConfig.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.IsInitialized && PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.PanicAndReset();
        }

        [Test]
        public void Paranoia_LockOrdering_Assertion_Enabled()
        {
            ParanoiaConfig.EnableParanoia = true;

            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            // Stage writes out-of-order deliberately (b then a). Commit should still acquire in ascending order.
            // We assert that if TxIds are not strictly increasing in acquisition, paranoia throws.
            Assert.DoesNotThrow(() =>
            {
                TransactionV2.Run(tx =>
                {
                    tx.RecordRead(a); tx.RecordRead(b);
                    tx.StageWrite(b, new QuBit<int>(new[] { 1 }).Any());
                    tx.StageWrite(a, new QuBit<int>(new[] { 1 }).Any());
                });
            });
        }

        [Test]
        public void GlobalFallback_Activates_OnThreshold()
        {
            ParanoiaConfig.EnableGlobalFallback = true;
            ParanoiaConfig.AbortRetryThreshold = 0; // force activation immediately

            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            TransactionV2.RunWithRetry(tx =>
            {
                tx.RecordRead(a); tx.RecordRead(b);
                int av = a.GetCurrentQBit().ToCollapsedValues().First();
                int bv = b.GetCurrentQBit().ToCollapsedValues().First();
                tx.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
                tx.StageWrite(b, new QuBit<int>(new[] { bv + 1 }).Any());
            }, maxAttempts: 1, baseDelayMs: 0, maxDelayMs: 0);

            Assert.That(ParanoiaConfig.FallbackActive, Is.True);
        }
    }
}
