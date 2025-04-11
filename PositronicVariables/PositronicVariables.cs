using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using QuantumSuperposition.QuantumSoup;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class PositronicEntryAttribute : Attribute
{
}

/// <summary>
/// Initializes the metaphysical I/O trap, redirecting stdout into a memory buffer
/// so we can toy with reality without the console knowing.
/// </summary>
internal static class AethericRedirectionGrid
{
    public static bool Initialized { get; } = true;

    // Store the actual StringWriter we create.
    private static readonly StringWriter _outputBuffer;

    public static StringWriter OutputBuffer => _outputBuffer;

    static AethericRedirectionGrid()
    {
        var originalOut = Console.Out;
        _outputBuffer = new StringWriter();

        // Redirect Console output into our buffer.
        Console.SetOut(_outputBuffer);
        PositronicRuntime.Instance.OracularStream = _outputBuffer;
        PositronicRuntime.Instance.Reset();

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            // Locate the simulation method by searching for our custom attribute.
            var entryPoints = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(asm =>
                {
                    try { return asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
                })
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttribute<PositronicEntryAttribute>() != null)
                .ToList();

            if (entryPoints.Count == 0)
            {
                throw new InvalidOperationException("No method marked with [PositronicEntry] was found. Please annotate a static method to use as the convergence entry point.");
            }

            if (entryPoints.Count > 1)
            {
                var methodNames = string.Join(Environment.NewLine, entryPoints.Select(m => $" - {m.DeclaringType.FullName}.{m.Name}"));
                throw new InvalidOperationException($"Multiple methods marked with [PositronicEntry] were found:{Environment.NewLine}{methodNames}{Environment.NewLine}Please mark only one method with [PositronicEntry].");
            }

            var entryPoint = entryPoints[0];


            PositronicVariable<int>.RunConvergenceLoop(() =>
            {
                entryPoint.Invoke(null, null);
            });

            // Restore the original output and write only the converged output.
            Console.SetOut(originalOut);
            originalOut.Write(_outputBuffer.ToString());
        };
    }
}


#region Initiate the matrix
/// <summary>
/// The Matrix is everywhere. It is all around us. Even now, in this very room.
/// </summary>
public static class NeuroCascadeInitializer
{
    /// <summary>
    /// Activates the latent matrix initializer. It doesn’t *do* anything,
    /// but the side effects are, frankly, terrifying.
    /// </summary>
    public static void AutoEnable()
    {
        var dummy = AethericRedirectionGrid.Initialized;
    }
}

#endregion


#region Interfaces and Runtime

/// <summary>
/// Direction of time: +1 for forward, -1 for reverse. Like a microwave, but emotionally.
/// </summary>
public interface IPositronicVariable
{
    /// <summary>
    /// Unifies all timeline slices into a single disjunctive state,
    /// kind of like a group project where everyone finally agrees on something.
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
    /// Controls the emotional direction of time: +1 means “we’re moving on,”
    /// -1 means “let’s try that whole simulation again but sadder.”
    /// </summary>
    int Entropy { get; set; }

    /// <summary>
    /// Global convergence flag.
    /// </summary>
    bool Converged { get; set; }

    /// <summary>
    /// Where all your ill-fated `Console.WriteLine` dreams are stored during convergence.
    /// It’s like a black box for your simulation.
    /// </summary>
    TextWriter OracularStream { get; set; }

    /// <summary>
    /// The collection of all Positronic variables registered.
    /// </summary>
    IList<IPositronicVariable> Variables { get; }

    /// <summary>
    /// Performs a ceremonial memory wipe on the runtime state.
    /// Ideal for fresh starts, debugging, or pretending the last run didn’t happen.
    /// </summary>
    void Reset();

    // --- Added Diagnostics ---
    /// <summary>
    /// Total iterations spent during convergence.
    /// </summary>
    int TotalConvergenceIterations { get; set; }

    /// <summary>
    /// Triggered when all positronic variables achieve harmony
    /// and agree on something for once in their chaotic lives.
    /// </summary>
    event Action OnAllConverged;
}

