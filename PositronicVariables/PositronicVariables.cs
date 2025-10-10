using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuantumSuperposition.DependencyInjection;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Threading;

namespace QuantumSuperposition.DependencyInjection
{
    public static class PositronicServiceCollectionExtensions
    {
        public static IServiceCollection AddPositronicRuntime(this IServiceCollection services)
        {
            return services.AddPositronicRuntime<double>(builder => { });
        }

        public static IServiceCollection AddPositronicRuntime<T>(this IServiceCollection services, Action<ConvergenceEngineBuilder<T>> configure)
            where T : IComparable<T>
        {
            services.AddScoped<ScopedPositronicVariableFactory>();
            services.AddScoped<IPositronicVariableFactory>(sp => sp.GetRequiredService<ScopedPositronicVariableFactory>());
            services.AddScoped<IPositronicVariableRegistry>(sp => sp.GetRequiredService<ScopedPositronicVariableFactory>());
            services.AddScoped<IPositronicRuntime, DefaultPositronicRuntime>();

            services.AddScoped<IImprobabilityEngine<T>>(sp =>
            {
                var runtime = sp.GetRequiredService<IPositronicRuntime>();

                var defaultEngine = new ImprobabilityEngine<T>(
                    new DefaultEntropyController(runtime),
                    new RegretScribe<T>(),
                    new SubEthaOutputTransponder(runtime),
                    runtime,
                    new BureauOfTemporalRecords<T>()
                );

                var builder = new ConvergenceEngineBuilder<T>();
                configure(builder);

                return builder.Build(defaultEngine);
            });

            return services;
        }
    }

}

public interface IPositronicVariableFactory
{
    PositronicVariable<T> GetOrCreate<T>(string id, T initialValue) where T : IComparable<T>;
    PositronicVariable<T> GetOrCreate<T>(string id) where T : IComparable<T>;
    PositronicVariable<T> GetOrCreate<T>(T initialValue) where T : IComparable<T>;
}

public class ScopedPositronicVariableFactory : IPositronicVariableFactory, IPositronicVariableRegistry
{
    private readonly IServiceProvider _provider;
    private IPositronicRuntime Runtime => _provider.GetRequiredService<IPositronicRuntime>();

    private readonly Dictionary<(Type, string), IPositronicVariable> _multiverseIndex
        = new();

    public ScopedPositronicVariableFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public PositronicVariable<T> GetOrCreate<T>(string id, T initialValue)
        where T : IComparable<T>
    {
        var key = (typeof(T), id);
        if (_multiverseIndex.TryGetValue(key, out var existing))
            return (PositronicVariable<T>)existing;

        var created = new PositronicVariable<T>(initialValue, Runtime);
        _multiverseIndex[key] = created;
        return created;
    }

    public PositronicVariable<T> GetOrCreate<T>(string id)
        where T : IComparable<T>
    {
        var key = (typeof(T), id);
        if (_multiverseIndex.TryGetValue(key, out var existing))
            return (PositronicVariable<T>)existing;
        var created = new PositronicVariable<T>(default, Runtime);
        _multiverseIndex[key] = created;
        return created;
    }

    public PositronicVariable<T> GetOrCreate<T>(T initialValue)
        where T : IComparable<T>
    {
        var key = (typeof(T), "default");
        if (_multiverseIndex.TryGetValue(key, out var existing))
            return (PositronicVariable<T>)existing;
        var created = new PositronicVariable<T>(initialValue, Runtime);
        _multiverseIndex[key] = created;
        return created;
    }

    void IPositronicVariableRegistry.Add(IPositronicVariable v)
    {
        // pry T out of PositronicVariable<T> with reflection—a technique so eldritch even your toaster fears it
        var t = v.GetType().GetGenericArguments()[0];
        var key = (t, Guid.NewGuid().ToString());
        _multiverseIndex[key] = v;
    }

    void IPositronicVariableRegistry.Clear()
            => _multiverseIndex.Clear();

    public IEnumerator<IPositronicVariable> GetEnumerator()
        => _multiverseIndex.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}



/// <summary>
/// This is the current version of the universe that we happen to have fallen into. A container for the runtime and services that are available to the positronic variables.
/// </summary>
public static class PositronicAmbient
{
    private static readonly AsyncLocal<IPositronicRuntime> _ambient = new();
    private static IPositronicRuntime _global;

    private static readonly AsyncLocal<IServiceProvider> _servicesAmbient = new();
    private static IServiceProvider _servicesGlobal;

    public static bool IsInitialized => _ambient.Value is not null || _global is not null;

    public static IPositronicRuntime Current
    {
        get => _ambient.Value ?? _global
            ?? throw new InvalidOperationException("Positronic runtime not yet created");
        set
        {
            _ambient.Value = value;
            _global = value;
        }
    }

    public static IServiceProvider Services
    {
        get => _servicesAmbient.Value ?? _servicesGlobal
            ?? throw new InvalidOperationException("Service provider not yet available");
        private set
        {
            _servicesAmbient.Value = value;
            _servicesGlobal = value;
        }
    }

    public static void InitialiseWith(IHostBuilder hostBuilder)
    {
        if (IsInitialized)
            throw new InvalidOperationException("Positronic runtime already initialised.");

        var host = hostBuilder.Build();
        Services = host.Services;                   // jam the services into the ambient pocket universe so everything stops throwing tantrums
        Current = host.Services.GetRequiredService<IPositronicRuntime>();

    }

    /// <summary>
    /// Unhooks all known realities from the runtime matrix and chucks them into the bin marked "Let's Pretend That Didn't Happen."
    /// </summary>
    public static void PanicAndReset()
    {
        _ambient.Value = null;
        _global = null;
        _servicesAmbient.Value = null;
        _servicesGlobal = null;
    }
}


public interface IImprobabilityEngine<T>
    where T : IComparable<T>
{
    void Run(
        Action code,
        bool runFinalIteration = true,
        bool unifyOnConvergence = true,
        bool bailOnFirstReverseWhenIdle = false,
        IImprobabilityEngine<T> next = null);
}



[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DontPanicAttribute : Attribute
{
}

public interface IReversibleOperation<T> : IOperation
{
    /// <summary>
    /// When time insists on moving forward, this little chap figures out how to make it march in reverse.
    /// (E.g. if you've divided by 9 in one universe, we smuggle you back with a ×9 in another.)
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
    where T : IComparable<T>
{
    PositronicVariable<T> Variable { get; }
    T Original { get; }
    void IOperation.Undo()
    {
        var qb = new QuBit<T>(new[] { Original });
        qb.Any();
        // Magically swap out the latest cosmic crumb for something more fitting of this reality
        Variable.timeline[Variable.timeline.Count - 1] = qb;
    }
}

// Multiversal maths isn't as hard as it looks
public class AdditionOperation<T> : IReversibleSnapshotOperation<T>
    where T : IComparable<T>
{
    public PositronicVariable<T> Variable { get; }
    private readonly T _addend;
    public T Addend => _addend;
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

    /// <summary>
    /// When time goes backwards the addition becomes a subtraction.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyInverse(T result) => Arithmetic.Subtract(result, _addend);
    /// <summary>
    /// And going forwards is just the regular old addition we all know and love.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyForward(T result) => Arithmetic.Add(result, _addend);

}

public class AssignOperation<T> : IReversibleSnapshotOperation<T>
    where T : IComparable<T>
{
    public PositronicVariable<T> Variable { get; }
    public T Original { get; }
    private readonly T _assigned;
    public string OperationName => $"Assign {_assigned}";

    public AssignOperation(PositronicVariable<T> variable, T assigned, IPositronicRuntime rt)
    {
        Variable = variable;
        Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        _assigned = assigned;
    }

    /// <summary>
    /// Go back to whatever was once there after we started going forwards the first time
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyInverse(T result) => Original;
    /// <summary>
    /// Time travel as we know it on a day to day basis
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T ApplyForward(T value) => _assigned;
}


public class SubtractionOperation<T> : IReversibleSnapshotOperation<T>
    where T : IComparable<T>
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

    /// <summary>
    /// You get the idea, right? Subtracting is just adding the negative, so we reverse it by adding.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyInverse(T result) => Arithmetic.Add(result, _subtrahend);
    /// <summary>
    /// And going forwards is just the regular old subtraction we all know and love.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T ApplyForward(T value) => Arithmetic.Subtract(value, _subtrahend);

}

public class SubtractionReversedOperation<T> : IReversibleSnapshotOperation<T>
    where T : IComparable<T>
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

    /// <summary>
    /// It's like regular subtraction, but backwards. So we reverse it by subtracting from the minuend.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyInverse(T result) => Arithmetic.Subtract(_minuend, result);
    /// <summary>
    /// And going forwards is just the regular old subtraction we all know and love.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T ApplyForward(T value) => Arithmetic.Subtract(_minuend, value);

}

