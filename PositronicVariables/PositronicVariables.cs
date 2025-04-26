using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using QuantumSuperposition.QuantumSoup;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Numerics;
using System.Collections;
using Microsoft.Win32;

namespace QuantumSuperposition.DependencyInjection
{
    public static class PositronicServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Positronic runtime and initializes the static fallback provider.
        /// </summary>
        public static IServiceCollection AddPositronicRuntime(this IServiceCollection services)
        {
            services.AddScoped<ScopedPositronicVariableFactory>();

            // Register the runtime as scoped and rely on constructor injection
            services.AddScoped<IPositronicVariableFactory>(sp => sp.GetRequiredService<ScopedPositronicVariableFactory>());
            services.AddScoped<IPositronicVariableRegistry>(sp => sp.GetRequiredService<ScopedPositronicVariableFactory>());

            services.AddScoped<IPositronicRuntime, DefaultPositronicRuntime>();

            return services;
        }

    }
}

public interface IPositronicVariableFactory
{
    PositronicVariable<T> GetOrCreate<T>(string id, T initialValue);
    PositronicVariable<T> GetOrCreate<T>(string id);
    PositronicVariable<T> GetOrCreate<T>(T initialValue);
}

public class ScopedPositronicVariableFactory : IPositronicVariableFactory, IPositronicVariableRegistry
{
    private readonly IServiceProvider _provider;
    private IPositronicRuntime Runtime => _provider.GetRequiredService<IPositronicRuntime>();

    private readonly Dictionary<(Type, string), IPositronicVariable> _registry
        = new();

    public ScopedPositronicVariableFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public PositronicVariable<T> GetOrCreate<T>(string id, T initialValue)
    {
        var key = (typeof(T), id);
        if (_registry.TryGetValue(key, out var existing))
            return (PositronicVariable<T>)existing;

        var created = new PositronicVariable<T>(initialValue, Runtime);
        _registry[key] = created;
        return created;
    }

    public PositronicVariable<T> GetOrCreate<T>(string id)
    {
        var key = (typeof(T), id);
        if (_registry.TryGetValue(key, out var existing))
            return (PositronicVariable<T>)existing;
        var created = new PositronicVariable<T>(default, Runtime);
        _registry[key] = created;
        return created;
    }

    public PositronicVariable<T> GetOrCreate<T>(T initialValue)
    {
        var key = (typeof(T), "default");
        if (_registry.TryGetValue(key, out var existing))
            return (PositronicVariable<T>)existing;
        var created = new PositronicVariable<T>(initialValue, Runtime);
        _registry[key] = created;
        return created;
    }

    void IPositronicVariableRegistry.Add(IPositronicVariable v)
    {
        // avoid dynamic – grab the T from the concrete PositronicVariable<T>
        var t = v.GetType().GetGenericArguments()[0];
        var key = (t, Guid.NewGuid().ToString());
        _registry[key] = v;
    }

void IPositronicVariableRegistry.Clear()
        => _registry.Clear();

    public IEnumerator<IPositronicVariable> GetEnumerator()
        => _registry.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}



/// <summary>
/// Lightweight, swappable holder for the “current” runtime.
/// Tests can push their own with <c>PositronicAmbient.Current = fake;</c>
/// </summary>
public static class PositronicAmbient
{
    private static readonly AsyncLocal<IPositronicRuntime> _ambient = new();
    public static IPositronicRuntime Current
    {
        get => _ambient.Value
                          ?? throw new InvalidOperationException("Positronic runtime not yet created");
        set => _ambient.Value = value;
    }
}

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
    private readonly IPositronicRuntime _rt;

    public AdditionOperation(PositronicVariable<T> variable, T addend, IPositronicRuntime rt)
    {
        Variable = variable;
        _addend = addend;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();   // snapshot
        _rt = rt;
    }

    public T ApplyInverse(T result) => Arithmetic.Subtract(result, _addend);
    public T ApplyForward(T result) => Arithmetic.Add(result, _addend);

}

// Subtraction: forward op is x - B, so inverse is (result + B).
public class SubtractionOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _subtrahend;
    public T Original { get; }

    public string OperationName => $"Subtraction of {_subtrahend}";
    private readonly IPositronicRuntime _rt;

    public SubtractionOperation(PositronicVariable<T> variable, T subtrahend, IPositronicRuntime rt)
    {
        Variable = variable;
        _subtrahend = subtrahend;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        _rt = rt;
    }

    public T ApplyInverse(T result) => Arithmetic.Add(result, _subtrahend);
    public T ApplyForward(T value) => Arithmetic.Subtract(value, _subtrahend);

}

// SubtractionReversed: for T - x. Forward op is: result = M - x, so the inverse is x = M - result.
public class SubtractionReversedOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _minuend;
    private readonly IPositronicRuntime _rt;
    public T Original { get; }

    public string OperationName => $"SubtractionReversed with minuend {_minuend}";

    public SubtractionReversedOperation(PositronicVariable<T> variable, T minuend, IPositronicRuntime rt)
    {
        Variable = variable;
        _minuend = minuend;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        _rt = rt;
    }

    public T ApplyInverse(T result) => Arithmetic.Subtract(_minuend, result);
    public T ApplyForward(T value) => Arithmetic.Subtract(_minuend, value);

}