/// <summary>
/// Default implementation of IPositronicRuntime using the original static behavior.
/// </summary>
public class DefaultPositronicRuntime : IPositronicRuntime
{
    public int Entropy { get; set; } = -1;
    public bool Converged { get; set; } = false;
    public TextWriter OracularStream { get; set; } = null;
    public IList<IPositronicVariable> Variables { get; } = new List<IPositronicVariable>();

    // --- Added Diagnostics ---
    public int TotalConvergenceIterations { get; set; } = 0;
    public event Action OnAllConverged;

    public void Reset()
    {
        Entropy = -1;
        Converged = false;
        OracularStream = null;
        Variables.Clear();
        TotalConvergenceIterations = 0;
    }

    // Helper to invoke the global convergence event.
    public void FireAllConverged() => OnAllConverged?.Invoke();
}

/// <summary>
/// A static holder for the Positronic runtime context.
/// In production this is set to a DefaultPositronicRuntime,
/// but tests can swap it out with a fake implementation.
/// </summary>
public static class PositronicRuntime
{
    /// <summary>
    /// The one global runtime to rule them all.
    /// Swappable for testing, regrettably not swappable for existential stability.
    /// </summary>
    public static IPositronicRuntime Instance { get; set; } = new DefaultPositronicRuntime();
}

#endregion

#region PositronicVariable<T> (Value-type version)

/// <summary>
/// A "positronic" variable that stores a timeline of QuBit&lt;T&gt; states.
/// Supports negative-time convergence, partial unification, etc.
/// </summary>
/// <typeparam name="T">A value type that implements IComparable.</typeparam>
public class PositronicVariable<T> : IPositronicVariable where T : struct, IComparable
{
    private static readonly bool _ = EnableSimulation();
    private static bool EnableSimulation()
    {
        // This call triggers the static constructor of HiddenPositronicEnvironment.
        NeuroCascadeInitializer.AutoEnable();
        return true;
    }

    static PositronicVariable()
    {
        // Access a member on HiddenPositronicEnvironment to trigger its static constructor.
        var trigger = AethericRedirectionGrid.Initialized;
    }

    private static Dictionary<string, PositronicVariable<T>> registry = new Dictionary<string, PositronicVariable<T>>();

    public readonly List<QuBit<T>> timeline = new();
    private bool replacedInitialSlice = false;

    public event Action OnConverged;
    public event Action OnCollapse;
    public event Action OnTimelineAppended;

    /// <summary>
    /// The number of reality slices we've hoarded so far.
    /// The more you have, the less likely you are to sleep peacefully.
    /// </summary>
    public int TimelineLength => timeline.Count;

    internal PositronicVariable(T initialValue)
    {
        var qb = new QuBit<T>(new[] { initialValue });
        qb.Any();
        timeline.Add(qb);
        PositronicRuntime.Instance.Variables.Add(this);
    }

    internal PositronicVariable(QuBit<T> qb)
    {
        qb.Any();
        timeline.Add(qb);
        PositronicRuntime.Instance.Variables.Add(this);
    }

    public static void ResetStaticVariables()
    {
        registry.Clear(); 
        PositronicRuntime.Instance.Reset();
    }

    public static void SetEntropy(int e) => PositronicRuntime.Instance.Entropy = e;
    public static int GetEntropy() => PositronicRuntime.Instance.Entropy;

    /// <summary>
    /// Let's start madness with a bang!
    /// </summary>
    public static PositronicVariable<T> GetOrCreate(string id, T initialValue)
    {
        if (registry.TryGetValue(id, out var instance))
        {
            return instance;
        }
        else
        {
            instance = new PositronicVariable<T>(initialValue);
            registry[id] = instance;
            return instance;
        }
    }


    /// <summary>
    /// Returns true if all registered PositronicVariables have converged.
    /// </summary>
    public static bool AllConverged()
    {
        bool all = PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
        if (all)
            (PositronicRuntime.Instance as DefaultPositronicRuntime)?.FireAllConverged();
        return all;
    }

