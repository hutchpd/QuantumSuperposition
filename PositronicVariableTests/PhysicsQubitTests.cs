using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using QuantumSuperposition.QuantumSoup;

namespace QuantumSoupTester
{
    [TestFixture]
    public class PhysicsQubitTests
    {
        [Test]
        public void Zero_And_One_Shortcuts_Work()
        {
            PhysicsQubit z = PhysicsQubit.Zero;
            PhysicsQubit o = PhysicsQubit.One;

            var zw = z.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            var ow = o.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            Assert.That(zw.ContainsKey(0), Is.True);
            Assert.That(zw.ContainsKey(1), Is.True);
            Assert.That(zw[0].Magnitude, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(zw[1].Magnitude, Is.EqualTo(0.0).Within(1e-12));

            Assert.That(ow.ContainsKey(0), Is.True);
            Assert.That(ow.ContainsKey(1), Is.True);
            Assert.That(ow[0].Magnitude, Is.EqualTo(0.0).Within(1e-12));
            Assert.That(ow[1].Magnitude, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void Amplitude_Ctor_Normalises()
        {
            PhysicsQubit q = new(new Complex(2, 0), new Complex(0, 0));
            var w = q.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            Assert.That(w[0].Magnitude, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(w[1].Magnitude, Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void Components_Ctor_Works()
        {
            PhysicsQubit q = new(0.0, 0.0, 1.0, 0.0);
            var w = q.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);
            Assert.That(w[1].Magnitude, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void BlochSphere_Ctor_Produces_Equal_Superposition_For_Theta_PiOver2_Phi_0()
        {
            PhysicsQubit q = new(Math.PI / 2.0, 0.0);
            var w = q.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            // |alpha| = |beta| = 1/sqrt(2)
            double invSqrt2 = 1.0 / Math.Sqrt(2.0);
            Assert.That(w[0].Magnitude, Is.EqualTo(invSqrt2).Within(1e-12));
            Assert.That(w[1].Magnitude, Is.EqualTo(invSqrt2).Within(1e-12));

            // Probabilities sum to 1
            double probSum = w.Values.Sum(c => c.Magnitude * c.Magnitude);
            Assert.That(probSum, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void BlochSphere_Ctor_Preserves_Phase_On_Beta()
        {
            double theta = Math.PI / 2.0;
            double phi = Math.PI / 2.0; // 90 degrees phase
            PhysicsQubit q = new(theta, phi);
            var w = q.ToWeightedValues().ToDictionary(x => x.value, x => x.weight);

            Assert.That(w[0], Is.Not.EqualTo(Complex.Zero));
            Assert.That(w[1].Phase, Is.EqualTo(phi).Within(1e-12));
        }
    }
}
