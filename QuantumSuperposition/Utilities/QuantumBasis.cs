
using QuantumSuperposition.Core;
using System.Numerics;

namespace QuantumSuperposition.Utilities
{
    #region Collapse Logic → QuantumCollapseService<T>

    /// <summary>
    /// Minimal interface describing what the collapse service needs to manipulate
    /// (i.e. your QuBit-like class should implement this).
    /// </summary>
    public interface IQuantumCollapsible<T>
    {
        /// <summary>
        /// If true, the next Observe call forces a specific mocked value
        /// rather than a real (probabilistic) collapse.
        /// </summary>
        bool MockCollapseEnabled { get; }

        /// <summary>
        /// If <see cref="MockCollapseEnabled"/> is true, then use this forced value.
        /// </summary>
        T? MockCollapseValue { get; }

        /// <summary>
        /// Indicates whether the state is already collapsed.
        /// </summary>
        bool IsActuallyCollapsed { get; set; }

        /// <summary>
        /// The single collapsed value, if collapsed. Otherwise null or default.
        /// </summary>
        T? CollapsedValue { get; set; }

        /// <summary>
        /// Whether to forbid default(T) upon collapse.
        /// (Equivalent to your QuantumConfig.ForbidDefaultOnCollapse check.)
        /// </summary>
        bool ForbidDefault { get; }

        /// <summary>
        /// Valid states the qubit can collapse into (possibly unweighted).
        /// </summary>
        IReadOnlyCollection<T> States { get; }

        /// <summary>
        /// Dictionary of states to complex amplitudes, if weighted. Otherwise null means uniform or unweighted.
        /// </summary>
        Dictionary<T, Complex>? Weights { get; set; }

        /// <summary>
        /// Called after a final collapse occurs to mark the quantum state as `CollapsedResult`.
        /// (For example, in your QuBit you might set `_eType = QuantumStateType.CollapsedResult`.)
        /// </summary>
        void MarkCollapsed();

        /// <summary>
        /// A helper to get one value from the superposition by weighting / random selection,
        /// but not commit to collapse. (Equivalent to <c>SampleWeighted()</c>.)
        /// </summary>
        T SampleOneValue(Random rng);
    }

    /// <summary>
    /// Provides shared collapse logic for any qubit-like type implementing <see cref="IQuantumCollapsible{T}"/>.
    /// </summary>
    public static class QuantumCollapseService<T>
    {
        /// <summary>
        /// Perform a final collapse (or use a mock collapse if enabled).
        /// Checks <see cref="IQuantumCollapsible{T}.ForbidDefault"/> to optionally block default(T).
        /// </summary>
        /// <param name="qubit">The qubit-like instance.</param>
        /// <param name="rng">Optional random for reproducible outcomes.</param>
        /// <returns>The single collapsed value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no valid states or if default(T) is forbidden but chosen.</exception>
        public static T PerformCollapse(IQuantumCollapsible<T> qubit, Random? rng = null)
        {
            rng ??= Random.Shared;

            // If mock collapse is active, just return forced value:
            if (qubit.MockCollapseEnabled)
            {
                return qubit.MockCollapseValue is null
                    ? throw new InvalidOperationException("Mock collapse enabled but no mock value set.")
                    : qubit.MockCollapseValue;
            }

            // If already collapsed, do nothing:
            if (qubit.IsActuallyCollapsed && qubit.CollapsedValue is not null)
            {
                return qubit.CollapsedValue;
            }

            // Otherwise, sample from the superposition:
            if (qubit.States.Count == 0)
            {
                throw new InvalidOperationException("No states available to collapse.");
            }

            T? picked = qubit.SampleOneValue(rng);

            // Enforce 'no default(T)' policy if requested:
            if (qubit.ForbidDefault && EqualityComparer<T>.Default.Equals(picked, default!))
            {
                throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");
            }

            // Update qubit’s internal fields:
            qubit.CollapsedValue = picked;
            qubit.IsActuallyCollapsed = true;

            // Make the qubit's Weights reflect the single outcome:
            if (qubit.Weights != null)
            {
                qubit.Weights = new Dictionary<T, Complex> { { picked, Complex.One } };
            }

            qubit.MarkCollapsed();
            return picked;
        }
    }

    #endregion

    #region System Integration → IQuantumSystemIntegration

    /// <summary>
    /// Extracted interface for hooking a qubit (or other quantum reference) into a broader QuantumSystem.
    /// This replaces the monolithic approach inside QuBit, focusing purely on "system-level" usage.
    /// </summary>
    public interface IQuantumSystemIntegration
    {
        /// <summary>
        /// Which qubit indices does this object correspond to in the global wavefunction?
        /// e.g., a single qubit might be index 0, or a multi-qubit register might be {0,1}.
        /// </summary>
        int[] GetQubitIndices();

        /// <summary>
        /// Called by the system to notify that a global wavefunction collapse has occurred.
        /// The implementer can refresh local state or set an 'IsCollapsed' flag.
        /// </summary>
        void NotifyWavefunctionCollapsed(Guid collapseId);

        /// <summary>
        /// True if the local reference is collapsed (due to local or system-wide observation).
        /// </summary>
        bool IsCollapsed { get; }

        /// <summary>
        /// Retrieves the observed value if collapsed; otherwise either throw or return null.
        /// </summary>
        object? GetObservedValue();

