using PositronicVariables.Engine.Logging;
using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Linq;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;

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
        private readonly T _divisor;
        public T Divisor => _divisor;
        private readonly T _quotient;

        public T Original { get; }

        public string OperationName => $"Modulus by {_divisor}";

        public ReversibleModulusOp(PositronicVariable<T> variable, T divisor, IPositronicRuntime runtime)
        {
            Variable = variable;
            _divisor = divisor;
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
        /// <param name="remainder"></param>
        /// <returns></returns>
        public T ApplyForward(T remainder)
           => Arithmetic.Add(Arithmetic.Multiply(_quotient, _divisor), remainder);

        /// <summary>
        /// Forward:   x  ->  x % d
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyInverse(T value)
            => Arithmetic.Modulus(value, _divisor);

        /// <summary>
        /// Undoing a modulus operation is a bit like trying to un bake a cake.
        /// </summary>
        void IOperation.Undo()
        {
            // jam the original quantum sandwich back into the timeline like nothing ever happened
            var qb = new QuBit<T>(new[] { Original });
            qb.Any();
            Variable.timeline[^1] = qb;

            // Tell the convergence loop we're done
            _runtime.Converged = true;
        }

    }
}
