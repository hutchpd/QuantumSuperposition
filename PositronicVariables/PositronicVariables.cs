//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace YourNamespace
//{
//    /// <summary>
//    /// Interface representing non-generic operations for a Positronic variable.
//    /// </summary>
//    public interface IPositronicVariable
//    {
//        /// <summary>
//        /// Determines how far back the timeline has converged.
//        /// Returns 0 if there is no convergence.
//        /// </summary>
//        int Converged();

//        /// <summary>
//        /// Unifies all timeline slices into a single disjunctive state.
//        /// </summary>
//        void UnifyAll();
//    }

//    /// <summary>
//    /// Interface that encapsulates the global runtime state for PositronicVariables.
//    /// This abstraction makes it easier to substitute the global state during testing.
//    /// </summary>
//    public interface IPositronicRuntime
//    {
//        /// <summary>
//        /// Direction of time: +1 for forward, -1 for reverse.
//        /// </summary>
//        int Entropy { get; set; }

//        /// <summary>
//        /// Global convergence flag.
//        /// </summary>
//        bool Converged { get; set; }

//        /// <summary>
//        /// Captured console writer for restoring output.
//        /// </summary>
//        TextWriter CapturedWriter { get; set; }

//        /// <summary>
//        /// The collection of all Positronic variables registered.
//        /// </summary>
//        IList<IPositronicVariable> Variables { get; }

//        /// <summary>
//        /// Reset the runtime state to its initial configuration.
//        /// </summary>
//        void Reset();
//    }

//    /// <summary>
//    /// Default implementation of IPositronicRuntime using the original static behavior.
//    /// </summary>
//    public class DefaultPositronicRuntime : IPositronicRuntime
//    {
//        public int Entropy { get; set; } = -1;
//        public bool Converged { get; set; } = false;
//        public TextWriter CapturedWriter { get; set; } = null;
//        public IList<IPositronicVariable> Variables { get; } = new List<IPositronicVariable>();

//        public void Reset()
//        {
//            Entropy = -1;
//            Converged = false;
//            CapturedWriter = null;
//            Variables.Clear();
//        }
//    }

//    /// <summary>
//    /// A static holder for the Positronic runtime context.
//    /// In production this is set to a DefaultPositronicRuntime,
//    /// but tests can swap it out with a fake implementation.
//    /// </summary>
//    public static class PositronicRuntime
//    {
//        public static IPositronicRuntime Instance { get; set; } = new DefaultPositronicRuntime();
//    }

//    /// <summary>
//    /// A "positronic" variable that stores a timeline of QuBit&lt;T&gt; states.
//    /// Supports negative-time convergence, partial unification, etc.
//    /// </summary>
//    /// <typeparam name="T">A value type that implements IComparable.</typeparam>
//    public class PositronicVariable<T> : IPositronicVariable where T : struct, IComparable
//    {
//        // --------------------------------------------------------------------------
//        //   Instance: timeline tracking
//        // --------------------------------------------------------------------------
//        // Each slice in the timeline is a QuBit<T> representing the variable's state
//        // at that moment. Negative-time steps append slices, or unify them if needed.
//        public readonly List<QuBit<T>> timeline = new();

//        // This flag prevents overwriting the single initial slice more than once.
//        private bool replacedInitialSlice = false;

//        // --------------------------------------------------------------------------
//        //   Constructors
//        // --------------------------------------------------------------------------
//        public PositronicVariable(T initialValue)
//        {
//            // Create a new QuBit with the initial value and set it to "any" mode 
//            // (disjunctive) if multiple states appear.
//            var qb = new QuBit<T>(new[] { initialValue });
//            qb.Any();
//            timeline.Add(qb);
//            PositronicRuntime.Instance.Variables.Add(this);
//        }

//        public PositronicVariable(QuBit<T> qb)
//        {
//            // Force "Any()" so if the QuBit has multiple distinct values it is represented correctly.
//            qb.Any();
//            timeline.Add(qb);
//            PositronicRuntime.Instance.Variables.Add(this);
//        }

//        // --------------------------------------------------------------------------
//        //   Static Tools for Testing / Setup via the runtime context
//        // --------------------------------------------------------------------------
//        public static void ResetStaticVariables()
//        {
//            PositronicRuntime.Instance.Reset();
//        }

