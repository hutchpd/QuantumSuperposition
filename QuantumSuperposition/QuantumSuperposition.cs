using System;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;

// Because quantum code should be at least as confusing as quantum physics.
#region QuantumCore

/// <summary>
/// Represents the current mood of the QuBit. Is it feeling inclusive (All)?
/// Or indecisive (Any)? Or has it finally made up its mind (Collapsed)?
/// </summary>
public enum QuantumStateType
{
    SuperpositionAll,      // All states must be true. Like group projects, but successful.
    SuperpositionAny,      // Any state can be true. Like excuses for missing deadlines.
    CollapsedResult        // Only one state remains after collapse. R.I.P. potential.
}

// A set of mathematical operations tailored for types that wish they were numbers.
public interface IQuantumOperators<T>
{
    T Add(T a, T b);
    T Subtract(T a, T b);
    T Multiply(T a, T b);
    T Divide(T a, T b);
    T Mod(T a, T b);
    bool GreaterThan(T a, T b);
    bool GreaterThanOrEqual(T a, T b);
    bool LessThan(T a, T b);
    bool LessThanOrEqual(T a, T b);
    bool Equal(T a, T b);
    bool NotEqual(T a, T b);
}

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
/// It acts as a “handle” that the QuantumSystem can use to communicate with individual qubits.
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
    void ApplyLocalUnitary(Complex[,] gate);
}


/// <summary>
/// Like a gym trainer, but for ints. Does the usual heavy lifting.
/// </summary>
public class IntOperators : IQuantumOperators<int>
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
    public int Divide(int a, int b) => a / b;
    public int Mod(int a, int b) => a % b;
    public bool GreaterThan(int a, int b) => a > b;
    public bool GreaterThanOrEqual(int a, int b) => a >= b;
    public bool LessThan(int a, int b) => a < b;
    public bool LessThanOrEqual(int a, int b) => a <= b;
    public bool Equal(int a, int b) => a == b;
    public bool NotEqual(int a, int b) => a != b;
}

public class BooleanOperators : IQuantumOperators<bool>
{
    public bool Add(bool a, bool b) => a || b; // logical OR
    public bool Subtract(bool a, bool b) => a && !b; // arbitrary definition
    public bool Multiply(bool a, bool b) => a && b; // logical AND
    public bool Divide(bool a, bool b) => throw new NotSupportedException("Division not supported for booleans.");
    public bool Mod(bool a, bool b) => throw new NotSupportedException("Modulus not supported for booleans.");
    public bool GreaterThan(bool a, bool b) => false; // not well-defined
    public bool GreaterThanOrEqual(bool a, bool b) => a == b; // or false
    public bool LessThan(bool a, bool b) => false;
    public bool LessThanOrEqual(bool a, bool b) => a == b;
    public bool Equal(bool a, bool b) => a == b;
    public bool NotEqual(bool a, bool b) => a != b;
}

/// <summary>
/// Like a quantum therapist, but for complex numbers. Helps them add, subtract, and multiply their feelings.
/// </summary>
public class ComplexOperators : IQuantumOperators<Complex>
{
    public Complex Add(Complex a, Complex b) => a + b;
    public Complex Subtract(Complex a, Complex b) => a - b;
    public Complex Multiply(Complex a, Complex b) => a * b;
    public Complex Divide(Complex a, Complex b) => a / b;

    // Modulo isn't mathematically defined for Complex, so we throw if attempted
    public Complex Mod(Complex a, Complex b) => throw new NotSupportedException("Modulus not supported for complex numbers.");

    // Comparisons on complex numbers are ambiguous, but we’ll define some heuristics based on magnitude:
    public bool GreaterThan(Complex a, Complex b) => a.Magnitude > b.Magnitude;
    public bool GreaterThanOrEqual(Complex a, Complex b) => a.Magnitude >= b.Magnitude;
    public bool LessThan(Complex a, Complex b) => a.Magnitude < b.Magnitude;
    public bool LessThanOrEqual(Complex a, Complex b) => a.Magnitude <= b.Magnitude;
    public bool Equal(Complex a, Complex b) => a == b;
    public bool NotEqual(Complex a, Complex b) => a != b;
}

public class EntanglementGroupVersion
{
    public Guid GroupId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public List<IQuantumReference> Members { get; init; } = new();
    public string? ReasonForChange { get; init; }
}

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

/// <summary>
/// Represents a density matrix system for mixed quantum states.
/// For when your wavefunction has commitment issues and prefers statistical ambiguity.
/// </summary>
/// <typeparam name="T"></typeparam>
public class DensityMatrixSystem<T>
{
    // Internal density matrix stored as a dictionary with keys (bra, ket)
    // where each key is an int[] representing a basis state.
    private Dictionary<(int[] bra, int[] ket), Complex> _matrix;

    /// <summary>
    /// Constructs a density matrix from a pure quantum system.
    /// That is, if |ψ⟩ has amplitudes from system.Amplitudes, then
    /// ρ = |ψ⟩⟨ψ| is built with elements ρ(bra, ket) = ψ(bra) * Conjugate(ψ(ket)).
    /// </summary>
    public DensityMatrixSystem(QuantumSystem quantumSystem)
    {
        if (quantumSystem == null)
            throw new ArgumentNullException(nameof(quantumSystem));

        // Initialize the matrix dictionary using an appropriate comparer.
        _matrix = new Dictionary<(int[] bra, int[] ket), Complex>(new BasisPairComparer());

        // For each pair of basis states in the quantum system, form the outer product.
        foreach (var bra in quantumSystem.Amplitudes.Keys)
        {
            Complex ampBra = quantumSystem.Amplitudes[bra];
            foreach (var ket in quantumSystem.Amplitudes.Keys)
            {
                Complex ampKet = quantumSystem.Amplitudes[ket];
                // Use clones of the arrays to avoid reference issues.
                int[] braCopy = (int[])bra.Clone();
                int[] ketCopy = (int[])ket.Clone();
                _matrix[(braCopy, ketCopy)] = ampBra * Complex.Conjugate(ampKet);
            }
        }
    }

    /// <summary>
    /// A second constructor that allows for a custom density matrix.
    /// </summary>
    /// <param name="matrix"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DensityMatrixSystem(Dictionary<(int[] bra, int[] ket), Complex> matrix)
    {
        _matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
    }

    /// <summary>
    /// Returns the internal density matrix dictionary.
    /// </summary>
    public Dictionary<(int[] bra, int[] ket), Complex> GetMatrix() => _matrix;

    /// <summary>
    /// Updates the density matrix via: ρ → U ρ U†.
    /// The unitary gate (of size 2^n × 2^n) is applied on the qubits specified in targetQubits.
    // It's math cosplay for matrices. But with quantum flair.
    /// </summary>
    public void ApplyUnitary(Complex[,] gate, int[] targetQubits)
    {
        if (gate == null) throw new ArgumentNullException(nameof(gate));
        if (targetQubits == null) throw new ArgumentNullException(nameof(targetQubits));

        // Number of qubits the gate acts on.
        int n = targetQubits.Length;
        int d = gate.GetLength(0);
        if (gate.GetLength(1) != d)
            throw new ArgumentException("Gate must be a square matrix.");
        if (d != (1 << n))
            throw new ArgumentException("Gate dimension does not match the number of target qubits.");

        var newMatrix = new Dictionary<(int[] bra, int[] ket), Complex>(new BasisPairComparer());

        // For each element of the original density matrix:
        foreach (var kvp in _matrix)
        {
            int[] bra = kvp.Key.bra;
            int[] ket = kvp.Key.ket;
            Complex value = kvp.Value;

            // Extract the parts (substates) corresponding to target qubits.
            int[] braSub = ExtractSubstate(bra, targetQubits);
            int[] ketSub = ExtractSubstate(ket, targetQubits);
            int braIndex = BitsToIndex(braSub);
            int ketIndex = BitsToIndex(ketSub);

            // The unitary acts as: new bra = Σ₍i₌0₎^(d–1) U[i, braIndex] (updated target bits),
            // and new ket = Σ₍j₌0₎^(d–1) U[j, ketIndex] (with conjugation on the ket side).
            for (int i = 0; i < d; i++)
            {
                int[] newBra = ReplaceSubstate(bra, targetQubits, IndexToBits(i, n));
                for (int j = 0; j < d; j++)
                {
                    int[] newKet = ReplaceSubstate(ket, targetQubits, IndexToBits(j, n));
                    Complex contribution = gate[i, braIndex] * value * Complex.Conjugate(gate[j, ketIndex]);
                    var key = (newBra, newKet);
                    if (newMatrix.ContainsKey(key))
                        newMatrix[key] += contribution;
                    else
                        newMatrix[key] = contribution;
                }
            }
        }
        _matrix = newMatrix;
    }

    /// <summary>
    /// Returns the probability of measuring the system in the given complete basis state.
    /// This is the diagonal element ρ(outcome, outcome).
    /// </summary>
    public double GetProbabilityOf(int[] outcome)
    {
        if (outcome == null) throw new ArgumentNullException(nameof(outcome));
        foreach (var kvp in _matrix)
        {
            if (ArraysEqual(kvp.Key.bra, outcome) && ArraysEqual(kvp.Key.ket, outcome))
                return kvp.Value.Real; // Diagonal elements should be real.
        }
        return 0.0;
    }

    /// <summary>
    /// Measures the qubits specified by targetQubits and collapses the density matrix.
    /// It computes the probability for each outcome on those qubits, samples one,
    /// then projects and normalises the density matrix accordingly.
    /// Returns the measured outcome (as an int[] for the target qubits).
    /// </summary>
    public int[] MeasureAndCollapse(int[] targetQubits)
    {
        if (targetQubits == null) throw new ArgumentNullException(nameof(targetQubits));
        int n = targetQubits.Length;
        // We'll accumulate probabilities for each outcome by reading the diagonal elements.
        var outcomeProbs = new Dictionary<string, double>();
        var outcomeMapping = new Dictionary<string, int[]>();

        foreach (var kvp in _matrix)
        {
            // Only consider diagonal elements.
            if (ArraysEqual(kvp.Key.bra, kvp.Key.ket))
            {
                int[] sub = ExtractSubstate(kvp.Key.bra, targetQubits);
                string key = string.Join(",", sub);
                if (outcomeProbs.ContainsKey(key))
                    outcomeProbs[key] += kvp.Value.Real;
                else
                {
                    outcomeProbs[key] = kvp.Value.Real;
                    outcomeMapping[key] = sub;
                }
            }
        }

        double totalProb = outcomeProbs.Values.Sum();
        if (totalProb <= 0)
            throw new InvalidOperationException("Total probability is zero. Measurement cannot be performed.");

        // Sample an outcome based on the probabilities.
        double roll = Random.Shared.NextDouble() * totalProb;
        double cumulative = 0.0;
        string chosenKey = null;
        foreach (var kvp in outcomeProbs)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
            {
                chosenKey = kvp.Key;
                break;
            }
        }
        if (chosenKey == null)
            chosenKey = outcomeProbs.Keys.Last();
        int[] chosenOutcome = outcomeMapping[chosenKey];