    public static IEnumerable<IPositronicVariable> GetAllVariables() => PositronicRuntime.Instance.Variables;

    /// <summary>
    /// Runs your code in a magical simulation loop until all variables calm down.
    /// Think of it as a spa treatment for logic, with a mild chance of infinite recursion.
    /// </summary>
    public static void RunConvergenceLoop(Action code)
    {
        if (PositronicRuntime.Instance.OracularStream == null)
            PositronicRuntime.Instance.OracularStream = Console.Out;

        PositronicRuntime.Instance.Converged = false;
        // Redirect console output to TextWriter.Null during convergence
        Console.SetOut(TextWriter.Null);
        PositronicRuntime.Instance.Entropy = -1;

        const int maxIters = 1000;
        int iteration = 0;

        while (!PositronicRuntime.Instance.Converged && iteration < maxIters)
        {
            code();

            // Debug prints omitted for real console.
            (PositronicRuntime.Instance as DefaultPositronicRuntime).TotalConvergenceIterations = iteration;

            bool allVarsConverged = PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
            if (allVarsConverged)
            {
                PositronicRuntime.Instance.Converged = true;
                foreach (var pv in PositronicRuntime.Instance.Variables)
                    pv.UnifyAll();

                // Clear prior output so that only final output appears.
                AethericRedirectionGrid.OutputBuffer.GetStringBuilder().Clear();
                Console.SetOut(PositronicRuntime.Instance.OracularStream);
                break;
            }

            iteration++;
        }

        Console.SetOut(PositronicRuntime.Instance.OracularStream);
        PositronicRuntime.Instance.Entropy = 1;
        // Final run produces only converged output.
        code();
    }