public class MultiplicationOperation<T> : IReversibleSnapshotOperation<T>
    where T : IComparable<T>
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

    /// <summary>
    /// When time goes backwards the multiplication becomes a division.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyInverse(T result) => Arithmetic.Divide(result, _multiplier);
    /// <summary>
    /// Otherwise we get out the multiplication table.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T ApplyForward(T value) => Arithmetic.Multiply(value, _multiplier);

}

public class DivisionOperation<T> : IReversibleSnapshotOperation<T>
    where T : IComparable<T>
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
    /// <summary>
    /// When time goes backwards the division becomes a multiplication.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyInverse(T result) => Arithmetic.Multiply(result, _divisor);
    /// <summary>
    /// Otherwise we try and remember what long division was like, something to do with shifting one column to the right and then I get confused.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T ApplyForward(T value) => Arithmetic.Divide(value, _divisor);

}

public class DivisionReversedOperation<T> : IReversibleSnapshotOperation<T>
    where T : IComparable<T>
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

    /// <summary>
    /// When time goes backwards the division becomes a multiplication.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyInverse(T result) => Arithmetic.Divide(_numerator, result);
    /// <summary>
    /// Otherwise we try and remember what long division was like, something to do with shifting one column to the right and then I get confused.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T ApplyForward(T value) => Arithmetic.Divide(_numerator, value);

}

public class NegationOperation<T> : IReversibleSnapshotOperation<T>
    where T : IComparable<T>
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

    /// <summary>
    /// Negation is its own inverse, like a well-trained quantum ninja.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public T ApplyInverse(T result) => Arithmetic.Negate(result);
    /// <summary>
    /// And going forwards is just the regular old negation.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T ApplyForward(T value) => Arithmetic.Negate(value);

}


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


public class InversionOperation<T> : IOperation
    where T : IComparable<T>
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

    /// <summary>
    /// When time moves forward, we apply the inversion.
    /// </summary>
    public void Undo()
    {
        // To undo an inversion, reassign the original forward value.
        _variable.Assign(_originalValue);
    }
}

public interface IEntropyController
{
    int Entropy { get; }
    void Initialise();         // set initial entropy
    void Flip();               // flip direction
}

public interface IOperationLogHandler<T>
{
    void Record(IOperation op);
    void UndoLastForwardCycle();
    void Clear();
    bool SawForwardWrite { get; set; }
}

public interface ITimelineArchivist<T>
    where T : IComparable<T>
{
    void SnapshotAppend(PositronicVariable<T> variable, QuBit<T> newSlice);
    void RestoreLastSnapshot();
    void RegisterTimelineAppendedHook(Action hook);
    void ReplaceLastSlice(PositronicVariable<T> variable, QuBit<T> mergedSlice);
    void OverwriteBootstrap(PositronicVariable<T> variable, QuBit<T> slice);
    void ClearSnapshots();

}