        // Project: Only keep matrix elements for which both bra and ket have the chosen outcome on target qubits.
        var newMatrix = new Dictionary<(int[] bra, int[] ket), Complex>(new BasisPairComparer());
        double projProb = outcomeProbs[chosenKey];
        foreach (var kvp in _matrix)
        {
            int[] bra = kvp.Key.bra;
            int[] ket = kvp.Key.ket;
            int[] braSub = ExtractSubstate(bra, targetQubits);
            int[] ketSub = ExtractSubstate(ket, targetQubits);
            if (ArraysEqual(braSub, chosenOutcome) && ArraysEqual(ketSub, chosenOutcome))
            {
                // Normalise by dividing by the probability.
                int[] braCopy = (int[])bra.Clone();
                int[] ketCopy = (int[])ket.Clone();
                newMatrix[(braCopy, ketCopy)] = kvp.Value / projProb;
            }
        }
        _matrix = newMatrix;
        return chosenOutcome;
    }

    /// <summary>
    /// Performs a partial trace over the qubits specified in qubitIndices.
    /// This returns a new DensityMatrixSystem with a reduced state.
    /// </summary>
    public DensityMatrixSystem<T> TraceOutQubits(int[] qubitIndices)
    {
        if (qubitIndices == null) throw new ArgumentNullException(nameof(qubitIndices));
        if (_matrix.Count == 0)
            throw new InvalidOperationException("Density matrix is empty.");

        // Determine which indices remain after tracing out.
        int totalQubits = _matrix.Keys.First().bra.Length;
        int[] remainingIndices = Enumerable.Range(0, totalQubits)
                                           .Where(i => !qubitIndices.Contains(i))
                                           .ToArray();

        var newMatrix = new Dictionary<(int[] bra, int[] ket), Complex>(new BasisPairComparer());
        foreach (var kvp in _matrix)
        {
            int[] bra = kvp.Key.bra;
            int[] ket = kvp.Key.ket;
            // Only contribute if the traced-out parts are identical.
            bool include = true;
            foreach (int i in qubitIndices)
            {
                if (bra[i] != ket[i])
                {
                    include = false;
                    break;
                }
            }
            if (include)
            {
                int[] newBra = ExtractSubstate(bra, remainingIndices);
                int[] newKet = ExtractSubstate(ket, remainingIndices);
                var key = (newBra, newKet);
                if (newMatrix.ContainsKey(key))
                    newMatrix[key] += kvp.Value;
                else
                    newMatrix[key] = kvp.Value;
            }
        }
        return new DensityMatrixSystem<T>(newMatrix);
    }

    /// <summary>
    /// Converts this density matrix back to a pure QuantumSystem if the state is rank‑1.
    /// </summary>
    public QuantumSystem ToQuantumSystem()
    {
        // A pure state density matrix has the form ρ = |ψ⟩⟨ψ|.
        // One way to recover ψ is to choose a diagonal element (with nonzero value)
        // and then compute ψ(bra) = ρ(bra, refState) / sqrt(ρ(refState, refState)).
        var diag = _matrix.Where(kvp => ArraysEqual(kvp.Key.bra, kvp.Key.ket))
                          .ToDictionary(kvp => kvp.Key.bra, kvp => kvp.Value);
        int[] referenceState = null;
        Complex refValue = 0;
        foreach (var kvp in diag)
        {
            if (kvp.Value.Magnitude > 1e-12)
            {
                referenceState = kvp.Key;
                refValue = kvp.Value;
                break;
            }
        }
        if (referenceState == null)
            throw new InvalidOperationException("Cannot convert to pure state; density matrix is zero.");
        double norm = Math.Sqrt(refValue.Real);

        // Recover amplitudes for each basis state.
        var amplitudes = new Dictionary<int[], Complex>(new IntArrayComparer());
        foreach (var kvp in _matrix)
        {
            // Look for elements of the form ρ(bra, referenceState)
            if (ArraysEqual(kvp.Key.ket, referenceState))
            {
                int[] bra = kvp.Key.bra;
                amplitudes[bra] = kvp.Value / norm;
            }
        }
        return new QuantumSystem(amplitudes);
    }

    /// <summary>
    /// Computes the purity of the density matrix, i.e. Tr(ρ²).
    /// For a pure state this equals 1.
    /// </summary>
    public double Purity()
    {
        double purity = 0.0;
        foreach (var kvp in _matrix)
        {
            purity += kvp.Value.Magnitude * kvp.Value.Magnitude;
        }
        return purity;
    }

    #region Helper Functions

    // Extracts the substate (subset of bits) from a full basis state.
    private static int[] ExtractSubstate(int[] fullState, int[] indices)
    {
        int[] sub = new int[indices.Length];
        for (int i = 0; i < indices.Length; i++)
            sub[i] = fullState[indices[i]];
        return sub;
    }

    // Checks whether two int arrays are element‐wise equal.
    private static bool ArraysEqual(int[] a, int[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }

    // Converts an array of bits (assumed ordered from most significant to least) into an integer index.
    private static int BitsToIndex(int[] bits)
    {
        int index = 0;
        foreach (int bit in bits)
            index = (index << 1) | bit;
        return index;
    }

    // Converts an integer into an array of bits of the given length.
    private static int[] IndexToBits(int index, int length)
    {
        int[] bits = new int[length];
        for (int i = length - 1; i >= 0; i--)
        {
            bits[i] = index & 1;
            index >>= 1;
        }
        return bits;
    }

    // Replaces the bits in fullState at positions given by indices with newBits.
    private static int[] ReplaceSubstate(int[] fullState, int[] indices, int[] newBits)
    {
        int[] result = (int[])fullState.Clone();
        for (int i = 0; i < indices.Length; i++)
            result[indices[i]] = newBits[i];
        return result;
    }

    // Comparer for (int[] bra, int[] ket) keys in the density matrix dictionary.
    private class BasisPairComparer : IEqualityComparer<(int[] bra, int[] ket)>
    {
        public bool Equals((int[] bra, int[] ket) x, (int[] bra, int[] ket) y)
        {
            return ArraysEqual(x.bra, y.bra) && ArraysEqual(x.ket, y.ket);
        }
        public int GetHashCode((int[] bra, int[] ket) obj)
        {
            int hash = 17;
            foreach (var val in obj.bra)
                hash = hash * 31 + val;
            foreach (var val in obj.ket)
                hash = hash * 31 + val;
            return hash;
        }
    }

    #endregion
}

/// <summary>
/// Currently supports int, and complex numbers. Futer support may include irrational hope, and emotional baggage.
/// </summary>
public static class QuantumOperatorsFactory
{
    public static IQuantumOperators<T> GetOperators<T>()
    {
        if (typeof(T) == typeof(int))
            return (IQuantumOperators<T>)(object)new IntOperators();

        if (typeof(T) == typeof(Complex))
            return (IQuantumOperators<T>)(object)new ComplexOperators();

        if (typeof(T) == typeof(bool))
            return (IQuantumOperators<T>)(object)new BooleanOperators();

        throw new NotImplementedException("Default operators not implemented for type " + typeof(T));
    }
}

// Utility for combining values. Think of it like a quantum kitchen mixer.
public static class QuantumMathUtility<T>
{
    public static IEnumerable<T> CombineAll(IEnumerable<T> a, IEnumerable<T> b, Func<T, T, T> op) =>
        a.SelectMany(x => b.Select(y => op(x, y)));

    public static IEnumerable<T> Combine(IEnumerable<T> a, T b, Func<T, T, T> op) =>
        a.Select(x => op(x, b));

    public static IEnumerable<T> Combine(T a, IEnumerable<T> b, Func<T, T, T> op) =>
        b.Select(x => op(a, x));

    /// <summary>
    /// Applies a matrix to a vector, returning the resulting vector.
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="matrix"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Complex[] ApplyMatrix(Complex[] vector, Complex[,] matrix)
    {
        int dim = vector.Length;

        if (matrix.GetLength(0) != dim || matrix.GetLength(1) != dim)
            throw new ArgumentException($"Matrix must be square with size {dim}×{dim}.");

        var result = new Complex[dim];
        for (int i = 0; i < dim; i++)
        {
            result[i] = Complex.Zero;
            for (int j = 0; j < dim; j++)
            {
                result[i] += matrix[i, j] * vector[j];
            }
        }

        return result;
    }

    /// <summary>
    /// Applies a quantum gate (unitary matrix) to a state vector.
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="gate"></param>
    /// <returns></returns>
    public static Complex[] ApplyGate(Complex[] vector, Complex[,] gate)
    => ApplyMatrix(vector, gate);

    /// <summary>
    /// Computes the tensor product of multiple QuBits, returning a dictionary of state combinations with combined amplitudes.
    /// </summary>
    public static Dictionary<T[], Complex> TensorProduct<T>(params QuBit<T>[] qubits)
    {
        if (qubits == null || qubits.Length == 0)
            throw new ArgumentException("At least one qubit must be provided.");

        // Start with a single empty state with amplitude 1
        var result = new List<(List<T> state, Complex amplitude)>
        {
            (new List<T>(), Complex.One)
        };

        foreach (var qubit in qubits)
        {
            var newResult = new List<(List<T>, Complex)>();
            foreach (var (prefix, amp) in result)
            {
                foreach (var (value, weight) in qubit.ToWeightedValues())
                {
                    var newState = new List<T>(prefix) { value };
                    newResult.Add((newState, amp * weight));
                }
            }
            result = newResult;
        }

        // Convert to final dictionary
        var dict = new Dictionary<T[], Complex>(new TensorKeyComparer<T>());
        foreach (var (state, amp) in result)
        {
            dict[state.ToArray()] = amp;
        }

        return dict;
    }

    // A comparer for arrays of T
    private class TensorKeyComparer<TK> : IEqualityComparer<TK[]>
    {
        public bool Equals(TK[]? x, TK[]? y)
        {
            if (x == null || y == null) return false;
            return x.SequenceEqual(y);
        }

        public int GetHashCode(TK[] obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var item in obj)
                {
                    hash = hash * 31 + (item?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }
    }


}

/// <summary>
/// static class to hold basis transforms (e.g. Hadamard):
/// </summary>
public static class QuantumBasis
{
    public static Complex[,] Hadamard => new Complex[,]
    {
        { 1 / Math.Sqrt(2),  1 / Math.Sqrt(2) },
        { 1 / Math.Sqrt(2), -1 / Math.Sqrt(2) }
    };

    public static Complex[,] Identity => new Complex[,]
    {
        { 1, 0 },
        { 0, 1 }
    };

    public static Complex[] ApplyBasis(Complex[] amplitudes, Complex[,] basisMatrix)
    {
        int dim = amplitudes.Length;
        Complex[] result = new Complex[dim];

        for (int i = 0; i < dim; i++)
        {
            result[i] = Complex.Zero;
            for (int j = 0; j < dim; j++)
            {
                result[i] += basisMatrix[i, j] * amplitudes[j];
            }
        }

        return result;
    }
}

public class QuantumSystem
{
    private Dictionary<int[], Complex> _amplitudes = new Dictionary<int[], Complex>();
    private readonly List<IQuantumReference> _registered = new();
    private EntanglementManager _entanglement = new();
    public EntanglementManager Entanglement => _entanglement;


    /// <summary>
    /// Optionally construct the system with explicit amplitudes.
    /// </summary>

    public QuantumSystem(Dictionary<int[], Complex>? initialAmps = null)

    {
        if (initialAmps != null)
        {

            _amplitudes = initialAmps;
            NormaliseAmplitudes();
        }
    }
    /// <summary>
    /// I only want to know a little bit about you.
    /// </summary>
    /// <param name="measuredIndices"></param>
    /// <param name="rng"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public int[] PartialObserve(int[] measuredIndices, Random? rng = null)
    {
        rng ??= Random.Shared;

        // Group the basis states by the values on the measured indices.
        var groups = new Dictionary<string, List<(int[] state, Complex amplitude)>>();
        foreach (var kvp in _amplitudes)
        {
            int[] state = kvp.Key;
            int[] projection = measuredIndices.Select(i => state[i]).ToArray();
            string key = string.Join(",", projection);
            if (!groups.ContainsKey(key))
                groups[key] = new List<(int[] state, Complex amplitude)>();
            groups[key].Add((state, kvp.Value));
        }

        // Compute probabilities and sample an outcome.
        var outcomeProbs = groups.ToDictionary(
            g => g.Key,
            g => g.Value.Sum(item => item.amplitude.Magnitude * item.amplitude.Magnitude)
        );
        double totalProb = outcomeProbs.Values.Sum();
        if (totalProb < 1e-15)
            throw new InvalidOperationException("Wavefunction is effectively zero.");

        double roll = rng.NextDouble() * totalProb;
        double cumulative = 0.0;
        string? chosenKey = null;
        foreach (var kv in outcomeProbs)
        {
            cumulative += kv.Value;
            if (roll <= cumulative)
            {
                chosenKey = kv.Key;
                break;
            }
        }
        chosenKey ??= outcomeProbs.Keys.Last();
        int[] chosenOutcome = chosenKey.Split(',').Select(int.Parse).ToArray();

        // Instead of fully projecting the global state,
        // we do NOT update _amplitudes (i.e. do not replace it with newAmplitudes).
        // Instead, we only notify the qubits whose indices are measured to update
        // their local state (via a new method, e.g. PartialCollapse) without altering _amplitudes.

        var collapseId = Guid.NewGuid();
        foreach (var refQ in _registered)
        {
            // Only update local state for qubits whose indices intersect measuredIndices.
            if (refQ.GetQubitIndices().Intersect(measuredIndices).Any())
            {
                // Call a “partial collapse” that only sets this qubit’s local observed value.
                (refQ as QuBit<bool>)?.PartialCollapse(chosenOutcome);
            }
        }

        // Return the measured outcome for the measured qubit(s).
        return chosenOutcome;
    }




    /// <summary>
    /// Returns the complete collapsed state from the system
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public int[] GetCollapsedState()
    {
        if (_amplitudes.Count != 1)
        {
            throw new InvalidOperationException("System is not fully collapsed.");
        }
        return _amplitudes.Keys.First();
    }


    public void Register(IQuantumReference qubitRef)
    {
        _registered.Add(qubitRef);
    }

