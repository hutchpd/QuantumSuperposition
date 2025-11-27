using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicVariableTransactionTests
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
        public void Atomicity_SingleVariable_ConcurrentIncrements_SumsCorrectly()
        {
            var v = PositronicVariable<int>.GetOrCreate("ctr", 0, _rt);
            int workers = 16;
            int iterations = 100;

            var tasks = ConcurrencyTestHelpers.RunConcurrentTasks(workers, () =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    PositronicVariable<int>.InTransaction(() =>
                    {
                        // read
                        int cur = v.GetCurrentQBit().ToCollapsedValues().First();
                        // replace last slice deterministically using Required
                        v.Required = PositronicVariable<int>.Project(v, _ => cur + 1);
                    });
                }
            });

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));

            int final = v.GetCurrentQBit().ToCollapsedValues().First();
            Assert.That(final, Is.EqualTo(workers * iterations));
        }

        [Test]
        public void Atomicity_MultiVariable_Transfers_AreConsistent()
        {
            var a = PositronicVariable<int>.GetOrCreate("a", 1000, _rt);
            var b = PositronicVariable<int>.GetOrCreate("b", 0, _rt);

            int workers = 8;
            int iterations = 200;

            var tasks = ConcurrencyTestHelpers.RunConcurrentTasks(workers, () =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    PositronicVariable<int>.InTransaction(() =>
                    {
                        int av = a.GetCurrentQBit().ToCollapsedValues().First();
                        if (av > 0)
                        {
                            int bv = b.GetCurrentQBit().ToCollapsedValues().First();
                            a.Required = PositronicVariable<int>.Project(a, _ => av - 1);
                            b.Required = PositronicVariable<int>.Project(b, _ => bv + 1);
                        }
                    });
                }
            });

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));

            int finalA = a.GetCurrentQBit().ToCollapsedValues().First();
            int finalB = b.GetCurrentQBit().ToCollapsedValues().First();

            Assert.That(finalA + finalB, Is.EqualTo(1000));
        }
    }
}