        /// <summary>
        /// Force an observation. This might cause the system to do a partial or global measurement.
        /// Typically calls <c>QuantumSystem.ObserveGlobal(...)</c> or local fallback.
        /// </summary>
        object Observe(Random? rng = null);

        /// <summary>
        /// Applies a local unitary transformation (gate) to the wavefunction portion tracked by this reference.
        /// </summary>
        void ApplyLocalUnitary(Complex[,] gate);
    }

    #endregion

    #region 3) Arithmetic Overloads → QuBitArithmetics<T> Utility

    /// <summary>
    /// A minimal interface describing the internal data needed for superposition arithmetic.
    /// (Think: your QuBit has _qList, _weights, _ops, etc.)
    /// This interface just exposes those in a read/write or read-only form,
    /// so the extension methods can do the heavy lifting.
    /// </summary>
    public interface IQuantumArithmeticSource<T>
    {
        /// <summary>
        /// Raw set/list of possible states (like _qList).
        /// </summary>
        IEnumerable<T> States { get; }

        /// <summary>
        /// Optional dictionary of state => amplitude. Null means unweighted.
        /// </summary>
        Dictionary<T, Complex>? Weights { get; }

        /// <summary>
        /// Operators used for Add/Sub/Mul/Div/Mod, etc.
        /// </summary>
        IQuantumOperators<T> Operators { get; }
    }

    /// <summary>
    /// Provides extension methods for performing arithmetic on "qubit-like" superpositions.
    /// Instead of operator overloads, we use method calls: e.g. <c>a.Add(b)</c> instead of <c>a + b</c>.
    /// </summary>
    public static class QuBitArithmetics
    {
        /// <summary>
        /// Add every combination of (aState, bState). Weighted results are combined accordingly.
        /// </summary>
        public static (IEnumerable<T> states, Dictionary<T, Complex>? weights) Add<T>(
            this IQuantumArithmeticSource<T> a,
            IQuantumArithmeticSource<T> b)
        {
            return DoArithmetic(a, b, a.Operators.Add);
        }

        /// <summary>
        /// Multiply every combination of (aState, bState). Weighted results are combined accordingly.
        /// </summary>
        public static (IEnumerable<T> states, Dictionary<T, Complex>? weights) Multiply<T>(
            this IQuantumArithmeticSource<T> a,
            IQuantumArithmeticSource<T> b)
        {
            return DoArithmetic(a, b, a.Operators.Multiply);
        }

        /// <summary>
        /// Subtract every combination of (aState, bState). Weighted results are combined accordingly.
        /// </summary>
        public static (IEnumerable<T> states, Dictionary<T, Complex>? weights) Subtract<T>(
            this IQuantumArithmeticSource<T> a,
            IQuantumArithmeticSource<T> b)
        {
            return DoArithmetic(a, b, a.Operators.Subtract);
        }

        /// <summary>
        /// Divide every combination of (aState, bState). Weighted results are combined accordingly.
        /// </summary>
        public static (IEnumerable<T> states, Dictionary<T, Complex>? weights) Divide<T>(
            this IQuantumArithmeticSource<T> a,
            IQuantumArithmeticSource<T> b)
        {
            return DoArithmetic(a, b, a.Operators.Divide);
        }

        /// <summary>
        /// Mod every combination of (aState, bState). Weighted results are combined accordingly.
        /// </summary>
        public static (IEnumerable<T> states, Dictionary<T, Complex>? weights) Modulo<T>(
            this IQuantumArithmeticSource<T> a,
            IQuantumArithmeticSource<T> b)
        {
            return DoArithmetic(a, b, a.Operators.Mod);
        }


        /// <summary>
        /// Core mechanism: combine all states from `a` with all states from `b` via the provided operation.
        /// Combine weights (amplitudes) multiplicatively.
        /// </summary>
        private static (IEnumerable<T>, Dictionary<T, Complex>?) DoArithmetic<T>(
            IQuantumArithmeticSource<T> a,
            IQuantumArithmeticSource<T> b,
            Func<T, T, T> operation)
        {
            List<T> newStatesList = [];
            Dictionary<T, Complex>? newWeights = null;

            // If either side is weighted, result is definitely weighted.
            bool isWeighted = a.Weights != null || b.Weights != null;
            if (isWeighted)
            {
                newWeights = [];
            }

            foreach (T? aState in a.States)
            {
                // amplitude of aState
                Complex aAmp = (a.Weights != null && a.Weights.TryGetValue(aState, out Complex xAmp))
                    ? xAmp : Complex.One;

                foreach (T? bState in b.States)
                {
                    // amplitude of bState
                    Complex bAmp = (b.Weights != null && b.Weights.TryGetValue(bState, out Complex yAmp))
                        ? yAmp : Complex.One;

                    // combine values
                    T newVal = operation(aState, bState);
                    newStatesList.Add(newVal);

                    // multiply amplitudes if weighted
                    if (newWeights != null)
                    {
                        Complex combined = aAmp * bAmp;
                        if (!newWeights.ContainsKey(newVal))
                        {
                            newWeights[newVal] = Complex.Zero;
                        }

                        newWeights[newVal] += combined;
                    }
                }
            }

            return (newStatesList, newWeights);
        }
    }

    #endregion
}