    /// <summary>
    /// A simple “observe everything fully” method.
    /// TODO: for partial measurement, expand this to do partial sums.
    /// </summary>
    /// <param name="qubitIndices"></param>
    /// <param name="rng"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public int[] ObserveGlobal(int[] qubitIndices, Random rng)
    {
        if (qubitIndices == null || qubitIndices.Length == 0)
            throw new ArgumentNullException(nameof(qubitIndices));

        // Group amplitudes by projected values on qubitIndices
        var projectionGroups = new Dictionary<int[], List<(int[] state, Complex amplitude)>>(new IntArrayComparer());

        foreach (var kvp in _amplitudes)
        {
            var fullState = kvp.Key;
            var projected = qubitIndices.Select(i => fullState[i]).ToArray();

            if (!projectionGroups.ContainsKey(projected))
                projectionGroups[projected] = new();

            projectionGroups[projected].Add((fullState, kvp.Value));
        }

        // Step 2: Compute total probabilities for each projection group
        var probSums = projectionGroups.ToDictionary(
            g => g.Key,
            g => g.Value.Sum(x => x.amplitude.Magnitude * x.amplitude.Magnitude),
            new IntArrayComparer()
        );

        double totalProb = probSums.Values.Sum();
        if (totalProb <= 1e-15)
            throw new InvalidOperationException("All probabilities are zero or wavefunction is empty.");

        // Sample a group based on probability
        double roll = rng.NextDouble() * totalProb;
        double cumulative = 0.0;

        int[] chosenProjection = Array.Empty<int>();
        foreach (var (proj, p) in probSums)
        {
            cumulative += p;
            if (roll <= cumulative)
            {
                chosenProjection = proj;
                break;
            }
        }

        // Retain only amplitudes matching the chosen projection
        var newAmps = new Dictionary<int[], Complex>(new IntArrayComparer());
        foreach (var (state, amp) in projectionGroups[chosenProjection])
        {
            newAmps[state] = amp;
        }

        _amplitudes = newAmps;

        // Renormalise
        NormaliseAmplitudes();

        // Notify
        var collapseId = Guid.NewGuid();
        foreach (var refQ in _registered)
        {
            // Check if the reference's qubit indices overlap with the observed indices.
            if (refQ.GetQubitIndices().Intersect(qubitIndices).Any())
            {
                refQ.NotifyWavefunctionCollapsed(collapseId);
                if (refQ is QuBit<int> qi && qi.EntanglementGroupId is Guid g1)
                {
                    _entanglement.PropagateCollapse(g1, collapseId);
                }
                else if (refQ is QuBit<bool> qb && qb.EntanglementGroupId is Guid g2)
                {
                    _entanglement.PropagateCollapse(g2, collapseId);
                }
                else if (refQ is QuBit<Complex> qc && qc.EntanglementGroupId is Guid g3)
                {
                    _entanglement.PropagateCollapse(g3, collapseId);
                }
            }
        }

        // Return observed values
        return chosenProjection;
    }

    /// <summary>
    /// Builds a full wavefunction from individual qubits.
    /// It’s like assembling IKEA furniture, but all the screws are in superposition.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="qubits"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void SetFromTensorProduct<T>(bool propagateCollapse, params QuBit<T>[] qubits)
    {
        if (qubits == null || qubits.Length == 0)
            throw new ArgumentException("At least one qubit must be provided.");

        int totalQubits = qubits.Length;

        // Update each qubit with a system-linked version.
        for (int i = 0; i < totalQubits; i++)
        {
            var original = qubits[i];
            var systemQubit = new QuBit<T>(original.States, original.Operators);
            var weights = original.ToWeightedValues().ToDictionary(w => w.value, w => w.weight);
            var withWeights = systemQubit.WithWeights(weights, autoNormalise: false);
            qubits[i] = withWeights;
        }

        // Compute tensor product using the updated qubits.
        var product = QuantumMathUtility<T>.TensorProduct(qubits);

        // Convert to a global int[] wavefunction.
        var result = new Dictionary<int[], Complex>(new IntArrayComparer());
        foreach (var (state, amplitude) in product)
        {
            int[] intState = state.Select(s =>
            {
                if (s is int i) return i;
                if (s is bool b) return b ? 1 : 0;
                throw new InvalidOperationException($"Unsupported type {typeof(T)} in tensor product state.");
            }).ToArray();

            result[intState] = amplitude;
        }

        _amplitudes = result;
        NormaliseAmplitudes();

        if (propagateCollapse)
        {
            var collapseId = Guid.NewGuid();
            foreach (var q in _registered)
                q.NotifyWavefunctionCollapsed(collapseId);
        }
    }


    /// <summary>
    /// Applies a two-qubit gate, because one qubit can't handle this much chaos alone.
    /// Perfect for entangling your problems in pairs.
    /// </summary>
    public void ApplyTwoQubitGate(int qubitA, int qubitB, Complex[,] gate)
    {
        if (gate.GetLength(0) != 4 || gate.GetLength(1) != 4)
            throw new ArgumentException("Gate must be a 4x4 matrix.");

        // Group basis states by fixed values of all qubits *except* qubitA and qubitB
        var grouped = new Dictionary<string, List<int[]>>();

        foreach (var state in _amplitudes.Keys.ToList())
        {
            // Build a string key excluding qubitA and qubitB (used to group together the 4 basis states)
            var key = string.Join(",", state.Select((val, idx) => (idx != qubitA && idx != qubitB) ? val.ToString() : "*"));

            if (!grouped.ContainsKey(key))
                grouped[key] = new();

            grouped[key].Add(state);
        }

        var newAmplitudes = new Dictionary<int[], Complex>(new IntArrayComparer());

        // Process each group (should contain exactly 4 states representing the 2 qubits)
        foreach (var group in grouped.Values)
        {
            // Build a 4-vector for current basis states
            var basisMap = new Dictionary<(int, int), int[]>();
            var vec = new Complex[4];

            foreach (var basis in group)
            {
                int a = basis[qubitA];
                int b = basis[qubitB];
                int index = (a << 1) | b; // binary encoding: 00, 01, 10, 11 → 0–3

                vec[index] = _amplitudes.TryGetValue(basis, out var amp) ? amp : Complex.Zero;
                basisMap[(a, b)] = basis;
            }

            // Apply the gate: newVec = gate * vec
            var newVec = QuantumMathUtility<Complex>.ApplyMatrix(vec, gate);

            // Store updated amplitudes back in _amplitudes
            foreach (var ((a, b), state) in basisMap)
            {
                int idx = (a << 1) | b;
                newAmplitudes[state] = newVec[idx];
            }
        }

        _amplitudes = newAmplitudes;
        NormaliseAmplitudes();
    }

    public void ApplySingleQubitGate(int qubit, Complex[,] gate)
    {
        if (gate.GetLength(0) != 2 || gate.GetLength(1) != 2)
            throw new ArgumentException("Single-qubit gate must be a 2x2 matrix.");

        var newAmps = new Dictionary<int[], Complex>(new IntArrayComparer());

        foreach (var state in _amplitudes.Keys.ToList())
        {
            int bit = state[qubit];

            var basis0 = (int[])state.Clone(); basis0[qubit] = 0;
            var basis1 = (int[])state.Clone(); basis1[qubit] = 1;

            Complex a0 = _amplitudes.TryGetValue(basis0, out var amp0) ? amp0 : Complex.Zero;
            Complex a1 = _amplitudes.TryGetValue(basis1, out var amp1) ? amp1 : Complex.Zero;

            Complex newAmp = gate[bit, 0] * a0 + gate[bit, 1] * a1;

            newAmps[state] = newAmp;
        }

        _amplitudes = newAmps;
        NormaliseAmplitudes();
    }

    /// <summary>
    /// Normalises amplitudes so the universe doesn’t implode.
    /// Math nerds call this “preserving probability”. We call it “not being weird.”
    /// </summary>
    private void NormaliseAmplitudes()
    {
        double total = _amplitudes.Values.Sum(a => a.Magnitude * a.Magnitude);
        if (total < 1e-15)
        {
            // All zero => do nothing or reset
            return;
        }
        double norm = Math.Sqrt(total);
        foreach (var k in _amplitudes.Keys.ToList())
        {
            _amplitudes[k] /= norm;
        }
    }

    /// <summary>
    /// Exposes the dictionary for debugging or advanced usage.
    /// Be careful: direct modifications can break normalisation.
    /// </summary>
    public IReadOnlyDictionary<int[], Complex> Amplitudes => _amplitudes;
}

public class IntArrayComparer : IEqualityComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        if (x == null || y == null) return false;
        return x.SequenceEqual(y);
    }

    public int GetHashCode(int[] obj)
    {
        unchecked
        {
            int hash = 17;
            foreach (int val in obj)
            {
                hash = hash * 31 + val;
            }
            return hash;
        }
    }
}

#endregion

#region QuantumConfig

public static class QuantumConfig
{
    // Set to true to disallow collapse if the resulting value equals default(T).
    public static bool ForbidDefaultOnCollapse { get; set; } = true;
}

#endregion

#region QuantumSoup
public abstract class QuantumSoup<T> : IQuantumObservable<T>
{
    protected Dictionary<T, Complex>? _weights;
    protected bool _isActuallyCollapsed;
    protected T? _collapsedValue;
    protected bool _mockCollapseEnabled;
    protected T? _mockCollapseValue;
    protected Guid? _collapseHistoryId;
    protected int? _lastCollapseSeed;
    protected Func<T, bool> _valueValidator = _ => true;
    protected QuantumStateType _eType;
    protected IQuantumOperators<T> _ops = QuantumOperatorsFactory.GetOperators<T>();
    protected bool _weightsAreNormalised;
    protected static readonly double _tolerance = 1e-9;
    private bool _isFrozen => _isActuallyCollapsed;

    public abstract IReadOnlyCollection<T> States { get; }

    public bool IsWeighted => _weights != null;
    public bool IsActuallyCollapsed => _isActuallyCollapsed && _eType == QuantumStateType.CollapsedResult;
    public Guid? LastCollapseHistoryId => _collapseHistoryId;
    public int? LastCollapseSeed => _lastCollapseSeed;
    public QuantumStateType GetCurrentType() => _eType;

    public bool IsLocked { get; private set; } = false;

    public void Lock() => IsLocked = true;
    public void Unlock() => IsLocked = false;

    public void SetType(QuantumStateType t)
    {
        EnsureMutable();
        _eType = t;
    }


    /// <summary>
    /// Guard to ensure mutable operations are only allowed when not collapsed.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void EnsureMutable()
    {
        // _isFrozen checks for collapse; add _isLocked to enforce locking too.
        if (_isFrozen || IsLocked)
        {
            throw new InvalidOperationException("Cannot modify a locked or collapsed QuBit.");
        }
    }

    /// <summary>
    /// Normalises the amplitudes of the QuBit so that the sum of their squared magnitudes equals 1.
    /// </summary>
    public void NormaliseWeights()
    {
        if (_weights == null || _weightsAreNormalised) return;
        double totalProbability = _weights.Values.Select(a => a.Magnitude * a.Magnitude).Sum();
        if (totalProbability <= double.Epsilon) return;
        double normFactor = Math.Sqrt(totalProbability);
        foreach (var k in _weights.Keys.ToList())
        {
            _weights[k] /= normFactor;
        }
        _weightsAreNormalised = true;
    }


    /// <summary>
    /// Reality check: converts quantum indecision into a final verdict.
    /// Basically forces the whole wavefunction to agree like it’s group therapy for particles.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool EvaluateAll()
    {
        SetType(QuantumStateType.SuperpositionAll);

        // More descriptive exception if empty
        if (!States.Any())
        {
            throw new InvalidOperationException(
                $"No states to evaluate. IsCollapsed={_isActuallyCollapsed}, StateCount={States.Count}, Type={_eType}."
            );
        }

        return States.All(state => !EqualityComparer<T>.Default.Equals(state, default(T)));
    }

    public virtual IEnumerable<(T value, Complex weight)> ToWeightedValues()
    {
        if (_weights == null)
        {
            foreach (var v in States.Distinct()) yield return (v, Complex.One);
        }
        else
        {
            foreach (var kvp in _weights)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }
    }

