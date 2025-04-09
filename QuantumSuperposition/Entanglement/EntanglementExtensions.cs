using QuantumSuperposition.Systems;
using QuantumSuperposition.QuantumSoup;

namespace QuantumSuperposition.Entanglement
{
    public static class EntanglementExtensions
    {
        /// <summary>
        /// Bond them in holy quantum matrimony. Till decoherence do them part.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="system"></param>
        /// <param name="groupLabel"></param>
        /// <param name="qubits"></param>
        public static void Entangle<T>(this QuantumSystem system, string groupLabel, params QuBit<T>[] qubits)
        {
            // Use the updated Link method with the provided label.
            var groupId = system.Entanglement.Link(groupLabel, qubits);
            foreach (var q in qubits)
                q.SetEntanglementGroup(groupId);
        }

    }

    public class EntanglementGroupVersion
    {
        public Guid GroupId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public List<IQuantumReference> Members { get; init; } = new();
        public string? ReasonForChange { get; init; }
    }
}