    /// <summary>
    /// Checks for repetitive states in the timeline, like déjà vu but for data.
    /// </summary>
    /// <returns></returns>
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
    /// Merges all timeline slices into a single coherent lie you can tell the debugger.
    /// Suitable for post-hoc rationalization and quantum gaslighting.
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
        OnCollapse?.Invoke();
        OnConverged?.Invoke();
    }

    /// <summary>
    /// Unifies the last 'count' timeline slices into one.
    /// </summary>
    public void Unify(int count)
    {
        if (count < 2 || timeline.Count < count) return;
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
        OnCollapse?.Invoke();
        OnConverged?.Invoke();
    }

    /// <summary>
    /// Assimilates the current quantum essence of another variable,
    /// Borg-style, into this timeline.
    /// Resistance is futile, and by futile, we mean overwritten.
    /// </summary>
    /// <param name="other"></param>
    public void Assign(PositronicVariable<T> other)
    {
        var qb = other.GetCurrentQBit();
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }

    /// <summary>
    /// Forcefully injects a boring old scalar value into our glorious quantum journal.
    /// Basically like writing in crayon on an ancient scroll.
    /// </summary>
    /// <param name="scalarValue"></param>
    public void Assign(T scalarValue)
    {
        var qb = new QuBit<T>(new[] { scalarValue });
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }

    /// <summary>
    /// Quantum journaling: either overwrite your past, append a new branch,
    /// or perform a retroactive group hug across all realities.
    /// </summary>
    private void ReplaceOrAppendOrUnify(QuBit<T> qb)
    {
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

        timeline.Add(qb);
        OnTimelineAppended?.Invoke();

        for (int cycle = 2; cycle <= 20; cycle++)
        {
            if (timeline.Count >= cycle + 1 && SameStates(timeline[^1], timeline[^(cycle + 1)]))
            {
                Unify(cycle);
                break;
            }
        }
    }


    public static PositronicVariable<T> operator +(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() + right;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }
    public static PositronicVariable<T> operator +(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() + left;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }
    public static PositronicVariable<T> operator +(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() + right.GetCurrentQBit();
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }

    public static PositronicVariable<T> operator %(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() % right;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }
    public static PositronicVariable<T> operator %(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() % left;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }
    public static PositronicVariable<T> operator %(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() % right.GetCurrentQBit();
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }


    public static PositronicVariable<T> operator -(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() - right;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }
    public static PositronicVariable<T> operator -(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() - left;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }

    public static PositronicVariable<T> operator -(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() - right.GetCurrentQBit();
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }

    public static PositronicVariable<T> operator -(PositronicVariable<T> value)
    {
        // We assume the type T supports negation via dynamic
        var qb = value.GetCurrentQBit();
        var negatedValues = qb.ToCollapsedValues()
            .Select(v => (T)(-(dynamic)v))
            .ToArray();
        var negatedQb = new QuBit<T>(negatedValues);
        negatedQb.Any();
        return new PositronicVariable<T>(negatedQb);
    }
    public static PositronicVariable<T> operator *(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() * right;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }

    public static PositronicVariable<T> operator *(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() * right.GetCurrentQBit();
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }
    public static PositronicVariable<T> operator *(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() * left;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }

    public static PositronicVariable<T> operator /(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() / right.GetCurrentQBit();
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }
    public static PositronicVariable<T> operator /(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() / left;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }
    public static PositronicVariable<T> operator /(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() / right;
        resultQB.Any();
        return new PositronicVariable<T>(resultQB);
    }


    public static bool operator <(PositronicVariable<T> left, T right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().CompareTo(right) < 0;
    }
    public static bool operator >(PositronicVariable<T> left, T right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().CompareTo(right) > 0;
    }
    public static bool operator <=(PositronicVariable<T> left, T right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().CompareTo(right) <= 0;
    }
    public static bool operator >=(PositronicVariable<T> left, T right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().CompareTo(right) >= 0;
    }
    public static bool operator <(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().CompareTo(
            right.GetCurrentQBit().ToCollapsedValues().First()) < 0;
    }
    public static bool operator >(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().CompareTo(
            right.GetCurrentQBit().ToCollapsedValues().First()) > 0;
    }
    public static bool operator <=(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().CompareTo(
            right.GetCurrentQBit().ToCollapsedValues().First()) <= 0;
    }
    public static bool operator >=(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().CompareTo(
            right.GetCurrentQBit().ToCollapsedValues().First()) >= 0;
    }

    public static bool operator ==(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.SameStates(left.GetCurrentQBit(), right.GetCurrentQBit());
    }
    public static bool operator !=(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return !(left == right);
    }
    public static bool operator ==(PositronicVariable<T> left, T right)
    {
        return left.GetCurrentQBit().ToCollapsedValues().First().Equals(right);
    }
    public static bool operator !=(PositronicVariable<T> left, T right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj)
    {
        if (obj is PositronicVariable<T> other)
            return this == other;
        return false;
    }

    public override int GetHashCode()
    {
        return GetCurrentQBit().ToCollapsedValues().Aggregate(0, (acc, x) => acc ^ x.GetHashCode());
    }

    /// <summary>
    /// Applies a binary function across two positronic variables
    /// and combines every possible future into one glorious mess.
    /// </summary>
    /// <param name="op"></param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static PositronicVariable<T> Apply(Func<T, T, T> op, PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var leftValues = left.ToValues();
        var rightValues = right.ToValues();
        var results = leftValues.SelectMany(l => rightValues, (l, r) => op(l, r)).Distinct().ToArray();
        var newQB = new QuBit<T>(results);
        newQB.Any();
        return new PositronicVariable<T>(newQB);
    }

    /// <summary>
    /// Peers into the variable’s haunted past,
    /// retrieving a specific slice based on how far back you want to dig.
    /// (Warning: side effects may include déjà vu and existential dread.)
    /// </summary>
    /// <param name="stepsBack"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public QuBit<T> GetSlice(int stepsBack)
    {
        if (stepsBack < 0 || stepsBack >= timeline.Count)
            throw new ArgumentOutOfRangeException(nameof(stepsBack));
        return timeline[timeline.Count - 1 - stepsBack];
    }

    /// <summary>
    /// Returns all timeline slices in order.
    /// </summary>
    public IEnumerable<QuBit<T>> GetTimeline() => timeline;

    /// <summary>
    /// Narrows the variable’s uncertainty to a single value from the most recent slice.
    /// It’s like making a life decision based on your last text message.
    /// </summary>
    public void CollapseToLastSlice()
    {
        var last = timeline.Last();
        var baseline = last.ToCollapsedValues().First();
        var collapsedQB = new QuBit<T>(new[] { baseline });
        collapsedQB.Any();
        timeline.Clear();
        timeline.Add(collapsedQB);
        OnCollapse?.Invoke();
    }

    /// <summary>
    /// Collapse the timeline using a custom strategy.
    /// Ideal for control freaks, determinists, or rogue AI overlords.
    /// </summary>
    public void CollapseToLastSlice(Func<IEnumerable<T>, T> strategy)
    {
        var last = timeline.Last();
        var chosenValue = strategy(last.ToCollapsedValues());
        var collapsedQB = new QuBit<T>(new[] { chosenValue });
        collapsedQB.Any();
        timeline.Clear();
        timeline.Add(collapsedQB);
        OnCollapse?.Invoke();
    }

    // --- Built-in collapse strategies ---
    /// <summary>
    /// Even the smallest person can change the course of the future
    /// </summary>
    public static Func<IEnumerable<T>, T> CollapseMin = values => values.Min();
    /// <summary>
    /// Selects the max value from the quantum soup. This is just a tribute.
    /// (To the greatest value in the world, apparently.)
    /// </summary>
    public static Func<IEnumerable<T>, T> CollapseMax = values => values.Max();
    /// <summary>
    /// The average of all values, like a group project where everyone contributes equally.
    /// </summary>
    public static Func<IEnumerable<T>, T> CollapseFirst = values => values.First();

    /// <summary>
    /// Choose your own adventure collapse strategies.
    /// </summary>
    public static Func<IEnumerable<T>, T> CollapseRandom = values => {
        var list = values.ToList();
        var rnd = new Random();
        return list[rnd.Next(list.Count)];
    };


    /// <summary>
    /// Clones the timeline and starts a new branch,
    /// like a multiverse version of Ctrl+C.
    /// The butterfly effect is not included, but strongly implied.
    /// </summary>
    public PositronicVariable<T> Fork()
    {
        var forkedTimeline = timeline.Select(qb =>
        {
            var newQB = new QuBit<T>(qb.ToCollapsedValues().ToArray());
            newQB.Any();
            return newQB;
        }).ToList();

        var forked = new PositronicVariable<T>(forkedTimeline[0]);
        forked.timeline.Clear();
        forkedTimeline.ForEach(qb => forked.timeline.Add(qb));
        return forked;
    }

    /// <summary>
    /// Spawns a twisted mirror version of this variable
    /// where each value has been mildly tampered with.
    /// (Do not use on yourself.)
    /// </summary>
    public PositronicVariable<T> Fork(Func<T, T> transform)
    {
        var forked = Fork();
        var last = forked.timeline.Last();
        var transformedValues = last.ToCollapsedValues().Select(transform).ToArray();
        forked.timeline[forked.timeline.Count - 1] = new QuBit<T>(transformedValues);
        forked.timeline[forked.timeline.Count - 1].Any();
        return forked;
    }


    /// <summary>
    /// Returns a pretty-printed string of all slices.
    /// Great for debugging, storytelling, or just reminiscing about your variable’s midlife crisis.
    /// </summary>
    public string ToTimelineString()
    {
        return string.Join(Environment.NewLine,
            timeline.Select((qb, index) => $"Slice {index}: {qb}"));
    }

    /// <summary>
    /// Serializes this tangled web of quantum regret into something
    /// you can copy-paste into a Slack thread with no context.
    /// </summary>
    public string ExportToJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(timeline, options);
    }


    public PositronicValueWrapper Value => new(GetCurrentQBit());

    public class PositronicValueWrapper
    {
        private readonly QuBit<T> qb;
        public PositronicValueWrapper(QuBit<T> q) => qb = q;
        public IEnumerable<T> ToValues() => qb.ToCollapsedValues();
    }

    /// <summary>
    /// Collapses the current quantum cloud into discrete, mortal values.
    /// Your deterministic lizard brain will appreciate this.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<T> ToValues() => GetCurrentQBit().ToCollapsedValues();

    public QuBit<T> GetCurrentQBit() => timeline[^1];

    /// <summary>
    /// Checks whether two QuBits share the same existential baggage.
    /// Spoiler: they usually don't.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private bool SameStates(QuBit<T> a, QuBit<T> b)
    {
        var av = a.ToCollapsedValues().OrderBy(x => x).ToList();
        var bv = b.ToCollapsedValues().OrderBy(x => x).ToList();
        if (av.Count != bv.Count) return false;
        for (int i = 0; i < av.Count; i++)
            if (!av[i].Equals(bv[i])) return false;
        return true;
    }

    public override string ToString()
    {
        return GetCurrentQBit().ToString();
    }
}

