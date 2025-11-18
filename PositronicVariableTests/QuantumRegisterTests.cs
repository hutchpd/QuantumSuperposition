using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;

namespace QuantumSoupTester
{
    [TestFixture]
    public class QuantumRegisterTests
    {
        [Test]
        public void FromInt_ConstructsCorrectBasisState()
        {
            var system = new QuantumSystem();
            var reg = QuantumRegister.FromInt(3, 2, system);
            int value = reg.GetValue();
            Assert.That(value, Is.EqualTo(3));
        }

        [Test]
        public void FromAmplitudes_ConstructsSuperposition()
        {
            var system = new QuantumSystem();
            Complex amp = new Complex(1.0 / Math.Sqrt(4), 0);
            var vec = new[] { amp, amp, amp, amp }; // uniform 2-bit state
            var reg = QuantumRegister.FromAmplitudes(vec, system);
            Assert.That(system.Amplitudes.Count, Is.EqualTo(4));
        }

        [Test]
        public void Collapse_PartialObserveOnlyTargetBits()
        {
            var system = new QuantumSystem();
            var q0 = new QuBit<int>(system, new[] { 0 });
            var q2 = new QuBit<int>(system, new[] { 2 });
            var q1 = new QuBit<int>(system, new[] { 1 });
            var amps = new System.Collections.Generic.Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer());
            for (int i = 0; i < 8; i++)
            {
                int[] bits = QuantumAlgorithms.IndexToBits(i, 3);
                amps[bits] = new Complex(1.0 / Math.Sqrt(8), 0);
            }
            system.SetAmplitudes(amps);
            var reg = new QuantumRegister(q0, q2);
            int[] measured = reg.Collapse(new Random(123));
            Assert.That(measured.Length, Is.EqualTo(2));
        }

        [Test]
        public void GetValue_SubSlice_Works()
        {
            var system = new QuantumSystem();
            var reg = QuantumRegister.FromInt(13, 4, system); // 13 = 1101
            int low2 = reg.GetValue(offset: 2, length: 2); // last two bits '0','1' => 1
            Assert.That(low2, Is.EqualTo(1));
        }
    }
}
