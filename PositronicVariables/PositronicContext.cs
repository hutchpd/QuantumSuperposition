//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Numerics;

//namespace QuantumSuperposition
//{
//    /// <summary>
//    /// Marks the method where a positronic computation starts.
//    /// </summary>
//    [AttributeUsage(AttributeTargets.Method)]
//    public sealed class PositronicEntryAttribute : Attribute
//    {
//    }


//#pragma region ─────────────── Shared Context  ────────────
//    public interface IPositronicContext
//    {
//        int Entropy { get; set; }    // +1 → forward, −1 → reverse
//        bool Converged { get; set; }    // global “done” flag
//        int LoopDepth { get; set; }    // guards nested Run()
//        void Reset();
//    }

//    public sealed class PositronicContext : IPositronicContext
//    {
//        public int Entropy { get; set; } = +1;
//        public bool Converged { get; set; }
//        public int LoopDepth { get; set; }
//        public void Reset() { Entropy = +1; Converged = false; LoopDepth = 0; }
//    }
//#pragma endregion


//#pragma region ──────────── Deep‑Immutable QuantumSlice<T> & helpers ────────────────
//    public readonly record struct QuantumSlice<T>(ImmutableArray<T> States)
//        where T : IComparable<T>
//    {
//        public QuantumSlice(params T[] single)
//            : this(ImmutableArray.Create(single)) { }

//        public bool IsScalar => States.Length == 1;

//        public override string ToString() =>
//            States.Length switch
//            {
//                0 => "∅",
//                1 => States[0]?.ToString() ?? "null",
//                _ => $"{{{string.Join(", ", States)}}}"
//            };
//    }

//    public static class QuantumMath
//    {
//        /* deterministic functional helpers – never collapses             */
//        public static QuantumSlice<T> ApplyUnary<T>
//        (
//            QuantumSlice<T> src,
//            Func<T, T> op
//        ) where T : IComparable<T>
//            => new(src.States.Select(op).ToImmutableArray());

//        public static QuantumSlice<T> ApplyBinary<T>
//        (
//            QuantumSlice<T> left,
//            QuantumSlice<T> right,
//            Func<T, T, T> op
//        ) where T : IComparable<T>
//            => new(left.States.SelectMany(l => right.States, op)
//                               .Distinct()
//                               .ToImmutableArray());
//    }
//#pragma endregion


//#pragma region ────────────────────── Reversible Operation Model ─────────────────────
//    public interface IOperation<T> where T : IComparable<T>
//    {
//        string Name { get; }
//        QuantumSlice<T> Forward(QuantumSlice<T> input);
//        ImmutableHashSet<T> Reverse(ImmutableArray<T> result);
//    }

//    internal sealed class UnaryOp<T> : IOperation<T> where T : IComparable<T>
//    {
//        private readonly Func<T, T> _fwd;
//        private readonly Func<T, T> _inv;
//        public string Name { get; }
//        public UnaryOp(string n, Func<T, T> forward, Func<T, T> inverse)
//        { Name = n; _fwd = forward; _inv = inverse; }

//        public QuantumSlice<T> Forward(QuantumSlice<T> input)
//            => QuantumMath.ApplyUnary(input, _fwd);

//        public ImmutableHashSet<T> Reverse(ImmutableArray<T> result)
//            => result.Select(_inv).ToImmutableHashSet();
//    }

//    internal abstract class BinaryOp<T>
//    (
//        string Name,
//        Func<T, T, T> Fwd,
//        Func<T, T, T> Inv /* inv(r,y)=x */
//    ) : IOperation<T> where T : IComparable<T>
//    {
//        public abstract QuantumSlice<T> Right { get; }

//        public string Name { get; }

//        public QuantumSlice<T> Forward(QuantumSlice<T> input)
//            => QuantumMath.ApplyBinary(input, Right, Fwd);

//        public ImmutableHashSet<T> Reverse(ImmutableArray<T> result)
//        {
//            var seed = ImmutableHashSet.CreateBuilder<T>();
//            foreach (var r in result)
//                foreach (var y in Right.States)
//                    seed.Add(Inv(r, y));
//            return seed.ToImmutable();
//        }
//    }

