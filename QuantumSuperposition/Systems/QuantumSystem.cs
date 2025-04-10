using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using QuantumSuperposition.Utilities;
using QuantumSuperposition.Entanglement;
using QuantumSuperposition.QuantumSoup;

namespace QuantumSuperposition.Systems
{
    public class QuantumSystem
    {
        private Dictionary<int[], Complex> _amplitudes = new Dictionary<int[], Complex>();
        private readonly List<IQuantumReference> _registered = new();
        private EntanglementManager _entanglement = new();
        public EntanglementManager Entanglement => _entanglement;
        private Queue<GateOperation> _gateQueue = new Queue<GateOperation>();

        /// <summary>
        /// Applies a unitary gate to a set of target qubits.
        /// This implementation groups amplitudes that differ only on the target indices,
        /// multiplies each group’s subvector with the provided gate,
        /// and then updates the full state.
        /// </summary>
        public void ApplyMultiQubitGate(int[] targetQubits, Complex[,] gate, string gateName)
        {
            // Get the current amplitudes (assumed accessible via a property or internal field).
            var currentAmps = new Dictionary<int[], Complex>(this.Amplitudes, new IntArrayComparer());
            int numTargets = targetQubits.Length;
            int d = 1 << numTargets;
            var newAmps = new Dictionary<int[], Complex>(new IntArrayComparer());

            _gateQueue.Enqueue(new GateOperation(GateType.MultiQubit, targetQubits, gate, gateName));

            // Group the basis states by the bits in positions not among the target qubits.
            var groups = currentAmps.Keys.GroupBy(state =>
            {
                // Create a key from the bits of the state at indices not in targetQubits.
                var keyBits = new List<int>();
                for (int i = 0; i < state.Length; i++)
                {
                    if (!targetQubits.Contains(i))
                        keyBits.Add(state[i]);
                }
                return string.Join(",", keyBits);
            });

            foreach (var group in groups)
            {
                // Order the states in the group by the integer value of the substate on the target qubits.
                var stateList = group.ToList();
                stateList.Sort((a, b) =>
                {
                    int idxA = BitsToIndex(ExtractSubstate(a, targetQubits));
                    int idxB = BitsToIndex(ExtractSubstate(b, targetQubits));
                    return idxA.CompareTo(idxB);
                });

                // Build the vector of amplitudes for the target subspace.
                Complex[] vec = new Complex[d];
                for (int i = 0; i < d; i++)
                {
                    currentAmps.TryGetValue(stateList[i], out Complex amp);
                    vec[i] = amp;
                }

                // Apply the gate to the vector.
                Complex[] newVec = QuantumMathUtility<Complex>.ApplyMatrix(vec, gate);

                // Update the corresponding entries in newAmps.
                for (int i = 0; i < d; i++)
                {
                    newAmps[stateList[i]] = newVec[i];
                }
            }

            // Replace the system's amplitudes (assume you add a setter method).
            this.SetAmplitudes(newAmps);
            this.NormaliseAmplitudes();
        }

        // Helper: Converts the bits from a full basis state based on target indices.
        private static int[] ExtractSubstate(int[] fullState, int[] indices)
        {
            int[] sub = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
                sub[i] = fullState[indices[i]];
            return sub;
        }

