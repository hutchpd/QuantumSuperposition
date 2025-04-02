using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// A "positronic" variable that stores a timeline of QuBit<T> states.
/// Supports negative-time convergence, partial unification, etc.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PositronicVariable<T> where T : struct, IComparable
{
    // --------------------------------------------------------------------------
    //   Static fields
    // --------------------------------------------------------------------------
    private static int entropy = -1;         // +1=forward, -1=reverse
    private static bool s_converged = false; // Once true, all future assignments unify
    private static readonly List<PositronicVariable<T>> antivars = new();

    // For capturing user console writer during negative-time loops
    private static TextWriter _capturedWriter = null;

    // --------------------------------------------------------------------------
    //   Per-instance: timeline tracking
    // --------------------------------------------------------------------------
    // Each slice in the timeline is a QuBit<T> representing the variable's state
    // at that moment. Negative-time steps append slices, or unify them if needed.
    public readonly List<QuBit<T>> timeline = new();

    // This is so we don't keep overwriting the single slice if timeline.Count==1
    private bool replacedInitialSlice = false;

    // --------------------------------------------------------------------------
    //   Constructors
    // --------------------------------------------------------------------------
    public PositronicVariable(T initialValue)
    {
        // Force a new QuBit and set it to "any" if multiple states appear
        var qb = new QuBit<T>(new[] { initialValue });
        qb.Any();  // ensures eDisj if we ever have multiple distinct states
        timeline.Add(qb);
        antivars.Add(this);
    }

    public PositronicVariable(QuBit<T> qb)
    {
        // Force "Any()" so if the QuBit has multiple distinct values, it shows up as "any(...)"
        qb.Any();
        timeline.Add(qb);
        antivars.Add(this);
    }

    // --------------------------------------------------------------------------
    //   Static Tools for Testing / Setup
    // --------------------------------------------------------------------------
    public static void ResetStaticVariables()
    {
        entropy = -1;
        s_converged = false;
        antivars.Clear();
        _capturedWriter = null;
    }

    public static void SetEntropy(int e) => entropy = e;
    public static int GetEntropy() => entropy;

    /// <summary>
    /// Returns true if *all* existing PositronicVariables found a repeated slice.
    /// This is used in some negative-time loops to see if we can unify everything.
    /// </summary>
    public static bool AllConverged()
    {
        return antivars.All(v => v.Converged() > 0);
    }

    public static IEnumerable<PositronicVariable<T>> GetAllVariables() => antivars;

    // --------------------------------------------------------------------------
    //   RunConvergenceLoop
    // --------------------------------------------------------------------------
    //
    //  1) Capture the console writer if not already done
    //  2) Switch to negative time, discard console
    //  3) Repeatedly run user code until all converge => unifyAll => break
    //  4) Switch to forward time, restore console, run code once more
    public static void RunConvergenceLoop(Action code)
    {
        // 1) Capture once
        if (_capturedWriter == null)
            _capturedWriter = Console.Out;

        s_converged = false;

        // 2) Negative-time => discard console
        Console.SetOut(TextWriter.Null);
        entropy = -1;

        const int maxIters = 1000;
        int iteration = 0;

        while (!s_converged && iteration < maxIters)
        {
            code(); // user code in negative time

            bool allVarsConverged = antivars.All(v => v.Converged() > 0);
            if (allVarsConverged)
            {
                s_converged = true;

                // We'll unify all variables, *then* do a quick debug dump
                foreach (var pv in antivars)
                {
                    pv.UnifyAll();
                }

                // Instead of using Console.Out (which is null), restore from _capturedWriter:
                Console.SetOut(_capturedWriter);
                // TODO: only print debug info if a verbose flag is enabled:
                // Console.WriteLine("[DEBUG] All variables unified in negative time =>");
                // foreach (var pv in antivars)
                // {
                //     Console.WriteLine("   " + pv);
                // }
                break;
            }

            iteration++;
        }

        // 4) forward pass => restore console
        Console.SetOut(_capturedWriter);
        entropy = 1;
        code(); // final forward run
    }

    // --------------------------------------------------------------------------
    //   Converged()
    // --------------------------------------------------------------------------
    // Return how far back the final slice matches an older slice. 0 => no match
    // Typically, if timeline < 2 => 0
    // else compare timeline[^1] with older slices from the second-last on back
    public int Converged()
    {
        if (timeline.Count < 2)
            return 0;

        var current = timeline[^1];
        // Compare with older slices
        for (int i = 2; i <= timeline.Count; i++)
        {
            var older = timeline[timeline.Count - i];
            if (SameStates(older, current))
                return i - 1; // how many steps back
        }
        return 0;
    }

    // --------------------------------------------------------------------------
    //   UnifyAll()
    // --------------------------------------------------------------------------
    // Merge all timeline slices into a single multi-state slice => "any(...)"
    // Then replace timeline with that single slice. Also sets global s_converged=true
    public void UnifyAll()
    {
        // Combine states from each slice
        var allStates = timeline
            .SelectMany(qb => qb.ToValues())
            .Distinct()
            .ToList();

        var unified = new QuBit<T>(allStates);
        // If multiple distinct states, QuBit sets eDisj => "any(...)"
        unified.Any();

        timeline.Clear();
        timeline.Add(unified);

        s_converged = true;

        // [NEW] debugging
        // (We do NOT forcibly print to normal console here, but we *could*.)
    }

    // --------------------------------------------------------------------------
    //   Unify(int count)
    // --------------------------------------------------------------------------
    // Merge the last 'count' slices only
    public void Unify(int count)
    {
        if (count < 2) return;
        if (timeline.Count < count) return;

        int start = timeline.Count - count;
        var merged = timeline
            .Skip(start)
            .SelectMany(qb => qb.ToValues())
            .Distinct()
            .ToList();

        // remove those slices
        timeline.RemoveRange(start, count);

        var newQb = new QuBit<T>(merged);
        newQb.Any();
        timeline.Add(newQb);

        // Mark the system as converged so further assignments merge with this slice.
        s_converged = true;
    }


    // --------------------------------------------------------------------------
    //   Assign(...) from another PositronicVariable or scalar
    // --------------------------------------------------------------------------
    public void Assign(PositronicVariable<T> other)
    {
        var qb = other.GetCurrentQBit();
        qb.Any(); // ensure correct eType if multiple states
        ReplaceOrAppendOrUnify(qb);
    }

    public void Assign(T scalarValue)
    {
        var qb = new QuBit<T>(new[] { scalarValue });
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }
    private void ReplaceOrAppendOrUnify(QuBit<T> qb)
    {
        if (s_converged)
        {
            // For both forward and negative time, merge the incoming value with the current union.
            var current = timeline[^1].ToValues().ToList();
            var incoming = qb.ToValues().ToList();
            var merged = current.Union(incoming).Distinct().ToList();
            var newQb = new QuBit<T>(merged);
            newQb.Any(); // ensure disjunctive representation
            timeline[^1] = newQb;
            return;
        }

        // If there's exactly 1 slice and we haven't replaced it yet, overwrite it.
        if (!replacedInitialSlice && timeline.Count == 1)
        {
            var existing = timeline[0];
            if (!SameStates(existing, qb))
            {
                timeline[0] = qb;
            }
            replacedInitialSlice = true;
            return;
        }

        // Otherwise, just append the new slice.
        timeline.Add(qb);
    }



    public void CollapseToLastSlice()
    {
        // Get the current unified QBit
        var last = timeline.Last();
        // Instead of using the whole union, select only the first (or last) value.
        var baseline = last.ToValues().First();
        // Create a new QBit from that single value.
        var collapsedQB = new QuBit<T>(new[] { baseline });
        collapsedQB.Any();
        timeline.Clear();
        timeline.Add(collapsedQB);
    }


    // --------------------------------------------------------------------------
    //   QuBit comparison helper
    // --------------------------------------------------------------------------
    private bool SameStates(QuBit<T> a, QuBit<T> b)
    {
        // Compare .ToValues(), ensuring they have same distinct states
        var av = a.ToValues().OrderBy(x => x).ToList();
        var bv = b.ToValues().OrderBy(x => x).ToList();
        if (av.Count != bv.Count) return false;
        for (int i = 0; i < av.Count; i++)
        {
            if (!av[i].Equals(bv[i])) return false;
        }
        return true;
    }

    public QuBit<T> GetCurrentQBit() => timeline[^1];

    // --------------------------------------------------------------------------
    //   Value / ToValues
    // --------------------------------------------------------------------------
    // For convenience in tests
    public PositronicValueWrapper Value => new(GetCurrentQBit());

    public class PositronicValueWrapper
    {
        private readonly QuBit<T> qb;
        public PositronicValueWrapper(QuBit<T> q) => qb = q;
        public IEnumerable<T> ToValues() => qb.ToValues();
    }

    public IEnumerable<T> ToValues() => GetCurrentQBit().ToValues();

    // --------------------------------------------------------------------------
    //   Operator Overloads (+, %, etc.)
    // --------------------------------------------------------------------------
    // Instead of re-implementing arithmetic, we just call your QuBit<T> operators.
    // e.g. left.GetCurrentQBit() + right => yields a QuBit<T> => wrap in a new PositronicVariable<T>.
    public static PositronicVariable<T> operator +(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() + right; // calls QuBit<T>.operator+(QuBit<T>, T)
        resultQB.Any();  // ensure multi-state is recognized
        return new PositronicVariable<T>(resultQB);
    }

    public static PositronicVariable<T> operator %(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() % right;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }

    // If you also want e.g. "T + PositronicVariable<T>", define that, but typically we do `variable + T`.

    // --------------------------------------------------------------------------
    //   ToString()
    // --------------------------------------------------------------------------
    public override string ToString()
    {
        // Delegate to QuBit<T>.ToString()
        // That way, if the unified QuBit is correctly marked as disjunctive (via Any()), it will print “any(…).”
        return GetCurrentQBit().ToString();
    }
}