//    internal sealed class AddOp<T> : BinaryOp<T> where T : INumber<T>
//    {
//        private readonly QuantumSlice<T> _rhs;

//        public AddOp(QuantumSlice<T> rhs)
//            : base("add",
//                   (x, y) => x + y,
//                   (r, y) => r - y)
//        {
//            _rhs = rhs;
//        }

//        public override QuantumSlice<T> Right => _rhs;
//    }

//#pragma endregion


//#pragma region ─────────── TimelineController<T>  (single authority) ─────────────────
//    public sealed class TimelineController<T> where T : IComparable<T>
//    {
//        private readonly List<QuantumSlice<T>> _slices = new();
//        public IReadOnlyList<QuantumSlice<T>> Slices => _slices.AsReadOnly();
//        public QuantumSlice<T> Current => _slices[^1];

//        public TimelineController(QuantumSlice<T> bootstrap)
//            => _slices.Add(bootstrap);

//        /// <summary>Main write API – append or overwrite last slice.</summary>
//        public void Write(QuantumSlice<T> next, bool overwrite = false)
//        {
//            if (overwrite && _slices.Count > 0) _slices[^1] = next;
//            else _slices.Add(next);
//        }

//        /* legacy helpers for any existing code compiled against v2.0    */
//        [Obsolete("Use Write(next) or Write(next, overwrite:true)")]
//        public void Append(QuantumSlice<T> next) => Write(next);

//        [Obsolete("Use Write(next, overwrite:true)")]
//        public void ReplaceLast(QuantumSlice<T> next) => Write(next, overwrite: true);

//        public int ConvergedDepth()
//        {
//            if (_slices.Count < 3) return 0;
//            int last = _slices.Count - 1;
//            for (int i = 2; i <= _slices.Count; i++)
//            {
//                if (Same(_slices[last], _slices[last - i]))
//                    return i;
//            }
//            return 0;
//        }

//        public void UnifyLast(int count)
//        {
//            if (count < 2 || _slices.Count < count) return;
//            var merged = _slices.Skip(_slices.Count - count)
//                                .SelectMany(s => s.States)
//                                .Distinct()
//                                .ToImmutableArray();
//            _slices.RemoveRange(_slices.Count - count, count);
//            _slices.Add(new QuantumSlice<T>(merged));
//        }

//        public void Collapse(Func<IEnumerable<T>, T> chooser)
//        {
//            var chosen = chooser(Current.States);
//            _slices.Clear();
//            _slices.Add(new QuantumSlice<T>(chosen));
//        }

//        private static bool Same(QuantumSlice<T> a, QuantumSlice<T> b)
//            => a.States.Length == b.States.Length &&
//               a.States.OrderBy(x => x).SequenceEqual(b.States.OrderBy(x => x));
//    }
//#pragma endregion


//#pragma region ─────────────── Reverse Replay Engine (stateless, pure) ───────────────
//    public sealed class ReverseReplayEngine<T> where T : IComparable<T>
//    {
//        public QuantumSlice<T> ApplyReverseCycle
//        (
//            IReadOnlyList<IOperation<T>> forwardOps,
//            QuantumSlice<T> incoming
//        )
//        {
//            var seeds = incoming.States.ToImmutableHashSet();
//            foreach (var op in forwardOps.Reverse())
//                seeds = op.Reverse(seeds.ToImmutableArray());
//            return new QuantumSlice<T>(seeds.ToImmutableArray());
//        }
//    }
//#pragma endregion


//#pragma region ──────────────── PositronicVariable<T> (public façade) ────────────────
//    public sealed class PositronicVariable<T> where T : IComparable<T>
//    {
//        private readonly IPositronicContext _ctx;
//        private readonly TimelineController<T> _tl;
//        private readonly List<IOperation<T>> _ops = new();
//        private readonly ReverseReplayEngine<T> _rr = new();

//        public PositronicVariable(T seed, IPositronicContext ctx)
//        {
//            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
//            _tl = new TimelineController<T>(new QuantumSlice<T>(seed));
//        }

