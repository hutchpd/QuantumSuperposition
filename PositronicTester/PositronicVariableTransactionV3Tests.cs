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
    public class PositronicVariableTransactionV3Tests
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
        public void RetryPolicy_AbortsAfterMaxAttempts()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);
            var c = PositronicVariable<int>.GetOrCreate("c", 0, _rt);

            using var cts = new System.Threading.CancellationTokenSource();
            Task writer = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    // Non-transactional increment of c to keep its version changing
                    int cv = c.GetCurrentQBit().ToCollapsedValues().First();
                    c.Assign(cv + 1);
                }
            }, cts.Token);

            bool aborted = false;
            // Try many single-attempt transactions; at least one should abort due to c changing
            for (int i = 0; i < 200 && !aborted; i++)
            {
                try
                {
                    TransactionV2.RunWithRetry(tx =>
                    {
                        tx.RecordRead(c); // read-only var to force validation against moving target
                        tx.RecordRead(a);
                        tx.RecordRead(b);
                        int av = a.GetCurrentQBit().ToCollapsedValues().First();
                        int bv = b.GetCurrentQBit().ToCollapsedValues().First();
                        tx.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
                        tx.StageWrite(b, new QuBit<int>(new[] { bv + 1 }).Any());
                    }, maxAttempts: 1, baseDelayMs: 0, maxDelayMs: 0);
                }
                catch (TransactionAbortedException)
                {
                    aborted = true;
                }
                catch
                {
                    // ignore other transient exceptions; we only care about aborts here
                }
            }

            cts.Cancel();
            try { writer.Wait(50); } catch { /* ignore */ }

            Assert.That(aborted, Is.True, "Expected at least one transaction abort under contention with single attempt.");
        }

        [Test]
        public void Stress_ManyConcurrentTransactions_Consistent()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            int workers = 16;
            int iterations = 200;
            var tasks = ConcurrencyTestHelpers.RunConcurrentTasks(workers, () =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    TransactionV2.RunWithRetry(tx =>
                    {
                        // randomly pick transfer direction
                        bool toB = (Environment.CurrentManagedThreadId + i) % 2 == 0;
                        if (toB)
                        {
                            tx.RecordRead(a);
                            tx.RecordRead(b);
                            int av = a.GetCurrentQBit().ToCollapsedValues().First();
                            int bv = b.GetCurrentQBit().ToCollapsedValues().First();
                            if (av > 0)
                            {
                                tx.StageWrite(a, new QuBit<int>(new[] { av - 1 }).Any());
                                tx.StageWrite(b, new QuBit<int>(new[] { bv + 1 }).Any());
                            }
                        }
                        else
                        {
                            tx.RecordRead(a);
                            tx.RecordRead(b);
                            int av = a.GetCurrentQBit().ToCollapsedValues().First();
                            int bv = b.GetCurrentQBit().ToCollapsedValues().First();
                            if (bv > 0)
                            {
                                tx.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
                                tx.StageWrite(b, new QuBit<int>(new[] { bv - 1 }).Any());
                            }
                        }
                    }, maxAttempts: 8, baseDelayMs: 0, maxDelayMs: 2);
                }
            });

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));

            int finalA = a.GetCurrentQBit().ToCollapsedValues().First();
            int finalB = b.GetCurrentQBit().ToCollapsedValues().First();
            Assert.That(finalA + finalB, Is.EqualTo(0));
        }
    }
}
