using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;

namespace QuantumSoupTester
{
    public enum BitEnum { Zero = 0, One = 1 }
    public enum FunkyEnum { Banana = 42, Apple = 7 }

    [TestFixture]
    public class BasisMappingTests
    {
        [Test]
        public void SetFromTensorProduct_EnumDefaultMapper_Works()
        {
            var system = new QuantumSystem();
            var q = new QuBit<BitEnum>(new[] { BitEnum.Zero, BitEnum.One });
            Assert.DoesNotThrow(() => system.SetFromTensorProduct(false, q));
            var keys = system.Amplitudes.Keys.ToArray();
            Assert.That(keys.Length, Is.EqualTo(2));
            Assert.That(keys.Any(k => k.Length == 1 && k[0] == 0));
            Assert.That(keys.Any(k => k.Length == 1 && k[0] == 1));
        }

        [Test]
        public void SetFromTensorProduct_CustomMapper_Works()
        {
            var system = new QuantumSystem();
            var q = new QuBit<FunkyEnum>(new[] { FunkyEnum.Banana, FunkyEnum.Apple });
            int Mapper(FunkyEnum v) => v == FunkyEnum.Banana ? 1 : 0;
            Assert.DoesNotThrow(() => system.SetFromTensorProduct(true, Mapper, q));
            var keys = system.Amplitudes.Keys.ToArray();
            Assert.That(keys.Length, Is.EqualTo(2));
            Assert.That(keys.Any(k => k.Length == 1 && k[0] == 0));
            Assert.That(keys.Any(k => k.Length == 1 && k[0] == 1));
        }

        private readonly struct BitPair
        {
            public readonly int High; public readonly int Low;
            public BitPair(int h, int l){ High = h; Low = l; }
        }

        [Test]
        public void SetFromTensorProduct_StructCustomMapper_Works()
        {
            var system = new QuantumSystem();
            int Map(BitPair bp) => (bp.High << 1) | bp.Low;
            var qx = new QuBit<BitPair>(new[] { new BitPair(0,0), new BitPair(0,1) });
            var qy = new QuBit<BitPair>(new[] { new BitPair(1,0), new BitPair(1,1) });
            system.SetFromTensorProduct(false, Map, qx, qy);
            Assert.That(system.Amplitudes.Count, Is.EqualTo(4));
            Assert.That(system.Amplitudes.Keys.Any(k => k[0] == 0 && k[1] == 2));
        }

        [Test]
        public void SetFromTensorProduct_TwoEnumQubits_BuildsFourStates()
        {
            var system = new QuantumSystem();
            var qa = new QuBit<BitEnum>(new[] { BitEnum.Zero, BitEnum.One });
            var qb = new QuBit<BitEnum>(new[] { BitEnum.Zero, BitEnum.One });
            system.SetFromTensorProduct(false, qa, qb);
            Assert.That(system.Amplitudes.Count, Is.EqualTo(4));
            Assert.That(system.Amplitudes.Keys.Any(k => k[0] == 0 && k[1] == 1));
        }
    }
}
