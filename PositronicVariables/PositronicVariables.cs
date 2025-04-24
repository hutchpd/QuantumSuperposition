using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using QuantumSuperposition.QuantumSoup;
using static System.Net.Mime.MediaTypeNames;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class PositronicEntryAttribute : Attribute
{
}

public interface IReversibleOperation<T> : IOperation
{
    /// <summary>
    /// Given the result of a forward operation, computes the inverse value.
    /// For example, if the forward op was division by 9 then the inverse is multiplication by 9.
    /// </summary>
    T ApplyInverse(T result);
    /// <summary>
    /// Given the result of a forward operation, computes the forward value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    T ApplyForward(T value);
}

public interface IReversibleSnapshotOperation<T> : IReversibleOperation<T>
{
    PositronicVariable<T> Variable { get; }
    T Original { get; }
    void IOperation.Undo()
    {
        var qb = new QuBit<T>(new[] { Original });
        qb.Any();
        // overwrite the slice produced by the forward op
        Variable.timeline[Variable.timeline.Count - 1] = qb;
    }
}

// Addition: forward op is x + A, so inverse is (result - A).
public class AdditionOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _addend;
    public T Original { get; }

    public string OperationName => $"Addition of {_addend}";

    public AdditionOperation(PositronicVariable<T> variable, T addend)
    {
        Variable = variable;
        _addend = addend;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();   // snapshot
    }

    public T ApplyInverse(T result) => (T)((dynamic)result - _addend);
    public T ApplyForward(T value) => (T)((dynamic)value + _addend);

}

// Subtraction: forward op is x - B, so inverse is (result + B).
public class SubtractionOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _subtrahend;
    public T Original { get; }

    public string OperationName => $"Subtraction of {_subtrahend}";

    public SubtractionOperation(PositronicVariable<T> variable, T subtrahend)
    {
        Variable = variable;
        _subtrahend = subtrahend;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
    }

    public T ApplyInverse(T result) => (T)((dynamic)result + _subtrahend);
    public T ApplyForward(T value) => (T)((dynamic)value - _subtrahend);

}

// SubtractionReversed: for T - x. Forward op is: result = M - x, so the inverse is x = M - result.
public class SubtractionReversedOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _minuend;
    public T Original { get; }

    public string OperationName => $"SubtractionReversed with minuend {_minuend}";

    public SubtractionReversedOperation(PositronicVariable<T> variable, T minuend)
    {
        Variable = variable;
        _minuend = minuend;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
    }

    public T ApplyInverse(T result) => (T)((dynamic)_minuend - result);
    public T ApplyForward(T value) => (T)((dynamic)_minuend - value);

}

// Multiplication: forward op is x * M, so inverse is (result / M).
public class MultiplicationOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _multiplier;
    public T Original { get; }

    public string OperationName => $"Multiplication by {_multiplier}";

    public MultiplicationOperation(PositronicVariable<T> variable, T multiplier)
    {
        Variable = variable;
        _multiplier = multiplier;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
    }

    public T ApplyInverse(T result) => (T)((dynamic)result / _multiplier);
    public T ApplyForward(T value) => (T)((dynamic)value * _multiplier);

}

// Division: forward op is x / D, so inverse is (result * D).
public class DivisionOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _divisor;
    public T Original { get; }

    public string OperationName => $"Division by {_divisor}";

    public DivisionOperation(PositronicVariable<T> variable, T divisor)
    {
        Variable = variable;
        _divisor = divisor;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
    }

    public T ApplyInverse(T result) => (T)((dynamic)result * _divisor);
    public T ApplyForward(T value) => (T)((dynamic)value / _divisor);

}

// DivisionReversed: for T / x. Forward op is: result = N / x, so inverse is x = N / result.
public class DivisionReversedOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _numerator;
    public T Original { get; }

    public string OperationName => $"DivisionReversed with numerator {_numerator}";

    public DivisionReversedOperation(PositronicVariable<T> variable, T numerator)
    {
        Variable = variable;
        _numerator = numerator;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
    }

    public T ApplyInverse(T result) => (T)((dynamic)_numerator / result);
    public T ApplyForward(T value) => (T)((dynamic)_numerator / value);

}