#endregion

#region PositronicVariableRef<T> (Reference-type version)

/// <summary>
/// An alternative version of PositronicVariable that supports non-struct types (e.g. strings or custom objects).
/// This version removes the struct constraint and uses object equality where needed.
/// </summary>
public class PositronicVariableRef<T> : IPositronicVariable
{
    private static readonly bool _ = EnableSimulation();
    private static bool EnableSimulation()
    {
        // This call triggers the static constructor of HiddenPositronicEnvironment.
        NeuroCascadeInitializer.AutoEnable();
        return true;
    }

    private static Dictionary<string, PositronicVariableRef<T>> registry
    = new Dictionary<string, PositronicVariableRef<T>>();

    public readonly List<QuBit<T>> timeline = new();
    private bool replacedInitialSlice = false;

    public event Action OnConverged;
    public event Action OnCollapse;
    public event Action OnTimelineAppended;

    /// <summary>
    /// The number of reality slices we've hoarded so far.
    /// The more you have, the less likely you are to sleep peacefully.
    /// </summary>
    public int TimelineLength => timeline.Count;

    /// <summary>
    /// Stargate SG-1 when Daniel kept touching things she shouldn't.
    /// </summary>
    public PositronicVariableRef<T> Fork()
    {
        var forkedTimeline = timeline.Select(qb =>
        {
            var newQB = new QuBit<T>(qb.ToCollapsedValues().ToArray());
            newQB.Any(); 
            return newQB;
        }).ToList();

        var forked = new PositronicVariableRef<T>(forkedTimeline[0]);
        forked.timeline.Clear();
        forkedTimeline.ForEach(qb => forked.timeline.Add(qb));
        return forked;
    }
    /// <summary>
    /// Think of it like Ryker's beardy twin: he looks the same when he shaves, but it's not Ryker really.
    /// </summary>
    public PositronicVariableRef<T> Fork(Func<T, T> transform)
    {
        var forked = Fork();
        var last = forked.timeline.Last();
        var transformedValues = last.ToCollapsedValues().Select(transform).ToArray();
        forked.timeline[forked.timeline.Count - 1] = new QuBit<T>(transformedValues);
        forked.timeline[forked.timeline.Count - 1].Any();
        return forked;
    }


