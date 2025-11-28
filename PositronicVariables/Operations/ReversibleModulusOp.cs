using PositronicVariables.Engine.Logging;
using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    /// <summary>
    ///  x % d   — but logged with enough information so the step
    ///  can be undone later.
    /// </summary>
    public sealed class ReversibleModulusOp<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        private readonly IPositronicRuntime _runtime;
        public PositronicVariable<T> Variable { get; }
        public T Divisor { get; }
        private readonly T _quotient;

        public T Original { get; }

        public string OperationName => $"Modulus by {Divisor}";

        public ReversibleModulusOp(PositronicVariable<T> variable, T divisor, IPositronicRuntime runtime)
        {
            Variable = variable;
            Divisor = divisor;
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

            // snapshot the value *before* the %
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
            // capture the Euclidean quotient q = floor(Original / divisor)
            // (for float/double/decimal this MUST be floored, not plain division)
            _quotient = Arithmetic.FloorDiv(Original, divisor);
        }

        /// <summary>
        /// Inverse:   x % d  ->  (x // d) * d + (x % d)   (i.e. reconstruct the original value)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value)
        {
            return Arithmetic.Modulus(value, Divisor);
        }

        /// <summary>
        /// Forward:   x  ->  x % d
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyInverse(T remainder)
        {
            return Arithmetic.Add(Arithmetic.Multiply(_quotient, Divisor), remainder);
        }

        /// <summary>
        /// Undoing a modulus operation is a bit like trying to un bake a cake.
        /// </summary>
        void IOperation.Undo()
        {
            // jam the original quantum sandwich back into the timeline like nothing ever happened
            QuBit<T> qb = new(new[] { Original });
            _ = qb.Any();
            Variable.ReplaceLastFromReverse(qb);

            // Tell the convergence loop we're done
            _runtime.Converged = true;
        }

    }
}