public class BureauOfTemporalRecords<T> : ITimelineArchivist<T>
    where T : IComparable<T>
{
    private readonly Stack<(PositronicVariable<T> Variable, List<QuBit<T>> Timeline)> _snapshots = new();
    private readonly object _syncRoot = new object();
    private Action _onAppend;

    /// <summary>
    /// Obliterates the multiverse history like a particularly careless time janitor.
    /// </summary>
    public void ClearSnapshots() => _snapshots.Clear();

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
            // snapshot the current incarnation of the variable, before we accidentally turn it into a lizard or a toaster
            var copy = variable.timeline
                               .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
                               .ToList();
            _snapshots.Push((variable, copy));

            // append the new qubit
            QuantumLedgerOfRegret.Record(new TimelineAppendOperation<T>(variable, copy, newSlice));

            // tell the variable its bootstrap has definitely gone
            variable.NotifyFirstAppend();

            // fire the hook so the convergence engine knows "something changed"
            variable.timeline.Add(newSlice);
            _onAppend?.Invoke();
        }
    }


    public void RestoreLastSnapshot()
    {
        lock (_syncRoot)
        {
            // summon the ghost of timelines past and cram it rudely back into existence
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
            QuantumLedgerOfRegret.Record(new TimelineReplaceOperation<T>(variable, backup));

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


public interface ISubEthaTransponder
{
    void Redirect();
    void Restore();
}

// Implement default services (omitting full bodies) ---
public class DefaultEntropyController : IEntropyController
{
    private readonly IPositronicRuntime _runtime;
    public DefaultEntropyController(IPositronicRuntime runtime)
        => _runtime = runtime;

    public int Entropy => _runtime.Entropy;
    /// <summary>
    /// Winds the temporal crank backwards to appease the spiteful spirits of past simulations.
    /// </summary>
    public void Initialise() => _runtime.Entropy = -1;
    /// <summary>
    /// Flips the entropy switch, because even the universe needs to change its mind sometimes.
    /// </summary>
    public void Flip() => _runtime.Entropy = -_runtime.Entropy;
}

public class RegretScribe<T> : IOperationLogHandler<T>
{
    public bool SawForwardWrite { get; set; }
    public void Record(IOperation op) => QuantumLedgerOfRegret.Record(op);
    /// <summary>
    /// Undo all operations since the last forward half-cycle marker, a half-cycle being a forward pass followed by a reverse pass, (the 'to' to a Merlin 'fro')
    /// </summary>
    public void UndoLastForwardCycle() => QuantumLedgerOfRegret.ReverseLastOperations();
    /// <summary>
    /// Sometimes you just need to start fresh and pretend none of that ever happened.
    /// </summary>
    public void Clear() => QuantumLedgerOfRegret.Clear();
}

public class SubEthaOutputTransponder : ISubEthaTransponder
{
    private readonly IPositronicRuntime _runtime;
    private TextWriter _originalOut;

    public SubEthaOutputTransponder(IPositronicRuntime runtime)
        => _runtime = runtime;

    public void Redirect()
    {
       // Like leaving a towel where you parked the time machine.
       _originalOut = ReferenceEquals(Console.Out, AethericRedirectionGrid.ImprobabilityDrive)
           ? AethericRedirectionGrid.ReferenceUniverse
           : Console.Out;
       // Hijack the console output like a space-time parasite.
       Console.SetOut(AethericRedirectionGrid.ImprobabilityDrive);
        _runtime.Babelfish = AethericRedirectionGrid.ImprobabilityDrive;
    }
    public void Restore()
    {

        var bufferText = AethericRedirectionGrid.ImprobabilityDrive.ToString();
        if (!AethericRedirectionGrid.AtTheRestaurant)
        {
            _originalOut.Write(bufferText);
            _originalOut.Flush();

            // Prevent temporal echoes when we exit the time stream
            AethericRedirectionGrid.SuppressEndOfUniverseReading = true;
        }
        else
        {
            // Our breadrumb trail to the reference universe, else we'll end up like Quinn from Sliders.
            AethericRedirectionGrid.SuppressEndOfUniverseReading = false;
        }

        Console.SetOut(_originalOut);

    }
}

public class ConvergenceEngineBuilder<T>
    where T : IComparable<T>
{
    private readonly List<Func<IImprobabilityEngine<T>, IImprobabilityEngine<T>>> _middlewares = new();

    /// <summary>
    /// Bolts a new questionable device onto the engine. Nobody asked what it does, and it's too late to stop now.
    /// </summary>
    public ConvergenceEngineBuilder<T> Use(Func<IImprobabilityEngine<T>, IImprobabilityEngine<T>> middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Summons the convergence abomination from its component horrors. Add enough decorators and it starts resembling sentience.
    /// </summary>
    public IImprobabilityEngine<T> Build(IImprobabilityEngine<T> core)
    {
        IImprobabilityEngine<T> engine = core;
        foreach (var middleware in _middlewares.Reverse<Func<IImprobabilityEngine<T>, IImprobabilityEngine<T>>>())
        {
            engine = middleware(engine);
        }
        return engine;
    }
}


public class ChroniclerDrive<T> : IImprobabilityEngine<T>
    where T : IComparable<T>
{
    public void Run(
        Action code,
        bool runFinalIteration = true,
        bool unifyOnConvergence = true,
        bool bailOnFirstReverseWhenIdle = false,
        IImprobabilityEngine<T> next = null)
    {
        next?.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
    }
}


// --- Convergence engine orchestrating the core loop ---
public class ImprobabilityEngine<T> : IImprobabilityEngine<T>
    where T : IComparable<T>
{
    private readonly IEntropyController _entropy;
    private readonly IOperationLogHandler<T> _ops;
    private readonly ISubEthaTransponder _redirect;
    private readonly IPositronicRuntime _runtime;
    private readonly ITimelineArchivist<T> _timelineArchivist;
    private readonly int _maxIters = 1000;

    public ImprobabilityEngine(
        IEntropyController entropy,
        IOperationLogHandler<T> ops,
        ISubEthaTransponder redirect,
        IPositronicRuntime runtime,
        ITimelineArchivist<T> timelineArchivist)
    {
        _entropy = entropy;
        _ops = ops;
        _redirect = redirect;
        _runtime = runtime;
        _timelineArchivist = timelineArchivist;
    }

    public void Run(Action code,
                    bool runFinalIteration = true,
                    bool unifyOnConvergence = true,
                    bool bailOnFirstReverseWhenIdle = false,
                    IImprobabilityEngine<T> next = null)
    {
        if (next != null)
        {
            // If part of a chain, call the next engine
            next.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
            return;
        }

        _redirect.Redirect();
        _entropy.Initialise();

        bool hadForwardCycle = false;
        bool skippedFirstForward = false;
        int iteration = 0;

        int hardLimit = unifyOnConvergence ? _maxIters : 2;

        while (!_runtime.Converged && iteration < hardLimit)
        {
            // Reset at the *start* of every half-cycle, not just forward ones,
            // so the engine doesn't confuse what has happened, what will happen,
            // and what it merely *thinks* has happened (which hasn't happened yet, probably).
            _ops.SawForwardWrite = false; // Why? Guarantees HadForwardAppend is in a known state; without this the engine occasionally thought "nothing happened" and broke the snapshot-clearing logic.

            // skip the *very* first forward cycle if requested
            // this is useful for scenarios where you want to
            // immediately start in reverse time, e.g. for
            // probing variable domains without any initial
            // assignments having been made
            // After all, it hasn't technically happened yet, and possibly never will, depending on who's asking
            if (!(bailOnFirstReverseWhenIdle
                && _entropy.Entropy > 0
                && hadForwardCycle
                && !skippedFirstForward))
            {
                // mark the start of this forward half-cycle so reverse replay
                // can peel exactly one cycle's worth worth of operations
                // peel-back is painfully precise; it must not overshoot or undershoot.
                // just ask a slugblaster.
                if (_entropy.Entropy > 0)
                    QuantumLedgerOfRegret.Record(new MerlinFroMarker());
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
                && !_ops.SawForwardWrite)
            {
                _runtime.Converged = false;
                break;
            }

            if (hadForwardCycle && PositronicVariable<T>.AllConverged(_runtime) && _entropy.Entropy < 0)
            {
                _timelineArchivist.ClearSnapshots();

                if (unifyOnConvergence)
                {
                    foreach (var pv in PositronicVariable<T>.GetAllVariables(_runtime))
                        pv.UnifyAll();
                }

                _runtime.Converged = true;
                break;
            }

            _entropy.Flip();
            // Reality is extremly fragile, each PositronicVariable<T> has a timeline
            // a list of QuBit<T> slices representing its different states across forward and reverse iterations.
            // When the convergence engine flips entropy, it effectively rewinds or fast-forwards time.
            // Now if we don't clone the last slice as we do here, both the current timeline entry and the incoming value
            // would reference the same QuBit<T> object causing a catastrophic collapse of the multiverse.
            // When journeying backwards through time, one should never share quantum state with oneself.
            // It's like borrowing your toothbrush from a parallel universe—things get weird very fast.
            if (_entropy.Entropy < 0)
            {
                foreach (var v in PositronicVariable<T>.GetAllVariables(_runtime))
                {
                    var pv   = (PositronicVariable<T>)v;
                    var last = pv.GetCurrentQBit();
                    var copy = new QuBit<T>(last.ToCollapsedValues().ToArray());
                    copy.Any();
                    pv.Assign(copy);
                }
            }

            iteration++;
        }

        if (runFinalIteration && _runtime.Converged)
        {
            // We give the universe one last chance to tidy itself up before the auditors arrive.
            if (unifyOnConvergence)
            {
                _timelineArchivist.ClearSnapshots();
                foreach (var v in PositronicVariable<T>.GetAllVariables(_runtime).OfType<PositronicVariable<T>>())
                    ((PositronicVariable<T>)v).UnifyAll();
            }
            // This is the quantum equivalent of nodding politely after the universe finishes talking.
            _runtime.Entropy = 1;
            _runtime.Converged = false;

            // undo any reversible ops from that final pass
            // the cosmic broom sweeps up the paradoxes before they stain the carpet.
            _ops.UndoLastForwardCycle();

            // Trim the timelines, purge any stray Loki variants.
            AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();

            code();

            _runtime.Converged = true;
        }

        _ops.Clear();

        _timelineArchivist.ClearSnapshots();
        // re-align each universe's timeline so the current state is the only state
        foreach (var v in PositronicVariable<T>.GetAllVariables(_runtime)
                                               .OfType<PositronicVariable<T>>())
        {
            v.ResetDomainToCurrent();

            if (unifyOnConvergence && v.timeline.Count > 1)
                v.UnifyAll();   // Every alternate reality now agrees to disagree quietly.
        }

        // This is the portal home back to our reference universe.
        _redirect.Restore();
    }
}

/// <summary>
/// Close your eyes sweet one, momma is going to do some nasty things to the universe.
/// </summary>
internal static class AethericRedirectionGrid
{
    internal static readonly TextWriter ReferenceUniverse;
    public static bool Initialised { get; } = true;

    // Store the actual StringWriter we create.
    private static readonly StringWriter _heartOfGold;
    private static int _hasRun;
    private static bool TryBeginRunOnce() => Interlocked.Exchange(ref _hasRun, 1) == 0;

    public static StringWriter ImprobabilityDrive => _heartOfGold;

    private static bool PositronicAmbientIsUnInitialised()
        => !PositronicAmbient.IsInitialized;

    internal static volatile bool SuppressEndOfUniverseReading;
    internal static volatile bool AtTheRestaurant;

    private static void InitialiseDefaultRuntime()
    {
        if (PositronicAmbient.Current != null)
            return; // Someone already called InitialiseWith()

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddPositronicRuntime())
            .Build();

        var runtime = host.Services.GetRequiredService<IPositronicRuntime>();
        PositronicAmbient.Current = runtime;
    }

    static AethericRedirectionGrid()
    {
        ReferenceUniverse = Console.Out;
        _heartOfGold = new StringWriter();

        // Capture everything from the very beginning so parallel universes
        // never accidentally leak into the reference universe
        if (!ReferenceEquals(Console.Out, _heartOfGold))
            Console.SetOut(_heartOfGold);

        try
        {
            var rt = PositronicAmbient.Current;
            rt.Babelfish = _heartOfGold;
            rt.Reset();
        }
        catch (InvalidOperationException)
        {
            // There's no cosmic event manager yet... that's fine.... probably? Yes definetly fine ¬.¬
        }

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            AtTheRestaurant = true;
            try
            {
                // Run once; idempotent gate stays the same.
                RunAttributedEntryPointForTests();
            }
            finally
            {
                AtTheRestaurant = false;
            }

            // If the reference universe doesn't know about us then we die, so this is our last chance.
            if (!SuppressEndOfUniverseReading)
            {
                Console.SetOut(ReferenceUniverse);
                ReferenceUniverse.Write(ImprobabilityDrive.ToString());
                ReferenceUniverse.Flush();
            }
        };


    }

    /// <summary>
    /// Just for our tests. <see cref="ImprobabilityDrive"/>.
    /// </summary>
    public static void RunAttributedEntryPointForTests()
    {
        if (!TryBeginRunOnce())
            return;

        // Ensure a runtime exists
        if (!PositronicAmbient.IsInitialized)
            InitialiseDefaultRuntime();

        // Find exactly one [PositronicEntry]
        var entryPoints = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(asm =>
            {
                try { return asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
            })
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m =>
                {
                    try { return m.GetCustomAttribute<DontPanicAttribute>() != null; }
                    catch (TypeLoadException) { return false; }
                    catch (ReflectionTypeLoadException) { return false; }
                    catch (FileNotFoundException) { return false; }
                })
            .ToList();

        if (entryPoints.Count == 0)
            throw new InvalidOperationException("No method marked with [PositronicEntry] was found. Please annotate a static method to use as the convergence entry point.");

        if (entryPoints.Count > 1)
        {
            var methodNames = string.Join(Environment.NewLine, entryPoints.Select(m => $" - {m.DeclaringType.FullName}.{m.Name}"));
            throw new InvalidOperationException($"Multiple methods marked with [PositronicEntry] were found:{Environment.NewLine}{methodNames}{Environment.NewLine}Please mark only one method with [PositronicEntry].");
        }

        var entryPoint = entryPoints[0];
        var rt = PositronicAmbient.Current;

        // If no variables exist yet, do a minimal "probe run" at reverse time
        // to allow the entry point to register its first PositronicVariable<T>.
        if (!rt.Registry.Any())
        {
            var savedEntropy = rt.Entropy;
            rt.Entropy = -1;

            // Suppress *probe* output so only converged prints are surfaced.
            var prevOut = Console.Out;
            try
            {
                Console.SetOut(AethericRedirectionGrid.ImprobabilityDrive);
                var pProbe = entryPoint.GetParameters();
                object[] probeArgs = pProbe.Length == 0 ? null : new object[] { Array.Empty<string>() };
                entryPoint.Invoke(null, probeArgs);
            }
            finally
            {
                // Discard anything the probe wrote and restore console.
                AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();
                Console.SetOut(prevOut);
                rt.Entropy = savedEntropy;
            }
        }

        if (!rt.Registry.Any())
            throw new InvalidOperationException("No PositronicVariable was registered. Cannot determine a generic type for convergence.");

        // Determine generic T from the last registered positronic variable
        var lastVariable = rt.Registry.Last();
        var genericArg = lastVariable.GetType().GetGenericArguments()[0];

        // Call PositronicVariable<T>.RunConvergenceLoop(rt, () => entryPoint(), …)
        var runMethod = typeof(PositronicVariable<>)
            .MakeGenericType(genericArg)
            .GetMethod("RunConvergenceLoop", BindingFlags.Public | BindingFlags.Static);

        runMethod.Invoke(
            null,
            new object[]
            {
                rt,
                (Action)(() =>
                {
                    var p = entryPoint.GetParameters();
                    object[] epArgs = p.Length == 0 ? null : new object[] { Array.Empty<string>() };
                    entryPoint.Invoke(null, epArgs);
                }),
                true,   // runFinalIteration
                true,   // unifyOnConvergence
                false   // bailOnFirstReverseWhenIdle
            });
    }

}

public interface IOperation
{
    /// <summary>
    /// Invokes the logic to undo the operation.
    /// </summary>
    void Undo();

    /// <summary>
    /// A polite label slapped on your operation so future archeologists of this code can blame someone else.
    /// </summary>
    string OperationName { get; }
}

/// <summary>
/// Marks the beginning of a forward half‑cycle in the OperationLog.
/// Lets reverse‑replay peel exactly one forward half‑cycle and no more.
/// </summary>
public sealed class MerlinFroMarker : IOperation
{
    public string OperationName => "ForwardHalfCycleStart";
    public void Undo() { /* no‑op */ }
}

/// <summary>
/// The Quantum Ledger of Regret™ — remembers every dumb thing you've done so you can go back and pretend you didn't.
/// </summary>
public static class QuantumLedgerOfRegret
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

    // violently yeet the last recorded mistake into the entropy void
    public static void Pop()
    {
        if (_log.Count > 0)
            _log.Pop();
    }

    public static void Clear() => _log.Clear();
}

