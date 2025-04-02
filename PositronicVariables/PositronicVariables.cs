using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class PositronicVariable<T> where T : struct, IComparable
{
    // --------------------------------------------------------------------------
    //   Static fields
    // --------------------------------------------------------------------------
    private static int entropy = -1;
    private static bool s_converged = false;

    // All known variables
    private static readonly List<PositronicVariable<T>> antivars = new();

    // Capture the user/test's console writer once
    private static TextWriter _capturedWriter = null;

    // --------------------------------------------------------------------------
    //   Timeline
    // --------------------------------------------------------------------------
    private readonly List<QuBit<T>> timeline = new();

    // --------------------------------------------------------------------------
    //   Constructors
    // --------------------------------------------------------------------------
    public PositronicVariable(T initialValue)
    {
        var qb = new QuBit<T>(new[] { initialValue });
        qb.Any();
        timeline.Add(qb);
        antivars.Add(this);
    }

    private PositronicVariable(QuBit<T> qb)
    {
        timeline.Add(qb);
        antivars.Add(this);
    }

    // --------------------------------------------------------------------------
    //   Reset / Entropy
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

    // --------------------------------------------------------------------------
    //   RunConvergenceLoop
    // --------------------------------------------------------------------------
    //
    // This approach:
    //   1) Force negative time
    //   2) Discard console output in negative runs
    //   3) Repeatedly run user code -> check if all converge
    //       if converge => unifyAll => break out of negative loop
    //   4) Switch console to user’s test writer, set entropy=1, run code once
    public static void RunConvergenceLoop(Action code)
    {
        if (_capturedWriter == null)
            _capturedWriter = Console.Out;  // store the test's writer

        s_converged = false;
        Console.SetOut(TextWriter.Null);
        entropy = -1;

        const int maxIterations = 1000;
        int iterationCount = 0;

        // Negative-time loop
        while (!s_converged && iterationCount < maxIterations)
        {
            code();  // user code in negative time

            // Check if *all* variables are converged
            bool allConverged = antivars.All(v => v.Converged() > 0);
            if (allConverged)
            {
                s_converged = true;
                // unify all slices for each variable, 
                // ensuring we gather all states
                foreach (var v in antivars)
                    v.UnifyAll();
                break;
            }
            iterationCount++;
        }

        // Now do exactly one forward pass, capturing output
        Console.SetOut(_capturedWriter);
        entropy = 1;
        code();
    }

    // --------------------------------------------------------------------------
    //   Converged (the "second to last vs. older" approach)
    // --------------------------------------------------------------------------
    public int Converged()
    {
        if (timeline.Count < 3)
            return 0;

        var currSlice = timeline[^2]; // second-to-last
        for (int i = 3; i <= timeline.Count; i++)
        {
            var older = timeline[timeline.Count - i];
            if (SameStates(older, currSlice))
            {
                // original Perl returns (i - 2)
                return i - 2;
            }
        }
        return 0;
    }

    // --------------------------------------------------------------------------
    //   UnifyAll
    // --------------------------------------------------------------------------
    // merges *all* timeline slices into a single "any(...)" superposition
    public void UnifyAll()
    {
        var combined = new List<T>();
        foreach (var qb in timeline)
        {
            combined.AddRange(qb._qList);
        }
        var allStates = combined.Distinct().ToList();
        var unified = new QuBit<T>(allStates);
        unified.Any();
        timeline.Add(unified);
    }

    // --------------------------------------------------------------------------
    //   Unify
    // --------------------------------------------------------------------------
    // Unify the last 'count' timeline slices into one superposition.
    // This method merges the states of the last count slices,
    // takes only distinct states, and adds a new timeline slice.
    public void Unify(int count)
    {
        if (count < 2 || timeline.Count < count)
            return;

        var combined = new List<T>();
        int startIndex = timeline.Count - count;
        for (int i = startIndex; i < timeline.Count; i++)
        {
            combined.AddRange(timeline[i]._qList);
        }
        // Remove duplicates and form a unified superposition.
        var unifiedStates = combined.Distinct().ToList();
        var unified = new QuBit<T>(unifiedStates);
        unified.Any();
        timeline.Add(unified);
    }


    private bool SameStates(QuBit<T> a, QuBit<T> b)
    {
        var av = a.ToValues().OrderBy(x => x).ToList();
        var bv = b.ToValues().OrderBy(x => x).ToList();
        if (av.Count != bv.Count) return false;
        for (int i = 0; i < av.Count; i++)
        {
            if (!av[i].Equals(bv[i])) return false;
        }
        return true;
    }

    // --------------------------------------------------------------------------
    //   Assign
    // --------------------------------------------------------------------------
    public void Assign(PositronicVariable<T> other)
    {
        var qb = other.GetCurrentQBit();
        qb.Any();
        timeline.Add(qb);
    }

    public void Assign(T scalarValue)
    {
        var qb = new QuBit<T>(new[] { scalarValue });
        qb.Any();
        timeline.Add(qb);
    }

    // --------------------------------------------------------------------------
    //   Current QBit
    // --------------------------------------------------------------------------
    private QuBit<T> GetCurrentQBit() => timeline[^1];

    // --------------------------------------------------------------------------
    //   Value / ToValues
    // --------------------------------------------------------------------------
    public PositronicValueWrapper Value => new PositronicValueWrapper(GetCurrentQBit());
    public IEnumerable<T> ToValues() => GetCurrentQBit().ToValues();

    public class PositronicValueWrapper
    {
        private readonly QuBit<T> qb;
        public PositronicValueWrapper(QuBit<T> q) => qb = q;
        public IEnumerable<T> ToValues() => qb.ToValues();
    }

    // --------------------------------------------------------------------------
    //   Operators +, %
    // --------------------------------------------------------------------------
    public static PositronicVariable<T> operator +(PositronicVariable<T> left, T right)
    {
        var leftQB = left.GetCurrentQBit();
        var newStates = new List<T>();
        foreach (var s in leftQB._qList)
        {
            dynamic ds = s;
            dynamic dr = right;
            newStates.Add((T)(ds + dr));
        }
        var combined = new QuBit<T>(newStates);
        combined.Any();
        return new PositronicVariable<T>(combined);
    }

    public static PositronicVariable<T> operator %(PositronicVariable<T> left, T right)
    {
        var leftQB = left.GetCurrentQBit();
        var newStates = new List<T>();
        foreach (var s in leftQB._qList)
        {
            dynamic ds = s;
            dynamic dr = right;
            newStates.Add((T)(ds % dr));
        }
        var combined = new QuBit<T>(newStates);
        combined.Any();
        return new PositronicVariable<T>(combined);
    }

    // --------------------------------------------------------------------------
    //   ToString
    // --------------------------------------------------------------------------
    public override string ToString()
    {
        var vals = GetCurrentQBit().ToValues().Distinct().ToList();
        if (vals.Count == 1)
            return vals[0].ToString();
        return $"any({string.Join(", ", vals)})";
    }

    public static bool AllConverged()
    {
        return antivars.All(v => v.Converged() > 0);
    }

    public static IEnumerable<PositronicVariable<T>> GetAllVariables()
    {
        return antivars;
    }

}
