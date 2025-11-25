using NUnit.Framework;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;
using System.Numerics;
using System.Linq;

namespace QuantumSoupTester
{
    [TestFixture]
    public class CollapsePropagationTests
    {
        [Test]
        public void ObserveGlobal_PropagatesCollapseAcrossEntanglementGroups()
        {
            var system = new QuantumSystem();
            var qA = new QuBit<int>(system, new[] { 0 }).WithWeights(new Dictionary<int, Complex>{{0,1.0},{1,1.0}}, true);
            var qB = new QuBit<int>(system, new[] { 1 }).WithWeights(new Dictionary<int, Complex>{{0,1.0},{1,1.0}}, true);
            // Link entanglement group
            _ = system.Entanglement.Link("GroupAB", qA, qB);
            system.SetFromTensorProduct(false, qA, qB);
            // Collapse via observing A only
            _ = qA.Observe(new Random(7));
            Assert.That(qA.IsCollapsed, Is.True);
            Assert.That(qB.IsCollapsed, Is.True, "Collapse should propagate to entangled partner.");
        }
    }
}