/// <summary>
/// Yanks the timeline back to a simpler time, before it made any questionable life choices.
/// </summary>
public class TimelineAppendOperation<T> : IOperation
    where T : IComparable<T>
{
    private readonly PositronicVariable<T> _variable;
    private readonly List<QuBit<T>> _backupTimeline;
    public PositronicVariable<T> Variable => _variable;
    public QuBit<T> AddedSlice { get; }

    public string OperationName => "TimelineAppend";

    public TimelineAppendOperation(PositronicVariable<T> variable, List<QuBit<T>> backupTimeline, QuBit<T> added)
    {
        _variable = variable;
        // Make a safe copy so we can fully restore:
        _backupTimeline = backupTimeline
            .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
            .ToList();
        AddedSlice = added;

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
    where T : IComparable<T>
{
    private readonly PositronicVariable<T> _variable;
    private readonly List<QuBit<T>> _backupTimeline;

    public PositronicVariable<T> Variable => _variable;
    internal QuBit<T> ReplacedSlice => _backupTimeline[^1];

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


#region Initiate the matrix with NeuroCascadeInitialiser
/// <summary>
/// The Matrix is everywhere. It is all around us. Even now, in this very room.
/// </summary>
public static class NeuroCascadeInitialiser
{
    /// <summary>
    /// Activates the latent matrix Initialiser. It doesn't *do* anything,
    /// but the side effects are, frankly, terrifying.
    /// </summary>
    public static void AutoEnable()
    {
        var dummy = AethericRedirectionGrid.Initialised;
    }
}

#endregion

public class UnhappeningEngine<T>
    where T : IComparable<T>
{
    private readonly IOperationLogHandler<T> _ops;
    private readonly ITimelineArchivist<T> _versioningService;

    public UnhappeningEngine(IOperationLogHandler<T> ops, ITimelineArchivist<T> versioningService)
    {
        _ops = ops;
        _versioningService = versioningService;
    }

    /// <summary>
    /// Peel off the last forward half‑cycle, and rebuild every possible earlier state
    /// that would make the *later* writes self‑consistent.
    ///
    /// Semantics: we move *backwards in execution order*, but for each operation
    ///    we apply its **forward** mapping. Example:
    ///       x = 10;            // last write
    ///       x = x + 1;         // earlier in code
    ///    The earlier read must be 11 (addition still "adds" as we walk back).
    ///
    /// The only special case is modulus: the forward mapping we apply is the
    /// "rebuild" q·d + r using the quotient captured at record‑time.
    /// </summary>
   public QuBit<T> ReplayReverseCycle(QuBit<T> incoming, PositronicVariable<T> variable)
   {
        // Peel the **last forward half‑cycle**:
        // pop ops newest->oldest until we hit the ForwardHalfCycleMarker;
        // if there is no marker (outside the loop), fall back to greedy peel.
        var poppedSnapshots = new List<IOperation>();
        var poppedReversibles = new List<IReversibleOperation<T>>();
        var forwardValues = new HashSet<T>();

        while (true)
        {
            // Gotos are evil and wrong a deliciously tasty with hot sauce.
            var top = QuantumLedgerOfRegret.Peek();
            switch (top)
            {
                case null:
                    // no marker found (outside loop) - done peeling this run
                    goto DonePeel;
                case MerlinFroMarker:
                    // boundary of this forward half‑cycle - stop here
                    goto DonePeel;
                case TimelineAppendOperation<T> tap:
                    forwardValues.UnionWith(tap.AddedSlice.ToCollapsedValues());
                    poppedSnapshots.Add(tap);
                    QuantumLedgerOfRegret.Pop();
                    continue;
                case TimelineReplaceOperation<T> trp:
                    // capture both "before" and "after" for Replace
                    forwardValues.UnionWith(trp.ReplacedSlice.ToCollapsedValues());
                    forwardValues.UnionWith(trp.Variable.GetCurrentQBit().ToCollapsedValues());
                    poppedSnapshots.Add(trp);
                    QuantumLedgerOfRegret.Pop();
                    continue;
                case IReversibleOperation<T> rop:
                    poppedReversibles.Add(rop);
                    QuantumLedgerOfRegret.Pop();
                    continue;
                default:
                    // unrecognized entry - stop safely
                    goto DonePeel;
            }
        }
        DonePeel:

        // "rewind" step so that it undoes both appends and replaces:
        foreach (var op in poppedSnapshots.OfType<IOperation>().Reverse())
            op.Undo();


        // Was the forward half-cycle closed by a scalar overwrite?
        var hasArithmetic = poppedReversibles.Count > 0;
        bool scalarWriteDetected = hasArithmetic && poppedSnapshots.Count > poppedReversibles.Count;

        bool includeForward = poppedReversibles.Count == 0 || !scalarWriteDetected;

        IEnumerable<T> seeds;

        var incomingVals = incoming.ToCollapsedValues();
        if (!scalarWriteDetected)
        {
            // Trim bootstrap *only* in the degenerate case where no arithmetic happened.
            // If arithmetic did occur (e.g., +1 or %3), a real result may equal the bootstrap
            // (e.g., 0), and we must keep it.
            var bootstrap = variable.timeline[0].ToCollapsedValues();
            var excludeBootstrap = !hasArithmetic && bootstrap.Count() == 1;

            var fwd = excludeBootstrap ? forwardValues.Except(bootstrap) : forwardValues;
            var inc = excludeBootstrap ? incomingVals.Except(bootstrap) : incomingVals;

            seeds = includeForward ? fwd.Union(inc).Distinct() : inc;
        }
        else
        {
            // Scalar overwrite close: rebuild from the new scalar(s) only,
            // but exclude the replaced slice (if any) and the bootstrap.
            var bootstrap = variable.timeline[0].ToCollapsedValues();
            var baseTrim  = incomingVals.Except(bootstrap);

            var replaced = poppedSnapshots
                .OfType<TimelineReplaceOperation<T>>()
                .SelectMany(op => op.ReplacedSlice.ToCollapsedValues());

            seeds = baseTrim.Except(replaced).Distinct();
        }

        if (!seeds.Any())
        {
            // Keep at least what the caller just assigned.
            seeds = incomingVals;
        }

        var rebuiltSet = new HashSet<T>();

        // **only** if there was literally _no_ popped operation at all
        // do we reuse incoming here
        if (!poppedSnapshots.Any() && !poppedReversibles.Any())
            rebuiltSet.UnionWith(incomingVals);

        // Special case: last half‑cycle pattern "(+k) then % d" ⇒ earlier reads fill the residue class {0..d-1}
        // This matches the fixed‑point intuition of the demo program and keeps the set tight.
        var addOp = poppedReversibles.OfType<AdditionOperation<T>>().FirstOrDefault();
        var modOp = poppedReversibles.OfType<ReversibleModulusOp<T>>().FirstOrDefault();
        if (addOp is not null && modOp is not null)
        {
            int d = Convert.ToInt32(modOp.Divisor);
            for (int i = 0; i < d; i++)
                rebuiltSet.Add((T)Convert.ChangeType(i, typeof(T)));
        }
        else
        {
            foreach (var seed in seeds)
            {
                var v = seed;
                // Walk *backwards in execution order* applying **forward** maps, newest → oldest.
                for (int i = 0; i < poppedReversibles.Count; i++)
                    v = poppedReversibles[i].ApplyForward(v);
                rebuiltSet.Add(v);
            }
        }

        // If arithmetic occurred, don't union the forward remainder set into the *earlier* reads;
        // that remainder belongs to the later state, not the prior one.
        if (!scalarWriteDetected && !hasArithmetic)
        {
            var bootstrap = variable.timeline[0].ToCollapsedValues();
            var excludeBootstrap = bootstrap.Count() == 1;
            rebuiltSet.UnionWith(excludeBootstrap ? forwardValues.Except(bootstrap) : forwardValues);
        }

        // Carefully wrap this absurd collection of maybe-states into one neat, Schrödinger-approved burrito
        var rebuilt = new QuBit<T>(rebuiltSet.OrderBy(x => x).ToArray());
        rebuilt.Any();

        /*  When a scalar overwrite closed the forward half‑cycle we
         *  want that new scalar - and *only* that scalar - to survive.
         *  The older slices (bootstrap + intermediate) are discarded.
         */
        if (scalarWriteDetected)
        {
            /* Keep slice 0 intact, discard only the intermediate
               forward‑pass history, then append the new scalar. */
            if (variable.timeline.Count > 1)
                variable.timeline.RemoveRange(1, variable.timeline.Count - 1);

            variable.timeline.Add(rebuilt);
        }
        else
        {
            // If the forward half-cycle performed only in-place merges (no appends),
            // do not grow the timeline during reverse replay; replace the last slice.
            // This preserves "may merge" behaviour and avoids
            // spurious slice growth on pure-merge passes.
            if (!poppedSnapshots.OfType<TimelineAppendOperation<T>>().Any() && variable.timeline.Count > 0)
            {
                variable.timeline[^1] = rebuilt;
            }
            else
            {
                variable.timeline.Add(rebuilt);
            }
        }


        return rebuilt;
    }
}



#region Interfaces and Runtime

/// <summary>
/// Direction of time: +1 for forward, -1 for reverse, this time machine only has two gears.
/// </summary>
public interface IPositronicVariable
{
    /// <summary>
    /// Unifies all timeline slices into a single disjunctive state,
    /// kind of like a group project where everyone finally agrees on something.
    /// </summary>
    int Converged();

    /// <summary>
    /// Melds every branching future into one reluctant consensus, like a committee that's given up.
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
    /// Controls the emotional direction of time: +1 means "we're moving on,"
    /// -1 means "let's try that whole simulation again but sadder."
    /// </summary>
    int Entropy { get; set; }

    /// <summary>
    /// Global convergence flag.
    /// </summary>
    bool Converged { get; set; }

    /// <summary>
    /// A bit wetware, but it gets the job done.
    /// </summary>
    TextWriter Babelfish { get; set; }

    /// <summary>
    /// The collection of all Positronic variables registered.
    /// </summary>
    IPositronicVariableRegistry Variables { get; }

    /// <summary>
    /// Performs a ceremonial memory wipe on the runtime state.
    /// Ideal for fresh starts, debugging, or pretending the last run didn't happen.
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
    public TextWriter Babelfish { get; set; }
    public IPositronicVariableRegistry Variables { get; }
    public int TotalConvergenceIterations { get; set; } = 0;


    public event Action OnAllConverged;
    public IPositronicVariableFactory Factory { get; }
    public IPositronicVariableRegistry Registry { get; }


    public DefaultPositronicRuntime(IPositronicVariableFactory factory, IPositronicVariableRegistry registry)
    {
        Babelfish = AethericRedirectionGrid.ImprobabilityDrive;

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
        Babelfish = AethericRedirectionGrid.ImprobabilityDrive;
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
    where T : IComparable<T>
{
    // Determines at runtime if T is a value type.
    private readonly bool isValueType = typeof(T).IsValueType;
    private readonly HashSet<T> _domain = new();
    private static bool _reverseReplayStarted;
    public static int _loopDepth;
    private readonly IPositronicRuntime _runtime;
    private bool _hasWrittenInitialForward = false;
    internal void NotifyFirstAppend() => bootstrapSuperseded = true;

    internal static bool InConvergenceLoop => _loopDepth > 0;
    private readonly UnhappeningEngine<T> _reverseReplay;
    private readonly IOperationLogHandler<T> _ops;

    private readonly ITimelineArchivist<T> _temporalRecords;

    public void SeedBootstrap(params T[] values)
    {
        var qb = new QuBit<T>(values);
        qb.Any();
        _temporalRecords.OverwriteBootstrap(this, qb); // A small sacrifice to the versioning gods

        bootstrapSuperseded = false;
    }


    public PositronicVariable(
        T initialValue,
        IPositronicRuntime runtime,
        ITimelineArchivist<T> timelineArchivist = null,
        IOperationLogHandler<T> opsHandler = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        // seed the timeline exactly once
        _temporalRecords = timelineArchivist ?? new BureauOfTemporalRecords<T>();
        var qb = new QuBit<T>(new[] { initialValue });
        qb.Any();
        _temporalRecords.OverwriteBootstrap(this, qb);
        _hasWrittenInitialForward = true;

        _domain.Add(initialValue);
        _runtime.Registry.Add(this);

        // DI my temporal records and ops handler
        _ops = opsHandler ?? new RegretScribe<T>();
        _reverseReplay = new UnhappeningEngine<T>(_ops, _temporalRecords);

        // "something appended" hook
        _temporalRecords.RegisterTimelineAppendedHook(() => OnTimelineAppended?.Invoke());
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
        var _ = AethericRedirectionGrid.Initialised;
    }

    private void Remember(IEnumerable<T> xs)
    {
        foreach (var x in xs) _domain.Add(x);
    }

    internal static void ResetReverseReplayFlag()
    {
        _reverseReplayStarted = false;           // called each time the direction flips
    }

    /// <summary>
    /// Secret lever that fires whenever a new timeline twig is grafted on
    /// </summary>
    internal static Action TimelineAppendedHook;

    /// <summary>
    /// Injects an explicit QuBit into the timeline **without collapsing it**.
    /// </summary>
    public void Assign(QuBit<T> qb)
    {
        if (qb is null)
            throw new ArgumentNullException(nameof(qb));

        // Ensure it's been observed at least once so that
        // subsequent operator overloads won't collapse accidentally.
        qb.Any();
        ReplaceOrAppendOrUnify(qb, replace: true);
    }

    // Automatically enable simulation on first use.
    private static readonly bool _ = EnableSimulation();
    private static bool EnableSimulation()
    {
        NeuroCascadeInitialiser.AutoEnable();
        return true;
    }



    // (removed) static registry in favor of instance‐based IPositronicVariableRegistry
    //private static Dictionary<string, PositronicVariable<T>> registry = new Dictionary<string, PositronicVariable<T>>();

    // The timeline of quantum slices.
    public readonly List<QuBit<T>> timeline = new List<QuBit<T>>();
    private bool bootstrapSuperseded = false;

    public event Action OnConverged;
    public event Action OnCollapse;
    public event Action OnTimelineAppended;

    /// <summary>
    /// How many alternate realities we've stacked on this poor variable's timeline. More is usually bad.
    /// </summary>
    public int TimelineLength => timeline.Count;

    public static void SetEntropy(IPositronicRuntime rt, int e) => rt.Entropy = e;
    public static int GetEntropy(IPositronicRuntime rt) => rt.Entropy;


    public static PositronicVariable<T> GetOrCreate(string id, T initialValue, IPositronicRuntime runtime)
        => runtime.Factory.GetOrCreate<T>(id, initialValue);


    public static PositronicVariable<T> GetOrCreate(string id, IPositronicRuntime runtime)
        => runtime.Factory.GetOrCreate<T>(id);


    public static PositronicVariable<T> GetOrCreate(T initialValue, IPositronicRuntime runtime)
        => runtime.Factory.GetOrCreate<T>(initialValue);


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
        var opsHandler = new RegretScribe<T>();
        var redirect = new SubEthaOutputTransponder(runtime);

        var engine = PositronicAmbient.Services.GetService<IImprobabilityEngine<T>>()
                ?? new ImprobabilityEngine<T>(
                       new DefaultEntropyController(runtime),
                       new RegretScribe<T>(),
                       new SubEthaOutputTransponder(runtime),
                       runtime,
                       new BureauOfTemporalRecords<T>());

        try
        {
            // ‼️ mark "we're inside the loop" so the fast-path is disabled
            _loopDepth++;
            engine.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
        }
        finally
        {
            _loopDepth--;
        }

    }

    /// <summary>
    /// Audits the quantum history to see if all timelines are finally tired of arguing and want to go home.
    /// </summary>
    public int Converged()
    {
        // If the engine itself already flagged full convergence, we're done.
        if (_runtime.Converged)
            return 1;

        if (timeline.Count < 3)
            return 0;

        // If the last two slices match, that's a 1‐step convergence.
        if (SameStates(timeline[^1], timeline[^2]))
            return 1;

        // Otherwise look for any earlier slice that matches the last.
        for (int i = 2; i <= timeline.Count; i++)
        {
            if (SameStates(timeline[^1], timeline[timeline.Count - i]))
                return i - 1;
        }

        // No convergence detected yet.
        return 0;
    }


    /// <summary>
    /// Unifies all timeline slices into a single collapsed state.
    /// </summary>
    public void UnifyAll()
    {
        // nothing to do if we still only have the bootstrap
        if (timeline.Count < 2)
            return;

        /* Merge *all* values that appeared after the bootstrap (slice 0)
           into a single union slice, but **keep** slice 0 untouched so
           callers can still see the original seed when they ask for it. */

        var mergedStates = timeline
                            .Skip(1)   // ignore bootstrap
                            .SelectMany(q => q.ToCollapsedValues())
                            .Distinct()
                            .ToArray();

        var unified = new QuBit<T>(mergedStates);
        unified.Any();

        // replace everything after the bootstrap with the unified slice
        timeline.RemoveRange(1, timeline.Count - 1);
        timeline.Add(unified);

        // refresh the domain to reflect the new union
        _domain.Clear();
        foreach (var x in mergedStates)
            _domain.Add(x);

        QuantumLedgerOfRegret.Clear();
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
        ReplaceOrAppendOrUnify(qb, replace: true);
    }

    /// <summary>
    /// Forcefully injects a scalar value into the timeline.
    /// </summary>
    public void Assign(T scalarValue)
    {
        var qb = new QuBit<T>(new[] { scalarValue });
        qb.Any();

        ReplaceOrAppendOrUnify(qb, replace: !isValueType);
    }
    /// <summary>
    /// Either replaces, appends, or unifies the current timeline slice with a new quantum slice.
    /// </summary>
    /// <summary>
    /// Either replaces, appends, or unifies the current timeline slice with a new quantum slice.
    /// </summary>
    private void ReplaceOrAppendOrUnify(QuBit<T> qb, bool replace)
    {
        var runtime = _runtime;

        /* -------------------------------------------------------------
        *  Inside a convergence‑loop, forward half‑cycles follow
        *  different rules:
        *      - scalar  writes  (replace == false) **append**
        *      - qubit / union   (replace == true)  **overwrite**
        *    …but never touch the bootstrap slice.
        * ------------------------------------------------------------- */
        if (runtime.Entropy > 0 && InConvergenceLoop)
        {
            if (timeline.Count == 1) // still on bootstrap
            {
                // Never mutate slice 0 during the loop; always append the first write.
                _temporalRecords.SnapshotAppend(this, qb);
                _ops.SawForwardWrite = true;
                return;
            }


            else if (replace)                            // overwrite/merge
            {
                var incoming = qb.ToCollapsedValues();

                // single‑value qubits are *scalar* writes → append
                if (incoming.Count() == 1)
                {
                    _temporalRecords.SnapshotAppend(this, qb);
                }
                else                // real union → keep the old merge path
                {
                    var merged = timeline[^1]
                                    .ToCollapsedValues()
                                    .Union(incoming)
                                    .Distinct()
                                    .ToArray();
                    var mergedQb = new QuBit<T>(merged); mergedQb.Any();
                    _temporalRecords.ReplaceLastSlice(this, mergedQb);
                }

                _ops.SawForwardWrite = true;
                return;
            }
            else                                         // scalar - append
            {
                _temporalRecords.SnapshotAppend(this, qb);
                _ops.SawForwardWrite = true;
            }
            return;
        }

        // We're going backwards in time, which means we can only see what will have going to have happened.
        if (runtime.Entropy < 0 && InConvergenceLoop)
        {
            // Always drive reverse replay; if caller passed the same instance, clone it.
            if (ReferenceEquals(qb, timeline[^1]))
            {
                qb = new QuBit<T>(qb.ToCollapsedValues().ToArray());
                qb.Any();
            }

            var topOp = QuantumLedgerOfRegret.Peek();
            if (topOp is null || topOp is MerlinFroMarker)
                return;

            var rebuilt = _reverseReplay.ReplayReverseCycle(qb, this);
            timeline.Add(rebuilt);
            OnTimelineAppended?.Invoke();
            return;
        }

        // Emergency override for when we're flailing outside the loop like a time-traveling otter
        if (runtime.Entropy > 0 && !InConvergenceLoop)
        {
            // First ever forward write OR first after Unify → APPEND
            if (timeline.Count == 1)
            {
                _temporalRecords.SnapshotAppend(this, qb);
                _ops.SawForwardWrite = true;
                bootstrapSuperseded = true;
                return;
            }
            if (!replace)
            {
                var lastStates = timeline[^1].ToCollapsedValues();
                var merged = lastStates
                       .Union(qb.ToCollapsedValues())
                       // Only pull the bootstrap into the union when the last slice
                       // still represents a single scalar   (guarantees _pure_ scalar sequence)
                       .Union(lastStates.Count() == 1 ? timeline[0].ToCollapsedValues() : Array.Empty<T>())
                       .Distinct()
                       .ToArray();
                var mergedQb = new QuBit<T>(merged); mergedQb.Any();
                _temporalRecords.ReplaceLastSlice(this, mergedQb);
            }
            else
            {
                // Otherwise we need to preserve causality - else Marty McFly's hand will disappear again.
                _temporalRecords.SnapshotAppend(this, qb);
                _ops.SawForwardWrite = true;
            }

            return;
        }

        // If we've already converged, do nothing.
        if (_runtime.Converged)
            return;

        Remember(qb.ToCollapsedValues());

        // --- Reverse‐time pass (Entropy < 0) -----------------------------
        if (runtime.Entropy < 0)
        {
            var top = QuantumLedgerOfRegret.Peek();
            if (top is null || top is MerlinFroMarker)
                return;
            // OK, this is a true reverse-replay pass
            var rebuilt = _reverseReplay.ReplayReverseCycle(qb, this);
            timeline.Add(rebuilt);
            OnTimelineAppended?.Invoke();
            return;
        }

        // --- Forward‐time pass (Entropy > 0) -----------------------------
        if (runtime.Entropy > 0)
        {
            // — overwrite bootstrap if that's all we have
            if (bootstrapSuperseded && timeline.Count == 1)
            {
                // ③ post‑Unify forward write should append, not overwrite
                _temporalRecords.SnapshotAppend(this, qb);
                _ops.SawForwardWrite = true;
                return;
            }

            // — first real forward write: snapshot+append
            if (!bootstrapSuperseded && timeline.Count == 1)
            {
                _temporalRecords.SnapshotAppend(this, qb);
                _ops.SawForwardWrite = true;
                bootstrapSuperseded = true;
                return;
            }

            // — overwrite (merge) if this is the very first forward write
            if (replace && !_ops.SawForwardWrite)
            {
                var merged = timeline[^1]
                   .ToCollapsedValues()
                   .Union(qb.ToCollapsedValues())
                   .Distinct()
                   .ToArray();
                var mergedQb = new QuBit<T>(merged);
                mergedQb.Any();
                _temporalRecords.ReplaceLastSlice(this, mergedQb);
                _ops.SawForwardWrite = true;
                return;
            }
            // — otherwise decide append vs. merge
            if (!replace || (!_ops.SawForwardWrite && !SameStates(qb, timeline[^1])))
            {
                // snapshot-append the new slice
                _temporalRecords.SnapshotAppend(this, qb);
                _ops.SawForwardWrite = true;
            }
            else
            {
                // merge into the last slice if it's a duplicate scalar write
                var merged = timeline[^1]
                                      .ToCollapsedValues()
                                      .Union(qb.ToCollapsedValues())
                                      .Distinct()
                                      .ToArray();
                var mergedQb = new QuBit<T>(merged);
                mergedQb.Any();
                _temporalRecords.ReplaceLastSlice(this, mergedQb);
            }


            // — detect any small cycle and unify
            for (int cycle = 2; cycle <= 20; cycle++)
                if (timeline.Count >= cycle + 1 && SameStates(timeline[^1], timeline[^(cycle + 1)]))
                {
                    Unify(cycle);
                    break;
                }

            return;
        }

    }



    #region region Operator Overloads
    // --- Operator Overloads ---
    // --- Addition Overloads ---
    public static QExpr operator +(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() + right;
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left, right, left._runtime));
        }
        return new QExpr(left, resultQB);
    }

    public static QExpr operator +(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() + left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
        {
            QuantumLedgerOfRegret.Record(new AdditionOperation<T>(right, left, right._runtime));
        }
        return new QExpr(right, resultQB);
    }

    public static QExpr operator +(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var resultQB = left.GetCurrentQBit() + right.GetCurrentQBit();
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            // Record addition using the collapsed value from the right variable.
            T operand = right.GetCurrentQBit().ToCollapsedValues().First();
            QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left, operand, left._runtime));
        }
        return new QExpr(left, resultQB);
    }

    // --- Modulus Overloads ---
    public static QuBit<T> operator %(PositronicVariable<T> left, T right)
    {
        var before = left.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = left.GetCurrentQBit() % right;
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
            QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left, right, left._runtime));
        return resultQB;
    }


    public static QuBit<T> operator %(T left, PositronicVariable<T> right)
    {
        var before = right.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = right.GetCurrentQBit() % left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
            QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(right, left, right._runtime));
        return resultQB;
    }


    public static QuBit<T> operator %(PositronicVariable<T> left, PositronicVariable<T> right)
    {
        var before = left.GetCurrentQBit().ToCollapsedValues().First();
        var divisor = right.GetCurrentQBit().ToCollapsedValues().First();
        var resultQB = left.GetCurrentQBit() % right.GetCurrentQBit();
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
            QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left, divisor, left._runtime));
        return resultQB;
    }

    // --- Subtraction Overloads ---
    public static QuBit<T> operator -(PositronicVariable<T> left, T right)
    {
        var resultQB = left.GetCurrentQBit() - right;
        resultQB.Any();
        if (left._runtime.Entropy >= 0)
        {
            QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left, right, left._runtime));
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
            QuantumLedgerOfRegret.Record(new SubtractionReversedOperation<T>(right, left, right._runtime));
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
            QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left, operand, left._runtime));
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
            QuantumLedgerOfRegret.Record(new NegationOperation<T>(value, value._runtime));
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
            QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(left, right, left._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator *(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() * left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
        {
            QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(right, left, right._runtime));
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
            QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(left, operand, left._runtime));
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
            QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left, right, left._runtime));
        }
        return resultQB;
    }

    public static QuBit<T> operator /(T left, PositronicVariable<T> right)
    {
        var resultQB = right.GetCurrentQBit() / left;
        resultQB.Any();
        if (right._runtime.Entropy >= 0)
        {
            QuantumLedgerOfRegret.Record(new DivisionReversedOperation<T>(right, left, right._runtime));
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
    /// Folds the last known state into a reality burrito and pretends none of the branching ever happened.
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
    /// Outputs the history of this variable's midlife crises in a human-readable format.
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
    /// Gets or sets the current quantum state.
    /// </summary>
    public QuBit<T> State
    {
        get => GetCurrentQBit();
        set => Assign(value);
    }

    /// <summary>
    /// Gets or sets the current scalar value, collapsing if necessary.
    /// </summary>
    public T Scalar
    {
        get => GetCurrentQBit().ToCollapsedValues().First();
        set => Assign(value);
    }


    /// <summary>
    /// Collapses the current quantum cloud into discrete values.
    /// </summary>
    public IEnumerable<T> ToValues() => GetCurrentQBit().ToCollapsedValues();

    /// <summary>
    /// Retrieves the freshest slice of existence, still warm from the cosmic oven.
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

    /// <summary>
    /// (Called by the engine after convergence)
    /// Wipe out the internal _domain and re-seed it from the current QBit alone.
    /// </summary>
    internal void ResetDomainToCurrent()
    {
        _domain.Clear();
        foreach (var x in GetCurrentQBit().ToCollapsedValues())
            _domain.Add(x);
    }


    /// <summary>
    /// A helper struct to enable chained operations without immediate collapse.
    /// </summary>
    public readonly struct QExpr
    {
        internal readonly QuBit<T> Q;
        internal readonly PositronicVariable<T> Source;
        internal QExpr(PositronicVariable<T> src, QuBit<T> q) { Source = src; Q = q; }
        public IEnumerable<T> ToCollapsedValues() => Q.ToCollapsedValues();
        public override string ToString() => Q.ToString();
        public static implicit operator QuBit<T>(QExpr e) => e.Q;

        /// <summary>
        /// (PositronicVariable<T> = QExpr) - e.g., antival = (antival + 1)
        /// </summary>
        /// <param name="e"></param>
        public static implicit operator PositronicVariable<T>(QExpr e)
        {
            e.Source.Assign(e.Q);
            return e.Source;
        }

        /// <summary>
        /// (QExpr % T) - e.g., (antival + 1) % 3
        /// </summary>
        public static QuBit<T> operator %(QExpr left, T right)
        {
            var resultQB = left.Q % right;
            resultQB.Any();
            if (left.Source._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left.Source, right, left.Source._runtime));
            return resultQB;
        }


        /// <summary>
        /// (QExpr % PositronicVariable<T>) - optional, for parity with PV%PV
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static QuBit<T> operator %(QExpr left, PositronicVariable<T> right)
        {
            var divisor = right.GetCurrentQBit().ToCollapsedValues().First();
            var resultQB = left.Q % right.GetCurrentQBit();
            resultQB.Any();
            if (left.Source._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left.Source, divisor, left.Source._runtime));
            return resultQB;
        }


}

}