    public static PositronicVariableRef<T> GetOrCreate(string id, T initialValue)
    {
        if (registry.TryGetValue(id, out var instance))
        {
            return instance;
        }
        else
        {
            instance = new PositronicVariableRef<T>(initialValue);
            registry[id] = instance;
            return instance;
        }
    }


    internal PositronicVariableRef(T initialValue)
    {
        var qb = new QuBit<T>(new[] { initialValue });
        qb.Any();
        timeline.Add(qb);
        PositronicRuntime.Instance.Variables.Add(this);
    }

    internal PositronicVariableRef(QuBit<T> qb)
    {
        qb.Any();
        timeline.Add(qb);
        PositronicRuntime.Instance.Variables.Add(this);
    }

    public static void ResetStaticVariables()
    {
        registry.Clear();
        PositronicRuntime.Instance.Reset();
    }

    public static void SetEntropy(int e) => PositronicRuntime.Instance.Entropy = e;
    public static int GetEntropy() => PositronicRuntime.Instance.Entropy;

    public static bool AllConverged()
    {
        bool all = PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
        if (all)
            (PositronicRuntime.Instance as DefaultPositronicRuntime)?.FireAllConverged();
        return all;
    }

    public static IEnumerable<IPositronicVariable> GetAllVariables() => PositronicRuntime.Instance.Variables;

