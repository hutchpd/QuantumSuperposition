using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Engine.Coordinator;
using PositronicVariables.Runtime;
using PositronicVariables.Transactions;
using PositronicVariables.Variables;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class ConvergenceCoordinatorTests
    {
        private IPositronicRuntime _rt;
        private ConvergenceCoordinator _coord;

        private class SimpleWriteItem<T> : IConvergenceWorkItem where T : IComparable<T>
        {
            private readonly PositronicVariable<T> _var;
            private readonly T _value;

            public SimpleWriteItem(PositronicVariable<T> v, T value)
            {
                _var = v;
                _value = value;
            }

            public void BuildWrites(TransactionV2 tx)
            {
                tx.StageWrite(_var, new QuantumSuperposition.QuantumSoup.QuBit<T>(new[] { _value }).Any());
            }

            public System.Collections.Generic.IEnumerable<Action> BuildCommitHooks()
            {
                yield break;
            }

            public object? GetResultAfterCommit() => null;
        }

        [SetUp]
        public void SetUp()
        {
            var hb = new HostBuilder()
                .ConfigureServices(s => s.AddPositronicRuntime<int>(b => { }));

            PositronicAmbient.InitialiseWith(hb);

            _rt = PositronicAmbient.Current;
            _coord = (ConvergenceCoordinator)PositronicAmbient.Services.GetService(typeof(ConvergenceCoordinator));
        }

        [TearDown]
        public void TearDown()
        {
            if (PositronicAmbient.IsInitialized && PositronicAmbient.Services is IDisposable disp)
                disp.Dispose();

            PositronicAmbient.PanicAndReset();
        }

        [Test]
        public async Task Enqueue_Multiple_Writes_Serially_Applied()
        {
            var v = PositronicVariable<int>.GetOrCreate("coord_var", 0, _rt);
            int count = 50;

            for (int i = 1; i <= count; i++)
            {
                await _coord.EnqueueAsync(new SimpleWriteItem<int>(v, i));
            }

            await _coord.FlushAsync();

            int final = v.GetCurrentQBit().ToCollapsedValues().First();

            Assert.That(final, Is.EqualTo(count));
        }

        [Test]
        public async Task FlushAsync_Waits_For_All_Work()
        {
            var v = PositronicVariable<int>.GetOrCreate("coord_flush", 0, _rt);

            for (int i = 1; i <= 10; i++)
            {
                await _coord.EnqueueAsync(new SimpleWriteItem<int>(v, i));
            }

            await _coord.FlushAsync();

            Assert.That(_coord.Processed, Is.GreaterThanOrEqualTo(_coord.Enqueued));
        }
    }
}