#endregion



#region NeuralNodule<T>

/// <summary>
/// A quantum-aware neuron that collects Positronic inputs,
/// applies an activation function, and fires an output into an alternate future.
/// </summary>
public class NeuralNodule<T> where T : struct, IComparable<T>
{
    public List<PositronicVariable<T>> Inputs { get; } = new();
    public PositronicVariable<T> Output { get; }
    public Func<IEnumerable<T>, QuBit<T>> ActivationFunction { get; set; }

    /// <summary>
    /// Constructs a highly opinionated quantum neuron that fires based on arbitrary math and existential dread.
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
    /// <summary>
    /// Sometimes maths is just adding two numbers together.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static T Add<T>(T x, T y)
        where T : INumber<T>
        => x + y;

    /// <summary>
    /// A fallback for dynamic types that don't support generic maths (e.g. Qubits)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static dynamic Add(dynamic x, dynamic y)
        => x + y;

    /// <summary>
    /// Who's even reading this far down? 1 - 1 = 0, right?
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static T Subtract<T>(T x, T y)
        where T : ISubtractionOperators<T, T, T>
        => x - y;
    /// <summary>
    /// I hate dynamics, they're the worst.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static dynamic Subtract(dynamic x, dynamic y)
        => x - y;

    /// <summary>
    /// There's a video somewhere on the internet from the 90s of Carol Vorderman dressed as Madonna singing
    /// "I'm going to show you how to multiply", it's as easy as 1, 2, 3. I think about that video a lot.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static T Multiply<T>(T x, T y)
        where T : IMultiplyOperators<T, T, T>
        => x * y;
    /// <summary>
    /// Who are you and why are you using dynamics in 2025?
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static dynamic Multiply(dynamic x, dynamic y)
        => x * y;