    // Does a little dance, rolls a quantum die, picks a value.
    // May cause minor existential dread or major debugging regrets.
    public virtual T SampleWeighted(Random? rng = null)
    {
        rng ??= Random.Shared;
        if (!States.Any()) throw new InvalidOperationException("No states available.");
        if (!IsWeighted) return States.First();
        NormaliseWeights();
        var probabilities = _weights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Magnitude * kvp.Value.Magnitude);
        double totalProb = probabilities.Values.Sum();
        if (totalProb <= 1e-15) return States.First();
        double roll = rng.NextDouble() * totalProb;
        double cumulative = 0.0;
        foreach (var (key, prob) in probabilities)
        {
            cumulative += prob;
            if (roll <= cumulative) return key;
        }
        return probabilities.Last().Key;
    }

    public virtual QuantumSoup<T> WithWeights(Dictionary<T, Complex> weights, bool autoNormalise = false)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));
        var filtered = new Dictionary<T, Complex>();
        foreach (var kvp in weights)
        {
            if (States.Contains(kvp.Key)) filtered[kvp.Key] = kvp.Value;
        }
        var clone = Clone();
        clone._weights = filtered;
        clone._weightsAreNormalised = false;
        if (autoNormalise) clone.NormaliseWeights();
        return clone;
    }

    public virtual T CollapseWeighted()
    {
        if (!States.Any()) throw new InvalidOperationException("No states available for collapse.");
        if (!IsWeighted) return States.First();
        var key = _weights.MaxBy(x => x.Value.Magnitude)!.Key;
        return key;
    }

    protected bool AllWeightsEqual(Dictionary<T, Complex> dict)
    {
        if (dict.Count <= 1) return true;
        var first = dict.Values.First();
        return dict.Values.Skip(1).All(w => Complex.Abs(w - first) < 1e-14);
    }

    protected bool AllWeightsProbablyEqual(Dictionary<T, Complex> dict)
    {
        if (dict.Count <= 1) return true;
        double firstProb = dict.Values.First().Magnitude * dict.Values.First().Magnitude;
        return dict.Values.Skip(1).All(w => Math.Abs((w.Magnitude * w.Magnitude) - firstProb) < 1e-14);
    }

    /// <summary>
    /// Compares with the grace of a therapist and the precision of a passive-aggressive spreadsheet.
    /// Compare Squared Magnitudes (Probabilistic Equality)
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType()) return false;
        var other = (QuantumSoup<T>)obj;
        var mySet = States.Distinct().ToHashSet();
        var otherSet = other.States.Distinct().ToHashSet();
        if (!mySet.SetEquals(otherSet)) return false;
        if (!IsWeighted && !other.IsWeighted) return true;
        foreach (var s in mySet)
        {
            double p1 = 1.0, p2 = 1.0;
            if (_weights != null && _weights.TryGetValue(s, out var amp1)) p1 = amp1.Magnitude * amp1.Magnitude;
            if (other._weights != null && other._weights.TryGetValue(s, out var amp2)) p2 = amp2.Magnitude * amp2.Magnitude;
            if (Math.Abs(p1 - p2) > _tolerance) return false;
        }
        return true;
    }

    /// <summary>
    /// Checks to see if two things are really the same, or just pretending to be.
    /// Compare Complex Amplitudes (Strict State Equality, as opposed to Probabilistic Equality)
    /// </summary>
    public virtual bool StrictlyEquals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType()) return false;
        var other = (QuantumSoup<T>)obj;
        var mySet = States.Distinct().ToHashSet();
        var otherSet = other.States.Distinct().ToHashSet();
        if (!mySet.SetEquals(otherSet)) return false;
        if (!IsWeighted && !other.IsWeighted) return true;
        foreach (var s in mySet)
        {
            Complex a1 = Complex.One, a2 = Complex.One;
            if (_weights != null && _weights.TryGetValue(s, out var w1)) a1 = w1;
            if (other._weights != null && other._weights.TryGetValue(s, out var w2)) a2 = w2;
            if ((a1 - a2).Magnitude > _tolerance) return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var s in States.Distinct().OrderBy(x => x))
            {
                hash = hash * 23 + (s?.GetHashCode() ?? 0);
                if (IsWeighted && _weights != null && _weights.TryGetValue(s, out var amp))
                {
                    double real = Math.Round(amp.Real, 12);
                    double imag = Math.Round(amp.Imaginary, 12);
                    long realBits = BitConverter.DoubleToInt64Bits(real);
                    long imagBits = BitConverter.DoubleToInt64Bits(imag);
                    hash = hash * 23 + realBits.GetHashCode();
                    hash = hash * 23 + imagBits.GetHashCode();
                }
            }
            return hash;
        }
    }

    public virtual string WeightSummary()
    {
        if (!IsWeighted) return "Weighted: false";
        double totalProbability = _weights.Values.Sum(a => a.Magnitude * a.Magnitude);
        double maxP = _weights.Values.Max(a => a.Magnitude * a.Magnitude);
        double minP = _weights.Values.Min(a => a.Magnitude * a.Magnitude);
        return $"Weighted: true, Sum(|amp|²): {totalProbability}, Max(|amp|²): {maxP}, Min(|amp|²): {minP}";
    }

    // I promise to pick you up from the airport, but I might never buy the car.
    /// <summary>
    /// The quantum clononing paradox: perfect cloning is actually forbidden, but we can still make copies.
    /// </summary>
    /// <returns></returns>
    public abstract QuantumSoup<T> Clone();

    public abstract T Observe(Random? rng = null);


    /// <summary>
    /// Exposes the interface for Qubit Observables
    /// </summary>
    public interface IQuantumObservable<T>

    {
        T Observe(Random? rng = null);
        T CollapseWeighted();
        T SampleWeighted(Random? rng = null);
        IEnumerable<(T value, Complex weight)> ToWeightedValues();
    }
}
#endregion

#region QuBit<T> Implementation

/// <summary>
/// QuBit<T> represents a superposition of values of type T, optionally weighted.
/// It's like Schrödinger's inbox: everything's unread and somehow also read.
/// </summary>
public partial class QuBit<T> : QuantumSoup<T>, IQuantumReference
{
    private readonly Func<T, bool> _valueValidator;
    private readonly int[] _qubitIndices;
    private readonly QuantumSystem? _system;
    private bool _isCollapsedFromSystem;
    private object? _systemObservedValue;
    public QuantumSystem? System => _system;

    private Guid? _entanglementGroupId;
    public Guid? EntanglementGroupId => _entanglementGroupId;
    public void SetEntanglementGroup(Guid id) => _entanglementGroupId = id;

    public bool IsInSuperposition => _eType == QuantumStateType.SuperpositionAny && !_isActuallyCollapsed;

    #region Constructors
    // Constructors that enable you to manifest chaotic energy into a typed container.
    // Also known as: Creating a mess in a mathematically defensible way.


    // A simpler constructor for the entangled case:

    public QuBit(QuantumSystem system, int[] qubitIndices)

    {

        // If T = (int,bool) then qubitIndices might be new[] {0,1}.
        // If T = int only, maybe qubitIndices = new[] {0}, etc.

        _system = system;
        _qubitIndices = qubitIndices ?? Array.Empty<int>();
        _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);

        // I exist yelled the qubit, and the system is my parent! Daddy!!!
        _system.Register(this);

        _qList = new List<T> { (T)Convert.ChangeType(0, typeof(T)), (T)Convert.ChangeType(1, typeof(T)) };