//        public static void SetEntropy(int e) => PositronicRuntime.Instance.Entropy = e;
//        public static int GetEntropy() => PositronicRuntime.Instance.Entropy;

//        /// <summary>
//        /// Returns true if all registered PositronicVariables have converged.
//        /// </summary>
//        public static bool AllConverged()
//        {
//            return PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
//        }

//        public static IEnumerable<IPositronicVariable> GetAllVariables() => PositronicRuntime.Instance.Variables;

//        // --------------------------------------------------------------------------
//        //   RunConvergenceLoop
//        // --------------------------------------------------------------------------
//        //  1) Capture the console writer if not already done
//        //  2) Switch to negative time and discard console output
//        //  3) Repeatedly run user code until all variables converge, then unify
//        //  4) Switch to forward time, restore console, and run the user code once more
//        public static void RunConvergenceLoop(Action code)
//        {
//            // 1) Capture the console writer if not already captured
//            if (PositronicRuntime.Instance.CapturedWriter == null)
//                PositronicRuntime.Instance.CapturedWriter = Console.Out;

//            PositronicRuntime.Instance.Converged = false;

//            // 2) Switch to negative time and discard console output
//            Console.SetOut(TextWriter.Null);
//            PositronicRuntime.Instance.Entropy = -1;

//            const int maxIters = 1000;
//            int iteration = 0;

//            while (!PositronicRuntime.Instance.Converged && iteration < maxIters)
//            {
//                code(); // Execute user code in negative time

//                bool allVarsConverged = PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
//                if (allVarsConverged)
//                {
//                    PositronicRuntime.Instance.Converged = true;

//                    // Unify all variables now that convergence is achieved
//                    foreach (var pv in PositronicRuntime.Instance.Variables)
//                    {
//                        pv.UnifyAll();
//                    }

//                    // Restore the console writer
//                    Console.SetOut(PositronicRuntime.Instance.CapturedWriter);
//                    // Optionally, debug info can be printed here if a verbose flag is set.
//                    break;
//                }

//                iteration++;
//            }

//            // 4) Forward pass: restore console, set entropy to forward, and run user code once more.
//            Console.SetOut(PositronicRuntime.Instance.CapturedWriter);
//            PositronicRuntime.Instance.Entropy = 1;
//            code();
//        }

//        // --------------------------------------------------------------------------
//        //   Convergence and Unification Methods
//        // --------------------------------------------------------------------------
//        /// <summary>
//        /// Determines how many steps back the current state matches a previous one.
//        /// Returns 0 if there is no match.
//        /// </summary>
//        public int Converged()
//        {
//            if (timeline.Count < 2)
//                return 0;

//            var current = timeline[^1];
//            // Compare current slice with each older slice.
//            for (int i = 2; i <= timeline.Count; i++)
//            {
//                var older = timeline[timeline.Count - i];
//                if (SameStates(older, current))
//                    return i - 1;
//            }
//            return 0;
//        }

//        /// <summary>
//        /// Unifies all timeline slices into a single multi-state slice ("any(...)").
//        /// Also sets the global convergence flag.
//        /// </summary>
//        public void UnifyAll()
//        {
//            var allStates = timeline
//                .SelectMany(qb => qb.ToValues())
//                .Distinct()
//                .ToList();

//            var unified = new QuBit<T>(allStates);
//            unified.Any();

//            timeline.Clear();
//            timeline.Add(unified);

//            PositronicRuntime.Instance.Converged = true;
//        }

//        /// <summary>
//        /// Unifies the last 'count' timeline slices into one.
//        /// </summary>
//        public void Unify(int count)
//        {
//            if (count < 2) return;
//            if (timeline.Count < count) return;

//            int start = timeline.Count - count;
//            var merged = timeline
//                .Skip(start)
//                .SelectMany(qb => qb.ToValues())
//                .Distinct()
//                .ToList();

//            timeline.RemoveRange(start, count);

//            var newQb = new QuBit<T>(merged);
//            newQb.Any();
//            timeline.Add(newQb);

//            PositronicRuntime.Instance.Converged = true;
//        }

//        // --------------------------------------------------------------------------
//        //   Assignment Methods
//        // --------------------------------------------------------------------------
//        public void Assign(PositronicVariable<T> other)
//        {
//            var qb = other.GetCurrentQBit();
//            qb.Any();
//            ReplaceOrAppendOrUnify(qb);
//        }