// Unary Negation: forward op is -x, and its own inverse.
public class NegationOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    public T Original { get; }

    public string OperationName => "Negation";

    public NegationOperation(PositronicVariable<T> variable)
    {
        Variable = variable;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
    }

    public T ApplyInverse(T result) => (T)(-(dynamic)result);
    public T ApplyForward(T value) => (T)(-(dynamic)value);

}


/// <summary>
///  x % d   — but logged with enough information so the step
///  can be undone later.
/// </summary>
public sealed class ReversibleModulusOp<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _divisor;
    private readonly T _quotient;     // ⟵  floor(original/divisor)
    public T Original { get; }

    public string OperationName => $"Modulus by {_divisor}";

    public ReversibleModulusOp(PositronicVariable<T> variable, T divisor)
    {
        Variable  = variable;
        _divisor  = divisor;

        // snapshot the value *before* the %
        Original  = variable.GetCurrentQBit().ToCollapsedValues().First();
        _quotient = (T) ((dynamic) Original / divisor);
    }

    // Forward:   r  ⟶  q·d + r  (rebuild the original value)
    public T ApplyForward(T remainder)
        => (T)((dynamic)_quotient * _divisor + remainder);

    // Inverse:   x  ⟶  x % d   (rarely used by the engine but nice to have)
    public T ApplyInverse(T value)
        => (T)((dynamic)value % _divisor);

    void IOperation.Undo()
    {
        // 1. restore the original slice
        var qb = new QuBit<T>(new[] { Original });
        qb.Any();
        Variable.timeline[^1] = qb;

        // 2. tell the convergence loop we're done
        PositronicRuntime.Instance.Converged = true;
    }

}


public class InversionOperation<T> : IOperation
{
    private readonly PositronicVariable<T> _variable;
    private readonly T _originalValue;
    private readonly T _invertedValue;
    public string OperationName { get; }

    public InversionOperation(PositronicVariable<T> variable, T originalValue, T invertedValue, string opName)
    {
        _variable = variable;
        _originalValue = originalValue;
        _invertedValue = invertedValue;
        OperationName = $"Inverse of {opName}";
    }

    public void Undo()
    {
        // To undo an inversion, reassign the original forward value.
        _variable.Assign(_originalValue);
    }
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

            // Determine the generic type to use for the convergence loop.
            // Here we assume at least one PositronicVariable exists in the runtime.
            if (PositronicRuntime.Instance.Variables.Any())
            {
                // Get the runtime type of the last variable, e.g. PositronicVariable<double>
                var lastVariable = PositronicRuntime.Instance.Variables.Last();
                // Extract the generic type argument (e.g. double)
                var genericArg = lastVariable.GetType().GetGenericArguments()[0];

                // Get the generic method RunConvergenceLoop on PositronicVariable<>
                var methodInfo = typeof(PositronicVariable<>)
                    .MakeGenericType(genericArg)
                    .GetMethod("RunConvergenceLoop", BindingFlags.Public | BindingFlags.Static);

                // Invoke the convergence loop by passing the code (which calls the entry point)
                methodInfo.Invoke(
                    null,
                    new object[] { (Action)(() =>
            {
                entryPoint.Invoke(null, null);
            }) }
                );
            }
            else
            {
                throw new InvalidOperationException("No PositronicVariable was registered. Cannot determine a generic type for convergence.");
            }

            // Restore the original output and write only the converged output.
            Console.SetOut(originalOut);
            originalOut.Write(AethericRedirectionGrid.OutputBuffer.ToString());
        };

    }
}

public interface IOperation
{
    /// <summary>
    /// Invokes the logic to undo the operation.
    /// </summary>
    void Undo();

    /// <summary>
    /// Optional name for debugging/logging.
    /// </summary>
    string OperationName { get; }
}

/// <summary>
/// A stack-based operation log for recording and undoing operations.
/// </summary>
public static class OperationLog
{
    private static readonly Stack<IOperation> _log = new Stack<IOperation>();

    public static void Record(IOperation op)
    {
        _log.Push(op);
    }

    public static IOperation Peek()
    {
        return _log.Count > 0 ? _log.Peek() : null;
    }

