using NUnit.Framework;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicVariableConcurrencyTests
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
        public void Baseline_SingleThreaded_Intact()
        {
            var v = PositronicVariable<int>.GetOrCreate("x", 0, _rt);
            v.Assign(1);
            v.Assign(2);
            v.Assign(3);
            Assert.That(v.ToValues().OrderBy(x => x).ToArray(), Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void NonTransactional_Race_NoCrash()
        {
            var v = PositronicVariable<int>.GetOrCreate("concurrent", 0, _rt);

            // 20 workers concurrently mutating the same variable
            var tasks = ConcurrencyTestHelpers.RunConcurrentTasks(20, () =>
            {
                // interleave reads and writes
                var current = v.GetCurrentQBit().ToCollapsedValues().ToArray();
                v.Assign(v + 1);
                v.Assign(v + 2);
                _ = v.ToValues().ToArray();
            });

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));

            // We don't assert exact values (non-transactional); just that the API remained usable.
            var states = v.ToValues().OrderBy(x => x).ToArray();
            Assert.That(states.Length, Is.GreaterThanOrEqualTo(1));
        }
    }
}