// Multiplication: forward op is x * M, so inverse is (result / M).
public class MultiplicationOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _multiplier;
    private readonly IPositronicRuntime _rt;
    public T Original { get; }

    public string OperationName => $"Multiplication by {_multiplier}";

    public MultiplicationOperation(PositronicVariable<T> variable, T multiplier, IPositronicRuntime rt)
    {
        Variable = variable;
        _multiplier = multiplier;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        _rt = rt;
    }

    // use the Arithmetic class rather than dynamic
    public T ApplyInverse(T result) => Arithmetic.Divide(result, _multiplier);
    public T ApplyForward(T value) => Arithmetic.Multiply(value, _multiplier);

}

// Division: forward op is x / D, so inverse is (result * D).
public class DivisionOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _divisor;
    private readonly IPositronicRuntime _rt;
    public T Original { get; }

    public string OperationName => $"Division by {_divisor}";

    public DivisionOperation(PositronicVariable<T> variable, T divisor, IPositronicRuntime rt)
    {
        Variable = variable;
        _divisor = divisor;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        _rt = rt;
    }
    // use the Arithmetic class rather than dynamic
    public T ApplyInverse(T result) => Arithmetic.Multiply(result, _divisor);
    public T ApplyForward(T value) => Arithmetic.Divide(value, _divisor);

}

// DivisionReversed: for T / x. Forward op is: result = N / x, so inverse is x = N / result.
public class DivisionReversedOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _numerator;
    private readonly IPositronicRuntime _rt;
    public T Original { get; }

    public string OperationName => $"DivisionReversed with numerator {_numerator}";

    public DivisionReversedOperation(PositronicVariable<T> variable, T numerator, IPositronicRuntime rt)
    {
        Variable = variable;
        _numerator = numerator;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        _rt = rt;
    }

    // use the Arithmetic class rather than dynamic
    public T ApplyInverse(T result) => Arithmetic.Divide(_numerator, result);
    public T ApplyForward(T value) => Arithmetic.Divide(_numerator, value);

}

// Unary Negation: forward op is -x, and its own inverse.
public class NegationOperation<T> : IReversibleSnapshotOperation<T>
{
    public PositronicVariable<T> Variable { get; }
    public T Original { get; }
    private readonly IPositronicRuntime _rt;

    public string OperationName => "Negation";

    public NegationOperation(PositronicVariable<T> variable, IPositronicRuntime rt)
    {
        Variable = variable;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        _rt = rt;
    }

    // use the Arithmetic class rather than dynamic
    public T ApplyInverse(T result) => Arithmetic.Negate(result);
    public T ApplyForward(T value) => Arithmetic.Negate(value);

}


/// <summary>
///  x % d   — but logged with enough information so the step
///  can be undone later.
/// </summary>
public sealed class ReversibleModulusOp<T> : IReversibleSnapshotOperation<T>
{
    private readonly IPositronicRuntime _runtime;
    public PositronicVariable<T> Variable { get; }
    private readonly T _divisor;
    private readonly T _quotient;     // ⟵  floor(original/divisor)

    public T Original { get; }

    public string OperationName => $"Modulus by {_divisor}";

    public ReversibleModulusOp(PositronicVariable<T> variable, T divisor, IPositronicRuntime runtime)
    {
        Variable  = variable;
        _divisor  = divisor;
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        // snapshot the value *before* the %
        Original  = variable.GetCurrentQBit().ToCollapsedValues().First();
        // user the arithmetic class rather than dynamic
        _quotient = Arithmetic.Divide(Original, divisor);
    }

    // Forward:   r  ⟶  q·d + r  (rebuild the original value)
    public T ApplyForward(T remainder)
       => Arithmetic.Add( Arithmetic.Multiply(_quotient, _divisor), remainder);

    // Inverse:   x  ⟶  x % d   (rarely used by the engine but nice to have)
    public T ApplyInverse(T value)
        => Arithmetic.Modulus(value, _divisor);

    void IOperation.Undo()
    {
        // 1. restore the original slice
        var qb = new QuBit<T>(new[] { Original });
        qb.Any();
        Variable.timeline[^1] = qb;

        // 2. tell the convergence loop we're done
        _runtime.Converged = true;
    }

}


public class InversionOperation<T> : IOperation
{
    private readonly PositronicVariable<T> _variable;
    private readonly T _originalValue;
    private readonly T _invertedValue;
    private readonly IPositronicRuntime _rt;
    public string OperationName { get; }

    public InversionOperation(PositronicVariable<T> variable, T originalValue, T invertedValue, string opName, IPositronicRuntime rt)
    {
        _variable = variable;
        _originalValue = originalValue;
        _invertedValue = invertedValue;
        OperationName = $"Inverse of {opName}";
        _rt = rt;
    }

    public void Undo()
    {
        // To undo an inversion, reassign the original forward value.
        _variable.Assign(_originalValue);
    }
}

public interface IEntropyController
{
    int Entropy { get; }
    void Initialize();         // set initial entropy
    void Flip();               // flip direction
}

public interface IOperationLogHandler<T>
{
    void Record(IOperation op);
    void UndoLastForwardCycle();
    void Clear();
    bool HadForwardAppend { get; set; }
}