    /// <summary>
    /// Run your code until the entire universe agrees on what happened.
    /// Don’t worry—it only *feels* like an infinite loop.
    /// </summary>
    /// <param name="code"></param>
    public static void RunConvergenceLoop(Action code)
    {
        if (PositronicRuntime.Instance.OracularStream == null)
            PositronicRuntime.Instance.OracularStream = Console.Out;

        PositronicRuntime.Instance.Converged = false;
        Console.SetOut(TextWriter.Null);
        PositronicRuntime.Instance.Entropy = -1;

        const int maxIters = 1000;
        int iteration = 0;

        while (!PositronicRuntime.Instance.Converged && iteration < maxIters)
        {
            code();
            System.Diagnostics.Debug.WriteLine($"[Debug] Negative-Time Iteration: {iteration}");
            foreach (var variable in PositronicRuntime.Instance.Variables)
                System.Diagnostics.Debug.WriteLine($"   => {variable}");

            (PositronicRuntime.Instance as DefaultPositronicRuntime).TotalConvergenceIterations = iteration;

            bool allVarsConverged = PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
            if (allVarsConverged)
            {
                PositronicRuntime.Instance.Converged = true;
                foreach (var pv in PositronicRuntime.Instance.Variables)
                    pv.UnifyAll();
                Console.SetOut(PositronicRuntime.Instance.OracularStream);
                break;
            }

            iteration++;
        }

        Console.SetOut(PositronicRuntime.Instance.OracularStream);
        PositronicRuntime.Instance.Entropy = 1;
        code();
    }