        // Helper: Converts an array of bits to its integer representation.
        private static int BitsToIndex(int[] bits)
        {
            int index = 0;
            foreach (var bit in bits)
                index = (index << 1) | bit;
            return index;
        }

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
        /// Will return a multiline string that you can print to the console visualising the gate schedule.
        /// </summary>
        /// <param name="totalQubits"></param>
        /// <returns></returns>
        public string VisualiseGateSchedule(int totalQubits)
        {
            //  return "no operations" if

            if (_gateQueue.Count == 0)
                return "no operations";

            // Note: if _gateQueue is a Queue<GateOperation>, you may need to work on a copy.
            return GateSchedulingVisualiser.Visualise(_gateQueue.ToArray(), totalQubits);
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


        public enum GateType
        {
            SingleQubit,
            TwoQubit,
            MultiQubit
        }

        public class GateOperation
        {
            public GateType OperationType { get; }
            public int[] TargetQubits { get; }
            public Complex[,] GateMatrix { get; }
            public string GateName { get; }

            public GateOperation(GateType type, int[] targets, Complex[,] matrix, string gateName)
            {
                OperationType = type;
                TargetQubits = targets;
                GateMatrix = matrix;
                GateName = gateName;
            }

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
        public void ApplyTwoQubitGate(int qubitA, int qubitB, Complex[,] gate, string gateName)
        {
            if (gate.GetLength(0) != 4 || gate.GetLength(1) != 4)
                throw new ArgumentException("Gate must be a 4x4 matrix.");

            // Enqueue a new two-qubit gate operation with the two target indices.
            _gateQueue.Enqueue(new GateOperation(GateType.TwoQubit, new int[] { qubitA, qubitB }, gate, gateName));
        }

        /// <summary>
        /// Applies a single-qubit gate, because sometimes you just need to get your life together.
        /// </summary>
        /// <param name="qubit"></param>
        /// <param name="gate"></param>
        /// <exception cref="ArgumentException"></exception>
        public void ApplySingleQubitGate(int qubit, Complex[,] gate, string gateName)
        {
            if (gate.GetLength(0) != 2 || gate.GetLength(1) != 2)
                throw new ArgumentException("Single-qubit gate must be a 2x2 matrix.");

            // Enqueue a new single-qubit gate operation.
            _gateQueue.Enqueue(new GateOperation(GateType.SingleQubit, new int[] { qubit }, gate, gateName));
        }

        /// <summary>
        /// Processes and applies all the queued gate operations in FIFO order.
        /// </summary>
        public void ProcessGateQueue()
        {
            // First, optimize the current queue.
            var optimizedOps = OptimizeGateQueue(_gateQueue.ToArray());

            // Replace the internal queue with the optimized one.
            _gateQueue = new Queue<GateOperation>(optimizedOps);

            while (_gateQueue.Count > 0)
            {
                var op = _gateQueue.Dequeue();
                switch (op.OperationType)
                {
                    case GateType.SingleQubit:
                        ProcessSingleQubitGate(op.TargetQubits[0], op.GateMatrix);
                        break;
                    case GateType.TwoQubit:
                        ProcessTwoQubitGate(op.TargetQubits[0], op.TargetQubits[1], op.GateMatrix);
                        break;
                    case GateType.MultiQubit:
                        // You may have already applied these immediately,
                        // but if not, you could add a corresponding ProcessMultiQubitGate op.
                        // For now, simply throw or ignore if they were already applied.
                        throw new InvalidOperationException("MultiQubit operations should be processed immediately.");
                    default:
                        throw new InvalidOperationException("Unsupported gate operation.");
                }
            }
        }

        /// <summary>
        /// Optimizes the given sequence of gate operations by removing pairs that cancel out.
        /// In this simple example, consecutive "H" or "X" gates on the same qubit are removed.
        /// </summary>
        private GateOperation[] OptimizeGateQueue(GateOperation[] operations)
        {
            var stack = new Stack<GateOperation>();
            foreach (var op in operations)
            {
                // If the stack is not empty, check if the current operation cancels with the previous one.
                if (stack.Count > 0 && CanCancel(stack.Peek(), op))
                {
                    stack.Pop();
                }
                else
                {
                    stack.Push(op);
                }
            }
            // Return the stack in FIFO order.
            return stack.Reverse().ToArray();
        }

        /// <summary>
        /// Determines whether two gate operations cancel each other.
        /// For simplicity, this example checks for two identical single-qubit operations (like H or X)
        /// on the same qubit. In your real implementation, you might want to multiply matrices
        /// or consider a tolerance.
        /// </summary>
        private bool CanCancel(GateOperation op1, GateOperation op2)
        {
            // Only optimize if both operations are on the same qubits and are of the same type.
            if (op1.OperationType == op2.OperationType && op1.TargetQubits.SequenceEqual(op2.TargetQubits))
            {
                // As an example, assume two Hadamard ("H") gates cancel since H × H = I.
                if (op1.GateName == "H" && op2.GateName == "H")
                    return true;
                // Similarly for Pauli-X (X) if X × X = I.
                if (op1.GateName == "X" && op2.GateName == "X")
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Internal method that processes a single-qubit gate.
        /// </summary>
        private void ProcessSingleQubitGate(int qubit, Complex[,] gate)
        {
            var newAmps = new Dictionary<int[], Complex>(new IntArrayComparer());
            foreach (var state in _amplitudes.Keys.ToList())
            {
                int bit = state[qubit];

                var basis0 = (int[])state.Clone(); basis0[qubit] = 0;
                var basis1 = (int[])state.Clone(); basis1[qubit] = 1;

                Complex a0 = _amplitudes.TryGetValue(basis0, out var amp0) ? amp0 : Complex.Zero;
                Complex a1 = _amplitudes.TryGetValue(basis1, out var amp1) ? amp1 : Complex.Zero;

                // Note: if you need to adapt the math to your ordering you may do so here.
                Complex newAmp = gate[bit, 0] * a0 + gate[bit, 1] * a1;

                newAmps[state] = newAmp;
            }
            _amplitudes = newAmps;
            NormaliseAmplitudes();
        }

        /// <summary>
        /// Internal method that processes a two-qubit gate.
        /// </summary>
        private void ProcessTwoQubitGate(int qubitA, int qubitB, Complex[,] gate)
        {
            var grouped = new Dictionary<string, List<int[]>>();
            foreach (var state in _amplitudes.Keys.ToList())
            {
                var key = string.Join(",", state.Select((val, idx) => (idx != qubitA && idx != qubitB) ? val.ToString() : "*"));
                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<int[]>();
                grouped[key].Add(state);
            }

            var newAmplitudes = new Dictionary<int[], Complex>(new IntArrayComparer());
            foreach (var group in grouped.Values)
            {
                var basisMap = new Dictionary<(int, int), int[]>();
                var vec = new Complex[4];
                foreach (var basis in group)
                {
                    int a = basis[qubitA];
                    int b = basis[qubitB];
                    int index = (a << 1) | b;
                    vec[index] = _amplitudes.TryGetValue(basis, out var amp) ? amp : Complex.Zero;
                    basisMap[(a, b)] = basis;
                }
                var newVec = QuantumMathUtility<Complex>.ApplyMatrix(vec, gate);
                foreach (var ((a, b), state) in basisMap)
                {
                    int idx = (a << 1) | b;
                    newAmplitudes[state] = newVec[idx];
                }
            }
            _amplitudes = newAmplitudes;
            NormaliseAmplitudes();
        }

        /// <summary>
        /// Sets the amplitudes of the quantum system.
        /// A bit like changing the playlist of your favourite quantum music.
        /// This can break the universe if you’re not careful - as can break normalisation.
        /// </summary>
        /// <param name="newAmps"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void SetAmplitudes(Dictionary<int[], Complex> newAmps)
        {
            if (newAmps == null)
                throw new ArgumentNullException(nameof(newAmps));

            // Replace the internal amplitude dictionary.
            // We create a new dictionary using our custom IntArrayComparer to ensure that
            // two arrays representing the same basis state are treated as equal.
            _amplitudes = new Dictionary<int[], Complex>(newAmps, new IntArrayComparer());

            // Notify all registered qubits of the new state.
            var collapseId = Guid.NewGuid();
            foreach (var refQ in _registered)
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
}
