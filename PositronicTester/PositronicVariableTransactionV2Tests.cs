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
    }
}
