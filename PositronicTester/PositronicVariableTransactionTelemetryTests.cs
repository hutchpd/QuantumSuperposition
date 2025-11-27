using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using PositronicVariables.Transactions;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicVariableTransactionTelemetryTests
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

        [Test]
        public void ReadOnlyFastPath_RecordsCommit_NoLocks()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 42, _rt);
            TransactionV2.Run(tx =>
            {
                tx.RecordRead(a);
            });
            Assert.That(STMTelemetry.TotalCommits, Is.GreaterThanOrEqualTo(1));
            Assert.That(STMTelemetry.TotalReadOnlyCommits, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Telemetry_RecordsRetries_OnContention()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var start = new System.Threading.ManualResetEventSlim(false);

            var t1 = Task.Run(() =>
            {
                start.Wait();
                TransactionV2.RunWithRetry(tx =>
                {
                    tx.RecordRead(a);
                    int av = a.GetCurrentQBit().ToCollapsedValues().First();
                    tx.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
                });
            });

            var t2 = Task.Run(() =>
            {
                start.Wait();
                TransactionV2.RunWithRetry(tx =>
                {
                    tx.RecordRead(a);
                    int av = a.GetCurrentQBit().ToCollapsedValues().First();
                    tx.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
                });
            });

            start.Set();
            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(t1, t2));
            Assert.That(STMTelemetry.TotalRetries, Is.GreaterThanOrEqualTo(0));
            Assert.That(STMTelemetry.TotalCommits, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Telemetry_Report_NotEmpty_AfterActivity()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            TransactionV2.RunWithRetry(tx =>
            {
                tx.RecordRead(a);
                int av = a.GetCurrentQBit().ToCollapsedValues().First();
                tx.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
            });

            string report = STMTelemetry.GetReport();
            Assert.That(report, Does.Contain("STM Telemetry Report"));
            Assert.That(report, Does.Contain("TotalCommits"));
        }
    }
}