public interface IVersioningService<T>
{
    void SnapshotAppend(PositronicVariable<T> variable, QuBit<T> newSlice);
    void RestoreLastSnapshot();
    void RegisterTimelineAppendedHook(Action hook);
    void ReplaceLastSlice(PositronicVariable<T> variable, QuBit<T> mergedSlice);
    void OverwriteBootstrap(PositronicVariable<T> variable, QuBit<T> slice);

}

public class DefaultVersioningService<T> : IVersioningService<T>
{
    private readonly Stack<(PositronicVariable<T> Variable, List<QuBit<T>> Timeline)> _snapshots = new();
    private readonly object _syncRoot = new object();
    private Action _onAppend;

    public void RegisterTimelineAppendedHook(Action hook)
    {
        _onAppend = hook;
        lock (_syncRoot)
            _onAppend = hook;
    }

    public void SnapshotAppend(PositronicVariable<T> variable, QuBit<T> newSlice)
    {
        lock (_syncRoot)
        {
            // 1) snapshot current state
            var copy = variable.timeline
                           .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
                           .ToList();
            _snapshots.Push((variable, copy));

            // 2) append the new qubit
            OperationLog.Record(new TimelineAppendOperation<T>(variable, copy));

            // 3) fire the hook so the convergence engine knows “something changed”
            variable.timeline.Add(newSlice);
            _onAppend?.Invoke();
        }
    }

    public void RestoreLastSnapshot()
    {
        lock (_syncRoot)
        {
            // pop both the variable *and* its saved timeline
            var (variable, oldTimeline) = _snapshots.Pop();
            variable.timeline.Clear();
            variable.timeline.AddRange(oldTimeline);
        }
    }


    public void ReplaceLastSlice(PositronicVariable<T> variable, QuBit<T> mergedSlice)
    {
        lock (_syncRoot)
        {
            var backup = variable.timeline
                                        .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
                                        .ToList();
            OperationLog.Record(new TimelineReplaceOperation<T>(variable, backup));

            variable.timeline[^1] = mergedSlice;
            _onAppend?.Invoke();
        }
    }

    public void OverwriteBootstrap(PositronicVariable<T> variable, QuBit<T> slice)
    {
        lock (_syncRoot)
        {
            variable.timeline.Clear();
            variable.timeline.Add(slice);
            _onAppend?.Invoke();
        }
    }
}


public interface IOutputRedirector
{
    void Redirect();
    void Restore();
}

// --- 2. Implement default services (omitting full bodies) ---
public class DefaultEntropyController : IEntropyController
{
    private readonly IPositronicRuntime _runtime;
    public DefaultEntropyController(IPositronicRuntime runtime)
        => _runtime = runtime;

    public int Entropy => _runtime.Entropy;
    /// <summary>
    /// Start every convergence run in **reverse** time.
    /// </summary>
    public void Initialize() => _runtime.Entropy = -1;
    public void Flip() => _runtime.Entropy = -_runtime.Entropy;
}

public class DefaultOperationLogHandler<T> : IOperationLogHandler<T>
{
    public bool HadForwardAppend { get; set; }
    public void Record(IOperation op) => OperationLog.Record(op);
    public void UndoLastForwardCycle() => OperationLog.ReverseLastOperations();
    public void Clear() => OperationLog.Clear();
}

public class DefaultOutputRedirector : IOutputRedirector
{
    private readonly IPositronicRuntime _runtime;
    private TextWriter _originalOut;

    public DefaultOutputRedirector(IPositronicRuntime runtime)
        => _runtime = runtime;

    public void Redirect()
    {
        _originalOut = Console.Out;
        Console.SetOut(AethericRedirectionGrid.OutputBuffer);
        _runtime.OracularStream = AethericRedirectionGrid.OutputBuffer;
    }
    public void Restore() => Console.SetOut(_originalOut);
}

//public static class PositronicRuntimeProvider
//{
//    private static IPositronicRuntime _instance;

//    public static void Initialize(IPositronicRuntime runtime)
//    {
//        _instance = runtime;
//        _instance.OracularStream = AethericRedirectionGrid.OutputBuffer;
//        _instance.Reset();
//    }

//    public static IPositronicRuntime Instance =>
//        _instance ?? throw new InvalidOperationException(
//            "Positronic runtime not initialized. Call AddPositronicRuntime() on IServiceCollection.");
//}


// --- 3. Convergence engine orchestrating the core loop ---
public class ConvergenceEngine<T>
{
    private readonly IEntropyController _entropy;
    private readonly IOperationLogHandler<T> _ops;
    private readonly IOutputRedirector _redirect;
    private readonly IPositronicRuntime _runtime;
    private readonly int _maxIters = 1000;

    public ConvergenceEngine(
        IEntropyController entropy,
        IOperationLogHandler<T> ops,
        IOutputRedirector redirect,
        IPositronicRuntime runtime)
    {
        _entropy = entropy;
        _ops = ops;
        _redirect = redirect;
        _runtime = runtime;
    }

