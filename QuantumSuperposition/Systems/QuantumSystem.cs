using QuantumSuperposition.Entanglement;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Utilities;
using System.Numerics;
using System.Buffers;

namespace QuantumSuperposition.Systems
{
    /// <summary>
    /// Represents a global quantum wavefunction constructed from one or more <see cref="QuBit{T}"/> instances.
    /// Provides tensor product construction, gate scheduling and optimisation, global and partial observation,
    /// and entanglement propagation.
    /// </summary>
    public class QuantumSystem
    {
        /// <summary>
        /// Raised after a gate batch is processed. Provides gate name, target indices and current amplitude count.
        /// Subscribe for lightweight diagnostics (avoid heavy work inside handlers). Thread-safety: handlers may be
        /// added/removed concurrently; invocation copies delegate reference first to avoid race with nulling.
        /// </summary>
        public event Action<string,int[],int>? GateExecuted;
        /// <summary>
        /// Raised after a global collapse. Provides observed indices and the resulting projection.
        /// Thread-safety: see <see cref="GateExecuted"/> remarks. Core QuantumSystem operations are NOT thread-safe;
        /// external callers must provide their own synchronisation if invoking from multiple threads.
        /// </summary>
        public event Action<int[],int[]>? GlobalCollapse;

        private Dictionary<int[], Complex> _amplitudes = [];
        private readonly List<IQuantumReference> _registered = [];
        public EntanglementManager Entanglement { get; } = new();
        private Queue<GateOperation> _gateQueue = new();

        /// <summary>
        /// Ensures the internal wavefunction basis length is at least requiredLength.
        /// If empty: initialise |00...0> state.
        /// If shorter: pad existing basis states with zeros.
        /// </summary>
        private void EnsureWavefunctionLength(int requiredLength)
        {
            if (requiredLength <= 0) return;
            if (_amplitudes.Count == 0)
            {
                int[] zeroState = new int[requiredLength];
                _amplitudes[zeroState] = Complex.One;
                return;
            }
            int currentLength = _amplitudes.Keys.First().Length;
            if (currentLength == requiredLength) return;
            if (currentLength > requiredLength) return;
            Dictionary<int[], Complex> expanded = new(new IntArrayComparer());
            foreach (var kv in _amplitudes)
            {
                int[] newer = new int[requiredLength];
                Array.Copy(kv.Key, newer, kv.Key.Length);
                expanded[newer] = kv.Value;
            }
            _amplitudes = expanded;
        }