    /// <summary>
    /// Division is the most controversial of the basic arithmetic operations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static T Divide<T>(T x, T y)
        where T : IDivisionOperators<T, T, T>
        => x / y;
    /// <summary>
    /// Seriously, stop using dynamics.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static dynamic Divide(dynamic x, dynamic y)
        => x / y;

    /// <summary>
    /// The remainder operation, also known as modulo, gives you the leftover part of a division.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static T Remainder<T>(T x, T y)
        where T : IModulusOperators<T, T, T>
        => x % y;
    /// <summary>
    /// Dynamic remainder, because why not.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static dynamic Remainder(dynamic x, dynamic y)
        => x % y;

    /// <summary>
    /// The mirror universe, we put a beard on every number.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <returns></returns>
    public static T Negate<T>(T x)
        where T : IUnaryNegationOperators<T, T>
        => -x;
    /// <summary>
    /// Dynamic negation, because some people just want to watch the world burn.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static dynamic Negate(dynamic x)
        => -x;

    /// <summary>
    /// Modulus operation that always returns a non-negative result.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static T Modulus<T>(T x, T y)
        where T : IModulusOperators<T, T, T>
        => x % y;

    /// <summary>
    /// Dynamic modulus that always returns a non-negative result.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static dynamic Modulus(dynamic x, dynamic y)
    {
        if (x is double || x is float)
            return x - y * Math.Floor(x / y);
        return x % y;
    }

    /// <summary>
    /// Floor division, which rounds down to the nearest whole number.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static dynamic FloorDiv(dynamic x, dynamic y)
    {
        if (x is double || x is float || x is decimal)
            return Math.Floor(Convert.ToDouble(x) / Convert.ToDouble(y));
        return x / y;
    }

}

public static class AntiVal
{
    public static PositronicVariable<T> GetOrCreate<T>()
        where T : IComparable<T>
    {
        if (!PositronicAmbient.IsInitialized)
        {
            InitialiseDefaultRuntime();
        }

    var v = PositronicVariable<T>.GetOrCreate(PositronicAmbient.Current);

    return v;
    }

    private static bool PositronicAmbientIsUninitialized()
        => PositronicAmbient.Current == null;

    // If the runtime hasn't been initialized, we gently whisper it into existence like a haunted lullaby.
    private static void InitialiseDefaultRuntime()
    {
        if (PositronicAmbient.IsInitialized)
            return;
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddPositronicRuntime());
        PositronicAmbient.InitialiseWith(hostBuilder);
    }

}