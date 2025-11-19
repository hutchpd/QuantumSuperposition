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
    public class EqualityTests
    {
        [Test]
        public void QuantumRegister_AlmostEquals_ComparesSubspace()
        {
            var system = new QuantumSystem();
            var reg1 = QuantumRegister.EPRPair(system);
            var reg2 = QuantumRegister.GHZState(system, 2);
            Assert.That(reg1.AlmostEquals(reg2, 1e-12), Is.True);
            Assert.That(reg1.Equals(reg2), Is.True);
        }

        [Test]
        public void QuantumRegister_AlmostEquals_DetectsDifference()
        {
            var system1 = new QuantumSystem();
            var reg1 = QuantumRegister.EPRPair(system1);
            var phase = new QuantumGate(new Complex[,] {
                {1,0,0,0},
                {0,1,0,0},
                {0,0,1,0},
                {0,0,0, Complex.Exp(Complex.ImaginaryOne * 0.1)}
            });
            reg1 = phase * reg1; // apply phase to |11>

            var system2 = new QuantumSystem();
            var reg2 = QuantumRegister.EPRPair(system2);
            Assert.That(reg1.AlmostEquals(reg2, 1e-6), Is.False);
        }

        [Test]
        public void Eigenstates_AlmostEquals_ByProbabilityMass()
        {
            var e1 = new Eigenstates<int>(new[]{1,2,3});
            e1 = e1.WithWeights(new(){ {1, 1.0}, {2, 1.0}, {3, 0.0} });
            var e2 = new Eigenstates<int>(new[]{1,2,3});
            e2 = e2.WithWeights(new(){ {1, 1.0}, {2, 1.0}, {3, 1e-11} });
            Assert.That(e1.AlmostEquals(e2, 1e-9), Is.True);
        }

        [Test]
        public void Eigenstates_AlmostEquals_FailsOnDifferentSupport()
        {
            var e1 = new Eigenstates<int>(new[]{1,2});
            var e2 = new Eigenstates<int>(new[]{1,3});
            Assert.That(e1.AlmostEquals(e2), Is.False);
        }
    }
}
