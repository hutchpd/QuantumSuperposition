using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Interface representing non-generic operations for a Positronic variable.
/// </summary>
public interface IPositronicVariable
{
    /// <summary>
    /// Determines how far back the timeline has converged.
    /// Returns 0 if there is no convergence.
    /// </summary>
    int Converged();

    /// <summary>
    /// Unifies all timeline slices into a single disjunctive state.
    /// </summary>
    void UnifyAll();
}

/// <summary>
/// Interface that encapsulates the global runtime state for PositronicVariables.
/// This abstraction makes it easier to substitute the global state during testing.
/// </summary>
public interface IPositronicRuntime
{
    /// <summary>
    /// Direction of time: +1 for forward, -1 for reverse.
    /// </summary>
    int Entropy { get; set; }

    /// <summary>
    /// Global convergence flag.
    /// </summary>
    bool Converged { get; set; }

    /// <summary>
    /// Captured console writer for restoring output.
    /// </summary>
    TextWriter CapturedWriter { get; set; }

    /// <summary>
    /// The collection of all Positronic variables registered.
    /// </summary>
    IList<IPositronicVariable> Variables { get; }

    /// <summary>
    /// Reset the runtime state to its initial configuration.
    /// </summary>
    void Reset();
}

/// <summary>
/// Default implementation of IPositronicRuntime using the original static behavior.
/// </summary>
public class DefaultPositronicRuntime : IPositronicRuntime
{
    public int Entropy { get; set; } = -1;
    public bool Converged { get; set; } = false;
    public TextWriter CapturedWriter { get; set; } = null;
    public IList<IPositronicVariable> Variables { get; } = new List<IPositronicVariable>();

    public void Reset()
    {
        Entropy = -1;
        Converged = false;
        CapturedWriter = null;
        Variables.Clear();
    }
}

/// <summary>
/// A static holder for the Positronic runtime context.
/// In production this is set to a DefaultPositronicRuntime,
/// but tests can swap it out with a fake implementation.
/// </summary>
public static class PositronicRuntime
{
    public static IPositronicRuntime Instance { get; set; } = new DefaultPositronicRuntime();
}

/// <summary>
/// A "positronic" variable that stores a timeline of QuBit&lt;T&gt; states.
/// Supports negative-time convergence, partial unification, etc.
/// </summary>
/// <typeparam name="T">A value type that implements IComparable.</typeparam>
public class PositronicVariable<T> : IPositronicVariable where T : struct, IComparable
{
    // --------------------------------------------------------------------------
    //   Instance: timeline tracking
    // --------------------------------------------------------------------------
    public readonly List<QuBit<T>> timeline = new();

    // Prevent overwriting the single initial slice more than once.
    private bool replacedInitialSlice = false;

    // --------------------------------------------------------------------------
    //   Constructors
    // --------------------------------------------------------------------------
    public PositronicVariable(T initialValue)
    {
        // Create a new QuBit with the initial value; set it to "any" mode if multiple states appear.
        var qb = new QuBit<T>(new[] { initialValue });
        qb.Any();
        timeline.Add(qb);
        PositronicRuntime.Instance.Variables.Add(this);
    }

    public PositronicVariable(QuBit<T> qb)
    {
        qb.Any();
        timeline.Add(qb);
        PositronicRuntime.Instance.Variables.Add(this);
    }

    // --------------------------------------------------------------------------
    //   Static Tools for Testing / Setup via the runtime context
    // --------------------------------------------------------------------------
    public static void ResetStaticVariables()
    {
        PositronicRuntime.Instance.Reset();
    }

    public static void SetEntropy(int e) => PositronicRuntime.Instance.Entropy = e;
    public static int GetEntropy() => PositronicRuntime.Instance.Entropy;

    /// <summary>
    /// Returns true if all registered PositronicVariables have converged.
    /// </summary>
    public static bool AllConverged()
    {
        return PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
    }

    public static IEnumerable<IPositronicVariable> GetAllVariables() => PositronicRuntime.Instance.Variables;