//        public void Assign(T scalarValue)
//        {
//            var qb = new QuBit<T>(new[] { scalarValue });
//            qb.Any();
//            ReplaceOrAppendOrUnify(qb);
//        }

//        private void ReplaceOrAppendOrUnify(QuBit<T> qb)
//        {
//            if (PositronicRuntime.Instance.Converged)
//            {
//                // Merge incoming value with the current union.
//                var current = timeline[^1].ToValues().ToList();
//                var incoming = qb.ToValues().ToList();
//                var merged = current.Union(incoming).Distinct().ToList();
//                var newQb = new QuBit<T>(merged);
//                newQb.Any();
//                timeline[^1] = newQb;
//                return;
//            }

//            // If there is exactly one slice and we haven't replaced it yet, overwrite it.
//            if (!replacedInitialSlice && timeline.Count == 1)
//            {
//                var existing = timeline[0];
//                if (!SameStates(existing, qb))
//                {
//                    timeline[0] = qb;
//                }
//                replacedInitialSlice = true;
//                return;
//            }

//            // Otherwise, append the new slice.
//            timeline.Add(qb);
//        }

//        /// <summary>
//        /// Collapses the timeline to its last slice by selecting one baseline value.
//        /// </summary>
//        public void CollapseToLastSlice()
//        {
//            var last = timeline.Last();
//            var baseline = last.ToValues().First();
//            var collapsedQB = new QuBit<T>(new[] { baseline });
//            collapsedQB.Any();
//            timeline.Clear();
//            timeline.Add(collapsedQB);
//        }

//        // --------------------------------------------------------------------------
//        //   Helper for comparing QuBit states
//        // --------------------------------------------------------------------------
//        private bool SameStates(QuBit<T> a, QuBit<T> b)
//        {
//            var av = a.ToValues().OrderBy(x => x).ToList();
//            var bv = b.ToValues().OrderBy(x => x).ToList();
//            if (av.Count != bv.Count) return false;
//            for (int i = 0; i < av.Count; i++)
//            {
//                if (!av[i].Equals(bv[i])) return false;
//            }
//            return true;
//        }

//        /// <summary>
//        /// Returns the current QuBit slice.
//        /// </summary>
//        public QuBit<T> GetCurrentQBit() => timeline[^1];

//        // --------------------------------------------------------------------------
//        //   Value and ToValues (for testing)
//        // --------------------------------------------------------------------------
//        public PositronicValueWrapper Value => new(GetCurrentQBit());

//        public class PositronicValueWrapper
//        {
//            private readonly QuBit<T> qb;
//            public PositronicValueWrapper(QuBit<T> q) => qb = q;
//            public IEnumerable<T> ToValues() => qb.ToValues();
//        }

//        public IEnumerable<T> ToValues() => GetCurrentQBit().ToValues();

//        // --------------------------------------------------------------------------
//        //   Operator Overloads (+, %, etc.)
//        // --------------------------------------------------------------------------
//        public static PositronicVariable<T> operator +(PositronicVariable<T> left, T right)
//        {
//            // Perform arithmetic via the QuBit<T> operator.
//            var resultQB = left.GetCurrentQBit() + right;
//            resultQB.Any();

//            // Apply modulo wrapping for int types in negative time and when not yet unified.
//            if (typeof(T) == typeof(int) &&
//                PositronicRuntime.Instance.Entropy == -1 &&
//                !PositronicRuntime.Instance.Converged &&
//                resultQB.ToValues().Distinct().Count() > 1)
//            {
//                var modValues = resultQB.ToValues()
//                    .Select(x => ((int)(object)x) % 3)
//                    .Cast<T>()
//                    .Distinct()
//                    .ToList();

//                resultQB = new QuBit<T>(modValues);
//                resultQB.Any();
//            }

//            return new PositronicVariable<T>(resultQB);
//        }

//        public static PositronicVariable<T> operator %(PositronicVariable<T> left, T right)
//        {
//            var resultQB = left.GetCurrentQBit() % right;
//            resultQB.Any();
//            return new PositronicVariable<T>(resultQB);
//        }

//        // --------------------------------------------------------------------------
//        //   ToString()
//        // --------------------------------------------------------------------------
//        public override string ToString()
//        {
//            return GetCurrentQBit().ToString();
//        }
//    }
//}
