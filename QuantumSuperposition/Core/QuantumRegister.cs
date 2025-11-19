using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;

namespace QuantumSuperposition.Core
{
    /// <summary>
    /// Represents a contiguous (or disjoint) logical group of qubits inside a QuantumSystem.
    /// Provides construction from qubits, integer values, or explicit amplitude vectors.
    /// </summary>
    public readonly struct QuantumRegister : IEquatable<QuantumRegister>
    {
        public QuantumSystem System { get; }
        public int[] QubitIndices { get; }

        /// <summary>
        /// Construct a register from a set of QuBit<int> references bound to the same QuantumSystem.
        /// </summary>
        public QuantumRegister(params QuBit<int>[] qubits)
        {
            if (qubits == null || qubits.Length == 0)
            {
                throw new ArgumentException("Must supply at least one qubit", nameof(qubits));
            }
            QuantumSystem? sys = qubits[0].System;
            if (sys == null)
            {
                throw new InvalidOperationException("All qubits must belong to a QuantumSystem.");
            }
            if (qubits.Any(q => q.System != sys))
            {
                throw new InvalidOperationException("All qubits must share the same QuantumSystem.");
            }
            System = sys;
            // Flatten indices; allow disjoint ordering explicitly as supplied
            QubitIndices = qubits.SelectMany(q => q.GetQubitIndices()).Distinct().OrderBy(i => i).ToArray();
        }

        private QuantumRegister(QuantumSystem system, int[] indices)
        {
            System = system ?? throw new ArgumentNullException(nameof(system));
            QubitIndices = indices ?? throw new ArgumentNullException(nameof(indices));
            if (indices.Length == 0)
            {
                throw new ArgumentException("Register must have at least one qubit index", nameof(indices));
            }
        }

        /// <summary>
        /// Collapse (partial observe) this register's qubits.
        /// Returns the measured bit values in register index order.
        /// Freezes underlying qubits to prevent further mutation.
        /// </summary>
        public int[] Collapse(Random? rng = null)
        {
            rng ??= Random.Shared;
            int[] measured = System.PartialObserve(QubitIndices, rng);

            // Freeze each matching qubit reference
            foreach (var qref in GetQubitReferences())
            {
                if (qref is QuBit<int> qi)
                {
                    qi.Lock();
                }
                else if (qref is QuBit<bool> qb)
                {
                    qb.Lock();
                }
            }
            return measured;
        }

        /// <summary>
        /// Decode a slice of the register into an integer value.
        /// If the system has not yet collapsed those qubits, a partial observation is performed.
        /// </summary>
        /// <param name="offset">Bit offset within this register (0 = least significant if indices ascending)</param>
        /// <param name="length">Number of bits; -1 means until end.</param>
        public int GetValue(int offset = 0, int length = -1)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < -1) throw new ArgumentOutOfRangeException(nameof(length));

            int total = QubitIndices.Length;
            if (offset >= total)
            {
                throw new ArgumentException("Offset beyond register length");
            }
            int effectiveLen = length == -1 ? (total - offset) : length;
            if (effectiveLen <= 0 || offset + effectiveLen > total)
            {
                throw new ArgumentException("Invalid length slice");
            }

            // Attempt to get global collapsed state; if unavailable perform partial observe
            int[] sliceBits;
            try
            {
                int[] global = System.GetCollapsedState();
                // Extract bits corresponding to this register's indices
                int[] regBits = QubitIndices.Select(i => global[i]).ToArray();
                sliceBits = regBits.Skip(offset).Take(effectiveLen).ToArray();
            }
            catch (InvalidOperationException)
            {
                // Perform partial observation for just the needed subset
                int[] subsetIndices = QubitIndices.Skip(offset).Take(effectiveLen).ToArray();
                int[] measured = System.PartialObserve(subsetIndices, Random.Shared);
                // measured returned in order of subsetIndices; treat as slice directly
                sliceBits = measured;
            }