//        /* ---------------- Domain & timeline exposure ----------------- */
//        public IReadOnlyList<T> Domain => _tl.Current.States;
//        public IReadOnlyList<QuantumSlice<T>> Timeline => _tl.Slices;

//        /* -------------------- Deterministic updates ------------------ */
//        public void ApplyUnary
//        (
//            Func<T, T> forward,
//            Func<T, T> inverse
//        ) => ApplyInternal(new UnaryOp<T>("unary", forward, inverse));

//        public void ApplyUnaryStochastic(Func<T, T> forward)
//            => ApplyInternal(new UnaryOp<T>("unary‑stoch", forward,
//                                            _ => throw new InvalidOperationException(
//                                                 "Cannot invert stochastic op")));

//        /* backward‑compat shim – will throw if inverse missing */
//        [Obsolete("Use ApplyUnary(fwd, inv) or ApplyUnaryStochastic(fwd)")]
//        public void ApplyUnary(Func<T, T> forward, bool stochastic = false)
//        {
//            if (stochastic) ApplyUnaryStochastic(forward);
//            else throw new ArgumentException(
//                     "ApplyUnary now requires an inverse delegate.");
//        }

//        public void ApplyBinary
//        (
//            PositronicVariable<T> other,
//            Func<T, T, T> forward,
//            Func<T, T, T> inverse
//        ) => ApplyInternal(new BinaryAdapter(other, forward, inverse));

//        /* -------------------- Convergence engine --------------------- */
//        public void Run(Action code, int maxIter = 1000)
//        {
//            if (_ctx.LoopDepth++ > 0)
//                throw new InvalidOperationException("Nested Run() not supported.");
//            try
//            {
//                _ctx.Entropy = -1;                     // always start reverse
//                for (int i = 0; !_ctx.Converged && i < maxIter; i++)
//                {
//                    code();

//                    _ctx.Entropy = -_ctx.Entropy;      // flip

//                    if (_ctx.Entropy < 0)              // just entered reverse
//                    {
//                        var rebuilt = _rr.ApplyReverseCycle(_ops, _tl.Current);
//                        _tl.Write(rebuilt);
//                        _ops.Clear();
//                    }
//                    else                               // just entered forward
//                    {
//                        var depth = _tl.ConvergedDepth();
//                        if (depth > 0)
//                        {
//                            _tl.UnifyLast(depth);
//                            _ctx.Converged = true;
//                        }
//                    }
//                }

//                if (!_ctx.Converged)
//                    throw new InvalidOperationException("Failed to converge.");
//            }
//            finally { _ctx.Reset(); }
//        }

//        /* ------------------------- plumbing -------------------------- */
//        private void ApplyInternal(IOperation<T> op)
//        {
//            if (_ctx.Entropy < 0)
//                throw new InvalidOperationException("Write attempted in reverse pass.");

//            var next = op.Forward(_tl.Current);
//            _tl.Write(next);       // always append during forward pass
//            _ops.Add(op);
//        }

//        /* ------ BinaryAdapter keeps API symmetrical, holds inverse --- */
//        private sealed class BinaryAdapter : IOperation<T>
//        {
//            private readonly PositronicVariable<T> _rhs;
//            private readonly Func<T, T, T> _f, _inv;
//            public string Name { get; }
//            public BinaryAdapter(PositronicVariable<T> rhs,
//                                 Func<T, T, T> fwd,
//                                 Func<T, T, T> inv,
//                                 string n = "binary")
//            { _rhs = rhs; _f = fwd; _inv = inv; Name = n; }

//            public QuantumSlice<T> Forward(QuantumSlice<T> left)
//                => QuantumMath.ApplyBinary(left, _rhs._tl.Current, _f);

//            public ImmutableHashSet<T> Reverse(ImmutableArray<T> result)
//            {
//                var seeds = ImmutableHashSet.CreateBuilder<T>();
//                foreach (var r in result)
//                    foreach (var y in _rhs.Domain)
//                        seeds.Add(_inv(r, y));
//                return seeds.ToImmutable();
//            }
//        }
//    }
//#pragma endregion
//}
