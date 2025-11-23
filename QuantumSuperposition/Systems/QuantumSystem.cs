using QuantumSuperposition.Entanglement;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Utilities;
using System.Numerics;

namespace QuantumSuperposition.Systems
{
    public class QuantumSystem
    {
        private Dictionary<int[], Complex> _amplitudes = [];
        private readonly List<IQuantumReference> _registered = [];

        public EntanglementManager Entanglement { get; } = new();
        private Queue<GateOperation> _gateQueue = new();

        /// <summary>
        /// Applies a unitary gate to a set of target qubits.
        /// This implementation groups amplitudes that differ only on the target indices,
        /// multiplies each group’s subvector with the provided gate,
        /// and then updates the full state.
        /// </summary>
        public void ApplyMultiQubitGate(int[] targetQubits, Complex[,] gate, string gateName)
        {
            Dictionary<int[], Complex> currentAmps = new(_amplitudes, new IntArrayComparer());
            int numTargets = targetQubits.Length;
            int d = 1 << numTargets;
            Dictionary<int[], Complex> newAmps = new(new IntArrayComparer());
            _gateQueue.Enqueue(new GateOperation(GateType.MultiQubit, targetQubits, gate, gateName));
            // Group by unaffected qubits pattern
            var groups = currentAmps.Keys.GroupBy(state => string.Join(",", state.Select((v,i) => targetQubits.Contains(i)? "*" : v.ToString())));
            foreach (var group in groups)
            {
                int[] exemplar = group.First();
                // Build all 2^n variants
                var variants = new Dictionary<int, int[]>();
                for (int mask=0; mask<d; mask++)
                {
                    int[] basis = (int[])exemplar.Clone();
                    for (int bit=0; bit<numTargets; bit++)
                    {
                        int qIndex = targetQubits[bit];
                        basis[qIndex] = (mask >> (numTargets-1-bit)) & 1; // MSB-first ordering
                    }
                    variants[mask] = basis;
                }
                Complex[] vec = new Complex[d];
                for (int i=0;i<d;i++)
                {
                    _ = currentAmps.TryGetValue(variants[i], out Complex amp);
                    vec[i] = amp;
                }
                Complex[] newVec = QuantumMathUtility<Complex>.ApplyMatrix(vec, gate);
                for (int i=0;i<d;i++)
                {
                    if (newVec[i] != Complex.Zero)
                        newAmps[variants[i]] = newVec[i];
                }
            }
            _amplitudes = newAmps.Count>0? newAmps : newAmps;
            NormaliseAmplitudes();
        }

        // Helper: Converts the bits from a full basis state based on target indices.
        private static int[] ExtractSubstate(int[] fullState, int[] indices)
        {
            int[] sub = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                sub[i] = fullState[indices[i]];
            }

            return sub;
        }