            // BitsToIndex expects MSB-first or some ordering? Using QuantumAlgorithms.BitsToIndex directly.
            return QuantumAlgorithms.BitsToIndex(sliceBits);
        }

        /// <summary>
        /// Build a register representing bits of an integer value. Populates the system so only that basis state has amplitude 1.
        /// </summary>
        public static QuantumRegister FromInt(int value, int bits, QuantumSystem system)
        {
            if (bits <= 0) throw new ArgumentOutOfRangeException(nameof(bits));
            if (value < 0 || value >= (1 << bits)) throw new ArgumentOutOfRangeException(nameof(value), "Value does not fit in specified bit width.");
            if (system == null) throw new ArgumentNullException(nameof(system));

            // Create qubits for each bit position if not already registered
            var qubits = new List<QuBit<int>>();
            for (int i = 0; i < bits; i++)
            {
                var q = new QuBit<int>(system, new[] { i });
                qubits.Add(q);
            }

            int[] bitArray = QuantumAlgorithms.IndexToBits(value, bits); // returns MSB-first expected by library

            // Build amplitude dictionary for full system (only one basis state amplitude = 1)
            var basisState = bitArray.ToArray();
            var amps = new Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer())
            {
                { basisState, Complex.One }
            };
            system.SetAmplitudes(amps);

            return new QuantumRegister(system, Enumerable.Range(0, bits).ToArray());
        }

        /// <summary>
        /// Build a register from an explicit amplitude vector (length must be 2^n); populates the system.
        /// </summary>
        public static QuantumRegister FromAmplitudes(Complex[] amplitudes, QuantumSystem system)
        {
            if (amplitudes == null) throw new ArgumentNullException(nameof(amplitudes));
            if (system == null) throw new ArgumentNullException(nameof(system));
            int length = amplitudes.Length;
            if (length == 0 || (length & (length - 1)) != 0) throw new ArgumentException("Length must be power of two", nameof(amplitudes));
            int bits = (int)Math.Log2(length);
            var dict = new Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer());
            for (int i = 0; i < length; i++)
            {
                int[] bitsArr = QuantumAlgorithms.IndexToBits(i, bits);
                dict[bitsArr] = amplitudes[i];
            }
            system.SetAmplitudes(dict);
            for (int i = 0; i < bits; i++)
            {
                _ = new QuBit<int>(system, new[] { i });
            }
            return new QuantumRegister(system, Enumerable.Range(0, bits).ToArray());
        }

        private static Complex[] BuildSubspaceVector(QuantumRegister reg)
        {
            var sys = reg.System ?? throw new InvalidOperationException("Register not bound to a QuantumSystem.");
            int[] indices = reg.QubitIndices;
            int n = indices.Length;
            int dim = 1 << n;
            Complex[] vec = new Complex[dim];
            foreach (var kv in sys.Amplitudes)
            {
                int[] full = kv.Key;
                Complex amp = kv.Value;
                int code = 0;
                for (int i = 0; i < n; i++)
                {
                    int bit = full[indices[i]] & 1;
                    code = (code << 1) | bit;
                }
                vec[code] += amp;
            }
            return vec;
        }

        // ---------------- Canonical Named States -----------------
        /// <summary>
        /// Creates a 2-qubit EPR pair (Bell state) |00> + |11> over the given system.
        /// Returns a QuantumRegister spanning qubits [0,1] and links them via an entanglement group label "EPRPair_A".
        /// </summary>
        public static QuantumRegister EPRPair(QuantumSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            int n = 2;
            double norm = 1.0 / Math.Sqrt(2.0);
            var dict = new Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer())
            {
                { new[] { 0, 0 }, new Complex(norm, 0) },
                { new[] { 1, 1 }, new Complex(norm, 0) }
            };
            system.SetAmplitudes(dict);
            QuBit<int>[] qubits = new QuBit<int>[n];
            for (int i = 0; i < n; i++) qubits[i] = new QuBit<int>(system, new[] { i });
            _ = system.Entanglement.Link("EPRPair_A", qubits);
            return new QuantumRegister(qubits);
        }

        /// <summary>
        /// Creates an n-qubit W state: equal superposition of all basis states with Hamming weight 1.
        /// </summary>
        public static QuantumRegister WState(QuantumSystem system, int length = 3)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (length < 2) throw new ArgumentOutOfRangeException(nameof(length), "W state requires length >= 2");
            double amp = 1.0 / Math.Sqrt(length);
            var dict = new Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer());
            for (int pos = 0; pos < length; pos++)
            {
                int[] bitsArr = new int[length];
                bitsArr[pos] = 1;
                dict[bitsArr] = new Complex(amp, 0);
            }
            system.SetAmplitudes(dict);
            QuBit<int>[] qubits = new QuBit<int>[length];
            for (int i = 0; i < length; i++) qubits[i] = new QuBit<int>(system, new[] { i });
            _ = system.Entanglement.Link("WState_A", qubits);
            return new QuantumRegister(qubits);
        }

        /// <summary>
        /// Creates an n-qubit GHZ state: |00...0> + |11...1> normalised.
        /// </summary>
        public static QuantumRegister GHZState(QuantumSystem system, int length = 3)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (length < 2) throw new ArgumentOutOfRangeException(nameof(length), "GHZ state requires length >= 2");
            double amp = 1.0 / Math.Sqrt(2.0);
            int[] allZero = Enumerable.Repeat(0, length).ToArray();
            int[] allOne = Enumerable.Repeat(1, length).ToArray();
            var dict = new Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer())
            {
                { allZero, new Complex(amp, 0)},
                { allOne, new Complex(amp, 0)}
            };
            system.SetAmplitudes(dict);
            QuBit<int>[] qubits = new QuBit<int>[length];
            for (int i = 0; i < length; i++) qubits[i] = new QuBit<int>(system, new[] { i });
            _ = system.Entanglement.Link("GHZState_A", qubits);
            return new QuantumRegister(qubits);
        }
        // ----------------------------------------------------------

        // ---------------- Operator Sugar: gate * register ----------
        /// <summary>
        /// Applies a QuantumGate to the entire register (arity inferred from gate dimension).
        /// For 1-qubit gates: enqueued then processed. For 2-qubit gates: enqueued then processed.
        /// For >2 qubits: applied immediately via multi-qubit gate API.
        /// Returns a new register view on the same indices.
        /// </summary>
        public static QuantumRegister operator *(QuantumGate gate, QuantumRegister register)
        {
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            if (register.System == null) throw new InvalidOperationException("Register not bound to a QuantumSystem.");
            int dim = gate.Matrix.GetLength(0);
            if (dim != gate.Matrix.GetLength(1)) throw new ArgumentException("Gate matrix must be square.");
            if ((dim & (dim - 1)) != 0) throw new ArgumentException("Gate dimension must be a power of two.");
            int qubitCount = (int)Math.Log2(dim);
            if (qubitCount != register.QubitIndices.Length)
            {
                throw new ArgumentException("Gate arity mismatch");
            }
            QuantumSystem system = register.System;
            int[] targets = register.QubitIndices;
            string gateName = $"U{dim}"; // generic name
            if (qubitCount == 1)
            {
                system.ApplySingleQubitGate(targets[0], gate.Matrix, gateName);
                system.ProcessGateQueue();
            }
            else if (qubitCount == 2)
            {
                system.ApplyTwoQubitGate(targets[0], targets[1], gate.Matrix, gateName);
                system.ProcessGateQueue();
            }
            else
            {
                system.ApplyMultiQubitGate(targets, gate.Matrix, gateName);
            }
            // Return a fresh register pointing to same indices
            return new QuantumRegister(system, targets.ToArray());
        }
        // ----------------------------------------------------------

        // ---------------- Equality & Approximate Equality ----------
        /// <summary>
        /// Compares this register with another by aggregating the underlying system amplitudes
        /// over just the qubits in each register and checking elementwise closeness with tolerance.
        /// </summary>
        public bool AlmostEquals(QuantumRegister other, double tolerance = 1e-10)
        {
            // struct cannot be null; compare sizes
            if (QubitIndices.Length != other.QubitIndices.Length) return false;
            Complex[] v1 = BuildSubspaceVector(this);
            Complex[] v2 = BuildSubspaceVector(other);
            if (v1.Length != v2.Length) return false;
            for (int i = 0; i < v1.Length; i++)
            {
                if (Complex.Abs(v1[i] - v2[i]) > tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Exact equality check using zero tolerance on subspace amplitudes.
        /// </summary>
        public bool Equals(QuantumRegister other)
        {
            return AlmostEquals(other, 0.0);
        }

        public override bool Equals(object? obj)
        {
            return obj is QuantumRegister qr && Equals(qr);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (System?.GetHashCode() ?? 0);
                foreach (var i in QubitIndices) hash = hash * 23 + i.GetHashCode();
                return hash;
            }
        }
        // ------------------------------------------------------------

        private IEnumerable<IQuantumReference> GetQubitReferences()
        {
            var set = new HashSet<int>(QubitIndices);
            return System.GetRegisteredReferences().Where(r => r.GetQubitIndices().Any(set.Contains));
        }
    }
}
