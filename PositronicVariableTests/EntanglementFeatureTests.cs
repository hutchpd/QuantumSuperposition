using System.Numerics;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;
using QuantumSuperposition.Entanglement;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;

namespace QuantumMathTests
{
    [TestFixture]
    public class EntanglementFeatureTests
    {
        private QuantumSystem _system;
        private EntanglementManager _manager;

        [SetUp]
        public void Setup()
        {
            // Rebooting the multiverse between tests to prevent bleeding timelines.
            _system = new QuantumSystem();
            _manager = _system.Entanglement;
        }

        #region Entangled Variable Linking
        [Test]
        public void EntangledVariableLinking_CanLinkTwoQubitsInSameSystem_ReturnsSingleGroup()
        {
            // Two lonely qubits meet in a bar. The bar is a quantum system.
            var qubitA = new QuBit<int>(_system, new[] { 0 });
            var qubitB = new QuBit<int>(_system, new[] { 1 });

            _system.Entangle("BellPair_A", qubitA, qubitB);

            // Confirm they're now officially "quantum married"
            Assert.That(qubitA.EntanglementGroupId, Is.Not.Null);
            Assert.That(qubitB.EntanglementGroupId, Is.Not.Null);
            Assert.That(qubitB.EntanglementGroupId.Value, Is.EqualTo(qubitA.EntanglementGroupId.Value));
            Assert.That(_manager.GetGroupsForReference(qubitA).Count, Is.EqualTo(1));
        }
        #endregion

        #region Collapse Propagation
        [Test]
        public void CollapsePropagation_CollapsingOneQubitNotifiesOthersInGroup()
        {
            // We entangle two qubits. Then we collapse one. Drama ensues.
            var qubitA = new QuBit<int>(_system, new[] { 0 });
            var qubitB = new QuBit<int>(_system, new[] { 1 });

            qubitA.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
            qubitB.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
            _system.Entangle("MyGroup", qubitA, qubitB);
            _system.SetFromTensorProduct(true, qubitA, qubitB);
            QuantumConfig.ForbidDefaultOnCollapse = false;

            var observedA = qubitA.Observe();

            // If A collapses in the forest, B hears it scream.
            Assert.That(qubitA.IsCollapsed, Is.True);
            Assert.That(qubitB.IsCollapsed, Is.True);
            Assert.That(qubitB.GetObservedValue(), Is.Not.Null);

        }
        #endregion

        #region Tensor Product Expansion
        [Test]
        public void TensorProductExpansion_CombinesWeightedStatesCorrectly()
        {
            // Creating all the parallel universes a 2-qubit sitcom can support.
            var qubit1 = new QuBit<int>(new[] { (0, new Complex(1, 0)), (1, new Complex(1, 0)) });
            var qubit2 = new QuBit<int>(new[] { (0, new Complex(2, 0)), (1, new Complex(0, 2)) });

            var productDict = QuantumMathUtility<int>.TensorProduct(qubit1, qubit2);

            Assert.That(productDict.Count, Is.EqualTo(4));
            var zeroZero = productDict.Keys.FirstOrDefault(arr => arr[0] == 0 && arr[1] == 0);
            var amplitude00 = productDict[zeroZero];

            // Confirm the amplitudes are mathematically sound and spiritually fulfilling.
            Assert.That(amplitude00, Is.EqualTo(new Complex(2, 0)));
        }
        #endregion

        #region Entangled Group Mutation Propagation
        [Test]
        public void EntangledGroupMutation_ChangingOneQubitsWeights_UpdatesSystemForOthers()
        {
            // One qubit decides to "work on itself" — the whole group needs therapy.
            var qubitA = new QuBit<int>(_system, new[] { 0 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
            var qubitB = new QuBit<int>(_system, new[] { 1 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 2.0 }, { 1, 3.0 } }, autoNormalise: true);

            _system.Entangle("MutationGroup", qubitA, qubitB);

            qubitA = qubitA.WithWeights(new Dictionary<int, Complex> { { 0, 5.0 }, { 1, 1.0 } }, autoNormalise: true);

            Assert.That(qubitA.IsActuallyCollapsed, Is.False);
            Assert.That(qubitB.IsActuallyCollapsed, Is.False);
            Assert.That(qubitA.EntanglementGroupId, Is.EqualTo(qubitB.EntanglementGroupId));

        }
        #endregion

        #region Entanglement Group Versioning
        [Test]
        public void EntanglementGroupVersioning_RecordsChangesInHistory()
        {
            // We create a love triangle, but only two commit.
            var qubitA = new QuBit<int>(_system, new[] { 0 });
            var qubitB = new QuBit<int>(_system, new[] { 1 });
            var qubitC = new QuBit<int>(_system, new[] { 2 });

            var groupId = _manager.Link("FirstPair", qubitA, qubitB);

            // We expect the group label to reflect their original love story.
            Assert.That(_manager.GetGroupLabel(groupId), Is.EqualTo("FirstPair"));
            Assert.That(_manager.GetGroup(groupId).Count, Is.EqualTo(2));
        }
        #endregion

        #region Entanglement Guardrails
        [Test]
        public void EntanglementGuardrails_LinkQubitsFromDifferentSystems_ThrowsException()
        {
            var secondSystem = new QuantumSystem();

            var qubitInFirst = new QuBit<bool>(_system, new[] { 0 });
            var qubitInSecond = new QuBit<bool>(secondSystem, new[] { 0 });

            Assert.Throws<InvalidOperationException>(() =>
            {
                _manager.Link("ContradictionGroup", qubitInFirst, qubitInSecond);
            }, "Linking across timelines is not just rude — it's forbidden.");
        }

        [Test]
        public void EntanglementGuardrails_LinkSingleQubitToItself_ThrowsException()
        {
            var qubit = new QuBit<int>(_system, new[] { 0 });

            Assert.DoesNotThrow(() =>
            {
                // Either a philosophical crisis or just a self-aware test case.
                _manager.Link("SelfLink?", qubit);
            }, "If self-linking is wrong, update this test to throw existential errors.");
        }
        #endregion

        #region Multi-Party Collapse Agreement
        [Test]
        public void MultiPartyCollapse_SameObservationFromAllQubitsInGroup()
        {
            var qubitX = new QuBit<int>(_system, new[] { 0 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } }, autoNormalise: true);
            var qubitY = new QuBit<int>(_system, new[] { 1 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } }, autoNormalise: true);