    // --------------------------------------------------------------------------
    //   RunConvergenceLoop
    // --------------------------------------------------------------------------
    //  1) Capture the console writer if not already done
    //  2) Switch to negative time and discard console output
    //  3) Repeatedly run user code until all variables converge, or we give up
    //  4) Switch to forward time, restore console, and run user code once more
    public static void RunConvergenceLoop(Action code)
    {
        // 1) Capture the console writer if not already captured
        if (PositronicRuntime.Instance.CapturedWriter == null)
            PositronicRuntime.Instance.CapturedWriter = Console.Out;

        PositronicRuntime.Instance.Converged = false;

        // 2) Switch to negative time and discard console output
        Console.SetOut(TextWriter.Null);
        PositronicRuntime.Instance.Entropy = -1;

        const int maxIters = 1000;
        int iteration = 0;

        // Negative-time loop
        while (!PositronicRuntime.Instance.Converged && iteration < maxIters)
        {
            code();  // run the user logic in negative time

            // -- Debug prints: show timeline states for each iteration
            System.Diagnostics.Debug.WriteLine($"[Debug] Negative-Time Iteration: {iteration}");

            foreach (var variable in PositronicRuntime.Instance.Variables)
            {
                Console.WriteLine($"   => {variable}");
            }
            Console.SetOut(TextWriter.Null);
            // ---------------------------------

            bool allVarsConverged = PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
            if (allVarsConverged)
            {
                PositronicRuntime.Instance.Converged = true;

                // Unify all variables now that they converged
                foreach (var pv in PositronicRuntime.Instance.Variables)
                {
                    pv.UnifyAll();
                }

                // Restore console
                Console.SetOut(PositronicRuntime.Instance.CapturedWriter);
                break;
            }

            iteration++;
        }

        // 4) Forward pass: restore console, set entropy to forward, and run user code once more
        Console.SetOut(PositronicRuntime.Instance.CapturedWriter);
        PositronicRuntime.Instance.Entropy = 1;
        code();
    }

    // --------------------------------------------------------------------------
    //   Convergence and Unification Methods
    // --------------------------------------------------------------------------
    public int Converged()
    {
        if (PositronicRuntime.Instance.Converged)
            return 1;

        if (timeline.Count < 2)
            return 0;

        var current = timeline[^1];
        for (int i = 2; i <= timeline.Count; i++)
        {
            var older = timeline[timeline.Count - i];
            if (SameStates(older, current))
                return i - 1;
        }
        return 0;
    }


    /// <summary>
    /// Unifies all timeline slices into a single multi-state slice ("any(...)").
    /// Also sets the global convergence flag. Everyone agrees now.
    /// </summary>
    public void UnifyAll()
    {
        var allStates = timeline
            .SelectMany(qb => qb.ToCollapsedValues())
            .Distinct()
            .ToList();

        var unified = new QuBit<T>(allStates);
        unified.Any();

        timeline.Clear();
        timeline.Add(unified);

        PositronicRuntime.Instance.Converged = true;
    }

    /// <summary>
    /// Unifies the last 'count' timeline slices into one.
    /// </summary>
    public void Unify(int count)
    {
        if (count < 2) return;
        if (timeline.Count < count) return;

        int start = timeline.Count - count;
        var merged = timeline
            .Skip(start)
            .SelectMany(qb => qb.ToCollapsedValues())
            .Distinct()
            .ToList();

        timeline.RemoveRange(start, count);

        var newQb = new QuBit<T>(merged);
        newQb.Any();
        timeline.Add(newQb);

        PositronicRuntime.Instance.Converged = true;
    }

    /// <summary>
    /// Copies the timeline of another variable and pretends that was your idea all along.
    /// </summary>
    public void Assign(PositronicVariable<T> other)
    {
        var qb = other.GetCurrentQBit();
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }

    /// <summary>
    /// Assigns a scalar value to the variable.
    /// </summary>
    public void Assign(T scalarValue)
    {
        var qb = new QuBit<T>(new[] { scalarValue });
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }

    /// <summary>
    /// Quantum journaling: either overwrite your past,
    /// append a new branch in the multiverse,
    /// or unify everything if we detect a repetitive cycle.
    /// </summary>
    private void ReplaceOrAppendOrUnify(QuBit<T> qb)
    {
        // If we're already globally converged, unify with current value
        if (PositronicRuntime.Instance.Converged)
        {
            var current = timeline[^1].ToCollapsedValues().ToList();
            var incoming = qb.ToCollapsedValues().ToList();
            var merged = current.Union(incoming).Distinct().ToList();
            var newQb = new QuBit<T>(merged);
            newQb.Any();
            timeline[^1] = newQb;
            return;
        }

        // Overwrite the single slice if possible (only once).
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

        // Otherwise, append the new slice.
        timeline.Add(qb);

        // Enhanced cycle detection for 2- to 20-cycles:
        for (int cycle = 2; cycle <= 20; cycle++)
        {
            // We need at least (cycle + 1) slices to compare the new slice
            // against the one 'cycle' steps behind.
            if (timeline.Count >= cycle + 1 && SameStates(timeline[^1], timeline[^(cycle + 1)]))
            {
                // We found a repeating cycle of length 'cycle'
                Unify(cycle);
                break;
            }
        }
    }


    /// <summary>
    /// Collapse the wavefunction into the last slice only.
    /// </summary>
    public void CollapseToLastSlice()
    {
        var last = timeline.Last();
        var baseline = last.ToCollapsedValues().First();
        var collapsedQB = new QuBit<T>(new[] { baseline });
        collapsedQB.Any();
        timeline.Clear();
        timeline.Add(collapsedQB);
    }

    private bool SameStates(QuBit<T> a, QuBit<T> b)
    {
        var av = a.ToCollapsedValues().OrderBy(x => x).ToList();
        var bv = b.ToCollapsedValues().OrderBy(x => x).ToList();
        if (av.Count != bv.Count) return false;
        for (int i = 0; i < av.Count; i++)
        {
            if (!av[i].Equals(bv[i])) return false;
        }
        return true;
    }

    public QuBit<T> GetCurrentQBit() => timeline[^1];

    // --------------------------------------------------------------------------
    //   Value and ToValues (for testing)
    // --------------------------------------------------------------------------
    public PositronicValueWrapper Value => new(GetCurrentQBit());

    public class PositronicValueWrapper
    {
        private readonly QuBit<T> qb;
        public PositronicValueWrapper(QuBit<T> q) => qb = q;
        public IEnumerable<T> ToValues() => qb.ToCollapsedValues();
    }

    public IEnumerable<T> ToValues() => GetCurrentQBit().ToCollapsedValues();

    // --------------------------------------------------------------------------
    //   Operator Overloads (+, %, etc.)
    // --------------------------------------------------------------------------
    public static PositronicVariable<T> operator +(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() + right;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }

    public static PositronicVariable<T> operator %(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() % right;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }

    // --------------------------------------------------------------------------
    //   ToString()
    // --------------------------------------------------------------------------
    public override string ToString()
    {
        return GetCurrentQBit().ToString();
    }
}

/// <summary>
/// A quantum-aware neuron that collects Positronic inputs,
/// applies an activation function, and fires an output into an alternate future.
/// </summary>
public class NeuralNodule<T> where T : struct, IComparable
{
    /// <summary>
    /// Input positronic variables that feed this neuron.
    /// </summary>
    public List<PositronicVariable<T>> Inputs { get; } = new();

    /// <summary>
    /// The output positronic variable that receives the activation result.
    /// </summary>
    public PositronicVariable<T> Output { get; }

    /// <summary>
    /// Activation function that returns a superposed (multi-value) QuBit.
    /// </summary>
    public Func<IEnumerable<T>, QuBit<T>> ActivationFunction { get; set; }

    public NeuralNodule(Func<IEnumerable<T>, QuBit<T>> activation)
    {
        ActivationFunction = activation;
        Output = new PositronicVariable<T>(default(T));
    }

    /// <summary>
    /// Fires the node: collects inputs, applies activation, and assigns result.
    /// </summary>
    public void Fire()
    {
        var inputValues = Inputs.SelectMany(i => i.ToValues());
        var result = ActivationFunction(inputValues);
        result.Any();
        Output.Assign(result);
    }

    /// <summary>
    /// Converges the network by running the activation function on all nodes,
    /// repeating until stable or max iterations is reached.
    /// </summary>
    public static void ConvergeNetwork(params NeuralNodule<T>[] nodes)
    {
        PositronicVariable<T>.RunConvergenceLoop(() =>
        {
            foreach (var node in nodes)
                node.Fire();
        });
    }
}
