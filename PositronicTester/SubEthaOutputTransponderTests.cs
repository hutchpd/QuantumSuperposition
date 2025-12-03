using NUnit.Framework;
using PositronicVariables.Engine.Transponder;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using PositronicVariables.Variables.Factory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PositronicTester
{
    [TestFixture]
    public class SubEthaOutputTransponderTests
    {
        private sealed class DummyFactory : IPositronicVariableFactory
        {
            public PositronicVariable<T> GetOrCreate<T>(string id, T initialValue) where T : IComparable<T>
                => throw new NotImplementedException();
            public PositronicVariable<T> GetOrCreate<T>(string id) where T : IComparable<T>
                => throw new NotImplementedException();
            public PositronicVariable<T> GetOrCreate<T>(T initialValue) where T : IComparable<T>
                => throw new NotImplementedException();
        }
        private sealed class DummyRegistry : IPositronicVariableRegistry
        {
            private readonly List<IPositronicVariable> _list = new();
            public void Add(IPositronicVariable variable) => _list.Add(variable);
            public void Clear() => _list.Clear();
            public IEnumerator<IPositronicVariable> GetEnumerator() => _list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        }

        private sealed class DummyRuntime : IPositronicRuntime
        {
            public int Entropy { get; set; }
            public bool Converged { get; set; }
            public TextWriter Babelfish { get; set; }
            public IPositronicVariableRegistry Variables => Registry;
            public void Reset() { }
            public int TotalConvergenceIterations { get; set; }
            public event Action OnAllConverged;
            public IPositronicVariableFactory Factory { get; } = new DummyFactory();
            public IPositronicVariableRegistry Registry { get; } = new DummyRegistry();
        }

        [SetUp]
        public void Setup()
        {
            Console.SetOut(AethericRedirectionGrid.ImprobabilityDrive);
            AethericRedirectionGrid.AtTheRestaurant = false;
            _ = AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();
        }

        [Test]
        public void Restore_DoesNotSelfAppend_WhenOriginalIsImprobabilityDrive()
        {
            var runtime = new DummyRuntime();
            var transponder = new SubEthaOutputTransponder(runtime);

            // Use a distinct original writer to avoid buffer self-append
            var original = new StringWriter();
            Console.SetOut(original);

            transponder.Redirect();

            Console.WriteLine("Line A");
            Console.WriteLine("Line B");

            string before = AethericRedirectionGrid.ImprobabilityDrive.ToString();
            Assert.That(before, Does.Contain("Line A"));
            Assert.That(before, Does.Contain("Line B"));

            transponder.Restore();

            string after = AethericRedirectionGrid.ImprobabilityDrive.ToString();
            // Buffer content must remain unchanged (no self-append)
            Assert.That(after, Is.EqualTo(before));
            // Original writer should now have received the buffer content exactly once
            string originalText = original.ToString();
            Assert.That(originalText, Is.EqualTo(before));
        }

        [Test]
        public void Restore_AtRestaurant_DoesNotModifyBuffer()
        {
            var runtime = new DummyRuntime();
            var transponder = new SubEthaOutputTransponder(runtime);

            _ = AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();

            transponder.Redirect();

            Console.WriteLine("Quantum Soup");
            string before = AethericRedirectionGrid.ImprobabilityDrive.ToString();

            AethericRedirectionGrid.AtTheRestaurant = true;
            transponder.Restore();

            string after = AethericRedirectionGrid.ImprobabilityDrive.ToString();
            Assert.That(after, Is.EqualTo(before));
        }
    }
}