        /// <summary>
        /// Applies a unitary gate to a set of target qubits.
        /// Optimised: structural grouping using sentinel -1 (no string allocations).
        /// Handles duplicate target indices & auto-expands wavefunction length if needed.
        /// </summary>
        public void ApplyMultiQubitGate(int[] targetQubits, Complex[,] gate, string gateName)
        {
            if (targetQubits == null || targetQubits.Length == 0) throw new ArgumentException("At least one target qubit required.");
            int[] distinctTargets = targetQubits.Distinct().OrderBy(i => i).ToArray();
            int numTargets = distinctTargets.Length;
            int requiredLength = Math.Max(_amplitudes.Count == 0 ? 0 : _amplitudes.Keys.First().Length, distinctTargets.Max() + 1);
            EnsureWavefunctionLength(requiredLength);
            int expectedDim = 1 << numTargets;
            if (gate.GetLength(0) != gate.GetLength(1) || gate.GetLength(0) != expectedDim)
                throw new ArgumentException($"Invalid multi-qubit gate matrix. GateName={gateName}, Targets=[{string.Join(',', distinctTargets)}], TargetCount={numTargets}, ExpectedDim={expectedDim}x{expectedDim}, ActualDim={gate.GetLength(0)}x{gate.GetLength(1)}.");
            _gateQueue.Enqueue(new GateOperation(GateType.MultiQubit, distinctTargets, gate, gateName));
            bool[] isTarget = new bool[requiredLength]; foreach (int tq in distinctTargets) isTarget[tq] = true;
            Dictionary<PatternKey, List<int[]>> groups = new();
            foreach (int[] state in _amplitudes.Keys)
            {
                int[] tmp = ArrayPool<int>.Shared.Rent(requiredLength);
                for (int i = 0; i < requiredLength; i++) tmp[i] = isTarget[i] ? -1 : state[i];
                int[] owned = new int[requiredLength]; Array.Copy(tmp, owned, requiredLength); ArrayPool<int>.Shared.Return(tmp);
                PatternKey key = new(owned);
                if (!groups.TryGetValue(key, out var list)) { list = []; groups[key] = list; }
                list.Add(state);
            }
            Dictionary<int[], Complex> newAmps = new(new IntArrayComparer());
            Complex[] localVector = new Complex[expectedDim];
            foreach (var group in groups)
            {
                int[] exemplar = group.Value[0];
                for (int mask = 0; mask < expectedDim; mask++)
                {
                    int[] variant = (int[])exemplar.Clone();
                    for (int bit = 0; bit < numTargets; bit++) variant[distinctTargets[bit]] = (mask >> (numTargets - 1 - bit)) & 1;
                    _ = _amplitudes.TryGetValue(variant, out Complex amp); localVector[mask] = amp;
                }
                Complex[] resultVector = QuantumMathUtility<Complex>.ApplyMatrix(localVector, gate);
                for (int mask = 0; mask < expectedDim; mask++)
                {
                    Complex res = resultVector[mask]; if (res == Complex.Zero) continue;
                    int[] variant = (int[])exemplar.Clone();
                    for (int bit = 0; bit < numTargets; bit++) variant[distinctTargets[bit]] = (mask >> (numTargets - 1 - bit)) & 1;
                    newAmps[variant] = res;
                }
            }
            _amplitudes = newAmps; // simplified
            NormaliseAmplitudes();
        }

        private readonly struct PatternKey : IEquatable<PatternKey>
        {
            public readonly int[] Pattern;
            public PatternKey(int[] pattern) { Pattern = pattern; }
            public bool Equals(PatternKey other)
            {
                if (ReferenceEquals(Pattern, other.Pattern)) return true;
                if (Pattern.Length != other.Pattern.Length) return false;
                for (int i = 0; i < Pattern.Length; i++) if (Pattern[i] != other.Pattern[i]) return false;
                return true;
            }
            public override bool Equals(object? obj) => obj is PatternKey pk && Equals(pk);
            public override int GetHashCode()
            {
                unchecked { int h = 17; for (int i = 0; i < Pattern.Length; i++) h = h * 31 + Pattern[i]; return h; }
            }
        }

        // Helper: Converts the bits from a full basis state based on target indices.
        private static int[] ExtractSubstate(int[] fullState, int[] indices)
        { int[] sub = new int[indices.Length]; for (int i = 0; i < indices.Length; i++) sub[i] = fullState[indices[i]]; return sub; }

        // Helper: Converts an array of bits to its integer representation.
        private static int BitsToIndex(int[] bits) { int index = 0; foreach (int bit in bits) index = (index << 1) | bit; return index; }

        /// <summary>
        /// Optionally construct the system with explicit amplitudes.
        /// </summary>
        public QuantumSystem(Dictionary<int[], Complex>? initialAmps = null)
        { if (initialAmps != null) { _amplitudes = initialAmps; NormaliseAmplitudes(); } }

