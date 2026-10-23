using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using PositronicVariables.Transactions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicVariableTransactionV2Tests
    {
        private IPositronicRuntime _rt;

        [SetUp]
        public void SetUp()
        {
            var hb = new HostBuilder().ConfigureServices(s => s.AddPositronicRuntime());
            PositronicAmbient.InitialiseWith(hb);
            _rt = PositronicAmbient.Current;
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.IsInitialized && PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();
            PositronicAmbient.PanicAndReset();
        }

        [Test]
        public void DeadlockAvoidance_DifferentOrder_NoDeadlock()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            var ta = Task.Run(() =>
            {
                TransactionV2.RunWithRetry(tx =>
                {
                    tx.RecordRead(a);
                    tx.RecordRead(b);
                    tx.StageWrite(b, new QuantumSuperposition.QuantumSoup.QuBit<int>(new[] { 1 }).Any());
                    tx.StageWrite(a, new QuantumSuperposition.QuantumSoup.QuBit<int>(new[] { 1 }).Any());
                }, maxAttempts: 8);
            });

            var tb = Task.Run(() =>
            {
                TransactionV2.RunWithRetry(tx =>
                {
                    tx.RecordRead(b);
                    tx.RecordRead(a);
                    tx.StageWrite(a, new QuantumSuperposition.QuantumSoup.QuBit<int>(new[] { 2 }).Any());
                    tx.StageWrite(b, new QuantumSuperposition.QuantumSoup.QuBit<int>(new[] { 2 }).Any());
                }, maxAttempts: 8);
            });

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(ta, tb));
        }

        [Test]
        public void DisjointTransactions_ProceedInParallel()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            var t1 = Task.Run(() =>
            {
                for (int i = 0; i < 200; i++)
                {
                    TransactionV2.RunWithRetry(tx =>
                    {
                        tx.RecordRead(a);
                        tx.StageWrite(a, new QuantumSuperposition.QuantumSoup.QuBit<int>(new[] { 1 }).Any());
                    });
                }
            });

            var t2 = Task.Run(() =>
            {
                for (int i = 0; i < 200; i++)
                {
                    TransactionV2.RunWithRetry(tx =>
                    {
                        tx.RecordRead(b);
                        tx.StageWrite(b, new QuantumSuperposition.QuantumSoup.QuBit<int>(new[] { 1 }).Any());
                    });
                }
            });

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(t1, t2));
        }

        [Test]
        public void ConcurrentReadWriteIncrement_NoLostUpdates()
        {
            var counter = PositronicVariable<int>.GetOrCreate("counter", 0, _rt);
            int workers = 8;
            int incrementsPerWorker = 25;

            var tasks = Enumerable.Range(0, workers).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < incrementsPerWorker; i++)
                {
                    TransactionV2.RunWithRetry(tx =>
                    {
                        tx.RecordRead(counter);
                        int current = counter.GetCurrentQBit().ToCollapsedValues().First();
                        tx.StageWrite(counter, new QuantumSuperposition.QuantumSoup.QuBit<int>(new[] { current + 1 }).Any(), TxMutationKind.ReplaceLast);
                    }, maxAttempts: 64, baseDelayMs: 1, maxDelayMs: 10);
                }
            })).ToArray();

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
            int finalValue = counter.GetCurrentQBit().ToCollapsedValues().First();
            Assert.That(finalValue, Is.EqualTo(workers * incrementsPerWorker));
        }

        [Test]
        public void NonCoprimeModulusCycle_RespectsStepAndSeedOrbit()
        {
            var v = PositronicVariable<int>.GetOrCreate("nonCoprime", 1, _rt);

            PositronicVariable<int>.RunConvergenceLoop(_rt, () =>
            {
                v.Assign((v + 2) % 4);
            });

            int[] result = v.ToValues().OrderBy(x => x).ToArray();
            Assert.That(result, Is.EqualTo(new[] { 1, 3 }),
                "Orbit of (v + 2) % 4 starting from 1 should be {1, 3}, not all of 0..3.");
        }

        [Test]
        public void BitwiseXorCycle_AndHeterogeneousRuntimeVariables_ConvergeWithoutInvalidCast()
        {
            // Register a variable of a different type in the same runtime
            _ = PositronicVariable<double>.GetOrCreate("otherTypeVar", 3.14, _rt);
            var v = PositronicVariable<int>.GetOrCreate("xorVar", 1, _rt);

            Assert.DoesNotThrow(() =>
            {
                PositronicVariable<int>.RunConvergenceLoop(_rt, () =>
                {
                    v.Assign(v ^ 1);
                });
            });

            int[] result = v.ToValues().OrderBy(x => x).ToArray();
            Assert.That(result, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void FileLedgerSink_DeduplicatesCommitIds()
        {
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ledger_{Guid.NewGuid():N}.jsonl");
            try
            {
                var sink = new PositronicVariables.Engine.Logging.FileLedgerSink(tempFile);
                var v = PositronicVariable<int>.GetOrCreate("auditVar", 5, _rt);
                var op = new PositronicVariables.Operations.BitwiseNotOperation<int>(v, _rt);
                Guid commitId = Guid.NewGuid();

                sink.Append(op, commitId);
                sink.Append(op, commitId); // duplicate commitId should not be written twice

                var entries = sink.ReadAuditTrail();
                Assert.That(entries.Count, Is.EqualTo(1));
                Assert.That(entries[0].CommitId, Is.EqualTo(commitId));
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                {
                    System.IO.File.Delete(tempFile);
                }
            }
        }
    }
}