        // This qubit is in superposition until a collapse occurs
        _eType = QuantumStateType.SuperpositionAny;

    }

    // If we have no quantum system, we fallback to local states:
    public QuBit(IEnumerable<T> Items, IQuantumOperators<T> ops, Func<T, bool>? valueValidator = null)
        : this(Items, ops)
    {
        _valueValidator = valueValidator ?? (v => !EqualityComparer<T>.Default.Equals(v, default));
    }

    public QuBit(IEnumerable<T> Items, Func<T, bool>? valueValidator = null)
        : this(Items, _defaultOps, valueValidator)
    { }

    public QuBit(IEnumerable<(T value, Complex weight)> weightedItems, IQuantumOperators<T> ops, Func<T, bool>? valueValidator = null)
        : this(weightedItems, ops)
    {
        _valueValidator = valueValidator ?? (v => !EqualityComparer<T>.Default.Equals(v, default));
    }

    public QuBit(IEnumerable<(T value, Complex weight)> weightedItems, Func<T, bool>? valueValidator = null)
        : this(weightedItems, _defaultOps, valueValidator)
    { }

    internal QuBit(IEnumerable<T> items, Dictionary<T, Complex>? weights, IQuantumOperators<T> ops, Func<T, bool>? valueValidator = null)
        : this(items, weights, ops)
    {
        _valueValidator = valueValidator ?? (v => !EqualityComparer<T>.Default.Equals(v, default));
    }

    /// <summary>
    /// Applies a quantum conditional — like an if-else, but across all timelines.
    /// Each branch of the superposition is checked with a predicate.
    /// If the predicate is true, the <paramref name="ifTrue"/> function is applied to that branch;
    /// otherwise, <paramref name="ifFalse"/> is applied.
    /// 
    /// The resulting states are merged, and their amplitudes are weighted accordingly.
    /// No collapse occurs. Nobody gets observed. Reality remains deeply confused.
    /// </summary>
    /// <param name="predicate">
    /// A function that decides whether a given branch deserves to go down the happy path.
    /// </param>
    /// <param name="ifTrue">
    /// Transformation applied to branches that satisfy the predicate.
    /// Think of this as the "yes, and..." timeline.
    /// </param>
    /// <param name="ifFalse">
    /// Transformation for the other branches — the "meh, fine" timeline.
    /// </param>
    /// <returns>
    /// A new QuBit that merges the transformed branches into one beautifully indecisive superposition.
    /// </returns>
    public QuBit<T> Conditional(
        Func<T, bool> predicate,
        Func<QuBit<T>, QuBit<T>> ifTrue,
        Func<QuBit<T>, QuBit<T>> ifFalse)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (ifTrue == null) throw new ArgumentNullException(nameof(ifTrue));
        if (ifFalse == null) throw new ArgumentNullException(nameof(ifFalse));

        // Gather the aftermath of this branching multiverse decision.
        var newWeights = new Dictionary<T, Complex>();
        var newStates = new List<T>();

        // Walk through each possibility — no judgment, just evaluation.
        foreach (var (value, weight) in ToWeightedValues())
        {
            // Isolate a single universe where this value exists alone.
            var branchQubit = new QuBit<T>(new[] { value }, this.Operators);
            // Optionally, assign the branch weight to the temporary instance.
            // (You might want to use an overload such as WithWeights.)
            branchQubit = branchQubit.WithWeights(new Dictionary<T, Complex> { { value, weight } }, autoNormalise: false);

            // Decide its fate: the good timeline or the disappointing one.
            QuBit<T> mappedBranch = predicate(value)
                ? ifTrue(branchQubit)
                : ifFalse(branchQubit);

            // Merge whatever reality that branch has become back into the main timeline.
            foreach (var (mappedValue, mappedWeight) in mappedBranch.ToWeightedValues())
            {
                // Multiply the weight coming from the mapping with the original branch weight.
                Complex combinedWeight = weight * mappedWeight;

                // Recombine states by accumulating weights.
                if (newWeights.ContainsKey(mappedValue))
                {
                    newWeights[mappedValue] += combinedWeight;
                }
                else
                {
                    newWeights[mappedValue] = combinedWeight;
                    newStates.Add(mappedValue);
                }
            }
        }

        // Return a new qubit that carries the recombined branches with their updated weights.
        return new QuBit<T>(newStates, newWeights, this.Operators);
    }

    /// <summary>
    /// Applies a transformation to every possible state in the qubit — like a map,
    /// but across all realities at once.
    ///
    /// Each branch is run through the <paramref name="selector"/> function, producing
    /// a new set of possibilities in a parallel type universe. The original quantum
    /// amplitudes (weights) are preserved, because we care about continuity.
    ///
    /// No collapse happens. No commitment is made. The universe remains beautifully noncommittal.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type each original state gets transformed into. Like evolving your indecision into a new, equally undecided form.
    /// </typeparam>
    /// <param name="selector">
    /// A function that transforms a state from T to TResult, without actually forcing it to pick one.
    /// </param>
    /// <returns>
    /// A new QuBit<TResult> holding the transformed superposition,
    /// complete with its inherited existential probabilities.
    /// </returns>
    public QuBit<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        // This is the quantum equivalent of "map", but without picking a side.
        // Each state gets passed through the selector, but their identity crisis (weight) stays intact.
        var mappedWeightedValues = this.ToWeightedValues()
            .Select(pair => (value: selector(pair.value), weight: pair.weight));

        // The new qubit lives in a different type universe now, so we find its corresponding math handlers.
        var newOps = QuantumOperatorsFactory.GetOperators<TResult>();

        // Create a new superposition in the new type space.
        // Note: nothing actually happens until someone observes this — classic quantum laziness.
        return new QuBit<TResult>(mappedWeightedValues, newOps);
    }


    public static QuBit<T> Superposed(IEnumerable<T> states)
    {
        var qubit = new QuBit<T>(states);
        var distinctCount = qubit.States.Distinct().Count();
        if (distinctCount > 1)
            qubit._eType = QuantumStateType.SuperpositionAny;
        return qubit;
    }

    // Main constructor for unweighted items (local useage)
    public QuBit(IEnumerable<T> Items, IQuantumOperators<T> ops)
    {
        if (Items == null) throw new ArgumentNullException(nameof(Items));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qList = Items;

        if (_qList.Distinct().Count() > 1)
            _eType = QuantumStateType.SuperpositionAny;

        // no system
        _system = null;
        _qubitIndices = Array.Empty<int>();
        _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);
    }

    public QuBit(IEnumerable<T> Items)
        : this(Items, _defaultOps)
    { }


    public QuBit(IEnumerable<(T value, Complex weight)> weightedItems)
        : this(weightedItems, _defaultOps)
    { }

    public QuBit(IEnumerable<(T value, Complex weight)> weightedItems, IQuantumOperators<T> ops)
    {
        if (weightedItems == null) throw new ArgumentNullException(nameof(weightedItems));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));

        var dict = new Dictionary<T, Complex>();
        foreach (var (val, w) in weightedItems)
        {
            if (!dict.ContainsKey(val))
                dict[val] = Complex.Zero;
            dict[val] += w;
        }
        _weights = dict;
        _qList = dict.Keys; // keep a fallback list of keys

        if (_weights.Count > 1)
            SetType(QuantumStateType.SuperpositionAny);

        // no system
        _system = null;
        _qubitIndices = Array.Empty<int>();
        _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);
    }

    internal QuBit(IEnumerable<T> items, Dictionary<T, Complex>? weights, IQuantumOperators<T> ops)
    {
        _qList = items;
        _weights = weights;
        _ops = ops;

        if (States.Distinct().Count() > 1)
            SetType(QuantumStateType.SuperpositionAny);

        // no system
        _system = null;
        _qubitIndices = Array.Empty<int>();
        _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);
    }


    #endregion


    #region State Type Helpers

    private int[] GetUnionOfGroupIndices()
    {
        if (_system == null)
            return _qubitIndices;
        var union = new HashSet<int>(_qubitIndices);
        // Get all groups that this qubit belongs to.
        var groups = _system.Entanglement.GetGroupsForReference(this);
        foreach (var groupId in groups)
        {
            // For each qubit in each group, add its indices.
            foreach (var q in _system.Entanglement.GetGroup(groupId))
            {
                union.UnionWith(q.GetQubitIndices());
            }
        }
        return union.OrderBy(i => i).ToArray();
    }


    public QuBit<T> Append(T element)
    {
        EnsureMutable();

        if (_weights != null)
        {
            if (!_weights.ContainsKey(element))
                _weights[element] = 0.0;
            _weights[element] += 1.0;
            _qList = _weights.Keys;
        }
        else
        {
            _qList = _qList.Concat(new[] { element });
        }

        if (States.Distinct().Count() > 1)
            SetType(QuantumStateType.SuperpositionAny);

        return this;
    }


    public static QuBit<T> WithEqualAmplitudes(IEnumerable<T> states)
    {
        var list = states.Distinct().ToList();
        double amp = 1.0 / Math.Sqrt(list.Count);
        var weighted = list.Select(s => (s, new Complex(amp, 0)));
        return new QuBit<T>(weighted);
    }
    #endregion

    #region Immutable Clone

    /// <summary>
    /// This quantum decision was final… but I copied it into a new universe where it wasn’t
    /// Cloning a collapsed QuBit resets its collapse status. The new instance retains the amplitudes and quantum state type, but is mutable and behaves as if never observed.
    /// </summary>
    public override QuantumSoup<T> Clone()
    {
        var clonedWeights = _weights != null ? new Dictionary<T, Complex>(_weights) : null;
        var clonedList = _qList.ToList();
        var clone = new QuBit<T>(clonedList, clonedWeights, _ops, _valueValidator);
        clone._isActuallyCollapsed = false;
        clone._isCollapsedFromSystem = false;
        clone._collapsedValue = default;
        clone._collapseHistoryId = null;
        clone._lastCollapseSeed = null;
        clone._eType = this._eType == QuantumStateType.CollapsedResult ? QuantumStateType.SuperpositionAny : this._eType;
        return clone;
    }


    #endregion

    #region Fields for Local Storage (used if _system == null)

    private IEnumerable<T> _qList;
    private static readonly IQuantumOperators<T> _defaultOps = QuantumOperatorsFactory.GetOperators<T>();


    public IQuantumOperators<T> Operators => _ops;

    #endregion
    #region State Type Helpers
    // Lets you toggle between 'All must be true', 'Any might be true', and 'Reality is now a lie'.
    // Think of it like mood settings, but for wavefunctions.
    // Also, it helps you avoid existential crises by keeping track of your quantum state.

    public QuantumStateType GetCurrentType() => _eType;

   

    public QuBit<T> Any() { SetType(QuantumStateType.SuperpositionAny); return this; }
    public QuBit<T> All() { SetType(QuantumStateType.SuperpositionAll); return this; }

    #endregion

    #region Arithmetic Helpers
    // Implements arithmetic on superpositions, because regular math wasn't confusing enough.
    // Bonus: Now you can multiply the feeling of indecision by a probability cloud.
    // Also, it avoids full key-pair expansion (M×N growth) by combining outputs.

    /// <summary>
    /// Performs the specified operation on two QuBit<T> instances or a QuBit<T> and a scalar.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private QuBit<T> Do_oper_type(QuBit<T> a, QuBit<T> b, Func<T, T, T> op)
    {
        var newList = QuantumMathUtility<T>.CombineAll(a._qList, b._qList, op);

        Dictionary<T, Complex>? newWeights = null;
        if (a._weights != null || b._weights != null)
        {
            newWeights = new Dictionary<T, Complex>();
            foreach (var (valA, wA) in a.ToWeightedValues())
            {
                foreach (var (valB, wB) in b.ToWeightedValues())
                {
                    var newVal = op(valA, valB);
                    var combinedWeight = wA * wB;
                    if (!newWeights.ContainsKey(newVal))
                        newWeights[newVal] = 0.0;
                    newWeights[newVal] += combinedWeight;
                }
            }
        }

        return new QuBit<T>(newList, newWeights, _ops);
    }

    /// <summary>
    /// Performs the specified operation on a QuBit<T> and a scalar.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private QuBit<T> Do_oper_type(QuBit<T> a, T b, Func<T, T, T> op)
    {
        var newList = QuantumMathUtility<T>.Combine(a._qList, b, op);

        Dictionary<T, Complex>? newWeights = null;
        if (a._weights != null)
        {
            newWeights = new Dictionary<T, Complex>();
            foreach (var (valA, wA) in a.ToWeightedValues())
            {
                var newVal = op(valA, b);
                if (!newWeights.ContainsKey(newVal))
                    newWeights[newVal] = 0.0;
                newWeights[newVal] += wA; // multiply by 1.0
            }
        }

        return new QuBit<T>(newList, newWeights, _ops);
    }

    /// <summary>
    /// Performs the specified operation on a scalar and a QuBit<T>.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private QuBit<T> Do_oper_type(T a, QuBit<T> b, Func<T, T, T> op)
    {
        var newList = QuantumMathUtility<T>.Combine(a, b._qList, op);

        Dictionary<T, Complex>? newWeights = null;
        if (b._weights != null)
        {
            newWeights = new Dictionary<T, Complex>();
            foreach (var (valB, wB) in b.ToWeightedValues())
            {
                var newVal = op(a, valB);
                if (!newWeights.ContainsKey(newVal))
                    newWeights[newVal] = 0.0;
                newWeights[newVal] += wB; // multiply by 1.0
            }
        }

        return new QuBit<T>(newList, newWeights, _ops);
    }

    #endregion

    #region Operator Overloads
    // Overloads basic math ops so you can add, subtract, multiply your way through parallel universes.
    // Because plain integers are too committed to a single outcome.


    public static QuBit<T> operator %(T a, QuBit<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Mod(x, y));
    public static QuBit<T> operator %(QuBit<T> a, QuBit<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Mod(x, y));
    public static QuBit<T> operator %(QuBit<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Mod(x, y));

    public static QuBit<T> operator +(T a, QuBit<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Add(x, y));
    public static QuBit<T> operator +(QuBit<T> a, QuBit<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Add(x, y));
    public static QuBit<T> operator +(QuBit<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Add(x, y));

    public static QuBit<T> operator -(T a, QuBit<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Subtract(x, y));
    public static QuBit<T> operator -(QuBit<T> a, QuBit<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Subtract(x, y));
    public static QuBit<T> operator -(QuBit<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Subtract(x, y));

    public static QuBit<T> operator *(T a, QuBit<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Multiply(x, y));
    public static QuBit<T> operator *(QuBit<T> a, QuBit<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Multiply(x, y));
    public static QuBit<T> operator *(QuBit<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Multiply(x, y));

    public static QuBit<T> operator /(T a, QuBit<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Divide(x, y));
    public static QuBit<T> operator /(QuBit<T> a, QuBit<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Divide(x, y));
    public static QuBit<T> operator /(QuBit<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Divide(x, y));

    #endregion

    #region Introspection

    /// <summary>
    /// Observes (collapses) the QuBit with an optional Random instance for deterministic replay.
    /// If the QuBit is already collapsed, returns the previously collapsed value.
    /// If mock-collapse is enabled, returns the forced value without changing the underlying state.
    /// </summary>
    /// <param name="rng">Optional random instance for deterministic replay.</param>
    /// <returns>The collapsed (observed) value.</returns>
    public override T Observe(Random? rng = null)
    {
        return PerformCollapse(rng);
    }


    /// <summary>
    /// Performs the collapse logic:
    /// samples a value from the superposition, validates it,
    /// updates internal state (e.g. _collapsedValue, _qList, _weights),
    /// and marks the qubit as collapsed.
    /// </summary>
    public T PerformCollapse(Random? rng = null)
    {
        rng ??= Random.Shared;

        // If mock collapse is enabled, return the forced value.
        if (_mockCollapseEnabled)
        {
            if (_mockCollapseValue == null)
                throw new InvalidOperationException("Mock collapse enabled but no mock value is set.");
            return _mockCollapseValue;
        }

        // If already collapsed, simply return the collapsed value.
        if (_isActuallyCollapsed && _collapsedValue != null && _eType == QuantumStateType.CollapsedResult)
        {
            return _collapsedValue;
        }

        // Like picking a random life choice and pretending you meant to do that all along.
        T picked = SampleWeighted(rng);

        if (QuantumConfig.ForbidDefaultOnCollapse && !_valueValidator(picked))
            throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");

        // Update internal state to reflect the collapse.
        _collapsedValue = picked;
        _qList = new[] { picked };
        if (_weights != null)
        {
            _weights = new Dictionary<T, Complex> { { picked, 1.0 } };
        }
        SetType(QuantumStateType.CollapsedResult);
        _isActuallyCollapsed = true;

        return picked;
    }

    public void PartialCollapse(int[] chosenOutcome)
    {
        // Only update local state: set _collapsedValue based on _qubitIndices.
        if (_qubitIndices.Length == 1)
        {
            int value = chosenOutcome[0];
            // For bool qubits, convert 0/1 to false/true.
            object observed = typeof(T) == typeof(bool) ? (object)(value != 0) : (object)value;
            _collapsedValue = (T)observed;
        }
        else
        {
            // For multi-index qubits.
            int[] values = _qubitIndices.Select(i => chosenOutcome[i]).ToArray();
            _collapsedValue = (T)(object)values;
        }
        // Mark this qubit as collapsed locally.
        // (Do not update _amplitudes so that other qubits remain uncollapsed.)
        _isActuallyCollapsed = true;
        // Do NOT trigger propagation here.
    }



    /// <summary>
    /// Observes (collapses) the QuBit using a supplied integer seed for deterministic replay.
    /// </summary>
    public T Observe(int seed)
    {
        // store the last collapse seed for debugging
        _lastCollapseSeed = seed;
        _collapseHistoryId = Guid.NewGuid();

        var rng = new Random(seed);
        return Observe(rng);
    }

    public T ObserveInBasis(Complex[,] basisMatrix, Random? rng = null)
    {
        if (_weights == null || _weights.Count == 0)
            throw new InvalidOperationException("No amplitudes available for basis transform.");

        int dimension = _weights.Count;

        if (basisMatrix.GetLength(0) != dimension || basisMatrix.GetLength(1) != dimension)
            throw new ArgumentException($"Basis transform must be a {dimension}×{dimension} square matrix.");

        // Capture the states and their amplitudes
        var states = _weights.Keys.ToArray();
        var amplitudes = states.Select(s => _weights[s]).ToArray();

        // Apply the unitary basis transformation
        var transformed = QuantumMathUtility<Complex>.ApplyMatrix(amplitudes, basisMatrix);

        // Construct new weights
        var newWeights = new Dictionary<T, Complex>();
        for (int i = 0; i < states.Length; i++)
            newWeights[states[i]] = transformed[i];

        // Because nothing says “science” like measuring something after you’ve changed the rules.
        var newQubit = new QuBit<T>(states, newWeights, _ops).WithNormalisedWeights();
        return newQubit.Observe(rng);
    }



    /// <summary>
    /// Returns true if this QuBit has actually collapsed (via a real observation).
    /// </summary>
    public bool IsActuallyCollapsed => _isActuallyCollapsed && _eType == QuantumStateType.CollapsedResult;

    // ---------------------------------------------------------------------
    // Collapse Mocking
    // ---------------------------------------------------------------------

    /// <summary>
    /// Enables mock collapse, causing Observe() to always return forcedValue
    /// without modifying the underlying quantum state.
    /// </summary>
    public QuBit<T> WithMockCollapse(T forcedValue)
    {
        _mockCollapseEnabled = true;
        _mockCollapseValue = forcedValue;
        return this;
    }

    /// <summary>
    /// Disables mock collapse so that Observe() performs a real collapse.
    /// </summary>
    public QuBit<T> WithoutMockCollapse()
    {
        _mockCollapseEnabled = false;
        _mockCollapseValue = default;
        return this;
    }

    /// <summary>
    /// Returns a string representation of the current superposition states and their weights
    /// without triggering a collapse. Useful for debugging and introspection.
    /// </summary>
    public string show_states() => ToDebugString();

    #endregion

    #region Weighting and Output

    // Converts the tangled quantum mess into something printable,
    // so you can lie to yourself and pretend you understand what's going on.
    public IEnumerable<T> ToCollapsedValues()
    {
        if (!States.Any())
        {
            throw new InvalidOperationException(
                $"No values to collapse. IsCollapsed={_isActuallyCollapsed}, StateCount={States.Count}, Type={_eType}."
            );
        }

        if (_eType == QuantumStateType.SuperpositionAny)
            return States;
        else if (_eType == QuantumStateType.SuperpositionAll)
        {
            var distinct = States.Distinct().ToList();
            return distinct.Count == 1 ? distinct : States;
        }
        else
        {
            // Collapsed
            return States.Take(1);
        }
    }

    /// <summary>
    /// Returns a collection of tuples containing the values and their corresponding weights.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<(T value, Complex weight)> ToWeightedValues()
    {
        if (_weights == null)
        {
            // All distinct items with weight=1.0
            var distinct = _qList.Distinct();
            foreach (var v in distinct)
                yield return (v, 1.0);
        }
        else
        {
            foreach (var kvp in _weights)
                yield return (kvp.Key, kvp.Value);
        }
    }


    /// <summary>
    /// An explicit “WithNormalisedWeights()” 
    /// that returns a new QuBit or modifies in place. Here we clone for safety.
    /// </summary>
    public QuBit<T> WithNormalisedWeights()
    {
        if (!IsWeighted) return this; // no weights to normalise



        var clonedWeights = new Dictionary<T, Complex>(_weights);

        var clonedQList = _qList.ToList();  // or just _weights.Keys

        var newQ = new QuBit<T>(clonedQList, clonedWeights, _ops)

        {

            _eType = this._eType

        };

        newQ.NormaliseWeights();

        return newQ;

    }



    /// <summary>
    /// Optionally, modify WithWeights(...) to auto-normalise if desired:
    /// </summary>
    public QuBit<T> WithWeights(Dictionary<T, Complex> weights, bool autoNormalise = false)
{
    if (weights == null)
        throw new ArgumentNullException(nameof(weights));

    // Filter weights to only include valid states.
    var filtered = new Dictionary<T, Complex>();
    foreach (var kvp in weights)
    {
        if (States.Contains(kvp.Key))
            filtered[kvp.Key] = kvp.Value;
    }

    if (this.System != null)
    {
        // Update this instance’s weights in place.
        _weights = filtered;
        if (autoNormalise)
            NormaliseWeights();
        return this;
    }
    else
    {
        // Because sometimes commitment is optional. Especially in quantum dating.
        var newQ = new QuBit<T>(_qList, filtered, _ops)
        {
            _eType = this._eType
        };
        newQ._weights = filtered;
        if (autoNormalise)
            newQ.NormaliseWeights();
        return newQ;
    }
}


    /// <summary>
    /// Returns a string representation of the current superposition states.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        // Legacy style if no weights or all weights equal
        if (_weights == null || AllWeightsEqual(_weights))
        {
            var distinct = States.Distinct();
            if (_eType == QuantumStateType.SuperpositionAny)
                return $"any({string.Join(", ", distinct)})";
            else
            {
                if (distinct.Count() == 1) return distinct.First().ToString();
                return $"all({string.Join(", ", distinct)})";
            }
        }
        else
        {
            // Weighted
            var entries = ToWeightedValues().Select(x => $"{x.value}:{x.weight}");
            return _eType == QuantumStateType.SuperpositionAny
                ? $"any({string.Join(", ", entries)})"
                : $"all({string.Join(", ", entries)})";
        }
    }

    /// <summary>
    /// Returns a string representation of the current superposition states and their weights
    /// </summary>
    /// <returns></returns>
    public string ToDebugString()
    {
        return string.Join(", ", ToWeightedValues()
            .Select(x => $"{x.value} (weight: {x.weight})"));
    }

    /// <summary>
    /// Returns a JSON string representation of the current superposition states and their weights.
    /// </summary>
    /// <returns></returns>
    public string ToJsonDebugString()
    {
        var obj = new
        {
            states = this.ToWeightedValues().Select(v => new {
                value = v.value,
                amplitude = new { real = v.weight.Real, imag = v.weight.Imaginary },
                probability = v.weight.Magnitude * v.weight.Magnitude
            }),
            collapsed = this.IsCollapsed,
            collapseId = this.LastCollapseHistoryId,
            qubitIndices = this.GetQubitIndices()
        };
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }


    /// <summary>
    /// Checks if all weights in the dictionary are equal.
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    private bool AllWeightsEqual(Dictionary<T, Complex> dict)
    {
        if (dict.Count <= 1) return true;
        var first = dict.Values.First();
        return dict.Values.Skip(1).All(w => Complex.Abs(w - first) < 1e-14);
    }

    /// <summary>
    /// Check Equal Probabilities (|amplitude|²)
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    private bool AllWeightsProbablyEqual(Dictionary<T, Complex> dict)
    {
        if (dict.Count <= 1) return true;

        double firstProb = SquaredMagnitude(dict.Values.First());

        return dict.Values
            .Skip(1)
            .All(w => Math.Abs(SquaredMagnitude(w) - firstProb) < 1e-14);
    }

    private double SquaredMagnitude(Complex c) => c.Real * c.Real + c.Imaginary * c.Imaginary;

    #endregion

    /// <summary>
    /// Indicates if the QuBit is burdened with knowledge (i.e., weighted).
    /// If not, it's just blissfully unaware of how much it should care.
    /// </summary>
    public bool IsWeighted => _weights != null;

    public bool IsCollapsed => _isCollapsedFromSystem || _isActuallyCollapsed;

    public override IReadOnlyCollection<T> States =>
    _weights != null ? (IReadOnlyCollection<T>)_weights.Keys : _qList.ToList();

    /// <summary>
    /// Returns the most probable state, i.e., the one that's been yelling the loudest in the multiverse.
    /// This is as close to democracy as quantum physics gets.
    /// </summary>
    public T MostProbable()
    {
        if (!States.Any())
            throw new InvalidOperationException("No states available to collapse.");

        if (!IsWeighted)
        {
            // fallback
            return ToCollapsedValues().First();
        }
        else
        {
            // pick the max by weight
            var (val, _) = ToWeightedValues()
                .OrderByDescending(x => x.weight)
                .First();
            return val;
        }
    }

    /// <summary>
    /// Tolerance used for comparing weights in equality checks.
    /// This allows for minor floating-point drift.
    /// </summary>
    private static readonly double _tolerance = 1e-9;

    
    /// <summary>
    /// Creates a hash that reflects the spiritual essence of your quantum mess.
    /// If you're lucky, two equal QuBits won't hash to the same black hole
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            foreach (var s in States.Distinct().OrderBy(x => x))
            {
                hash = hash * 23 + (s?.GetHashCode() ?? 0);

                if (IsWeighted)
                {
                    Complex amp = _weights != null && _weights.TryGetValue(s, out var w) ? w : Complex.One;

                    // Round real and imaginary parts separately to avoid hash instability
                    double real = Math.Round(amp.Real, 12);
                    double imag = Math.Round(amp.Imaginary, 12);

                    long realBits = BitConverter.DoubleToInt64Bits(real);
                    long imagBits = BitConverter.DoubleToInt64Bits(imag);

                    hash = hash * 23 + realBits.GetHashCode();
                    hash = hash * 23 + imagBits.GetHashCode();
                }
            }

            return hash;
        }
    }


    /// <summary>
    /// Provides a tiny status report that says: "Yes, you're still lost in the matrix."
    /// </summary>
    public string WeightSummary()
    {
        if (!IsWeighted) return "Weighted: false";

        double totalProbability = _weights.Values.Sum(amp => amp.Magnitude * amp.Magnitude);
        double maxMagnitudeSquared = _weights.Values.Max(amp => amp.Magnitude * amp.Magnitude);
        double minMagnitudeSquared = _weights.Values.Min(amp => amp.Magnitude * amp.Magnitude);

        return $"Weighted: true, Sum(|amp|²): {totalProbability}, Max(|amp|²): {maxMagnitudeSquared}, Min(|amp|²): {minMagnitudeSquared}";
    }


    /// <summary>
    /// Implicitly collapses the QuBit and returns a single value.
    /// <para>
    /// This is provided strictly for poetic license, lazy prototyping,
    /// and chaotic neutral debugging.
    /// </para>
    /// <para>
    /// This performs a probabilistic collapse (SampleWeighted) without preserving collapse state.
    /// </para>
    /// </summary>
    /// <param name="q"></param>
    [Obsolete("Implicit QuBit collapse is not safe for production. Use Observe() instead.")]
    public static implicit operator T(QuBit<T> q) => q.SampleWeighted();

    // A convenient way to slap some probabilities onto your states after the fact.
    // Like putting sprinkles on a Schrödinger cupcake — you won't know how it tastes until you eat it, and then it's too late.
    /// </summary>
    public QuBit<T> WithWeightsNormalised(Dictionary<T, Complex> weights)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));

        // Gather only the weights for keys we already have in States.
        var filtered = new Dictionary<T, Complex>();
        foreach (var kvp in weights)
        {
            if (States.Contains(kvp.Key))
                filtered[kvp.Key] = kvp.Value;
        }

        // Construct the new QuBit with the same _qList, same ops
        var newQ = new QuBit<T>(_qList, filtered, _ops)
        {
            _eType = this._eType  // preserve the existing quantum state type
        };
        return newQ;
    }

    public int[] GetQubitIndices() => _qubitIndices;

    public void NotifyWavefunctionCollapsed(Guid collapseId)
    {
        // If already collapsed, ensure we update the system-observed value if missing.
        if (_isActuallyCollapsed)
        {
            if (_systemObservedValue == null && _system != null)
            {
                int[] fullObserved;
                try
                {
                    fullObserved = _system.GetCollapsedState();
                }
                catch (InvalidOperationException)
                {
                    int[] unionIndices = GetUnionOfGroupIndices();
                    fullObserved = _system.ObserveGlobal(unionIndices, Random.Shared);
                }

                if (_qubitIndices.Length == 1)
                {
                    var value = fullObserved[_qubitIndices[0]];
                    object val = typeof(T) == typeof(bool) ? (object)(value != 0) : (object)value;
                    _systemObservedValue = val;
                    _collapsedValue = (T)val;
                }
                else
                {
                    var values = _qubitIndices.Select(i => fullObserved[i]).ToArray();
                    object val = typeof(T) == typeof(bool[]) ? (object)values.Select(v => v != 0).ToArray() : (object)values;
                    _systemObservedValue = val;
                    if (val is T t)
                        _collapsedValue = t;
                }
            }
            _collapseHistoryId = collapseId;
            return;
        }

        _isCollapsedFromSystem = true;
        _collapseHistoryId = collapseId;

        if (_system != null)
        {
            int[] fullObserved;
            try
            {
                fullObserved = _system.GetCollapsedState();
            }
            catch (InvalidOperationException)
            {
                int[] unionIndices = GetUnionOfGroupIndices();
                fullObserved = _system.ObserveGlobal(unionIndices, Random.Shared);
            }

            if (_qubitIndices.Length == 1)
            {
                var value = fullObserved[_qubitIndices[0]];
                object val = typeof(T) == typeof(bool) ? (object)(value != 0) : (object)value;
                _systemObservedValue = val;
                _collapsedValue = (T)val;
            }
            else
            {
                var values = _qubitIndices.Select(i => fullObserved[i]).ToArray();
                object val = typeof(T) == typeof(bool[]) ? (object)values.Select(v => v != 0).ToArray() : (object)values;
                _systemObservedValue = val;
                if (val is T t)
                    _collapsedValue = t;
            }

            _isActuallyCollapsed = true;
            _eType = QuantumStateType.CollapsedResult;
        }
    }



    public object? GetObservedValue()
    {
        if (IsCollapsed)
        {
            // If we collapsed locally, return _collapsedValue
            if (_collapsedValue != null) return _collapsedValue;

            // If the system forced the collapse, we might do a lazy read:
            // For a real partial measurement approach, you'd query
            // the system's wavefunction. Here we do a simplified approach
            // if T == (int,bool).
            if (_systemObservedValue != null) return _systemObservedValue;
        }

        return null;
    }



    object IQuantumReference.Observe(Random? rng)
    {
        if (_system != null)
        {
            if (IsCollapsed)
            {
                if (_systemObservedValue != null) return _systemObservedValue;
                if (_collapsedValue != null) return _collapsedValue;
            }
            rng ??= Random.Shared;
            // Collapse the full entangled state by measuring the union of indices
            int[] unionIndices = GetUnionOfGroupIndices();
            int[] measured = _system.ObserveGlobal(unionIndices, rng);

            // Now extract this qubit’s value from the complete collapsed state.
            object result;
            if (_qubitIndices.Length == 1)
            {
                result = measured[_qubitIndices[0]];
            }
            else
            {
                int[] myValues = _qubitIndices.Select(i => measured[i]).ToArray();
                result = myValues;
            }
            _systemObservedValue = result;
            return result;
        }

        // Fall back to local collapse if no system is associated.
        return ObserveLocal(rng);
    }


    public void ApplyLocalUnitary(Complex[,] gate)
    {
        if (_system != null)
        {
            if (_qubitIndices.Length == 1 && gate.GetLength(0) == 2 && gate.GetLength(1) == 2)
            {
                // Apply single-qubit gate
                _system.ApplySingleQubitGate(_qubitIndices[0], gate);
            }
            else if (_qubitIndices.Length == 2 && gate.GetLength(0) == 4 && gate.GetLength(1) == 4)
            {
                // Apply two-qubit gate
                _system.ApplyTwoQubitGate(_qubitIndices[0], _qubitIndices[1], gate);
            }
            else
            {
                throw new InvalidOperationException("Unsupported gate size or qubit index count.");
            }

            return;
        }

        // Local fallback for 2-state qubits
        if (_weights == null || _weights.Count != 2)
            throw new InvalidOperationException("Local unitary only supported for 2-state local qubits in this example.");

        var states = _weights.Keys.ToArray();
        var amplitudes = states.Select(s => _weights[s]).ToArray();

        var transformed = QuantumMathUtility<Complex>.ApplyMatrix(amplitudes, gate);

        for (int i = 0; i < states.Length; i++)
        {
            _weights[states[i]] = transformed[i];
        }

        NormaliseWeights();
    }


    #region Local Collapse Logic

    /// <summary>
    /// The original local collapse logic.
    /// </summary>
    private object ObserveLocal(Random? rng)
    {
        // If mock collapse is enabled, return the forced mock value
        if (_mockCollapseEnabled)
        {
            if (_mockCollapseValue == null)
                throw new InvalidOperationException(
                    $"Mock collapse enabled but no mock value is set. IsCollapsed={_isActuallyCollapsed}, States.Count={States.Count}, Type={_eType}."
                );
            return _mockCollapseValue;
        }

        // If already collapsed, return the same value
        if (_isActuallyCollapsed && _collapsedValue != null && _eType == QuantumStateType.CollapsedResult)
        {
            return _collapsedValue;
        }

        rng ??= Random.Shared;

        T picked = SampleWeighted(rng);

        // Use the configuration flag and value validator to protect against default(T)
        if (QuantumConfig.ForbidDefaultOnCollapse && !_valueValidator(picked))
        {
            throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");
        }

        if (EqualityComparer<T>.Default.Equals(picked, default(T)))
        {
            throw new InvalidOperationException(
                $"Collapse resulted in default value. IsCollapsed={_isActuallyCollapsed}, States.Count={States.Count}, Type={_eType}."
            );
        }

        // Mark as collapsed
        _collapsedValue = picked;
        _isActuallyCollapsed = true;
        _qList = new[] { picked };
        if (_weights != null)
        {
            _weights = new Dictionary<T, Complex> { { picked, Complex.One } };
        }
        SetType(QuantumStateType.CollapsedResult);

        return picked!;
    }

    #endregion

}
#endregion

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

