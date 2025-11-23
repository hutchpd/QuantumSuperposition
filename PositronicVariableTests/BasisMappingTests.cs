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
            // Local enum qubit with two states
            var q = new QuBit<BitEnum>(new[] { BitEnum.Zero, BitEnum.One });

            Assert.DoesNotThrow(() => system.SetFromTensorProduct(false, q));

            // Expect two basis states [0] and [1]
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

            // Custom mapper: Banana -> 1, Apple -> 0
            int Mapper(FunkyEnum v) => v == FunkyEnum.Banana ? 1 : 0;

            Assert.DoesNotThrow(() => system.SetFromTensorProduct(true, Mapper, q));

            // Should have two states [0] and [1] per mapper
            var keys = system.Amplitudes.Keys.ToArray();
            Assert.That(keys.Length, Is.EqualTo(2));
            Assert.That(keys.Any(k => k.Length == 1 && k[0] == 0));
            Assert.That(keys.Any(k => k.Length == 1 && k[0] == 1));
        }
    }
}
