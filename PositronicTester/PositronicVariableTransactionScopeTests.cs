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
    public class PositronicVariableTransactionScopeTests
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
        public async Task AsyncScope_PreservesContextAcrossAwait()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);

            await TransactionScope.RunAsync(async () =>
            {
                TransactionScope.RecordRead(a);
                int av = a.GetCurrentQBit().ToCollapsedValues().First();
                TransactionScope.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
                await Task.Delay(10);
                TransactionScope.RecordRead(a);
                av = a.GetCurrentQBit().ToCollapsedValues().First();
                TransactionScope.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
            });

            int final = a.GetCurrentQBit().ToCollapsedValues().First();
            Assert.That(final, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void NestedScopes_Flatten_And_CommitOnce()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            using (TransactionScope.Begin())
            {
                TransactionScope.RecordRead(a);
                int av = a.GetCurrentQBit().ToCollapsedValues().First();
                TransactionScope.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());

                using (TransactionScope.Begin())
                {
                    TransactionScope.RecordRead(b);
                    int bv = b.GetCurrentQBit().ToCollapsedValues().First();
                    TransactionScope.StageWrite(b, new QuBit<int>(new[] { bv + 1 }).Any());
                    TransactionScope.AddCommitHook(() => { /* no-op */ });
                }
            }

            int finalA = a.GetCurrentQBit().ToCollapsedValues().First();
            int finalB = b.GetCurrentQBit().ToCollapsedValues().First();

            Assert.That(finalA, Is.GreaterThanOrEqualTo(1));
            Assert.That(finalB, Is.GreaterThanOrEqualTo(1));
            Assert.That(finalA + finalB, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Hooks_Run_After_Commit_Outside_Locks()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 0, _rt);
            bool ran = false;

            using (TransactionScope.Begin())
            {
                TransactionScope.RecordRead(a);
                int av = a.GetCurrentQBit().ToCollapsedValues().First();
                TransactionScope.StageWrite(a, new QuBit<int>(new[] { av + 1 }).Any());
                TransactionScope.AddCommitHook(() => ran = true);
            }

            Assert.That(ran, Is.True);
        }
    }
}