#region Eigenstates<T> Implementation

/// <summary>
/// Eigenstates<T> preserves original input keys in a dictionary Key->Value,
/// because sometimes you just want your quantum states to stop changing the subject.
/// </summary>
public class Eigenstates<T> : QuantumSoup<T>
{
    private readonly Func<T, bool> _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);

    private Dictionary<T, T> _qDict;

    #region Constructors
    // Constructors that let you map values to themselves or to other values.
    // Also doubles as a safe space for anyone scared of full superposition commitment.

    public Eigenstates(IEnumerable<T> Items, IQuantumOperators<T> ops)
    {
        // same weight, then the waveform should collapse to the same value
        if (Items == null) throw new ArgumentNullException(nameof(Items));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qDict = Items.Distinct().ToDictionary(x => x, x => x);
    }

    public Eigenstates(IEnumerable<T> Items)
        : this(Items, QuantumOperatorsFactory.GetOperators<T>())
    {
    }

    public Eigenstates(IEnumerable<T> inputValues, Func<T, T> projection, IQuantumOperators<T> ops)
    {
        if (inputValues == null) throw new ArgumentNullException(nameof(inputValues));
        if (projection == null) throw new ArgumentNullException(nameof(projection));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));

        _qDict = inputValues.ToDictionary(x => x, projection);
    }

    public Eigenstates(IEnumerable<T> inputValues, Func<T, T> projection)
        : this(inputValues, projection, QuantumOperatorsFactory.GetOperators<T>())
    {
    }

    public Eigenstates(IEnumerable<(T value, Complex weight)> weightedItems, IQuantumOperators<T> ops)
    {
        if (weightedItems == null) throw new ArgumentNullException(nameof(weightedItems));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));

        var dict = new Dictionary<T, Complex>();
        foreach (var (val, w) in weightedItems)
        {
            if (!dict.ContainsKey(val))
                dict[val] = 0.0;
            dict[val] += w;
        }

        _qDict = dict.Keys.ToDictionary(x => x, x => x);
        _weights = dict;
    }

    public Eigenstates(IEnumerable<(T value, Complex weight)> weightedItems)
        : this(weightedItems, QuantumOperatorsFactory.GetOperators<T>())
    {
    }

    internal Eigenstates(Dictionary<T, T> dict, IQuantumOperators<T> ops)
    {
        _ops = ops;
        _qDict = dict;
    }

    #endregion

    #region Filtering Mode
    // Toggle between “any of these are fine” and “they all better agree”.
    // Like relationship statuses but for probability distributions.

    public Eigenstates<T> Any() { _eType = QuantumStateType.SuperpositionAny; return this; }
    public Eigenstates<T> All() { _eType = QuantumStateType.SuperpositionAll; return this; }

    #endregion

    #region Arithmetic Operations

    // When you want to do math to your eigenstates and still feel like a functional adult.
    // Avoids full combinatorial meltdown, unlike your weekend.
    // Also, it’s like a quantum blender: it mixes everything together but still keeps the labels.
    // Because who needs clarity when you can have chaos?

    /// <summary>
    /// Performs the specified operation on two Eigenstates<T> instances or an Eigenstates<T> and a scalar.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private Eigenstates<T> Do_oper_type(Eigenstates<T> a, Eigenstates<T> b, Func<T, T, T> op)
    {
        // Use a dictionary to accumulate combined weights keyed by the computed result value.
        var newWeights = new Dictionary<T, Complex>();

        // Loop through all weighted values from both operands.
        foreach (var (valA, wA) in a.ToMappedWeightedValues())
        {
            foreach (var (valB, wB) in b.ToMappedWeightedValues())
            {
                var newValue = op(valA, valB);
                Complex combinedWeight = wA * wB;
                newWeights[newValue] = newWeights.TryGetValue(newValue, out var existing)
                    ? existing + combinedWeight
                    : combinedWeight;
            }
        }

        // Create a new key->value mapping where each key maps to itself.
        var newDict = newWeights.Keys.ToDictionary(x => x, x => x);
        var e = new Eigenstates<T>(newDict, a._ops)
        {
            _weights = newWeights
        };
        return e;
    }

    /// <summary>
    /// Performs the specified operation on an Eigenstates<T> and a scalar.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private Eigenstates<T> Do_oper_type(Eigenstates<T> a, T b, Func<T, T, T> op)
    {
        var result = new Dictionary<T, T>();
        Dictionary<T, Complex>? newWeights = null;

        if (a._weights != null)
            newWeights = new Dictionary<T, Complex>();

        foreach (var kvp in a._qDict)
        {
            var newVal = op(kvp.Value, b);
            result[kvp.Key] = newVal;  // retain original key, update value

            if (newWeights != null)
            {
                Complex wA = a._weights != null && a._weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
                newWeights[kvp.Key] = wA;  // use original key
            }
        }

        var e = new Eigenstates<T>(result, a._ops);
        e._weights = newWeights;
        return e;
    }

    /// <summary>
    /// Performs the specified operation on a scalar and an Eigenstates<T>.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private Eigenstates<T> Do_oper_type(T a, Eigenstates<T> b, Func<T, T, T> op)
    {
        var result = new Dictionary<T, T>();
        Dictionary<T, Complex>? newWeights = null;

        if (b._weights != null)
            newWeights = new Dictionary<T, Complex>();

        foreach (var kvp in b._qDict)
        {
            var newVal = op(a, kvp.Value);
            result[kvp.Key] = newVal;  // retain original key, update value

            if (newWeights != null)
            {
                Complex wB = b._weights != null && b._weights.TryGetValue(kvp.Key, out var bw) ? bw : 1.0;
                newWeights[kvp.Key] = wB;  // use original key
            }
        }

        var e = new Eigenstates<T>(result, b._ops);
        e._weights = newWeights;
        return e;
    }


    public static Eigenstates<T> operator %(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Mod(x, y));
    public static Eigenstates<T> operator %(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Mod(x, y));
    public static Eigenstates<T> operator %(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Mod(x, y));

    public static Eigenstates<T> operator +(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Add(x, y));
    public static Eigenstates<T> operator +(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Add(x, y));
    public static Eigenstates<T> operator +(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Add(x, y));

    public static Eigenstates<T> operator -(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Subtract(x, y));
    public static Eigenstates<T> operator -(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Subtract(x, y));
    public static Eigenstates<T> operator -(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Subtract(x, y));

    public static Eigenstates<T> operator *(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Multiply(x, y));
    public static Eigenstates<T> operator *(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Multiply(x, y));
    public static Eigenstates<T> operator *(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Multiply(x, y));

    public static Eigenstates<T> operator /(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Divide(x, y));
    public static Eigenstates<T> operator /(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Divide(x, y));
    public static Eigenstates<T> operator /(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Divide(x, y));

    #endregion

    #region Filtering (Comparison) Operators
    // Let’s you ask “which of you is actually greater than 5?” in a very judgmental way.
    // Returns a trimmed-down existential crisis with weights.

    private Eigenstates<T> Do_condition_type(Func<T, T, bool> condition, T value)
    {
        var result = new Dictionary<T, T>();
        Dictionary<T, Complex>? newWeights = null;

        if (_weights != null)
            newWeights = new Dictionary<T, Complex>();

        foreach (var kvp in _qDict)
        {
            if (condition(kvp.Value, value))
            {
                result[kvp.Key] = kvp.Value;
                if (newWeights != null)
                {
                    Complex wA = _weights != null && _weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
                    newWeights[kvp.Key] = wA;
                }
            }
        }
        var e = new Eigenstates<T>(result, _ops);
        e._weights = newWeights;
        return e;
    }

    private Eigenstates<T> Do_condition_type(Func<T, T, bool> condition, Eigenstates<T> other)
    {
        var result = new Dictionary<T, T>();
        Dictionary<T, Complex>? newWeights = null;

        if (_weights != null || other._weights != null)
            newWeights = new Dictionary<T, Complex>();

        foreach (var kvp in _qDict)
        {
            foreach (var kvp2 in other._qDict)
            {
                if (condition(kvp.Value, kvp2.Value))
                {
                    result[kvp.Key] = kvp.Value;
                    if (newWeights != null)
                    {
                        Complex wA = _weights != null && _weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
                        Complex wB = other._weights != null && other._weights.TryGetValue(kvp2.Key, out var bw) ? bw : 1.0;
                        newWeights[kvp.Key] = wA * wB;
                    }
                    break; // break on first match
                }
            }
        }

        var e = new Eigenstates<T>(result, _ops);
        e._weights = newWeights;
        return e;
    }

    public static Eigenstates<T> operator <=(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.LessThanOrEqual(x, y), b);
    public static Eigenstates<T> operator <=(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.LessThanOrEqual(x, y), b);
    public static Eigenstates<T> operator >=(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.GreaterThanOrEqual(x, y), b);
    public static Eigenstates<T> operator >=(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.GreaterThanOrEqual(x, y), b);
    public static Eigenstates<T> operator <(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.LessThan(x, y), b);
    public static Eigenstates<T> operator <(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.LessThan(x, y), b);
    public static Eigenstates<T> operator >(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.GreaterThan(x, y), b);
    public static Eigenstates<T> operator >(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.GreaterThan(x, y), b);
    public static Eigenstates<T> operator ==(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.Equal(x, y), b);
    public static Eigenstates<T> operator ==(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.Equal(x, y), b);
    public static Eigenstates<T> operator !=(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.NotEqual(x, y), b);
    public static Eigenstates<T> operator !=(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.NotEqual(x, y), b);

    #endregion

    #region Weights / Output
    // The part where we pretend our quantum data is readable by humans.
    // Also provides debugging strings to impress people on code reviews.

    public IEnumerable<T> ToValues() => _qDict.Keys;

    /// <summary>
    /// Returns a string representation of the Eigenstates.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var distinctKeys = _qDict.Keys.Distinct().ToList();
        if (_weights == null || AllWeightsEqual(_weights))
        {
            // Return just the element if there is a single unique state
            if (distinctKeys.Count == 1)
                return distinctKeys.First().ToString();

            return _eType == QuantumStateType.SuperpositionAny
                ? $"any({string.Join(", ", distinctKeys)})"
                : $"all({string.Join(", ", distinctKeys)})";
        }
        else
        {
            var pairs = ToMappedWeightedValues().Select(x => $"{x.value}:{x.weight}");
            return _eType == QuantumStateType.SuperpositionAny
                ? $"any({string.Join(", ", pairs)})"
                : $"all({string.Join(", ", pairs)})";
        }
    }

    /// <summary>
    /// Returns a collection of (mapped value, weight) pairs,
    /// where weights correspond to the original input keys.
    /// </summary>
    public IEnumerable<(T value, Complex weight)> ToMappedWeightedValues()
    {
        if (_weights == null)
        {
            foreach (var k in _qDict.Keys.Distinct())
                yield return (_qDict[k], 1.0);
        }
        else
        {
            foreach (var kvp in _weights)
                yield return (_qDict[kvp.Key], kvp.Value);
        }
    }


    /// <summary>
    /// Checks if all weights are equal. If so, congratulations — your data achieved perfect balance, Thanos-style.
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    private bool AllWeightsEqual(Dictionary<T, Complex> dict)
    {
        if (dict.Count <= 1) return true;
        var first = dict.Values.First();
        return dict.Values.Skip(1).All(w => Complex.Abs(w - first) < 1e-14);
    }

    /// <summary>
    /// Checks if all weights are probably equal (i.e., squared magnitudes).
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    private bool AllWeightsProbablyEqual(Dictionary<T, Complex> dict)
    {
        if (dict.Count <= 1) return true;

        double firstProb = SquaredMagnitude(dict.Values.First());

        return dict.Values
            .Skip(1)
            .All(w => Math.Abs(SquaredMagnitude(w) - firstProb) < 1e-14);
    }

    private double SquaredMagnitude(Complex c) => c.Real * c.Real + c.Imaginary * c.Imaginary;

    /// <summary>
    /// Returns a string representation of the Eigenstates,
    /// perfect for when your code works and you still don’t know why.
    /// </summary>
    /// <returns></returns>
    public string ToDebugString()
    {
        if (_weights == null)
        {
            return string.Join(", ",
                _qDict.Select(kvp => $"{kvp.Key} => {kvp.Value}"));
        }
        else
        {
            return string.Join(", ",
                _qDict.Select(kvp =>
                {
                    Complex w = _weights.TryGetValue(kvp.Key, out var val) ? val : 1.0;
                    return $"{kvp.Key} => {kvp.Value} (weight: {w})";
                }));
        }
    }

    #endregion

    #region Observation & Collapse, Mocking, Introspection

    public Guid? CollapseHistoryId => _collapseHistoryId;

    /// <summary>
    /// Observes (collapses) the Eigenstates with an optional random instance
    /// If mock collapse is enabled, it will return the forced value without changing state.
    /// Otherwise, perform a real probabilistic collapse.
    /// </summary>
    public override T Observe(Random? rng = null)
    {
        if (_mockCollapseEnabled)
        {
            if (_mockCollapseValue == null)
                throw new InvalidOperationException("Mock collapse enabled but no mock value is set.");
            return _mockCollapseValue;
        }

        if (_isActuallyCollapsed && _collapsedValue != null && _eType == QuantumStateType.CollapsedResult)
        {
            return _collapsedValue;
        }

        rng ??= Random.Shared;

        // If this was not called via Observe(int seed), then we need to generate a new default collapse ID.
        _collapseHistoryId ??= Guid.NewGuid();

        T picked = SampleWeighted(rng);

        if (QuantumConfig.ForbidDefaultOnCollapse && !_valueValidator(picked))
        {
            throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");
        }

        _collapsedValue = picked;
        _isActuallyCollapsed = true;
        var newDict = new Dictionary<T, T> { { picked, picked } };
        _qDict = newDict;
        if (_weights != null)
        {
            _weights = new Dictionary<T, Complex> { { picked, 1.0 } };
        }
        _eType = QuantumStateType.CollapsedResult;

        return picked;
    }

    /// <summary>
    /// Observes (collapses) using a supplied seed for deterministic behavior.
    /// </summary>
    public T Observe(int seed)
    {
        _lastCollapseSeed = seed;
        _collapseHistoryId = Guid.NewGuid();
        var rng = new Random(seed);
        return Observe(rng);
    }

    /// <summary>
    /// Enables mock collapse for Eigenstates, forcing Observe() to return forcedValue.
    /// </summary>
    public Eigenstates<T> WithMockCollapse(T forcedValue)
    {
        _mockCollapseEnabled = true;
        _mockCollapseValue = forcedValue;
        return this;
    }

    /// <summary>
    /// Disables mock collapse for Eigenstates.
    /// </summary>
    public Eigenstates<T> WithoutMockCollapse()
    {
        _mockCollapseEnabled = false;
        _mockCollapseValue = default;
        return this;
    }

    /// <summary>
    /// Returns a string representation of the current eigenstates without collapsing.
    /// </summary>
    public string show_states()
    {
        return ToDebugString();
    }
    #endregion


    /// <summary>
    /// Indicates whether your states have developed opinions (i.e., weights).
    /// If false, they're blissfully indifferent.
    /// </summary>
    public bool IsWeighted => _weights != null;

    public override IReadOnlyCollection<T> States => _qDict.Keys;

    /// <summary>
    /// Grabs the top N keys based on weight. 
    /// Sort of like picking favorites, but mathematically justified.
    /// </summary>
    public IEnumerable<T> TopNByWeight(int n)
    {
        if (!States.Any() || n <= 0)
            return Enumerable.Empty<T>();

        // If unweighted, each key is weight=1 => they are all "tied".
        // We'll just return them in natural order, limited to N.
        if (!IsWeighted)
            return States.Take(n);

        // Weighted => sort descending by weight
        return _weights!
            .OrderByDescending(kvp => kvp.Value.Magnitude * kvp.Value.Magnitude)
            .Take(n)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Filter your states by weight like you're trimming down party invites.
    /// Only show up if your weight is greater than 0.75, Chad.”
    /// </summary>
    public Eigenstates<T> FilterByProbability(Func<Complex, bool> predicate)
    {
        var newDict = new Dictionary<T, T>();
        Dictionary<T, Complex>? newWeights = null;

        if (IsWeighted)
        {
            newWeights = new Dictionary<T, Complex>();
            foreach (var (key, wt) in _weights)
            {
                if (predicate(wt))
                {
                    newDict[key] = _qDict[key];
                    newWeights[key] = wt;
                }
            }
        }
        else
        {
            // unweighted => each key has weight=1
            bool keep = predicate(1.0);
            if (keep)
            {
                foreach (var key in _qDict.Keys)
                    newDict[key] = _qDict[key];
            }
        }

        var e = new Eigenstates<T>(newDict, _ops);
        e._weights = newWeights;
        return e;
    }

    /// <summary>
    /// Filter your states by amplitude like you're picking a favorite child.
    /// Useful if you're interested in real/imaginary structure instead of probability.
    /// </summary>
    /// <param name="amplitudePredicate">A predicate applied directly to the complex amplitude.</param>
    /// <returns>A filtered QuBit containing only states whose amplitude passes the test.</returns>
    public Eigenstates<T> FilterByAmplitude(Func<Complex, bool> amplitudePredicate)
    {
        var newDict = new Dictionary<T, T>();
        Dictionary<T, Complex>? newWeights = null;

        if (IsWeighted)
        {
            newWeights = new Dictionary<T, Complex>();
            foreach (var (key, amp) in _weights!)
            {
                if (amplitudePredicate(amp))
                {
                    newDict[key] = _qDict[key];
                    newWeights[key] = amp;
                }
            }
        }
        else
        {
            // unweighted => treat each amplitude as 1 + 0i
            Complex defaultAmp = Complex.One;
            if (amplitudePredicate(defaultAmp))
            {
                foreach (var key in _qDict.Keys)
                    newDict[key] = _qDict[key];
            }
        }

        var e = new Eigenstates<T>(newDict, _ops);
        e._weights = newWeights;
        return e;
    }

    /// <summary>
    /// Collapse the superposition by crowning the one true value.
    /// It's democracy, but weighted and quantum, so basically rigged.
    /// </summary>
    public T CollapseWeighted()
    {
        if (!States.Any())
            throw new InvalidOperationException("No states available to collapse.");

        if (!IsWeighted)
        {
            // fallback
            return _qDict.Keys.First();
        }
        else
        {
            var key = _weights!.MaxBy(x => x.Value.Magnitude)!.Key;
            return key;
        }
    }

    /// <summary>
    /// Makes a unique hash that somehow encodes the weight of your guilt, I mean, states.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            foreach (var k in _qDict.Keys.OrderBy(x => x))
            {
                // Hash the key and its projected value
                hash = hash * 23 + (k?.GetHashCode() ?? 0);
                hash = hash * 23 + (_qDict[k]?.GetHashCode() ?? 0);

                if (IsWeighted && _weights != null && _weights.TryGetValue(k, out var amp))
                {
                    // Round both real and imaginary parts
                    double real = Math.Round(amp.Real, 12);
                    double imag = Math.Round(amp.Imaginary, 12);

                    long realBits = BitConverter.DoubleToInt64Bits(real);
                    long imagBits = BitConverter.DoubleToInt64Bits(imag);

                    hash = hash * 23 + realBits.GetHashCode();
                    hash = hash * 23 + imagBits.GetHashCode();
                }
            }

            return hash;
        }
    }

    /// <summary>
    /// Returns a quick stats rundown so you can pretend you have control.
    /// </summary>
    public string WeightSummary()
    {
        if (!IsWeighted) return "Weighted: false";

        var probs = _weights!.Values.Select(amp => amp.Magnitude * amp.Magnitude).ToList();
        double sum = probs.Sum();
        double max = probs.Max();
        double min = probs.Min();

        return $"Weighted: true (complex amplitudes), Total Prob: {sum:F4}, Max |amp|²: {max:F4}, Min |amp|²: {min:F4}";
    }

    /// <summary>
    /// Applies new weights to the same old states. 
    /// Like giving your data a glow-up without changing its personality.
    /// </summary>
    public Eigenstates<T> WithWeights(Dictionary<T, Complex> weights)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));

        var filtered = new Dictionary<T, Complex>();
        foreach (var kvp in weights)
        {
            if (_qDict.ContainsKey(kvp.Key))
                filtered[kvp.Key] = kvp.Value;
        }

        // Create a new instance with the same key->value map, same ops
        var newEigen = new Eigenstates<T>(new Dictionary<T, T>(_qDict), _ops)
        {
            _weights = filtered,
            _eType = this._eType  // preserve the same quantum state type
        };
        return newEigen;
    }

    /// <summary>
    /// Returns a string representation of the Eigenstates,
    /// </summary>
    public string ToDebugString(bool includeCollapseMetadata = false)
    {
        var baseInfo = _weights == null
            ? string.Join(", ", _qDict.Select(kvp => $"{kvp.Key} => {kvp.Value}"))
            : string.Join(", ", _qDict.Select(kvp =>
            {
                Complex w = _weights.TryGetValue(kvp.Key, out var val) ? val : 1.0;
                return $"{kvp.Key} => {kvp.Value} (weight: {w})";
            }));

        if (!includeCollapseMetadata) return baseInfo;

        return $"{baseInfo}\nCollapsed: {_isActuallyCollapsed}, " +
               $"Seed: {_lastCollapseSeed}, ID: {_collapseHistoryId}";
    }

    /// <summary>
    /// This quantum decision was final… but I copied it into a new universe where it wasn’t
    /// Cloning a collapsed QuBit resets its collapse status. The new instance retains the amplitudes and quantum state type, but is mutable and behaves as if never observed.
    /// </summary>
    public override QuantumSoup<T> Clone()
    {
        // Deep clone the key-to-value mapping.
        var clonedDict = new Dictionary<T, T>(_qDict);

        // Clone the weights if available.
        Dictionary<T, Complex>? clonedWeights = null;
        if (_weights != null)
        {
            clonedWeights = new Dictionary<T, Complex>(_weights);
        }

        // Create a new Eigenstates<T> instance with the cloned dictionary and operators.
        var clone = new Eigenstates<T>(clonedDict, _ops)
        {
            _weights = clonedWeights,
            _eType = this._eType,
            // Optionally, copy over collapse-related metadata.
            _collapseHistoryId = this._collapseHistoryId,
            _lastCollapseSeed = this._lastCollapseSeed,
            _isActuallyCollapsed = this._isActuallyCollapsed,   
            _collapsedValue = this._collapsedValue
        };

        return clone;
    }
}

#endregion