        /// <summary>
        /// Will return a multiline string that you can print to the console visualising the gate schedule.
        /// </summary>
        /// <param name="totalQubits"></param>
        /// <returns></returns>
        public string VisualiseGateSchedule(int totalQubits)
        { return _gateQueue.Count == 0 ? "no operations" : GateSchedulingVisualiser.Visualise(_gateQueue.ToArray(), totalQubits); }

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
            if (measuredIndices == null || measuredIndices.Length == 0) throw new ArgumentNullException(nameof(measuredIndices));
            Dictionary<int[], List<(int[] state, Complex amplitude)>> groups = new(new IntArrayComparer());
            foreach (var kvp in _amplitudes)
            {
                int[] projection = new int[measuredIndices.Length];
                for (int i = 0; i < measuredIndices.Length; i++) projection[i] = kvp.Key[measuredIndices[i]];
                if (!groups.TryGetValue(projection, out var list)) { list = new List<(int[] state, Complex amplitude)>(); groups[projection] = list; }
                list.Add((kvp.Key, kvp.Value));
            }
            Dictionary<int[], double> outcomeProbs = new(new IntArrayComparer());
            foreach (var g in groups)
            {
                double p = 0.0; foreach (var item in g.Value) p += item.amplitude.Magnitude * item.amplitude.Magnitude; outcomeProbs[g.Key] = p;
            }
            double totalProb = outcomeProbs.Values.Sum();
            if (totalProb < 1e-15) throw new InvalidOperationException($"Wavefunction is effectively zero. MeasuredIndices=[{string.Join(',', measuredIndices)}], BasisStates={_amplitudes.Count}, OutcomeGroups={groups.Count}, TotalProb={totalProb:E3}");
            var ordered = outcomeProbs.OrderBy(kv => BitsToIndex(kv.Key)).ToList(); // deterministic ordering
            double roll = rng.NextDouble() * totalProb; double cumulative = 0.0; int[]? chosen = null;
            foreach (var kv in ordered) { cumulative += kv.Value; if (roll <= cumulative) { chosen = kv.Key; break; } }
            chosen ??= ordered[^1].Key;
            Guid collapseId = Guid.NewGuid();
            foreach (IQuantumReference refQ in _registered)
                if (refQ.GetQubitIndices().Intersect(measuredIndices).Any()) (refQ as QuBit<bool>)?.PartialCollapse(chosen);
            return chosen;
        }


        public enum GateType { SingleQubit, TwoQubit, MultiQubit }
        public class GateOperation
        {
            public GateType OperationType { get; }
            public int[] TargetQubits { get; }
            public Complex[,] GateMatrix { get; }
            public string GateName { get; }
            public GateOperation(GateType type, int[] targets, Complex[,] matrix, string gateName)
            { OperationType = type; TargetQubits = targets; GateMatrix = matrix; GateName = gateName; }
        }

        /// <summary>
        /// Returns the complete collapsed state from the system
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public int[] GetCollapsedState() => _amplitudes.Count != 1 ? throw new InvalidOperationException("System is not fully collapsed.") : _amplitudes.Keys.First();