        // Helper: Converts an array of bits to its integer representation.
        private static int BitsToIndex(int[] bits)
        {
            int index = 0;
            foreach (int bit in bits)
            {
                index = (index << 1) | bit;
            }

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
            {
                return "no operations";
            }

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
            Dictionary<string, List<(int[] state, Complex amplitude)>> groups = [];
            foreach (KeyValuePair<int[], Complex> kvp in _amplitudes)
            {
                int[] state = kvp.Key;
                int[] projection = measuredIndices.Select(i => state[i]).ToArray();
                string key = string.Join(",", projection);
                if (!groups.ContainsKey(key))
                {
                    groups[key] = [];
                }

                groups[key].Add((state, kvp.Value));
            }

            // Compute probabilities and sample an outcome.
            Dictionary<string, double> outcomeProbs = groups.ToDictionary(
                g => g.Key,
                g => g.Value.Sum(item => item.amplitude.Magnitude * item.amplitude.Magnitude)
            );
            double totalProb = outcomeProbs.Values.Sum();
            if (totalProb < 1e-15)
            {
                throw new InvalidOperationException("Wavefunction is effectively zero.");
            }

            double roll = rng.NextDouble() * totalProb;
            double cumulative = 0.0;
            string? chosenKey = null;
            foreach (KeyValuePair<string, double> kv in outcomeProbs)
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

            Guid collapseId = Guid.NewGuid();
            foreach (IQuantumReference refQ in _registered)
            {
                // Only update local state for qubits whose indices intersect measuredIndices.
                if (refQ.GetQubitIndices().Intersect(measuredIndices).Any())
                {
                    // Call a "partial collapse" that only sets this qubit’s local observed value.
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
            return _amplitudes.Count != 1 ? throw new InvalidOperationException("System is not fully collapsed.") : _amplitudes.Keys.First();
        }


        public void Register(IQuantumReference qubitRef)
        {
            _registered.Add(qubitRef);
        }

        /// <summary>
        /// A simple "observe everything fully" method.
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
            {
                throw new ArgumentNullException(nameof(qubitIndices));
            }

            // Group amplitudes by projected values on qubitIndices
            Dictionary<int[], List<(int[] state, Complex amplitude)>> projectionGroups = new(new IntArrayComparer());

            foreach (KeyValuePair<int[], Complex> kvp in _amplitudes)
            {
                int[] fullState = kvp.Key;
                int[] projected = qubitIndices.Select(i => fullState[i]).ToArray();

                if (!projectionGroups.ContainsKey(projected))
                {
                    projectionGroups[projected] = [];
                }

                projectionGroups[projected].Add((fullState, kvp.Value));
            }

            // Compute total probabilities for each projection group
            Dictionary<int[], double> probSums = projectionGroups.ToDictionary(
                g => g.Key,
                g => g.Value.Sum(x => x.amplitude.Magnitude * x.amplitude.Magnitude),
                new IntArrayComparer()
            );

            double totalProb = probSums.Values.Sum();
            if (totalProb <= 1e-15)
            {
                throw new InvalidOperationException("All probabilities are zero or wavefunction is empty.");
            }

            // Sample a group based on probability
            double roll = rng.NextDouble() * totalProb;
            double cumulative = 0.0;

            int[] chosenProjection = Array.Empty<int>();
            foreach ((int[] proj, double p) in probSums)
            {
                cumulative += p;
                if (roll <= cumulative)
                {
                    chosenProjection = proj;
                    break;
                }
            }

            // Retain only amplitudes matching the chosen projection
            Dictionary<int[], Complex> newAmps = new(new IntArrayComparer());
            foreach ((int[] state, Complex amp) in projectionGroups[chosenProjection])
            {
                newAmps[state] = amp;
            }

            _amplitudes = newAmps;

            // Renormalise
            NormaliseAmplitudes();

            // Notify
            Guid collapseId = Guid.NewGuid();
            foreach (IQuantumReference refQ in _registered)
            {
                // Check if the reference's qubit indices overlap with the observed indices.
                if (refQ.GetQubitIndices().Intersect(qubitIndices).Any())
                {
                    refQ.NotifyWavefunctionCollapsed(collapseId);
                    if (refQ is QuBit<int> qi && qi.EntanglementGroupId is Guid g1)
                    {
                        Entanglement.PropagateCollapse(g1, collapseId);
                    }
                    else if (refQ is QuBit<bool> qb && qb.EntanglementGroupId is Guid g2)
                    {
                        Entanglement.PropagateCollapse(g2, collapseId);
                    }
                    else if (refQ is QuBit<Complex> qc && qc.EntanglementGroupId is Guid g3)
                    {
                        Entanglement.PropagateCollapse(g3, collapseId);
                    }
                }
            }

            // Return observed values
            return chosenProjection;
        }

        /// <summary>
        /// Convenience overload preserving the previous API shape. Uses the default basis mapper.
        /// </summary>
        public void SetFromTensorProduct<T>(bool propagateCollapse, params QuBit<T>[] qubits)
        {
            SetFromTensorProduct(propagateCollapse, (Func<T, int>?)null, qubits);
        }

        /// <summary>
        /// Builds a full wavefunction from individual qubits.
        /// It’s like assembling IKEA furniture, but all the screws are in superposition.
        /// Ensures each qubit participating is system-managed and registered.
        /// Local qubits (System == null) are replaced with new system qubits preserving their weighted states.
        /// Existing system qubits are reused to keep entanglement and indices stable.
        /// Supports optional basis mapping for non-int/bool types (e.g., enums) via mapToBasis.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propagateCollapse">If true, notify registered qubits of a synthetic collapse after construction.</param>
        /// <param name="mapToBasis">Optional mapper converting values of T to computational basis integers.</param>
        /// <param name="qubits">Input qubits (local or system-managed).</param>
        public void SetFromTensorProduct<T>(bool propagateCollapse, Func<T, int>? mapToBasis = null, params QuBit<T>[] qubits)
        {
            if (qubits == null || qubits.Length == 0)
            {
                throw new ArgumentException("At least one qubit must be provided.");
            }

            // Default mapper covers int, bool, and enums; users may supply custom mappers for other types.
            mapToBasis ??= (v) =>
            {
                if (v is int iv) return iv;
                if (v is bool bv) return bv ? 1 : 0;
                if (typeof(T).IsEnum) return Convert.ToInt32(v);
                throw new InvalidOperationException($"Unsupported type {typeof(T)} in tensor product state. Provide a mapToBasis function.");
            };

            int totalQubits = qubits.Length;
            QuBit<T>[] systemManaged = new QuBit<T>[totalQubits];

            for (int i = 0; i < totalQubits; i++)
            {
                QuBit<T> original = qubits[i];

                if (original.System == this)
                {
                    // Already system-managed: reuse
                    systemManaged[i] = original;
                    continue;
                }

                // Assign indices sequentially if original had none.
                int[] indices = original.GetQubitIndices();
                if (indices == null || indices.Length == 0)
                {
                    indices = new[] { i }; // simple sequential mapping
                }

                // Create a new system qubit using the system constructor so registration occurs.
                QuBit<T> sysQ = new QuBit<T>(this, indices);

                // Copy weighted states from the original.
                Dictionary<T, Complex> weights = original.ToWeightedValues().ToDictionary(w => w.value, w => w.weight);
                _ = sysQ.WithWeights(weights, autoNormalise: false);

                systemManaged[i] = sysQ; // store for product build
            }

            // Compute tensor product using system-managed qubits only.
            Dictionary<T[], Complex> product = QuantumMathUtility<T>.TensorProduct(systemManaged);

            // Convert to a global int[] wavefunction using the mapper.
            Dictionary<int[], Complex> result = new(new IntArrayComparer());
            foreach ((T[] state, Complex amplitude) in product)
            {
                int[] intState = state.Select(s => mapToBasis(s)).ToArray();
                result[intState] = amplitude;
            }

            _amplitudes = result;
            NormaliseAmplitudes();

            // Propagate a synthetic collapse event to wire up entanglement and local observed values.
            if (propagateCollapse)
            {
                Guid collapseId = Guid.NewGuid();
                foreach (IQuantumReference q in _registered)
                {
                    q.NotifyWavefunctionCollapsed(collapseId);
                }
            }
        }


        /// <summary>
        /// Applies a two-qubit gate, because one qubit can't handle this much chaos alone.
        /// Perfect for entangling your problems in pairs.
        /// </summary>
        public void ApplyTwoQubitGate(int qubitA, int qubitB, Complex[,] gate, string gateName)
        {
            if (gate.GetLength(0) != 4 || gate.GetLength(1) != 4)
            {
                throw new ArgumentException("Gate must be a 4x4 matrix.");
            }

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
            {
                throw new ArgumentException("Single-qubit gate must be a 2x2 matrix.");
            }

            // Enqueue a new single-qubit gate operation.
            _gateQueue.Enqueue(new GateOperation(GateType.SingleQubit, new int[] { qubit }, gate, gateName));
        }

        /// <summary>
        /// This is like a quantum assembly line, but with more uncertainty and fewer safety regulations.
        /// </summary>
        public void ProcessGateQueue()
        {
            // First, optimize the current queue.
            GateOperation[] optimizedOps = OptimizeGateQueue(_gateQueue.ToArray());

            // Replace the internal queue with the optimized one.
            _gateQueue = new Queue<GateOperation>(optimizedOps);

            while (_gateQueue.Count > 0)
            {
                GateOperation op = _gateQueue.Dequeue();
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
        /// A bit like cleaning up your closet, but with more qubits and fewer sweaters.
        /// </summary>
        private GateOperation[] OptimizeGateQueue(GateOperation[] operations)
        {
            Stack<GateOperation> stack = new();
            foreach (GateOperation op in operations)
            {
                // If the stack is not empty, check if the current operation cancels with the previous one.
                if (stack.Count > 0 && CanCancel(stack.Peek(), op))
                {
                    _ = stack.Pop();
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
        /// With a bit of time travel you were trying to cancel out your bad decisions in life.
        /// </summary>
        private bool CanCancel(GateOperation op1, GateOperation op2)
        {
            if (op1.OperationType == op2.OperationType && op1.TargetQubits.SequenceEqual(op2.TargetQubits))
            {
                int n = op1.GateMatrix.GetLength(0);
                // Ensure both matrices are square and of the same dimension.
                if (op2.GateMatrix.GetLength(0) != n ||
                    op1.GateMatrix.GetLength(1) != n ||
                    op2.GateMatrix.GetLength(1) != n)
                {
                    return false;
                }

                // Compute the net effect: op2 * op1.
                Complex[,] product = MultiplyMatrices(op2.GateMatrix, op1.GateMatrix);
                Complex[,] identity = CreateIdentityMatrix(n);
                // Check if product and identity are equal within a tolerance.
                return AreMatricesEqual(product, identity, 1e-9);
            }
            return false;
        }

        /// <summary>
        /// Come on everybody, listen to me, rapping is as easy as 1, 2, 3.
        /// Don't be shy, it's easy if you try.
        /// I'm gonna show you how to mmm mmm do matrix manipulation.
        /// </summary>
        private Complex[,] MultiplyMatrices(Complex[,] A, Complex[,] B)
        {
            int n = A.GetLength(0);
            int m = A.GetLength(1);
            int p = B.GetLength(1);
            if (B.GetLength(0) != m)
            {
                throw new ArgumentException("Matrix dimensions do not match for multiplication.");
            }
            Complex[,] result = new Complex[n, p];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < p; j++)
                {
                    Complex sum = Complex.Zero;
                    for (int k = 0; k < m; k++)
                    {
                        sum += A[i, k] * B[k, j];
                    }
                    result[i, j] = sum;
                }
            }
            return result;
        }

        /// <summary>
        /// This is like a quantum mirror, reflecting your true self.
        /// Stargate SG-1 would be proud.
        /// </summary>
        private Complex[,] CreateIdentityMatrix(int n)
        {
            Complex[,] identity = new Complex[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    identity[i, j] = (i == j) ? Complex.One : Complex.Zero;
                }
            }
            return identity;
        }

        /// <summary>
        /// Checking if your quantum state is the same as your neighbour's.
        /// </summary>
        private bool AreMatricesEqual(Complex[,] A, Complex[,] B, double tolerance)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            if (B.GetLength(0) != rows || B.GetLength(1) != cols)
            {
                return false;
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (Complex.Abs(A[i, j] - B[i, j]) > tolerance)
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// A quantum spa day for your qubit.
        /// </summary>
        private void ProcessSingleQubitGate(int qubit, Complex[,] gate)
        {
            Dictionary<int[], Complex> newAmps = new(new IntArrayComparer());
            // Group states by all other qubits (pattern with *)
            var groups = _amplitudes.Keys.GroupBy(state => string.Join(",", state.Select((v, idx) => idx == qubit ? "*" : v.ToString())));
            foreach (var group in groups)
            {
                // Build amplitudes a0,a1 from existing or zero
                int[] exemplar = group.First();
                int length = exemplar.Length;
                int[] basis0 = (int[])exemplar.Clone(); basis0[qubit] = 0;
                int[] basis1 = (int[])exemplar.Clone(); basis1[qubit] = 1;
                _ = _amplitudes.TryGetValue(basis0, out Complex a0);
                _ = _amplitudes.TryGetValue(basis1, out Complex a1);
                // Apply gate: new amplitudes for |0> and |1>
                Complex new0 = gate[0,0]*a0 + gate[0,1]*a1;
                Complex new1 = gate[1,0]*a0 + gate[1,1]*a1;
                if (new0 != Complex.Zero) newAmps[basis0] = new0;
                if (new1 != Complex.Zero) newAmps[basis1] = new1;
            }
            _amplitudes = newAmps.Count > 0 ? newAmps : newAmps; // keep even if empty
            NormaliseAmplitudes();
        }

        /// <summary>
        /// A quantum dance party, but with more entanglement and fewer disco balls.
        /// </summary>
        private void ProcessTwoQubitGate(int qubitA, int qubitB, Complex[,] gate)
        {
            Dictionary<string, List<int[]>> grouped = [];
            foreach (int[] state in _amplitudes.Keys.ToList())
            {
                string key = string.Join(",", state.Select((val, idx) => (idx != qubitA && idx != qubitB) ? val.ToString() : "*"));
                if (!grouped.ContainsKey(key)) grouped[key] = [];
                grouped[key].Add(state);
            }
            Dictionary<int[], Complex> newAmplitudes = new(new IntArrayComparer());
            foreach (var group in grouped)
            {
                // Build complete 4 basis variants for local two qubits combined with other fixed bits.
                int[] exemplar = group.Value.First();
                int length = exemplar.Length;
                var localVariants = new Dictionary<(int a,int b), int[]>();
                for (int a=0;a<2;a++)
                {
                    for (int b=0;b<2;b++)
                    {
                        int[] basis = (int[])exemplar.Clone();
                        basis[qubitA] = a;
                        basis[qubitB] = b;
                        localVariants[(a,b)] = basis;
                    }
                }
                // Build input vector
                Complex[] vec = new Complex[4];
                foreach (var kv in localVariants)
                {
                    _ = _amplitudes.TryGetValue(kv.Value, out Complex amp);
                    int idx = (kv.Key.a << 1) | kv.Key.b;
                    vec[idx] = amp;
                }
                // Apply gate
                Complex[] newVec = QuantumMathUtility<Complex>.ApplyMatrix(vec, gate);
                foreach (var kv in localVariants)
                {
                    int idx = (kv.Key.a << 1) | kv.Key.b;
                    if (newVec[idx] != Complex.Zero)
                        newAmplitudes[kv.Value] = newVec[idx];
                }
            }
            _amplitudes = newAmplitudes.Count>0? newAmplitudes : newAmplitudes;
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
            {
                throw new ArgumentNullException(nameof(newAmps));
            }

            // Replace the internal amplitude dictionary.
            // We create a new dictionary using our custom IntArrayComparer to ensure that
            // two arrays representing the same basis state are treated as equal.
            _amplitudes = new Dictionary<int[], Complex>(newAmps, new IntArrayComparer());

            // Notify all registered qubits of the new state.
            Guid collapseId = Guid.NewGuid();
            foreach (IQuantumReference refQ in _registered)
            {
                refQ.NotifyWavefunctionCollapsed(collapseId);
                if (refQ is QuBit<int> qi && qi.EntanglementGroupId is Guid g1)
                {
                    Entanglement.PropagateCollapse(g1, collapseId);
                }
                else if (refQ is QuBit<bool> qb && qb.EntanglementGroupId is Guid g2)
                {
                    Entanglement.PropagateCollapse(g2, collapseId);
                }
                else if (refQ is QuBit<Complex> qc && qc.EntanglementGroupId is Guid g3)
                {
                    Entanglement.PropagateCollapse(g3, collapseId);
                }
            }
        }

        /// <summary>
        /// Normalises amplitudes so the universe doesn’t implode.
        /// Math nerds call this "preserving probability". We call it "not being weird."
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
            foreach (int[]? k in _amplitudes.Keys.ToList())
            {
                _amplitudes[k] /= norm;
            }
        }

        /// <summary>
        /// Exposes the dictionary for debugging or advanced usage.
        /// Be careful: direct modifications can break normalisation.
        /// </summary>
        public IReadOnlyDictionary<int[], Complex> Amplitudes => _amplitudes;

        /// <summary>
        /// Exposes registered references (internal use for QuantumRegister).
        /// </summary>
        internal IEnumerable<IQuantumReference> GetRegisteredReferences() => _registered;
    }
}