    public void Run(Action code,
                    bool runFinalIteration = true,
                    bool unifyOnConvergence = true,
                    bool bailOnFirstReverseWhenIdle = false)
    {
        _redirect.Redirect();
        _entropy.Initialize();

        bool hadForwardCycle = false;
        bool skippedFirstForward = false;
        int iteration = 0;

        while (!_runtime.Converged && iteration < _maxIters)
        {
            // clear append-flag at start of each forward half
            if (_entropy.Entropy > 0)
                _ops.HadForwardAppend = false;

            // skip the *very* first forward cycle if requested
            if (!(bailOnFirstReverseWhenIdle
                && _entropy.Entropy > 0
                && !skippedFirstForward))
            {
                code();
            }
            else
            {
                skippedFirstForward = true;
            }

            if (_entropy.Entropy > 0)
                hadForwardCycle = true;

            if (bailOnFirstReverseWhenIdle
                && _entropy.Entropy < 0
                && !_ops.HadForwardAppend)
            {
                _runtime.Converged = false;
                break;
            }

            if (hadForwardCycle && PositronicVariable<T>.AllConverged(_runtime) && _entropy.Entropy < 0)
            {
                if (unifyOnConvergence)
                {
                    foreach( var pv in PositronicVariable<T>.GetAllVariables(_runtime))
                        pv.UnifyAll();
                }

                _runtime.Converged = true;
                break;
            }

            _entropy.Flip();
            // if we just switched into reverse time,
            // trigger a self-assignment on every variable so that
            // ReverseReplayEngine can rebuild the original values
            if (_entropy.Entropy < 0)
                {
                    foreach (var v in PositronicVariable<T>.GetAllVariables(_runtime))
                    ((PositronicVariable<T>)v)
                                .Assign(((PositronicVariable<T>)v).GetCurrentQBit());
                }

            iteration++;
        }

        _redirect.Restore();

        if (runFinalIteration && _runtime.Converged)
        {
            // replay once forward so Console output and side‐effects occur
            _runtime.Entropy = 1;
            _runtime.Converged = false;

            code();

            // undo any reversible ops from that final pass
            _ops.UndoLastForwardCycle();

            _runtime.Converged = true;
        }

        _ops.Clear();
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
        Console.SetOut(_outputBuffer);

        try
        {
            var rt = PositronicAmbient.Current;
            rt.OracularStream = _outputBuffer;
            rt.Reset();
        }
        catch (InvalidOperationException)
        {
            // no runtime yet – defer until DI calls Initialize()
        }

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
            if (PositronicAmbient.Current.Registry.Any())
            {
                // Get the runtime type of the last variable, e.g. PositronicVariable<double>
                var lastVariable = PositronicAmbient.Current.Registry.Last();
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
    public PositronicVariable<T> Variable => _variable;


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

    public PositronicVariable<T> Variable => _variable;

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

public class ReverseReplayEngine<T>
{
    private readonly IOperationLogHandler<T> _ops;
    private readonly IVersioningService<T> _versioningService;

    public ReverseReplayEngine(IOperationLogHandler<T> ops, IVersioningService<T> versioningService)
    {
        _ops = ops;
        _versioningService = versioningService;
    }

    /// <summary>
    /// Peel off the last forward half‐cycle, rebuild every possible state,
    /// and return a single QuBit<T> that contains the union.
    /// </summary>
    public QuBit<T> ReplayReverseCycle(QuBit<T> incoming, PositronicVariable<T> variable)
    {
        // 1) Pop the whole forward half-cycle off the global OperationLog
        var poppedSnapshots = new List<IOperation>();
        var poppedReversibles = new List<IReversibleOperation<T>>();
        var forwardValues = new HashSet<T>();

        while (true)
        {
            var top = OperationLog.Peek();
            switch (top)
            {
                case TimelineAppendOperation<T> tap:
                    forwardValues.UnionWith(tap.Variable
                                                                    .GetCurrentQBit()
                                                                    .ToCollapsedValues());
                    poppedSnapshots.Add(tap);
                    OperationLog.Pop();
                    continue;

                case TimelineReplaceOperation<T> trp:
                    forwardValues.UnionWith(trp.Variable
                                                                    .GetCurrentQBit()
                                                                    .ToCollapsedValues());
                    poppedSnapshots.Add(trp);
                    OperationLog.Pop();
                    continue;

                case IReversibleOperation<T> rop:
                    poppedReversibles.Add(rop);
                    OperationLog.Pop();
                    continue;

                default:
                    break;          // nothing left to peel
            }
            break;
        }

        // 2) Rewind the timeline back to the state that preceded
        //    the whole forward half-cycle **only for operations that
        //    actually captured a snapshot** (i.e. TimelineAppendOperation).
        foreach (var op in poppedSnapshots.OfType<TimelineAppendOperation<T>>())
            _versioningService.RestoreLastSnapshot();



        // 3) Rebuild: for each seed (old forwards ∪ incoming), replay all reversibles
        var seeds = forwardValues.Union(incoming.ToCollapsedValues()).Distinct();
        var rebuiltSet = new HashSet<T>();
        bool hadSnapshots = poppedSnapshots.OfType<TimelineAppendOperation<T>>().Any();

        foreach (var seed in seeds)
        {
            var v = seed;
            if (hadSnapshots)
                for (int i = poppedReversibles.Count - 1; i >= 0; i--)
                    v = poppedReversibles[i].ApplyForward(v);

            rebuiltSet.Add(v);
        }

        // 4) Package into one QuBit<T> and return
        var rebuilt = new QuBit<T>(rebuiltSet.ToArray());
        rebuilt.Any();
        return rebuilt;
    }
}



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
    IPositronicVariableRegistry Variables { get; }

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

    // for consumers who need to *create* new variables:
    IPositronicVariableFactory Factory { get; }

    // for code that needs to *enumerate* or *clear* the registry:
    IPositronicVariableRegistry Registry { get; }
}

/// <summary>
/// Default implementation of IPositronicRuntime using the original static behavior.
/// </summary>
public class DefaultPositronicRuntime : IPositronicRuntime
{
    public int Entropy { get; set; } = 1;
    public bool Converged { get; set; } = false;
    public TextWriter OracularStream { get; set; }
    public IPositronicVariableRegistry Variables { get; }
    public int TotalConvergenceIterations { get; set; } = 0;


    public event Action OnAllConverged;
    public IPositronicVariableFactory Factory { get; }
    public IPositronicVariableRegistry Registry { get; }


    public DefaultPositronicRuntime(IPositronicVariableFactory factory, IPositronicVariableRegistry registry)
    {
        OracularStream = AethericRedirectionGrid.OutputBuffer;

        // Create a minimal fake IServiceProvider
        var provider = new FallbackServiceProvider(this);
        var scoped = new ScopedPositronicVariableFactory(provider);

        Factory = scoped;
        Variables = scoped;
        Registry = scoped;

        Reset();
        PositronicAmbient.Current = this;
    }

    private class FallbackServiceProvider : IServiceProvider
    {
        private readonly IPositronicRuntime _runtime;

        public FallbackServiceProvider(IPositronicRuntime runtime)
        {
            _runtime = runtime;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IPositronicRuntime))
                return _runtime;
            throw new InvalidOperationException($"Service {serviceType.Name} not available in fallback provider.");
        }
    }