    public static void ReverseLastOperations()
    {
        while (_log.Count > 0)
        {
            var op = _log.Pop();
            op.Undo();
        }
    }

    // pop
    public static void Pop()
    {
        if (_log.Count > 0)
            _log.Pop();
    }

    public static void Clear() => _log.Clear();
}

/// <summary>
/// Operation that reverts the timeline to a previous snapshot after an Append.
/// </summary>
public class TimelineAppendOperation<T> : IOperation
{
    private readonly PositronicVariable<T> _variable;
    private readonly List<QuBit<T>> _backupTimeline;

    public string OperationName => "TimelineAppend";

    public TimelineAppendOperation(PositronicVariable<T> variable, List<QuBit<T>> backupTimeline)
    {
        _variable = variable;
        // Make a safe copy so we can fully restore:
        _backupTimeline = backupTimeline
            .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
            .ToList();
    }

    public void Undo()
    {
        _variable.timeline.Clear();
        _variable.timeline.AddRange(_backupTimeline);
    }
}

/// <summary>
/// Operation that reverts the timeline to a previous snapshot after a Replace.
/// </summary>
public class TimelineReplaceOperation<T> : IOperation
{
    private readonly PositronicVariable<T> _variable;
    private readonly List<QuBit<T>> _backupTimeline;

    public string OperationName => "TimelineReplace";

    public TimelineReplaceOperation(PositronicVariable<T> variable, List<QuBit<T>> backupTimeline)
    {
        _variable = variable;
        _backupTimeline = backupTimeline
            .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
            .ToList();
    }

    public void Undo()
    {
        _variable.timeline.Clear();
        _variable.timeline.AddRange(_backupTimeline);
    }
}


