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
            if (qubits == null || qubits.Length == 0)
            {
                throw new ArgumentException("At least one qubit required for entanglement link.");
            }

            QuantumSystem? system = null;
            foreach (IQuantumReference q in qubits)
            {
                if (system == null)
                {
                    system = q.System;
                }
                else if (q.System != system)
                {
                    throw new InvalidOperationException("All qubits must belong to the same QuantumSystem.");
                }
            }

            Guid id = Guid.NewGuid();
            _groups[id] = qubits.ToList();
            _groupLabels[id] = groupLabel;
            foreach (IQuantumReference q in qubits)
            {
                if (!_referenceToGroups.TryGetValue(q, out HashSet<Guid>? set))
                {
                    set = [];
                    _referenceToGroups[q] = set;
                }
                _ = set.Add(id);
            }
            _groupHistory[id] = [ new EntanglementGroupVersion { GroupId = id, Members = qubits.ToList(), ReasonForChange = "Initial link" } ];
            return id;
        }

        public string GetGroupLabel(Guid groupId) => _groupLabels.TryGetValue(groupId, out string? label) ? label : "Unnamed Group";
        public IReadOnlyList<IQuantumReference> GetGroup(Guid id) => _groups.TryGetValue(id, out List<IQuantumReference>? list) ? list : Array.Empty<IQuantumReference>();
        public void PropagateCollapse(Guid groupId, Guid collapseId)
        {
            HashSet<Guid> visitedGroups = [];
            HashSet<IQuantumReference> visitedRefs = [];
            PropagateRecursive(groupId, collapseId, visitedGroups, visitedRefs);
        }
        private void PropagateRecursive(Guid current, Guid collapseId, HashSet<Guid> visitedGroups, HashSet<IQuantumReference> visitedRefs)
        {
            if (!visitedGroups.Add(current)) return;
            if (!_groups.TryGetValue(current, out List<IQuantumReference>? members)) return;
            foreach (IQuantumReference q in members)
            {
                if (!visitedRefs.Add(q)) continue;
                q.NotifyWavefunctionCollapsed(collapseId);
                foreach (Guid g in GetGroupsForReference(q)) if (g != current) PropagateRecursive(g, collapseId, visitedGroups, visitedRefs);
            }
        }
        public void PrintEntanglementStats()
        {
            Console.WriteLine("=== Entanglement Graph Diagnostics ===");
            Console.WriteLine("Total entangled groups: " + _groups.Count);
            int totalUnique = _referenceToGroups.Count;
            Console.WriteLine("Total unique qubits: " + totalUnique);
            foreach ((Guid id, List<IQuantumReference> list) in _groups) Console.WriteLine($"Group {id}: {list.Count} qubits");
            int circular = _referenceToGroups.Values.Count(g => g.Count > 1);
            double circularPercent = totalUnique > 0 ? circular / (double)totalUnique * 100.0 : 0;
            Console.WriteLine($"Circular references: {circular} qubits ({circularPercent:F2}%)");
            int totalLinks = _referenceToGroups.Values.Sum(g => g.Count);
            double chaos = totalUnique > 0 ? (totalLinks - totalUnique) / (double)totalUnique * 100.0 : 0;
            Console.WriteLine($"Chaos %: {chaos:F2}%");
        }
        public List<Guid> GetGroupsForReference(IQuantumReference q) => _referenceToGroups.TryGetValue(q, out HashSet<Guid>? s) ? s.ToList() : [];
    }
}