    public void Reset()
    {
        Entropy = 1;
        Converged = false;
        OracularStream = AethericRedirectionGrid.OutputBuffer;
        Registry.Clear();
        TotalConvergenceIterations = 0;
    }

    // Helper to invoke the global convergence event.
    public void FireAllConverged() => OnAllConverged?.Invoke();
}

#endregion

public interface IPositronicVariableRegistry : IEnumerable<IPositronicVariable>
{
    void Add(IPositronicVariable variable);
    void Clear();
}



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
    internal static int _loopDepth;
    private readonly IPositronicRuntime _runtime;
    private bool _hasWrittenInitialForward = false;

    internal static bool InConvergenceLoop => _loopDepth > 0;
    private readonly ReverseReplayEngine<T> _reverseReplay;
    private readonly IOperationLogHandler<T> _ops;

    private readonly IVersioningService<T> _versioningService;


    public PositronicVariable(
        T initialValue,
        IPositronicRuntime runtime,
        IVersioningService<T> versioningService = null,
        IOperationLogHandler<T> opsHandler = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        // seed the timeline exactly once
        _versioningService = versioningService ?? new DefaultVersioningService<T>();
        var qb = new QuBit<T>(new[] { initialValue });
        qb.Any();
        _versioningService.OverwriteBootstrap(this, qb);
        _hasWrittenInitialForward = true;

        _domain.Add(initialValue);
        _runtime.Registry.Add(this);

        // wire up the rest of our services

        _ops = opsHandler ?? new DefaultOperationLogHandler<T>();
        _reverseReplay = new ReverseReplayEngine<T>(_ops, _versioningService);

        // register the “something appended” hook (now thread-safe)
        _versioningService.RegisterTimelineAppendedHook(() => OnTimelineAppended?.Invoke());
    }

    static PositronicVariable()
    {
        // Do the old IComparable guard
        if (!(typeof(IComparable).IsAssignableFrom(typeof(T)) ||
              typeof(IComparable<T>).IsAssignableFrom(typeof(T))))
        {
            throw new InvalidOperationException(
                $"Type parameter '{typeof(T).Name}' for PositronicVariable " +
                "must implement IComparable or IComparable<T> to enable proper convergence checking.");
        }

        // Trigger the neuro-cascade init, etc.
        var _ = AethericRedirectionGrid.Initialized;
    }

    private void Remember(IEnumerable<T> xs)
     {
         foreach (var x in xs) _domain.Add(x);
     }

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



    // (removed) static registry in favor of instance‐based IPositronicVariableRegistry
    //private static Dictionary<string, PositronicVariable<T>> registry = new Dictionary<string, PositronicVariable<T>>();

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

    

    // removed
    ///// <summary>
    ///// Resets the registry and the global runtime.
    ///// </summary>
    //public static void ResetStaticVariables()
    //{
    //    registry.Clear();
    //    var rt = new DefaultPositronicRuntime();
    //    PositronicAmbient.Current = rt;
    //    rt.Reset();
    //    OperationLog.Clear();
    //}

    public static void SetEntropy(IPositronicRuntime rt, int e) => rt.Entropy = e;
    public static int GetEntropy(IPositronicRuntime rt) => rt.Entropy;

    ///// <summary>
    ///// Returns an existing instance or creates a new PositronicVariable with the given id and initial value.
    ///// </summary>
    //public static PositronicVariable<T> GetOrCreate(string id, T initialValue, IPositronicRuntime runtime)
    //{
    //    if (registry.TryGetValue(id, out var instance))
    //    {
    //        return instance;
    //    }
    //    else
    //    {
    //        instance = new PositronicVariable<T>(initialValue, runtime);
    //        registry[id] = instance;
    //        return instance;
    //    }
    //}

    public static PositronicVariable<T> GetOrCreate(string id, T initialValue, IPositronicRuntime runtime)
        => runtime.Factory.GetOrCreate<T>(id, initialValue);

    ///// <summary>
    ///// Returns an existing instance or creates a new PositronicVariable with the given id.
    ///// </summary>
    ///// <param name="id"></param>
    ///// <returns></returns>
    //public static PositronicVariable<T> GetOrCreate(string id, IPositronicRuntime runtime)
    //{
    //    if (registry.TryGetValue(id, out var instance))
    //    {
    //        return instance;
    //    }
    //    else
    //    {
    //        instance = new PositronicVariable<T>(default(T), runtime);
    //        registry[id] = instance;
    //        return instance;
    //    }
    //}

    public static PositronicVariable<T> GetOrCreate(string id, IPositronicRuntime runtime)
        => runtime.Factory.GetOrCreate<T>(id);

    ///// <summary>
    ///// Returns or creates the default PositronicVariable with the initial value.
    ///// </summary>
    //public static PositronicVariable<T> GetOrCreate(T initialValue, IPositronicRuntime runtime)
    //{
    //    if (registry.TryGetValue("default", out var instance))
    //    {
    //        return instance;
    //    }
    //    else
    //    {
    //        instance = new PositronicVariable<T>(initialValue, runtime);
    //        registry["default"] = instance;
    //        return instance;
    //    }
    //}
    public static PositronicVariable<T> GetOrCreate(T initialValue, IPositronicRuntime runtime)
        => runtime.Factory.GetOrCreate<T>(initialValue);

    ///// <summary>
    ///// Returns the or creates the default PositronicVariable given no value or id.
    ///// </summary>
    //public static PositronicVariable<T> GetOrCreate(IPositronicRuntime runtime)
    //{
    //    if (registry.TryGetValue("default", out var instance))
    //    {
    //        return instance;
    //    }
    //    else
    //    {
    //        instance = new PositronicVariable<T>(default(T), runtime);
    //        registry["default"] = instance;
    //        return instance;
    //    }
    //}

    // maybe lose idless creation? default for now
    public static PositronicVariable<T> GetOrCreate(IPositronicRuntime runtime)
            => runtime.Factory.GetOrCreate<T>("default");

    /// <summary>
    /// Returns true if all registered positronic variables have converged.
    /// </summary>
    public static bool AllConverged(IPositronicRuntime rt)
    {
        bool all = rt.Registry.All(v => v.Converged() > 0);
        if (all)
            (rt as DefaultPositronicRuntime)?.FireAllConverged();
        return all;
    }

    public static IEnumerable<IPositronicVariable> GetAllVariables(IPositronicRuntime rt) => rt.Registry;

    /// <summary>
    /// Runs your code in a convergence loop until all variables have settled.
    /// </summary>
    public static void RunConvergenceLoop(
        IPositronicRuntime rt,
        Action code,
        bool runFinalIteration = true,
        bool unifyOnConvergence = true,
        bool bailOnFirstReverseWhenIdle = false)
    {
        var runtime = rt;
        var entropy = new DefaultEntropyController(runtime);
        var opsHandler = new DefaultOperationLogHandler<T>();
        var redirect = new DefaultOutputRedirector(runtime);

        var engine = new ConvergenceEngine<T>(
            entropy,
            opsHandler,
            redirect,
            runtime);

        try
        {
            // ‼️ mark “we’re inside the loop” so the fast-path is disabled
            _loopDepth++;
            engine.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
        }
        finally
        {
            _loopDepth--;
        }

    }

    /// <summary>
    /// Checks for convergence by comparing timeline slices.
    /// </summary>
    public int Converged()
    {
        if (_runtime.Converged)
            return 1;

        // one slice means "done" only after we have *ever* executed a
        // forward <-> reverse pair (i.e. after we replaced the initial slice)
        if (timeline.Count == 1)
            return replacedInitialSlice ? 1 : 0;

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

        var vals = timeline                        // union of the *current* slices
                               .SelectMany(qb => qb.ToCollapsedValues())
                               .Distinct()
                               .ToList();

        // refresh the domain to match what we really kept
        _domain.Clear();
        foreach (var v in vals) _domain.Add(v);


        // build one canonical disjunction that still shows all possibilities
        var unified = new QuBit<T>(vals);

        unified.Any();

        timeline.Clear();
        timeline.Add(unified);

        OperationLog.Clear();

        _runtime.Converged = true;
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
        _runtime.Converged = true;
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
        var runtime = _runtime;

        // fast path
        if (runtime.Entropy > 0 && !InConvergenceLoop)
        {
            // ignore exact duplicates
            if (SameStates(qb, timeline[^1]))
                return;

            // first ever forward-only write: *replace* the bootstrap slice
            if (!_hasWrittenInitialForward)
            {
                _versioningService.OverwriteBootstrap(this,qb);
                _hasWrittenInitialForward = true;
                return;
            }

            // subsequent writes in the same forward-only context:
            //   – always **merge**,
            //   – but put the newest value first so iterative algorithms
            //     that read .First() see the fresh value.
            var merged = qb.ToCollapsedValues()                 // newest first
                               .Concat(timeline[^1].ToCollapsedValues())
                               .Distinct()
                               .ToList();
            _versioningService.OverwriteBootstrap(this, qb);
            return;
        }

        // If we’ve already converged, do nothing.
        if (_runtime.Converged)
            return;

        Remember(qb.ToCollapsedValues());

        // --- Reverse‐time pass (Entropy < 0) -----------------------------
        if (_runtime.Entropy < 0)
        {
            var rebuilt = _reverseReplay.ReplayReverseCycle(qb, this);
            if (timeline.Count == 0 || !SameStates(rebuilt, timeline[^1]))
            {
                timeline.Add(rebuilt);
                OnTimelineAppended?.Invoke();
            }
            return;
        }

        // --- Forward‐time pass, but we’ve already replaced the first slice ---
        //   Instead of appending a duplicate slice, *replace* the sole slice
        if (_runtime.Entropy > 0
            && replacedInitialSlice
            && timeline.Count == 1)
        {
            if (!InConvergenceLoop)                       // ← we’re outside the loop
            {
                // we are in “straight-forward, single-thread” land:
                // overwrite the slicence instead of union-merging so that iterative
                // algorithms (e.g. babylon sqrt) see the newest scalar value.
                _versioningService.OverwriteBootstrap(this, qb);
                return;
            }

            bool alreadyAppendedThisHalfCycle =
                    OperationLog.Peek() is TimelineAppendOperation<T> a
                    && ReferenceEquals(a.Variable, this);

            if (alreadyAppendedThisHalfCycle)
            {
                /* — replace, _but_ preserve everything that was already in there — */


                var merged = timeline[^1].ToCollapsedValues()
                                        .Union(qb.ToCollapsedValues())
                                        .Distinct()
                                        .ToList();

                _versioningService.ReplaceLastSlice(this, new QuBit<T>(merged));
                return;
            }

            /* --- this is the *first* write of the half-cycle → normal append --- */
            if (!SameStates(qb, timeline[^1]))          // ← ignore exact duplicates
            {
                _versioningService.SnapshotAppend(this, qb);
                _ops.HadForwardAppend = true;
            }
            return;
        }

        // --- If globally converged, just merge or overwrite the last slice ---
        if (runtime.Converged)
        {
            var collapsed = qb.ToCollapsedValues();
            if (collapsed.Count() == 1)
            {
                _versioningService.OverwriteBootstrap(this, qb);
            }
            else
            {
                var merged = timeline[^1].ToCollapsedValues()
                                         .Union(collapsed)
                                         .Distinct()
                                         .ToList();
                _versioningService.ReplaceLastSlice(this, new QuBit<T>(merged));
            }
            return;
        }

        // --- First genuine forward tick: append alongside the original slice ---
        if (!replacedInitialSlice && timeline.Count == 1)
        {
            // fully delegate snapshot + append (including hooks) to the service
            _versioningService.SnapshotAppend(this, qb);
            _ops.HadForwardAppend = true;
            replacedInitialSlice = true;
            return;
        }

        // --- Every subsequent forward tick: normal append ---
        if (!_ops.HadForwardAppend && !SameStates(qb, timeline[^1])) // skip dups
        {
            _versioningService.SnapshotAppend(this, qb);
            _ops.HadForwardAppend = true;
        }
        else
        {
            // “replace” semantics: merge into the last slice, record a replace-op
            var backup = timeline
                .Select(s => new QuBit<T>(s.ToCollapsedValues().ToArray()))
                .ToList();

            var merged = timeline[^1].ToCollapsedValues()
                             .Union(qb.ToCollapsedValues())
                             .Distinct()
                             .ToList();

            _versioningService.ReplaceLastSlice(this, new QuBit<T>(merged));
        }

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
        if (left._runtime.Entropy >= 0)
        {
            OperationLog.Record(new AdditionOperation<T>(left, right, left._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator +(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() + left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
        {
            OperationLog.Record(new AdditionOperation<T>(right, left, right._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator +(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() + right.GetCurrentQBit();
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            // Record addition using the collapsed value from the right variable.
            T operand = right.GetCurrentQBit().ToCollapsedValues().First();
            OperationLog.Record(new AdditionOperation<T>(left, operand, left._runtime));
        }
        return resultQB;
    }

    // --- Modulus Overloads ---
    public static QuBit<T> operator %(PositronicVariable<T> left, T right)
    {
        var before = left.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = left.GetCurrentQBit() % right;
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
            OperationLog.Record(new ReversibleModulusOp<T>(left, right, left._runtime));
        return resultQB;
    }


    public static QuBit<T> operator %(T left, PositronicVariable<T> right)
    {
        var before = right.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = right.GetCurrentQBit() % left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
            OperationLog.Record(new ReversibleModulusOp<T>(right, left, right._runtime));
        return resultQB;
    }


    public static QuBit<T> operator %(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var before = left.GetCurrentQBit().ToCollapsedValues().First();
        var divisor = right.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = left.GetCurrentQBit() % right.GetCurrentQBit();
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
            OperationLog.Record(new ReversibleModulusOp<T>(left, divisor, left._runtime));
        return resultQB;
    }

    // --- Subtraction Overloads ---
    public static QuBit<T> operator -(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() - right;
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            OperationLog.Record(new SubtractionOperation<T>(left, right, left._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator -(T left, PositronicVariable<T> right)
    {
        // Here, result = left - rightValue.
        var resultQB = right.GetCurrentQBit() - left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
        {
            // Use a reversed subtraction so that the inverse is: value = left - result.
            OperationLog.Record(new SubtractionReversedOperation<T>(right, left, right._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator -(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() - right.GetCurrentQBit();
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            T operand = right.GetCurrentQBit().ToCollapsedValues().First();
            OperationLog.Record(new SubtractionOperation<T>(left, operand, left._runtime));
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
        if (value._runtime.Entropy >= 0)
        {
            OperationLog.Record(new NegationOperation<T>(value, value._runtime));
        }
        return negatedQb;
    }

    // --- Multiplication Overloads ---
    public static QuBit<T> operator *(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() * right;
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            OperationLog.Record(new MultiplicationOperation<T>(left, right, left._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator *(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() * left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
        {
            OperationLog.Record(new MultiplicationOperation<T>(right, left, right._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator *(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() * right.GetCurrentQBit();
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            T operand = right.GetCurrentQBit().ToCollapsedValues().First();
            OperationLog.Record(new MultiplicationOperation<T>(left, operand, left._runtime));
        }
        return resultQB;
    }

    // --- Division Overloads ---
    public static QuBit<T> operator /(PositronicVariable<T> left, T right)
    {
        var currentQB = left.GetCurrentQBit();
        var resultQB = currentQB / right;
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            OperationLog.Record(new DivisionOperation<T>(left, right, left._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator /(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() / left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
        {
            OperationLog.Record(new DivisionReversedOperation<T>(right, left, right._runtime));
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
    public static PositronicVariable<T> Apply(Func<T, T, T> op, PositronicVariable<T> left, PositronicVariable<T> right, IPositronicRuntime runtime)
    {
        var leftValues = left.ToValues();
        var rightValues = right.ToValues();
        var results = leftValues.SelectMany(l => rightValues, (l, r) => op(l, r)).Distinct().ToArray();
        var newQB = new QuBit<T>(results);
        newQB.Any();
        return new PositronicVariable<T>(newQB, runtime);
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
    public PositronicVariable<T> Fork(IPositronicRuntime runtime)
    {
        var forkedTimeline = timeline.Select(qb =>
        {
            var newQB = new QuBit<T>(qb.ToCollapsedValues().ToArray());
            newQB.Any();
            return newQB;
        }).ToList();

        var forked = new PositronicVariable<T>(forkedTimeline[0], runtime);
        forked.timeline.Clear();
        forkedTimeline.ForEach(qb => forked.timeline.Add(qb));
        return forked;
    }

    /// <summary>
    /// Forks the timeline and applies a transformation to the final slice.
    /// </summary>
    public PositronicVariable<T> Fork(Func<T, T> transform, IPositronicRuntime runtime)
    {
        var forked = Fork(runtime);
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
    public NeuralNodule(Func<IEnumerable<T>, QuBit<T>> activation, IPositronicRuntime runtime)
    {
        ActivationFunction = activation;
        Output = new PositronicVariable<T>(default(T), runtime);
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
    public static void ConvergeNetwork(IPositronicRuntime runtime, params NeuralNodule<T>[] nodes)
    {
        PositronicVariable<T>.RunConvergenceLoop(runtime, () =>
        {
            foreach (var node in nodes)
                node.Fire();
        });
    }
}
#endregion


public static class Arithmetic
{
    // GENERIC, zero-overhead path when T : INumber<T>
    public static T Add<T>(T x, T y)
        where T : INumber<T>
        => x + y;

    // FALLBACK for *any* other T
    public static dynamic Add(dynamic x, dynamic y)
        => x + y;



    public static T Subtract<T>(T x, T y)
        where T : ISubtractionOperators<T, T, T>
        => x - y;
    public static dynamic Subtract(dynamic x, dynamic y)
        => x - y;



    public static T Multiply<T>(T x, T y)
        where T : IMultiplyOperators<T, T, T>
        => x * y;
    public static dynamic Multiply(dynamic x, dynamic y)
        => x * y;



    public static T Divide<T>(T x, T y)
        where T : IDivisionOperators<T, T, T>
        => x / y;
    public static dynamic Divide(dynamic x, dynamic y)
        => x / y;



    public static T Remainder<T>(T x, T y)
        where T : IModulusOperators<T, T, T>
        => x % y;
    public static dynamic Remainder(dynamic x, dynamic y)
        => x % y;



    public static T Negate<T>(T x)
        where T : IUnaryNegationOperators<T, T>
        => -x;
    public static dynamic Negate(dynamic x)
        => -x;

    public static T Modulus<T>(T x, T y)
        where T : IModulusOperators<T, T, T>
        => x % y;

    public static dynamic Modulus(dynamic x, dynamic y)
        => x % y;
}
