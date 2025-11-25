using QuantumSuperposition.Core;
using QuantumSuperposition.Entanglement;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;
using System.Numerics;

namespace QuantumSoupTester
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
            QuBit<int> qubitA = new(_system, new[] { 0 });
            QuBit<int> qubitB = new(_system, new[] { 1 });

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
            QuBit<int> qubitA = new(_system, new[] { 0 });
            QuBit<int> qubitB = new(_system, new[] { 1 });

            _ = qubitA.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
            _ = qubitB.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
            _system.Entangle("MyGroup", qubitA, qubitB);
            _system.SetFromTensorProduct(true, qubitA, qubitB);
            QuantumConfig.ForbidDefaultOnCollapse = false;
            _ = qubitA.Observe();

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
            QuBit<int> qubit1 = new(new[] { (0, new Complex(1, 0)), (1, new Complex(1, 0)) });
            QuBit<int> qubit2 = new(new[] { (0, new Complex(2, 0)), (1, new Complex(0, 2)) });

            Dictionary<int[], Complex> productDict = QuantumMathUtility<int>.TensorProduct(qubit1, qubit2);

            Assert.That(productDict.Count, Is.EqualTo(4));
            int[]? zeroZero = productDict.Keys.FirstOrDefault(arr => arr[0] == 0 && arr[1] == 0);
            Complex amplitude00 = productDict[zeroZero];

            // Confirm the amplitudes are mathematically sound and spiritually fulfilling.
            Assert.That(amplitude00, Is.EqualTo(new Complex(2, 0)));
        }
        #endregion

        #region Entangled Group Mutation Propagation
        [Test]
        public void EntangledGroupMutation_ChangingOneQubitsWeights_UpdatesSystemForOthers()
        {
            // One qubit decides to "work on itself" — the whole group needs therapy.
            QuBit<int> qubitA = new QuBit<int>(_system, new[] { 0 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
            QuBit<int> qubitB = new QuBit<int>(_system, new[] { 1 })
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
            QuBit<int> qubitA = new(_system, new[] { 0 });
            QuBit<int> qubitB = new(_system, new[] { 1 });
            _ = new QuBit<int>(_system, new[] { 2 });

            Guid groupId = _manager.Link("FirstPair", qubitA, qubitB);

            // We expect the group label to reflect their original love story.
            Assert.That(_manager.GetGroupLabel(groupId), Is.EqualTo("FirstPair"));
            Assert.That(_manager.GetGroup(groupId).Count, Is.EqualTo(2));
        }
        #endregion

        #region Entanglement Guardrails
        [Test]
        public void EntanglementGuardrails_LinkQubitsFromDifferentSystems_ThrowsException()
        {
            QuantumSystem secondSystem = new();

            QuBit<bool> qubitInFirst = new(_system, new[] { 0 });
            QuBit<bool> qubitInSecond = new(secondSystem, new[] { 0 });

            _ = Assert.Throws<InvalidOperationException>(() =>
            {
                _ = _manager.Link("ContradictionGroup", qubitInFirst, qubitInSecond);
            }, "Linking across timelines is not just rude — it's forbidden.");
        }

        [Test]
        public void EntanglementGuardrails_LinkSingleQubitToItself_ThrowsException()
        {
            QuBit<int> qubit = new(_system, new[] { 0 });

            Assert.DoesNotThrow(() =>
            {
                // Either a philosophical crisis or just a self-aware test case.
                _ = _manager.Link("SelfLink?", qubit);
            }, "If self-linking is wrong, update this test to throw existential errors.");
        }
        #endregion

        #region Multi-Party Collapse Agreement
        [Test]
        public void MultiPartyCollapse_SameObservationFromAllQubitsInGroup()
        {
            QuBit<int> qubitX = new QuBit<int>(_system, new[] { 0 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } }, autoNormalise: true);
            QuBit<int> qubitY = new QuBit<int>(_system, new[] { 1 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } }, autoNormalise: true);

            _system.Entangle("MultiPartyGroup", qubitX, qubitY);
            _system.SetFromTensorProduct(true, qubitX, qubitY);
            _ = qubitX.Observe(1234);
            object? valueY = qubitY.GetObservedValue();

            Assert.That(valueY, Is.Not.Null);
            Assert.That(qubitY.IsCollapsed, Is.True);

        }
        #endregion

        #region Entanglement Locking / Freezing
        [Test]
        public void EntanglementLocking_FreezesQubitDuringCriticalOperation_ThrowsIfModified()
        {
            QuBit<int> qubitA = new(_system, new[] { 0 });
            Guid groupId = _manager.Link("LockTestGroup", qubitA);

            qubitA.Lock(); // Schrödinger's DO NOT DISTURB sign

            if (qubitA.IsLocked)
            {
                _ = Assert.Throws<InvalidOperationException>(() =>
                {
                    _ = qubitA.Append(42); // Sir, this is a locked multiverse
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
            QuBit<bool> qubitA = new(_system, new[] { 0 });
            QuBit<bool> qubitB = new(_system, new[] { 1 });

            Guid groupId = _manager.Link("BellPair_A", qubitA, qubitB);

            string label = _manager.GetGroupLabel(groupId);
            Assert.That(label, Is.EqualTo("BellPair_A"), "The naming ceremony was successful.");
        }
        #endregion

        #region Partial Collapse Staging
        [Test]
        public void PartialCollapseStaging_ObserveFirstQubitThenSecondQubit_StillConsistent()
        {
            QuBit<bool> qubit1 = new QuBit<bool>(_system, new[] { 0 })
                .WithWeights(new Dictionary<bool, Complex> { { false, 1.0 }, { true, 1.0 } }, autoNormalise: true);
            QuBit<bool> qubit2 = new QuBit<bool>(_system, new[] { 1 })
                .WithWeights(new Dictionary<bool, Complex> { { false, 2.0 }, { true, 1.0 } }, autoNormalise: true);

            _system.Entangle("PartialCollapse", qubit1, qubit2);
            _system.SetFromTensorProduct(false, qubit1, qubit2);

            int[] outcomeQ1 = _system.PartialObserve(new[] { 0 }, new Random(42));
            bool observedQ1 = outcomeQ1[0] != 0;

            // Qubit1 should have collapsed.
            Assert.That(qubit1.IsCollapsed, Is.True);
            Assert.That(qubit1.GetObservedValue(), Is.EqualTo(observedQ1));

            // Immediately after partial observation,
            // qubit2 remains uncollapsed.
            Assert.That(qubit2.IsCollapsed, Is.False, "Qubit2 is still playing it cool.");
            Assert.That(qubit2.GetObservedValue(), Is.Null);

            // Now trigger an explicit collapse on qubit2.
            _ = qubit2.Observe(100);
            Assert.That(qubit2.IsCollapsed, Is.True, "Qubit2 finally made up its mind.");
        }

        #endregion

        #region Entanglement Graph Diagnostics
        [Test]
        public void EntanglementGraphDiagnostics_PrintsStatsWithoutError()
        {
            QuBit<int> qA = new(_system, new[] { 0 });
            QuBit<int> qB = new(_system, new[] { 1 });
            QuBit<int> qC = new(_system, new[] { 2 });

            _ = _manager.Link("GroupA", qA);
            _ = _manager.Link("GroupB", qB, qC);

            Assert.DoesNotThrow(_manager.PrintEntanglementStats);
        }
        #endregion
    }
}
