using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;
using System.Numerics;

namespace QuantumSuperposition.Entanglement
{
    /// <summary>
    /// Manages quantum entanglements, emotional baggage, and awkward dependencies between qubits.
    /// </summary>
    public class EntanglementManager
    {
        private readonly Dictionary<IQuantumReference, HashSet<Guid>> _referenceToGroups = [];
        private readonly Dictionary<Guid, List<IQuantumReference>> _groups = [];
        private readonly Dictionary<Guid, List<EntanglementGroupVersion>> _groupHistory = [];
        private readonly Dictionary<Guid, string> _groupLabels = [];


        /// <summary>
        /// Links two or more quantum references into the same entangled group.
        /// Returns the new group ID.
        /// </summary>
        public Guid Link(string groupLabel, params IQuantumReference[] qubits)
        {
            QuantumSystem? firstSystem = null;
            foreach (IQuantumReference q in qubits)
            {
                // If the reference is a QuBit<int> or QuBit<bool> or QuBit<Complex>, they have _system
                if (q is QuBit<int> qi && qi.System is { } sys)
                {
                    if (firstSystem == null)
                    {
                        firstSystem = sys;
                    }
                    else if (firstSystem != sys)
                    {
                        throw new InvalidOperationException("All qubits must belong to the same QuantumSystem.");
                    }
                }
                else if (q is QuBit<bool> qb && qb.System is { } sysB)
                {
                    if (firstSystem == null)
                    {
                        firstSystem = sysB;
                    }
                    else if (firstSystem != sysB)
                    {
                        throw new InvalidOperationException("All qubits must belong to the same QuantumSystem.");
                    }
                }
                else if (q is QuBit<Complex> qc && qc.System is { } sysC)
                {
                    if (firstSystem == null)
                    {
                        firstSystem = sysC;
                    }
                    else if (firstSystem != sysC)
                    {
                        throw new InvalidOperationException("All qubits must belong to the same QuantumSystem.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unsupported qubit type for linking.");
                }
            }

            Guid id = Guid.NewGuid();
            _groups[id] = qubits.ToList();
            _groupLabels[id] = groupLabel;

            // After creating the group, also update the referenceToGroups dictionary
            foreach (IQuantumReference q in qubits)
            {
                if (!_referenceToGroups.TryGetValue(q, out HashSet<Guid>? groupSet))
                {
                    groupSet = [];
                    _referenceToGroups[q] = groupSet;
                }
                _ = groupSet.Add(id);
            }

            _groupHistory[id] =
            [
                new EntanglementGroupVersion
                {
                    GroupId = id,
                    Members = qubits.ToList(),
                    ReasonForChange = "Initial link"
                },
            ];

            return id;
        }

        public string GetGroupLabel(Guid groupId)
        {
            return _groupLabels.TryGetValue(groupId, out string? label) ? label : "Unnamed Group";
        }

        /// <summary>
        /// Gets all references in the same entangled group, by ID.
        /// </summary>
        public IReadOnlyList<IQuantumReference> GetGroup(Guid id)
        {
            return _groups.TryGetValue(id, out List<IQuantumReference>? list) ? list : Array.Empty<IQuantumReference>();
        }

        /// <summary>
        /// Propagate a collapse event to all references in the same entangled group.
        /// Each reference should update its internal state accordingly.
        /// </summary>
        public void PropagateCollapse(Guid groupId, Guid collapseId)
        {
            HashSet<Guid> visitedGroups = [];
            HashSet<IQuantumReference> visitedReferences = [];

            PropagateRecursive(groupId, collapseId, visitedGroups, visitedReferences);
        }

        private void PropagateRecursive(Guid currentGroupId, Guid collapseId,
                                    HashSet<Guid> visitedGroups,
                                    HashSet<IQuantumReference> visitedReferences)
        {
            // If we’ve already visited this group, skip to avoid cycles
            if (!visitedGroups.Add(currentGroupId))
            {
                return;
            }

            // Retrieve group members
            if (!_groups.TryGetValue(currentGroupId, out List<IQuantumReference>? groupMembers))
            {
                return;
            }

            // For each qubit in the group:
            foreach (IQuantumReference q in groupMembers)
            {
                // If we’ve already visited this reference, skip
                if (!visitedReferences.Add(q))
                {
                    continue;
                }

                // Notify each qubit
                q.NotifyWavefunctionCollapsed(collapseId);

                // Then see which other groups this qubit belongs to
                List<Guid> otherGroups = GetGroupsForReference(q);
                foreach (Guid g in otherGroups)
                {
                    if (g != currentGroupId)
                    {
                        PropagateRecursive(g, collapseId, visitedGroups, visitedReferences);
                    }
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
            foreach ((Guid id, List<IQuantumReference> list) in _groups)
            {
                Console.WriteLine($"Group {id}: {list.Count} qubits");
            }

            // Circular references: qubits that belong to more than one group
            int circularCount = _referenceToGroups.Values.Count(g => g.Count > 1);
            double circularPercent = totalUniqueQubits > 0
                ? circularCount / (double)totalUniqueQubits * 100.0
                : 0;
            Console.WriteLine($"Circular references: {circularCount} qubits ({circularPercent:F2}%)");

            // Chaos %: extra entanglement links relative to unique qubits.
            int totalLinks = _referenceToGroups.Values.Sum(g => g.Count);
            double chaosPercent = totalUniqueQubits > 0
                ? (totalLinks - totalUniqueQubits) / (double)totalUniqueQubits * 100.0
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
            return _referenceToGroups.TryGetValue(q, out HashSet<Guid>? s) ? s.ToList() : [];
        }

    }
}