    /// <summary
    /// Checks for repetitive states in the timeline, like déjà vu but for data.
    /// If we detect a loop, we call it "convergence" instead of "bug."
    /// </summary>
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
    /// Merges all chaotic quantum states into a beautiful lie of consistency,
    /// like cleaning your browser history but for timelines.
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
        OnCollapse?.Invoke();
        OnConverged?.Invoke();
    }

    /// <summary>
    /// Smashes the last ‘count’ slices of the timeline into one.
    /// Think: quantum sandwich compression.
    /// </summary>
    /// <param name="count"></param>
    public void Unify(int count)
    {
        if (count < 2 || timeline.Count < count) return;
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
        OnCollapse?.Invoke();
        OnConverged?.Invoke();
    }

    /// <summary>
    /// Assimilates the current quantum essence of another variable,
    /// </summary>
    /// <param name="other"></param>
    public void Assign(PositronicVariableRef<T> other)
    {
        var qb = other.GetCurrentQBit();
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }

    /// <summary>
    /// Forcefully injects a boring old scalar value into our glorious quantum journal.
    /// </summary>
    /// <param name="scalarValue"></param>
    public void Assign(T scalarValue)
    {
        var qb = new QuBit<T>(new[] { scalarValue });
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }

    /// <summary>
    /// Replaces the current timeline slice with a new one,
    /// </summary>
    /// <param name="qb"></param>
    private void ReplaceOrAppendOrUnify(QuBit<T> qb)
    {
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

        if (!replacedInitialSlice && timeline.Count == 1)
        {
            var existing = timeline[0];
            if (!SameStates(existing, qb))
            {
                // Instead of replacing, merge the states:
                var merged = existing.ToCollapsedValues().Union(qb.ToCollapsedValues()).Distinct().ToList();
                timeline[0] = new QuBit<T>(merged);
                timeline[0].Any();
            }
            replacedInitialSlice = true;
            return;
        }


        timeline.Add(qb);
        OnTimelineAppended?.Invoke();

        for (int cycle = 2; cycle <= 20; cycle++)
        {
            if (timeline.Count >= cycle + 1 && SameStates(timeline[^1], timeline[^(cycle + 1)]))
            {
                Unify(cycle);
                break;
            }
        }
    }

    // For brevity, operator overloads (arithmetic, relational, equality) and multi-variable Apply
    // could be implemented similarly to PositronicVariable<T> if needed for reference types.

    /// <summary>
    /// Returns the current slice of quantum indecision.
    /// Warning: contents may have collapsed under observation.
    /// </summary>
    /// <returns></returns>
    public QuBit<T> GetCurrentQBit() => timeline[^1];

    public IEnumerable<T> ToValues() => GetCurrentQBit().ToCollapsedValues();

    /// <summary>
    /// Outputs a friendly string version of this timeline,
    /// like reading your diary but with more entanglement.
    /// </summary>
    /// <returns></returns>
    public string ToTimelineString()
    {
        return string.Join(Environment.NewLine,
            timeline.Select((qb, index) => $"Slice {index}: {qb}"));
    }

    /// <summary>
    /// Serializes this tangled web of quantum regret into something
    /// </summary>
    /// <returns></returns>
    public string ExportToJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(timeline, options);
    }

    /// <summary>
    /// Compares two QuBits to see if they are... cosmically in sync.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private bool SameStates(QuBit<T> a, QuBit<T> b)
    {
        var av = a.ToCollapsedValues().OrderBy(x => x).ToList();
        var bv = b.ToCollapsedValues().OrderBy(x => x).ToList();
        if (av.Count != bv.Count) return false;
        for (int i = 0; i < av.Count; i++)
            if (!av[i]?.Equals(bv[i]) ?? bv[i] != null) return false;
        return true;
    }

    /// <summary>
    /// Returns a pretty-printed string of the current state.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return GetCurrentQBit().ToString();
    }

    public int ConvergedForRef() => Converged();
}

#endregion

#region NeuralNodule<T>

/// <summary>
/// A quantum-aware neuron that collects Positronic inputs,
/// applies an activation function, and fires an output into an alternate future.
/// </summary>
public class NeuralNodule<T> where T : struct, IComparable
{
    public List<PositronicVariable<T>> Inputs { get; } = new();
    public PositronicVariable<T> Output { get; }
    public Func<IEnumerable<T>, QuBit<T>> ActivationFunction { get; set; }

    /// <summary>
    /// Creates a neural nodule with a specified activation function.
    /// </summary>
    /// <param name="activation"></param>
    public NeuralNodule(Func<IEnumerable<T>, QuBit<T>> activation)
    {
        ActivationFunction = activation;
        Output = new PositronicVariable<T>(default(T));
    }

    /// <summary>
    /// Gathers quantum input states, applies a questionable function,
    /// and hurls the result into the multiverse, hoping for the best.
    /// </summary>
    public void Fire()
    {
        var inputValues = Inputs.SelectMany(i => i.ToValues());
        var result = ActivationFunction(inputValues);
        result.Any();
        Output.Assign(result);
    }

    /// <summary>
    /// Fires all neural nodules in glorious unison until they stop arguing with themselves.
    /// Think: synchronized quantum therapy sessions.
    /// Side effects may include enlightenment or light smoking.
    /// </summary>
    /// <param name="nodes"></param>
    public static void ConvergeNetwork(params NeuralNodule<T>[] nodes)
    {
        PositronicVariable<T>.RunConvergenceLoop(() =>  
        {
            foreach (var node in nodes)
                node.Fire();
        });
    }
}

#endregion