        public void Register(IQuantumReference qubitRef) { _registered.Add(qubitRef); }

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
            if (qubitIndices == null || qubitIndices.Length == 0) throw new ArgumentNullException(nameof(qubitIndices));
            Dictionary<int[], List<(int[] state, Complex amplitude)>> projectionGroups = new(new IntArrayComparer());
            foreach (var kvp in _amplitudes)
            {
                int[] projected = qubitIndices.Select(i => kvp.Key[i]).ToArray();
                if (!projectionGroups.ContainsKey(projected)) projectionGroups[projected] = [];
                projectionGroups[projected].Add((kvp.Key, kvp.Value));
            }
            Dictionary<int[], double> probSums = projectionGroups.ToDictionary(g => g.Key, g => g.Value.Sum(x => x.amplitude.Magnitude * x.amplitude.Magnitude), new IntArrayComparer());
            double totalProb = probSums.Values.Sum();
            if (totalProb <= 1e-15) throw new InvalidOperationException($"Global observation failed: projected probability ~0. Indices=[{string.Join(',', qubitIndices)}], BasisStates={_amplitudes.Count}, ProjectionGroups={projectionGroups.Count}, TotalProb={totalProb:E3}");
            double roll = rng.NextDouble() * totalProb; double cumulative = 0.0; int[] chosenProjection = Array.Empty<int>();
            foreach (var kv in probSums) { cumulative += kv.Value; if (roll <= cumulative) { chosenProjection = kv.Key; break; } }
            Dictionary<int[], Complex> newAmps = new(new IntArrayComparer());
            foreach (var item in projectionGroups[chosenProjection]) newAmps[item.state] = item.amplitude;
            _amplitudes = newAmps; NormaliseAmplitudes(); Guid collapseId = Guid.NewGuid();
            foreach (IQuantumReference refQ in _registered)
            {
                if (refQ.GetQubitIndices().Intersect(qubitIndices).Any())
                {
                    refQ.NotifyWavefunctionCollapsed(collapseId);
                    foreach (Guid groupId in Entanglement.GetGroupsForReference(refQ)) Entanglement.PropagateCollapse(groupId, collapseId);
                }
            }
            SafeRaiseGlobalCollapse(qubitIndices, chosenProjection); return chosenProjection;
        }

        /// <summary>
        /// Convenience overload preserving the previous API shape. Uses the default basis mapper.
        /// </summary>
        public void SetFromTensorProduct<T>(bool propagateCollapse, params QuBit<T>[] qubits) => SetFromTensorProduct(propagateCollapse, (Func<T,int>?)null, qubits);

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
            if (qubits == null || qubits.Length == 0) throw new ArgumentException("At least one qubit must be provided.");
            mapToBasis ??= v => v switch { int iv => iv, bool bv => bv ? 1 : 0, _ when typeof(T).IsEnum => Convert.ToInt32(v), _ => throw new InvalidOperationException($"Unsupported type {typeof(T)} in tensor product state. Provide a mapToBasis function.") };
            int totalQubits = qubits.Length; QuBit<T>[] systemManaged = new QuBit<T>[totalQubits];
            for (int i = 0; i < totalQubits; i++)
            {
                var original = qubits[i];
                if (original.System == this) { systemManaged[i] = original; continue; }
                int[] indices = original.GetQubitIndices(); if (indices == null || indices.Length == 0) indices = new[] { i };
                QuBit<T> sysQ = new QuBit<T>(this, indices); var weights = original.ToWeightedValues().ToDictionary(w => w.value, w => w.weight); _ = sysQ.WithWeights(weights, false); systemManaged[i] = sysQ;
            }
            Dictionary<T[], Complex> product = QuantumMathUtility<T>.TensorProduct(systemManaged);
            Dictionary<int[], Complex> result = new(new IntArrayComparer());
            foreach (var kv in product) { int[] intState = kv.Key.Select(mapToBasis).ToArray(); result[intState] = kv.Value; }
            _amplitudes = result; NormaliseAmplitudes();
            if (propagateCollapse)
            {
                Guid collapseId = Guid.NewGuid(); foreach (IQuantumReference q in _registered) q.NotifyWavefunctionCollapsed(collapseId);
            }
        }

        /// <summary>
        /// Applies a two-qubit gate, because one qubit can't handle this much chaos alone.
        /// Perfect for entangling your problems in pairs.
        /// </summary>
        public void ApplyTwoQubitGate(int qubitA, int qubitB, Complex[,] gate, string gateName)
        { if (gate.GetLength(0) != 4 || gate.GetLength(1) != 4) throw new ArgumentException($"Two-qubit gate must be 4x4. GateName={gateName}, Targets={qubitA},{qubitB}, ActualDim={gate.GetLength(0)}x{gate.GetLength(1)}"); _gateQueue.Enqueue(new GateOperation(GateType.TwoQubit, new[]{qubitA, qubitB}, gate, gateName)); }
        /// <summary>
        /// Applies a single-qubit gate, because sometimes you just need to get your life together.
        /// </summary>
        /// <param name="qubit"></param>
        /// <param name="gate"></param>
        /// <exception cref="ArgumentException"></exception>
        public void ApplySingleQubitGate(int qubit, Complex[,] gate, string gateName)
        { if (gate.GetLength(0) != 2 || gate.GetLength(1) != 2) throw new ArgumentException($"Single-qubit gate must be 2x2. GateName={gateName}, QubitIndex={qubit}, ActualDim={gate.GetLength(0)}x{gate.GetLength(1)}"); _gateQueue.Enqueue(new GateOperation(GateType.SingleQubit, new[]{qubit}, gate, gateName)); }

        /// <summary>
        /// This is like a quantum assembly line, but with more uncertainty and fewer safety regulations.
        /// </summary>
        public void ProcessGateQueue()
        {
            GateOperation[] optimizedOps = OptimizeGateQueue(_gateQueue.ToArray()); _gateQueue = new Queue<GateOperation>(optimizedOps);
            while (_gateQueue.Count > 0)
            {
                GateOperation op = _gateQueue.Dequeue();
                switch (op.OperationType)
                {
                    case GateType.SingleQubit: ProcessSingleQubitGate(op.TargetQubits[0], op.GateMatrix); break;
                    case GateType.TwoQubit: ProcessTwoQubitGate(op.TargetQubits[0], op.TargetQubits[1], op.GateMatrix); break;
                    case GateType.MultiQubit: throw new InvalidOperationException("MultiQubit operations should be processed immediately.");
                    default: throw new InvalidOperationException("Unsupported gate operation.");
                }
            }
        }

        /// <summary>
        /// A bit like cleaning up your closet, but with more qubits and fewer sweaters.
        /// </summary>
        private GateOperation[] OptimizeGateQueue(GateOperation[] operations)
        { Stack<GateOperation> stack = new(); foreach (var op in operations) { if (stack.Count > 0 && CanCancel(stack.Peek(), op)) _ = stack.Pop(); else stack.Push(op); } return stack.Reverse().ToArray(); }
        /// <summary>
        /// With a bit of time travel you were trying to cancel out your bad decisions in life.
        /// </summary>
        private bool CanCancel(GateOperation op1, GateOperation op2)
        {
            if (op1.OperationType == op2.OperationType && op1.TargetQubits.SequenceEqual(op2.TargetQubits))
            {
                int n = op1.GateMatrix.GetLength(0);
                if (op2.GateMatrix.GetLength(0) != n || op1.GateMatrix.GetLength(1) != n || op2.GateMatrix.GetLength(1) != n) return false;
                Complex[,] product = MultiplyMatrices(op2.GateMatrix, op1.GateMatrix); Complex[,] identity = CreateIdentityMatrix(n); return AreMatricesEqual(product, identity, 1e-9);
            }
            return false;
        }
        private Complex[,] MultiplyMatrices(Complex[,] A, Complex[,] B)
        { int n = A.GetLength(0); int m = A.GetLength(1); int p = B.GetLength(1); if (B.GetLength(0) != m) throw new ArgumentException("Matrix dimensions do not match for multiplication."); Complex[,] result = new Complex[n,p]; for (int i=0;i<n;i++) for (int j=0;j<p;j++){ Complex sum=Complex.Zero; for(int k=0;k<m;k++) sum+=A[i,k]*B[k,j]; result[i,j]=sum;} return result; }
        private Complex[,] CreateIdentityMatrix(int n) { Complex[,] identity = new Complex[n,n]; for(int i=0;i<n;i++) for(int j=0;j<n;j++) identity[i,j]=(i==j)?Complex.One:Complex.Zero; return identity; }
        private bool AreMatricesEqual(Complex[,] A, Complex[,] B, double tolerance)
        { int rows=A.GetLength(0), cols=A.GetLength(1); if (B.GetLength(0)!=rows||B.GetLength(1)!=cols) return false; for(int i=0;i<rows;i++) for(int j=0;j<cols;j++) if (Complex.Abs(A[i,j]-B[i,j])>tolerance) return false; return true; }

        /// <summary>
        /// A quantum spa day for your qubit.
        /// </summary>
        private void ProcessSingleQubitGate(int qubit, Complex[,] gate)
        {
            Dictionary<int[], Complex> newAmps = new(new IntArrayComparer());
            var groups = _amplitudes.Keys.GroupBy(state => string.Join(",", state.Select((v, idx) => idx == qubit ? "*" : v.ToString())));
            foreach (var group in groups)
            {
                int[] exemplar = group.First();
                int[] basis0 = (int[])exemplar.Clone(); basis0[qubit] = 0;
                int[] basis1 = (int[])exemplar.Clone(); basis1[qubit] = 1;
                _ = _amplitudes.TryGetValue(basis0, out Complex a0);
                _ = _amplitudes.TryGetValue(basis1, out Complex a1);
                Complex new0 = gate[0,0]*a0 + gate[0,1]*a1;
                Complex new1 = gate[1,0]*a0 + gate[1,1]*a1;
                if (new0 != Complex.Zero) newAmps[basis0] = new0;
                if (new1 != Complex.Zero) newAmps[basis1] = new1;
            }
            _amplitudes = newAmps; NormaliseAmplitudes(); SafeRaiseGateExecuted("SingleQubitGate", new[]{qubit});
        }

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
                int[] exemplar = group.Value.First();
                var localVariants = new Dictionary<(int a,int b), int[]>();
                for (int a=0;a<2;a++) for (int b=0;b<2;b++) { int[] basis=(int[])exemplar.Clone(); basis[qubitA]=a; basis[qubitB]=b; localVariants[(a,b)] = basis; }
                Complex[] vec = new Complex[4];
                foreach (var kv in localVariants) { _ = _amplitudes.TryGetValue(kv.Value, out Complex amp); int idx=(kv.Key.a<<1)|kv.Key.b; vec[idx]=amp; }
                Complex[] newVec = QuantumMathUtility<Complex>.ApplyMatrix(vec, gate);
                foreach (var kv in localVariants) { int idx=(kv.Key.a<<1)|kv.Key.b; if (newVec[idx] != Complex.Zero) newAmplitudes[kv.Value] = newVec[idx]; }
            }
            _amplitudes = newAmplitudes; NormaliseAmplitudes(); SafeRaiseGateExecuted("TwoQubitGate", new[]{qubitA, qubitB});
        }

        private void SafeRaiseGateExecuted(string gateName, int[] targets) { var h = GateExecuted; if (h != null) h(gateName, targets, _amplitudes.Count); }
        private void SafeRaiseGlobalCollapse(int[] observedIndices, int[] projection) { var h = GlobalCollapse; h?.Invoke(observedIndices, projection); }

        /// <summary>
        /// Sets the amplitudes of the quantum system.
        /// A bit like changing the playlist of your favourite quantum music.
        /// This can break the universe if you’re not careful - as can break normalisation.
        /// </summary>
        /// <param name="newAmps"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void SetAmplitudes(Dictionary<int[], Complex> newAmps)
        {
            if (newAmps == null) throw new ArgumentNullException(nameof(newAmps));
            _amplitudes = new Dictionary<int[], Complex>(newAmps, new IntArrayComparer());
            Guid collapseId = Guid.NewGuid();
            foreach (IQuantumReference refQ in _registered)
            {
                refQ.NotifyWavefunctionCollapsed(collapseId);
                if (refQ is QuBit<int> qi && qi.EntanglementGroupId is Guid g1) Entanglement.PropagateCollapse(g1, collapseId);
                else if (refQ is QuBit<bool> qb && qb.EntanglementGroupId is Guid g2) Entanglement.PropagateCollapse(g2, collapseId);
                else if (refQ is QuBit<Complex> qc && qc.EntanglementGroupId is Guid g3) Entanglement.PropagateCollapse(g3, collapseId);
            }
        }

        /// <summary>
        /// Normalises amplitudes so the universe doesn’t implode.
        /// Math nerds call this "preserving probability". We call it "not being weird."
        /// </summary>
        private void NormaliseAmplitudes()
        {
            double total = _amplitudes.Values.Sum(a => a.Magnitude * a.Magnitude); if (total < 1e-15) return; double norm = Math.Sqrt(total);
            foreach (int[] k in _amplitudes.Keys.ToList()) _amplitudes[k] /= norm;
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