#region Initiate the matrix with NeuroCascadeInitializer
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
    public int Entropy { get; set; } = 1;
    public bool Converged { get; set; } = false;
    public TextWriter OracularStream { get; set; } = null;
    public IList<IPositronicVariable> Variables { get; } = new List<IPositronicVariable>();

    // --- Added Diagnostics ---
    public int TotalConvergenceIterations { get; set; } = 0;
    public event Action OnAllConverged;

    public void Reset()
    {
        Entropy = 1;
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
/// <typeparam name="T">A type that implements IComparable.</typeparam>
public class PositronicVariable<T> : IPositronicVariable
{
    // Determines at runtime if T is a value type.
    private readonly bool isValueType = typeof(T).IsValueType;
    private readonly HashSet<T> _domain = new();
    private static bool _reverseReplayDone;

    internal static void ResetReverseReplayFlag()
    {
        _reverseReplayDone = false;           // called each time the direction flips
    }

    /// <summary>
    /// Internal hook called on *any* forward append
    /// </summary>
    internal static Action TimelineAppendedHook;

    /// <summary>
    /// Injects an explicit QuBit into the timeline **without collapsing it**.
    /// </summary>
    public void Assign(QuBit<T> qb)
    {
        if (qb is null)
            throw new ArgumentNullException(nameof(qb));

        // Ensure it’s been observed at least once so that
        // subsequent operator overloads won’t collapse accidentally.
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }

    // Automatically enable simulation on first use.
    private static readonly bool _ = EnableSimulation();
    private static bool EnableSimulation()
    {
        NeuroCascadeInitializer.AutoEnable();
        return true;
    }

    static PositronicVariable()
    {
        // Ensure T implements IComparable or IComparable<T>
        if (!(typeof(IComparable).IsAssignableFrom(typeof(T)) ||
              typeof(IComparable<T>).IsAssignableFrom(typeof(T))))
        {
            throw new InvalidOperationException(
                $"Type parameter '{typeof(T).Name}' for PositronicVariable " +
                "must implement IComparable or IComparable<T> to enable proper convergence checking.");
        }

        // Trigger initialization of the environment.
        var trigger = AethericRedirectionGrid.Initialized;

    }


    // Unified registry for all PositronicVariable<T> instances.
    private static Dictionary<string, PositronicVariable<T>> registry = new Dictionary<string, PositronicVariable<T>>();

    // The timeline of quantum slices.
    public readonly List<QuBit<T>> timeline = new List<QuBit<T>>();
    private bool replacedInitialSlice = false;

    public event Action OnConverged;
    public event Action OnCollapse;
    public event Action OnTimelineAppended;

    /// <summary>
    /// The number of timeline slices currently stored.
    /// </summary>
    public int TimelineLength => timeline.Count;

    // Constructors
    internal PositronicVariable(T initialValue)
    {
        var qb = new QuBit<T>(new[] { initialValue });
        qb.Any();
        timeline.Add(qb);
        _domain.Add(initialValue);
        PositronicRuntime.Instance.Variables.Add(this);
    }

    internal PositronicVariable(QuBit<T> qb)
    {
        qb.Any();
        timeline.Add(qb);
        PositronicRuntime.Instance.Variables.Add(this);
    }

    /// <summary>
    /// Resets the registry and the global runtime.
    /// </summary>
    public static void ResetStaticVariables()
    {
        registry.Clear();
        PositronicRuntime.Instance.Reset();
        OperationLog.Clear();
    }

    public static void SetEntropy(int e) => PositronicRuntime.Instance.Entropy = e;
    public static int GetEntropy() => PositronicRuntime.Instance.Entropy;

    /// <summary>
    /// Returns an existing instance or creates a new PositronicVariable with the given id and initial value.
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
    /// Returns an existing instance or creates a new PositronicVariable with the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static PositronicVariable<T> GetOrCreate(string id)
    {
        if (registry.TryGetValue(id, out var instance))
        {
            return instance;
        }
        else
        {
            instance = new PositronicVariable<T>(default(T));
            registry[id] = instance;
            return instance;
        }
    }


    /// <summary>
    /// Returns or creates the default PositronicVariable with the initial value.
    /// </summary>
    public static PositronicVariable<T> GetOrCreate(T initialValue)
    {
        if (registry.TryGetValue("default", out var instance))
        {
            return instance;
        }
        else
        {
            instance = new PositronicVariable<T>(initialValue);
            registry["default"] = instance;
            return instance;
        }
    }

    /// <summary>
    /// Returns the or creates the default PositronicVariable given no value or id.
    /// </summary>
    public static PositronicVariable<T> GetOrCreate()
    {
        if (registry.TryGetValue("default", out var instance))
        {
            return instance;
        }
        else
        {
            instance = new PositronicVariable<T>(default(T));
            registry["default"] = instance;
            return instance;
        }
    }

    /// <summary>
    /// Returns true if all registered positronic variables have converged.
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
    /// Runs your code in a convergence loop until all variables have settled.
    /// </summary>
    public static void RunConvergenceLoop(Action code, bool runFinalIteration = true, bool unifyOnConvergence = true)
    {
        if (PositronicRuntime.Instance.OracularStream == null)
            PositronicRuntime.Instance.OracularStream = Console.Out;

        PositronicRuntime.Instance.Converged = false;
        Console.SetOut(TextWriter.Null);
        PositronicRuntime.Instance.Entropy = -1;    // ← begin with the reverse half-cycle

        const int maxIters = 1000;
        int iteration = 0;

        bool sawForwardAppend = false;   // an append during a forward half-cycle
        bool sawAnyAppend = false;   // an append during *any* half-cycle
        bool hadForwardCycle = false;   // have we ever done a forward pass?
        var previousHook = TimelineAppendedHook;
        TimelineAppendedHook = () =>
        {
            sawAnyAppend = true;
            if (PositronicRuntime.Instance.Entropy > 0)
                sawForwardAppend = true;
        };

        while (!PositronicRuntime.Instance.Converged && iteration < maxIters)
        {
            // reset-per-half-cycle bookkeeping for reverse replay
            if (PositronicRuntime.Instance.Entropy < 0)
                PositronicVariable<T>.ResetReverseReplayFlag();

            // ==== YOUR CODE RUNS ====
            code();

            // ← **NEW**: once we’ve executed code() in a forward half-cycle,
            // record that we’ve had at least one forward pass.
            if (PositronicRuntime.Instance.Entropy > 0)
                hadForwardCycle = true;

            if (PositronicRuntime.Instance.Entropy < 0)
            {
                // if we’ve already done a forward tick and it produced nothing new,
                // we really have converged
                if (hadForwardCycle && !sawForwardAppend)
                {
                    PositronicRuntime.Instance.Converged = true;
                    Console.SetOut(PositronicRuntime.Instance.OracularStream);
                    TimelineAppendedHook = previousHook;
                    break;
                }

                // undo whatever reversible ops the *just-finished* forward pass did
                OperationLog.ReverseLastOperations();

                // ← **THIS BLOCK IS REMOVED**:
                //   we no longer exit on “first ever reverse with no appends”
                //   because that was stopping us before any forward append.
                //
                // if (!hadForwardCycle && !sawAnyAppend)
                // {
                //     PositronicRuntime.Instance.Converged = true;
                //     Console.SetOut(PositronicRuntime.Instance.OracularStream);
                //     TimelineAppendedHook = previousHook;
                //     break;
                // }
            }

            // …then the existing “full convergence” check, direction flip, etc.
            bool allVarsConverged = PositronicRuntime.Instance.Variables.All(v => v.Converged() > 0);
            if (allVarsConverged && PositronicRuntime.Instance.Entropy < 0)
            {
                if (unifyOnConvergence)
                {
                    PositronicRuntime.Instance.Converged = true;
                    foreach (var pv in PositronicRuntime.Instance.Variables)
                        pv.UnifyAll();
                    AethericRedirectionGrid.OutputBuffer.GetStringBuilder().Clear();
                }
                PositronicRuntime.Instance.Converged = true;
                Console.SetOut(PositronicRuntime.Instance.OracularStream);
                break;
            }

            PositronicRuntime.Instance.Entropy = -PositronicRuntime.Instance.Entropy;
            iteration++;
        }

        // …and the final forward replay logic remains unchanged
        Console.SetOut(PositronicRuntime.Instance.OracularStream);
        if (runFinalIteration && PositronicRuntime.Instance.Converged)
        {
            PositronicRuntime.Instance.Entropy = 1;
            PositronicRuntime.Instance.Converged = false;
            code();
            OperationLog.ReverseLastOperations();
            PositronicRuntime.Instance.Converged = true;
        }

        TimelineAppendedHook = previousHook;
    }



    /// <summary>
    /// Checks for convergence by comparing timeline slices.
    /// </summary>
    public int Converged()
    {
        if (PositronicRuntime.Instance.Converged)
            return 1;

        if (timeline.Count < 3)
            return 0;

        if (SameStates(timeline[^1], timeline[^2]))
            return 1;


        for (int i = 2; i <= timeline.Count; i++)
        {
            var older = timeline[timeline.Count - i];
            if (SameStates(older, timeline[^1]))
                return i - 1;
        }
        return 0;
    }

    /// <summary>
    /// Unifies all timeline slices into a single collapsed state.
    /// </summary>
    public void UnifyAll()
    {
        // Everything we've ever seen.
        // Whatever is in the *current* slice is what survived the argument.
        var vals = timeline[^1].ToCollapsedValues()
                                   .Distinct()
                                   .ToList();      // ← could be 1 or many

        // build one canonical disjunction that still shows all possibilities
        var unified = new QuBit<T>(vals);

        unified.Any();

        timeline.Clear();
        timeline.Add(unified);

        OperationLog.Clear();

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
    /// Assimilates the quantum state of another PositronicVariable into this one.
    /// </summary>
    public void Assign(PositronicVariable<T> other)
    {
        var qb = other.GetCurrentQBit();
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }

    /// <summary>
    /// Forcefully injects a scalar value into the timeline.
    /// </summary>
    public void Assign(T scalarValue)
    {
        var qb = new QuBit<T>(new[] { scalarValue });
        qb.Any();
        ReplaceOrAppendOrUnify(qb);
    }
    /// <summary>
    /// Either replaces, appends, or unifies the current timeline slice with a new quantum slice.
    /// </summary>
    /// <summary>
    /// Either replaces, appends, or unifies the current timeline slice with a new quantum slice.
    /// </summary>
    private void ReplaceOrAppendOrUnify(QuBit<T> qb)
    {
        var runtime = PositronicRuntime.Instance;

        // If we’ve already converged, do nothing.
        if (runtime.Converged)
            return;

        // --- Reverse‐time pass (Entropy < 0) -----------------------------
        if (runtime.Entropy < 0)
        {
            // 1) Pop off everything belonging to the *last* forward half-cycle
            var poppedSnapshots = new List<IOperation>();
            var poppedReversibles = new List<IReversibleOperation<T>>();
            while (true)
            {
                var top = OperationLog.Peek();
                if (top is TimelineAppendOperation<T> || top is TimelineReplaceOperation<T>)
                {
                    poppedSnapshots.Add(top);
                    OperationLog.Pop();
                }
                else if (top is IReversibleOperation<T> rOp)
                {
                    poppedReversibles.Add(rOp);
                    OperationLog.Pop();
                }
                else
                {
                    break;
                }
            }

            // 2) Restore timeline to the snapshot before that forward pass
            foreach (var snap in poppedSnapshots)
                snap.Undo();

            // 3) Replay the reversible ops forward to rebuild the slice
            var value = qb.ToCollapsedValues().First();
            for (int i = poppedReversibles.Count - 1; i >= 0; i--)
                value = poppedReversibles[i].ApplyForward(value);

            var rebuilt = new QuBit<T>(new[] { value });
            rebuilt.Any();

            // 4) Overwrite timeline with the rebuilt slice
            timeline.Clear();
            timeline.Add(rebuilt);
            TimelineAppendedHook?.Invoke();

            // 5) Mark that the “original” slice has now been replaced
            replacedInitialSlice = true;

            // 6) Push everything back onto the log in original order
            foreach (var snap in poppedSnapshots.AsEnumerable().Reverse())
                OperationLog.Record(snap);
            foreach (var rop in poppedReversibles.AsEnumerable().Reverse())
                OperationLog.Record(rop);

            return;
        }

        // --- Forward‐time pass, but we’ve already replaced the first slice ---
        //   Instead of appending a duplicate slice, *replace* the sole slice
        if (runtime.Entropy > 0
            && replacedInitialSlice
            && timeline.Count == 1)
        {
            var snapshot = timeline
                .Select(x => new QuBit<T>(x.ToCollapsedValues().ToArray()))
                .ToList();

            timeline[0] = qb;
            OperationLog.Record(new TimelineReplaceOperation<T>(this, snapshot));
            // no OnTimelineAppended or TimelineAppendedHook, since count didn't grow
            return;
        }

        // --- If globally converged, just merge or overwrite the last slice ---
        if (runtime.Converged)
        {
            var collapsed = qb.ToCollapsedValues();
            if (collapsed.Count() == 1)
            {
                timeline[^1] = qb;
            }
            else
            {
                var merged = timeline[^1].ToCollapsedValues()
                                         .Union(collapsed)
                                         .Distinct()
                                         .ToList();
                timeline[^1] = new QuBit<T>(merged);
            }
            return;
        }

        // --- First genuine forward tick: append alongside the original slice ---
        if (!replacedInitialSlice && timeline.Count == 1)
        {
            var previousTimeline = timeline
                .Select(x => new QuBit<T>(x.ToCollapsedValues().ToArray()))
                .ToList();

            timeline.Add(qb);
            OnTimelineAppended?.Invoke();
            TimelineAppendedHook?.Invoke();
            OperationLog.Record(new TimelineAppendOperation<T>(this, previousTimeline));

            replacedInitialSlice = true;
            return;
        }

        // --- Every subsequent forward tick: normal append ---
        var snapshot2 = timeline
            .Select(x => new QuBit<T>(x.ToCollapsedValues().ToArray()))
            .ToList();

        timeline.Add(qb);
        OnTimelineAppended?.Invoke();
        TimelineAppendedHook?.Invoke();
        OperationLog.Record(new TimelineAppendOperation<T>(this, snapshot2));

        // Check for cycles to trigger unification
        for (int cycle = 2; cycle <= 20; cycle++)
        {
            if (timeline.Count >= cycle + 1
                && SameStates(timeline[^1], timeline[^(cycle + 1)]))
            {
                Unify(cycle);
                break;
            }
        }
    }



    #region region Operator Overloads
    // --- Operator Overloads ---
    // --- Addition Overloads ---
    public static QuBit<T> operator +(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() + right;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            OperationLog.Record(new AdditionOperation<T>(left, right));
        }
        return resultQB;
    }

    public static QuBit<T> operator +(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() + left;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            OperationLog.Record(new AdditionOperation<T>(right, left));
        }
        return resultQB;
    }

    public static QuBit<T> operator +(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() + right.GetCurrentQBit();
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            // Record addition using the collapsed value from the right variable.
            T operand = right.GetCurrentQBit().ToCollapsedValues().First();
            OperationLog.Record(new AdditionOperation<T>(left, operand));
        }
        return resultQB;
    }

    // --- Modulus Overloads ---
    public static QuBit<T> operator %(PositronicVariable<T> left, T right)
    {
        var before = left.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = left.GetCurrentQBit() % right;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
            OperationLog.Record(new ReversibleModulusOp<T>(left, right));
        return resultQB;
    }


    public static QuBit<T> operator %(T left, PositronicVariable<T> right)
    {
        var before = right.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = right.GetCurrentQBit() % left;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
            OperationLog.Record(new ReversibleModulusOp<T>(right, left));
        return resultQB;
    }


    public static QuBit<T> operator %(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var before = left.GetCurrentQBit().ToCollapsedValues().First();
        var divisor = right.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = left.GetCurrentQBit() % right.GetCurrentQBit();
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
            OperationLog.Record(new ReversibleModulusOp<T>(left, divisor));
        return resultQB;
    }

    // --- Subtraction Overloads ---
    public static QuBit<T> operator -(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() - right;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            OperationLog.Record(new SubtractionOperation<T>(left, right));
        }
        return resultQB;
    }

    public static QuBit<T> operator -(T left, PositronicVariable<T> right)
    {
        // Here, result = left - rightValue.
        var resultQB = right.GetCurrentQBit() - left;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            // Use a reversed subtraction so that the inverse is: value = left - result.
            OperationLog.Record(new SubtractionReversedOperation<T>(right, left));
        }
        return resultQB;
    }

    public static QuBit<T> operator -(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() - right.GetCurrentQBit();
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            T operand = right.GetCurrentQBit().ToCollapsedValues().First();
            OperationLog.Record(new SubtractionOperation<T>(left, operand));
        }
        return resultQB;
    }

    // --- Unary Negation ---
    public static QuBit<T> operator -(PositronicVariable<T> value)
    {
        var qb = value.GetCurrentQBit();
        var negatedValues = qb.ToCollapsedValues().Select(v => (T)(-(dynamic)v)).ToArray();
        var negatedQb = new QuBit<T>(negatedValues);
        negatedQb.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            OperationLog.Record(new NegationOperation<T>(value));
        }
        return negatedQb;
    }

    // --- Multiplication Overloads ---
    public static QuBit<T> operator *(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() * right;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            OperationLog.Record(new MultiplicationOperation<T>(left, right));
        }
        return resultQB;
    }

    public static QuBit<T> operator *(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() * left;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            OperationLog.Record(new MultiplicationOperation<T>(right, left));
        }
        return resultQB;
    }

    public static QuBit<T> operator *(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() * right.GetCurrentQBit();
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            T operand = right.GetCurrentQBit().ToCollapsedValues().First();
            OperationLog.Record(new MultiplicationOperation<T>(left, operand));
        }
        return resultQB;
    }

    // --- Division Overloads ---
    public static QuBit<T> operator /(PositronicVariable<T> left, T right)
    {
        var currentQB = left.GetCurrentQBit();
        var resultQB = currentQB / right;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            OperationLog.Record(new DivisionOperation<T>(left, right));
        }
        return resultQB;
    }

    public static QuBit<T> operator /(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() / left;
        resultQB.Any();
        if (PositronicRuntime.Instance.Entropy >= 0)
        {
            OperationLog.Record(new DivisionReversedOperation<T>(right, left));
        }
        return resultQB;
    }

    // --- Comparison Operators Using Comparer<T>.Default ---

    public static bool operator <(PositronicVariable<T> left, T right)
    {
        return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) < 0;
    }
    public static bool operator >(PositronicVariable<T> left, T right)
    {
        return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) > 0;
    }
    public static bool operator <=(PositronicVariable<T> left, T right)
    {
        return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) <= 0;
    }
    public static bool operator >=(PositronicVariable<T> left, T right)
    {
        return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) >= 0;
    }
    public static bool operator <(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return Comparer<T>.Default.Compare(
            left.GetCurrentQBit().ToCollapsedValues().First(),
            right.GetCurrentQBit().ToCollapsedValues().First()) < 0;
    }
    public static bool operator >(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return Comparer<T>.Default.Compare(
            left.GetCurrentQBit().ToCollapsedValues().First(),
            right.GetCurrentQBit().ToCollapsedValues().First()) > 0;
    }
    public static bool operator <=(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return Comparer<T>.Default.Compare(
            left.GetCurrentQBit().ToCollapsedValues().First(),
            right.GetCurrentQBit().ToCollapsedValues().First()) <= 0;
    }
    public static bool operator >=(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        return Comparer<T>.Default.Compare(
            left.GetCurrentQBit().ToCollapsedValues().First(),
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
    #endregion

    public override bool Equals(object obj)
    {
        if (obj is PositronicVariable<T> other)
            return this == other;
        return false;
    }

    public override int GetHashCode()
    {
        return GetCurrentQBit().ToCollapsedValues().Aggregate(0, (acc, x) => acc ^ (x?.GetHashCode() ?? 0));
    }

    /// <summary>
    /// Applies a binary function across two positronic variables, combining every possible future.
    /// </summary>
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
    /// Retrieves a specific slice from the timeline.
    /// </summary>
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
    /// Collapses the timeline to the value from the most recent slice.
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
    /// Collapses the timeline using a custom strategy.
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
    public static Func<IEnumerable<T>, T> CollapseMin = values => values.Min();
    public static Func<IEnumerable<T>, T> CollapseMax = values => values.Max();
    public static Func<IEnumerable<T>, T> CollapseFirst = values => values.First();
    public static Func<IEnumerable<T>, T> CollapseRandom = values =>
    {
        var list = values.ToList();
        var rnd = new Random();
        return list[rnd.Next(list.Count)];
    };

    /// <summary>
    /// Creates a new branch by forking the timeline.
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
    /// Forks the timeline and applies a transformation to the final slice.
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
    /// Returns a pretty-printed version of the timeline.
    /// </summary>
    public string ToTimelineString()
    {
        return string.Join(Environment.NewLine,
            timeline.Select((qb, index) => $"Slice {index}: {qb}"));
    }

    /// <summary>
    /// Serializes the timeline to JSON.
    /// </summary>
    public string ExportToJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(timeline, options);
    }

    /// <summary>
    /// A convenient wrapper to expose the current quantum values.
    /// </summary>
    public PositronicValueWrapper Value => new PositronicValueWrapper(GetCurrentQBit());

    public class PositronicValueWrapper
    {
        private readonly QuBit<T> qb;
        public PositronicValueWrapper(QuBit<T> q) => qb = q;
        public IEnumerable<T> ToValues() => qb.ToCollapsedValues();
    }

    /// <summary>
    /// Collapses the current quantum cloud into discrete values.
    /// </summary>
    public IEnumerable<T> ToValues() => GetCurrentQBit().ToCollapsedValues();

    /// <summary>
    /// Returns the most recent quantum slice.
    /// </summary>
    public QuBit<T> GetCurrentQBit() => timeline[^1];


    /// <summary>
    /// Checks whether two QuBits represent the same state.
    /// </summary>
    private bool SameStates(QuBit<T> a, QuBit<T> b)
    {
        var av = a.ToCollapsedValues().OrderBy(x => x).ToList();
        var bv = b.ToCollapsedValues().OrderBy(x => x).ToList();
        if (av.Count != bv.Count) return false;
        for (int i = 0; i < av.Count; i++)
            if (!Equals(av[i], bv[i]))
                return false;
        return true;
    }

    public override string ToString()
    {
        return GetCurrentQBit().ToString();
    }
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