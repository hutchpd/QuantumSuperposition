using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace QuantumSuperposition.QuantumSoup
{


    /// <summary>
    /// A polite interface so quantum observables can pretend to have structure.
    /// Think of it like customer service for probabilistic particles.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQuantumObservable<T>
    {
        /// <summary>
        /// Performs an observation (i.e. collapse) of the quantum state using an optional Random instance.
        /// </summary>
        T Observe(Random? rng = null);

        /// <summary>
        /// Collapses the quantum state based on weighted probabilities.
        /// </summary>
        T CollapseWeighted();

        /// <summary>
        /// Samples the quantum state probabilistically, without collapsing it.
        /// </summary>
        T SampleWeighted(Random? rng = null);

        /// <summary>
        /// Returns the weighted values of the quantum states.
        /// </summary>
        IEnumerable<(T value, Complex weight)> ToWeightedValues();
    }

    /// <summary>
    /// A common interface for all qubits that participate in an entangled system. 
    /// It acts as a "handle" that the QuantumSystem can use to communicate with individual qubits.
    /// </summary>
    public interface IQuantumReference
    {
        /// <summary>
        /// Returns the indices within the global quantum system this qubit spans.
        /// For example, a single qubit may be index 0, or a 2-qubit register may be [0,1].
        /// </summary>
        int[] GetQubitIndices();

        /// <summary>
        /// Called by QuantumSystem after a collapse or update.
        /// Used to refresh local views, cached states, or flags like IsCollapsed.
        /// </summary>
        void NotifyWavefunctionCollapsed(Guid collapseId);

        /// <summary>
        /// Whether this reference is currently collapsed.
        /// </summary>
        bool IsCollapsed { get; }

        /// <summary>
        /// Gets the current observed value, if already collapsed.
        /// Otherwise throws or returns default depending on implementation.
        /// </summary>
        object? GetObservedValue();

        /// <summary>
        /// Collapse this reference and return a value from the superposition.
        /// The return type is object for type-agnostic access. Use typed qubits to get T.
        /// </summary>
        object Observe(Random? rng = null);

        /// <summary>
        /// Applies a unitary gate matrix (e.g. Hadamard, Pauli-X) to the local qubit state.
        /// Or subspace of the global wavefunction if entangled.
        /// </summary>
        void ApplyLocalUnitary(Complex[,] gate, string gateName);
    }
}