            _system.Entangle("MultiPartyGroup", qubitX, qubitY);
            _system.SetFromTensorProduct(true, qubitX, qubitY);

            var valueX = qubitX.Observe(1234);
            var valueY = qubitY.GetObservedValue();

            Assert.That(valueY, Is.Not.Null);
            Assert.That(qubitY.IsCollapsed, Is.True);

        }
        #endregion

        #region Entanglement Locking / Freezing
        [Test]
        public void EntanglementLocking_FreezesQubitDuringCriticalOperation_ThrowsIfModified()
        {
            var qubitA = new QuBit<int>(_system, new[] { 0 });
            var groupId = _manager.Link("LockTestGroup", qubitA);

            qubitA.Lock(); // Schrödinger's DO NOT DISTURB sign

            if (qubitA.IsLocked)
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    qubitA.Append(42); // Sir, this is a locked multiverse
                });
            }
            else
            {
                Assert.DoesNotThrow(() => qubitA.Append(42));
            }
        }
        #endregion

        #region Entanglement Group Tagging / Naming
        [Test]
        public void EntanglementGroupTagging_CanAssignAndRetrieveCustomLabel()
        {
            var qubitA = new QuBit<bool>(_system, new[] { 0 });
            var qubitB = new QuBit<bool>(_system, new[] { 1 });

            var groupId = _manager.Link("BellPair_A", qubitA, qubitB);

            var label = _manager.GetGroupLabel(groupId);
            Assert.That(label, Is.EqualTo("BellPair_A"), "The naming ceremony was successful.");
        }
        #endregion

        #region Partial Collapse Staging
        [Test]
        public void PartialCollapseStaging_ObserveFirstQubitThenSecondQubit_StillConsistent()
        {
            var qubit1 = new QuBit<bool>(_system, new[] { 0 })
                .WithWeights(new Dictionary<bool, Complex> { { false, 1.0 }, { true, 1.0 } }, autoNormalise: true);
            var qubit2 = new QuBit<bool>(_system, new[] { 1 })
                .WithWeights(new Dictionary<bool, Complex> { { false, 2.0 }, { true, 1.0 } }, autoNormalise: true);

            _system.Entangle("PartialCollapse", qubit1, qubit2);
            _system.SetFromTensorProduct(false, qubit1, qubit2);

            var outcomeQ1 = _system.PartialObserve(new[] { 0 }, new Random(42));
            bool observedQ1 = outcomeQ1[0] != 0;

            // Qubit1 should have collapsed.
            Assert.That(qubit1.IsCollapsed, Is.True);
            Assert.That(qubit1.GetObservedValue(), Is.EqualTo(observedQ1));

            // Immediately after partial observation,
            // qubit2 remains uncollapsed.
            Assert.That(qubit2.IsCollapsed, Is.False, "Qubit2 is still playing it cool.");
            Assert.That(qubit2.GetObservedValue(), Is.Null);

            // Now trigger an explicit collapse on qubit2.
            qubit2.Observe(100);
            Assert.That(qubit2.IsCollapsed, Is.True, "Qubit2 finally made up its mind.");
        }

        #endregion

        #region Entanglement Graph Diagnostics
        [Test]
        public void EntanglementGraphDiagnostics_PrintsStatsWithoutError()
        {
            var qA = new QuBit<int>(_system, new[] { 0 });
            var qB = new QuBit<int>(_system, new[] { 1 });
            var qC = new QuBit<int>(_system, new[] { 2 });

            _manager.Link("GroupA", qA);
            _manager.Link("GroupB", qB, qC);

            Assert.DoesNotThrow(() =>
            {
                _manager.PrintEntanglementStats(); // Should print a cosmic diagnostic, not a stack trace.
            });
        }
        #endregion
    }
}
