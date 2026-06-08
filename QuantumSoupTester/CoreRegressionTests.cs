using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;

namespace QuantumSoupTester
{
    [TestFixture]
    public class CoreRegressionTests
    {
        private const double Tolerance = 1e-12;

        private static Complex GetAmplitude(QuantumSystem system, params int[] basis)
        {
            var match = system.Amplitudes.First(kvp => kvp.Key.SequenceEqual(basis));
            return match.Value;
        }

        [Test]
        public void SetFromTensorProduct_WithWeightedLocalQubits_PreservesAmplitudes()
        {
            var q0 = new QuBit<int>(new[] { 0, 1 }).WithWeights(
                new Dictionary<int, Complex>
                {
                    { 0, new Complex(3.0 / 5.0, 0) },
                    { 1, new Complex(4.0 / 5.0, 0) }
                },
                autoNormalise: false);

            var q1 = new QuBit<int>(new[] { 0, 1 }).WithWeights(
                new Dictionary<int, Complex>
                {
                    { 0, new Complex(12.0 / 13.0, 0) },
                    { 1, new Complex(5.0 / 13.0, 0) }
                },
                autoNormalise: false);

            var q0Before = q0.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            var q1Before = q1.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            var system = new QuantumSystem();
            system.SetFromTensorProduct(false, q0, q1);

            var q0After = q0.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            var q1After = q1.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            Assert.That(q0After[0].Real, Is.EqualTo(q0Before[0].Real).Within(Tolerance));
            Assert.That(q0After[1].Real, Is.EqualTo(q0Before[1].Real).Within(Tolerance));
            Assert.That(q1After[0].Real, Is.EqualTo(q1Before[0].Real).Within(Tolerance));
            Assert.That(q1After[1].Real, Is.EqualTo(q1Before[1].Real).Within(Tolerance));

            Assert.That(GetAmplitude(system, 0, 0).Real, Is.EqualTo(36.0 / 65.0).Within(Tolerance));
            Assert.That(GetAmplitude(system, 0, 1).Real, Is.EqualTo(15.0 / 65.0).Within(Tolerance));
            Assert.That(GetAmplitude(system, 1, 0).Real, Is.EqualTo(48.0 / 65.0).Within(Tolerance));
            Assert.That(GetAmplitude(system, 1, 1).Real, Is.EqualTo(20.0 / 65.0).Within(Tolerance));
        }

        [Test]
        public void ObserveGlobal_WithSameSeed_ProducesSameOutcome()
        {
            QuantumSystem BuildSystem()
            {
                var s = new QuantumSystem();
                var qa = new QuBit<int>(s, new[] { 0 }).WithWeights(
                    new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } },
                    autoNormalise: true);
                var qb = new QuBit<int>(s, new[] { 1 }).WithWeights(
                    new Dictionary<int, Complex> { { 0, 2.0 }, { 1, 1.0 } },
                    autoNormalise: true);
                s.SetFromTensorProduct(false, qa, qb);
                return s;
            }

            var systemA = BuildSystem();
            var systemB = BuildSystem();

            int[] observedA = systemA.ObserveGlobal(new[] { 0, 1 }, new Random(123456));
            int[] observedB = systemB.ObserveGlobal(new[] { 0, 1 }, new Random(123456));

            Assert.That(observedA, Is.EqualTo(observedB));
            Assert.That(systemA.Amplitudes.Count, Is.EqualTo(1));
            Assert.That(systemB.Amplitudes.Count, Is.EqualTo(1));
            Assert.That(systemA.Amplitudes.Keys.Single(), Is.EqualTo(systemB.Amplitudes.Keys.Single()));
        }

        [Test]
        public void PartialObserve_WithBoolQubit_PropagatesCollapse()
        {
            var system = new QuantumSystem();
            var measured = new QuBit<bool>(system, new[] { 0 }).WithWeights(
                new Dictionary<bool, Complex> { { false, 1.0 }, { true, 1.0 } },
                autoNormalise: true);
            var unmeasured = new QuBit<bool>(system, new[] { 1 }).WithWeights(
                new Dictionary<bool, Complex> { { false, 1.0 }, { true, 1.0 } },
                autoNormalise: true);

            system.SetFromTensorProduct(false, measured, unmeasured);

            int[] outcome = system.PartialObserve(new[] { 0 }, new Random(42));

            Assert.That(measured.IsCollapsed, Is.True);
            Assert.That(measured.GetObservedValue(), Is.EqualTo(outcome[0] != 0));
            Assert.That(unmeasured.IsCollapsed, Is.False);
        }

        [Test]
        public void PartialObserve_WithNonBoolQubit_PropagatesCollapse()
        {
            var system = new QuantumSystem();
            var measured = new QuBit<int>(system, new[] { 0 }).WithWeights(
                new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } },
                autoNormalise: true);
            var unmeasured = new QuBit<int>(system, new[] { 1 }).WithWeights(
                new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } },
                autoNormalise: true);

            system.SetFromTensorProduct(false, measured, unmeasured);

            int[] outcome = system.PartialObserve(new[] { 0 }, new Random(42));

            Assert.That(measured.IsCollapsed, Is.True);
            Assert.That(measured.GetObservedValue(), Is.EqualTo(outcome[0]));
            Assert.That(unmeasured.IsCollapsed, Is.False);
        }

        [Test]
        public void ApplyGateBatch_KeepsProbabilityMassNormalised()
        {
            var system = new QuantumSystem();
            var q0 = new QuBit<int>(system, new[] { 0 }).WithWeights(
                new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } },
                autoNormalise: true);
            var q1 = new QuBit<int>(system, new[] { 1 }).WithWeights(
                new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } },
                autoNormalise: true);
            system.SetFromTensorProduct(false, q0, q1);

            system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "H");
            system.ApplyTwoQubitGate(0, 1, QuantumGates.CNOT.Matrix, "CNOT");
            system.ApplySingleQubitGate(1, QuantumGates.T, "T");
            system.ApplySingleQubitGate(1, QuantumGates.T_Dagger, "T_Dagger");
            system.ProcessGateQueue();

            double probabilityMass = system.Amplitudes.Values.Sum(a => a.Magnitude * a.Magnitude);
            Assert.That(probabilityMass, Is.EqualTo(1.0).Within(Tolerance));
        }
    }
}
