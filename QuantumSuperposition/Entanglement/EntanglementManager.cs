using System.Numerics;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;

namespace QuantumSuperposition.Entanglement
{
    /// <summary>
    /// Manages quantum entanglements, emotional baggage, and awkward dependencies between qubits.
    /// </summary>
    public class EntanglementManager
    {
        private readonly Dictionary<IQuantumReference, HashSet<Guid>> _referenceToGroups = new();
        private readonly Dictionary<Guid, List<IQuantumReference>> _groups = new();
        private readonly Dictionary<Guid, List<EntanglementGroupVersion>> _groupHistory = new();
        private readonly Dictionary<Guid, string> _groupLabels = new();


        /// <summary>
        /// Links two or more quantum references into the same entangled group.
        /// Returns the new group ID.
        /// </summary>
        public Guid Link(string groupLabel, params IQuantumReference[] qubits)
        {
            QuantumSystem? firstSystem = null;
            foreach (var q in qubits)
            {
                // If the reference is a QuBit<int> or QuBit<bool> or QuBit<Complex>, they have _system
                if (q is QuBit<int> qi && qi.System is { } sys)
                {
                    if (firstSystem == null) firstSystem = sys;
                    else if (firstSystem != sys)
                        throw new InvalidOperationException("All qubits must belong to the same QuantumSystem.");
                }
                else if (q is QuBit<bool> qb && qb.System is { } sysB)
                {
                    if (firstSystem == null) firstSystem = sysB;
                    else if (firstSystem != sysB)
                        throw new InvalidOperationException("All qubits must belong to the same QuantumSystem.");
                }
                else if (q is QuBit<Complex> qc && qc.System is { } sysC)
                {
                    if (firstSystem == null) firstSystem = sysC;
                    else if (firstSystem != sysC)
                        throw new InvalidOperationException("All qubits must belong to the same QuantumSystem.");
                }
                else
                    throw new InvalidOperationException("Unsupported qubit type for linking.");
            }

            var id = Guid.NewGuid();
            _groups[id] = qubits.ToList();
            _groupLabels[id] = groupLabel;

            // After creating the group, also update the referenceToGroups dictionary
            foreach (var q in qubits)
            {
                if (!_referenceToGroups.TryGetValue(q, out var groupSet))
                {
                    groupSet = new HashSet<Guid>();
                    _referenceToGroups[q] = groupSet;
                }
                groupSet.Add(id);
            }

            _groupHistory[id] = new List<EntanglementGroupVersion>();

            _groupHistory[id].Add(new EntanglementGroupVersion
            {
                GroupId = id,
                Members = qubits.ToList(),
                ReasonForChange = "Initial link"
            });

            return id;
        }

        public string GetGroupLabel(Guid groupId) => _groupLabels.TryGetValue(groupId, out var label) ? label : "Unnamed Group";

        /// <summary>
        /// Gets all references in the same entangled group, by ID.
        /// </summary>
        public IReadOnlyList<IQuantumReference> GetGroup(Guid id)
            => _groups.TryGetValue(id, out var list) ? list : Array.Empty<IQuantumReference>();

        /// <summary>
        /// Propagate a collapse event to all references in the same entangled group.
        /// Each reference should update its internal state accordingly.
        /// </summary>
        public void PropagateCollapse(Guid groupId, Guid collapseId)
        {
            var visitedGroups = new HashSet<Guid>();
            var visitedReferences = new HashSet<IQuantumReference>();

            PropagateRecursive(groupId, collapseId, visitedGroups, visitedReferences);
        }

        private void PropagateRecursive(Guid currentGroupId, Guid collapseId,
                                    HashSet<Guid> visitedGroups,
                                    HashSet<IQuantumReference> visitedReferences)
        {
            // If we’ve already visited this group, skip to avoid cycles
            if (!visitedGroups.Add(currentGroupId))
                return;

            // Retrieve group members
            if (!_groups.TryGetValue(currentGroupId, out var groupMembers))
                return;

            // For each qubit in the group:
            foreach (var q in groupMembers)
            {
                // If we’ve already visited this reference, skip
                if (!visitedReferences.Add(q))
                    continue;

                // Notify each qubit
                q.NotifyWavefunctionCollapsed(collapseId);

                // Then see which other groups this qubit belongs to
                var otherGroups = GetGroupsForReference(q);
                foreach (var g in otherGroups)
                {
                    if (g != currentGroupId)
                        PropagateRecursive(g, collapseId, visitedGroups, visitedReferences);
                }
            }
        }

        /// <summary>
        /// Prints out your entanglement mess like a quantum therapist.
        /// Now featuring graphs, circular drama, and chaos percentages you definitely didn't ask for.
        /// </summary>
        public void PrintEntanglementStats()
        {
            Console.WriteLine("=== Entanglement Graph Diagnostics ===");
            Console.WriteLine("Total entangled groups: " + _groups.Count);

            // Total unique qubits participating in at least one group
            int totalUniqueQubits = _referenceToGroups.Count;
            Console.WriteLine("Total unique qubits: " + totalUniqueQubits);

            // Print each group's size
            foreach (var (id, list) in _groups)
            {
                Console.WriteLine($"Group {id}: {list.Count} qubits");
            }

            // Circular references: qubits that belong to more than one group
            int circularCount = _referenceToGroups.Values.Count(g => g.Count > 1);
            double circularPercent = totalUniqueQubits > 0
                ? (circularCount / (double)totalUniqueQubits) * 100.0
                : 0;
            Console.WriteLine($"Circular references: {circularCount} qubits ({circularPercent:F2}%)");

            // Chaos %: extra entanglement links relative to unique qubits.
            int totalLinks = _referenceToGroups.Values.Sum(g => g.Count);
            double chaosPercent = totalUniqueQubits > 0
                ? ((totalLinks - totalUniqueQubits) / (double)totalUniqueQubits) * 100.0
                : 0;
            Console.WriteLine($"Chaos %: {chaosPercent:F2}%");
        }

        /// <summary>
        /// Returns the social circles this qubit is entangled with.
        /// Warning: may include toxic group chats.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public List<Guid> GetGroupsForReference(IQuantumReference q)
        {
            return _referenceToGroups.TryGetValue(q, out var s) ? s.ToList() : new List<Guid>();
        }

    }
}
