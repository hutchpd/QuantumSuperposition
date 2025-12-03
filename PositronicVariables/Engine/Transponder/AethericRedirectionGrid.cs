using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PositronicVariables.Attributes;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PositronicVariables.Engine.Transponder
{
    /// <summary>
    /// Close your eyes sweet one, momma is going to do some nasty things to the universe.
    /// </summary>
    internal static class AethericRedirectionGrid
    {
        internal static readonly TextWriter ReferenceUniverse;
        public static bool Initialised { get; } = true;

        private static int _hasRun;
        private static bool TryBeginRunOnce()
        {
            return Interlocked.Exchange(ref _hasRun, 1) == 0;
        }

        private static volatile bool s_EmittedOnce;

        public static StringWriter ImprobabilityDrive { get; private set; }

        private static bool PositronicAmbientIsUnInitialised()
        {
            return !PositronicAmbient.IsInitialized;
        }

        internal static volatile bool SuppressEndOfUniverseReading;
        internal static volatile bool AtTheRestaurant;

        private static void InitialiseDefaultRuntime()
        {
            // Do not touch PositronicAmbient.Current here; the getter throws when uninitialized.
            if (PositronicAmbient.IsInitialized)
            {
                return; // Someone already called InitialiseWith()
            }

            IHostBuilder hb = Host.CreateDefaultBuilder()
                .ConfigureServices(services => services.AddPositronicRuntime());
            PositronicAmbient.InitialiseWith(hb);
        }

        static AethericRedirectionGrid()
        {
            ReferenceUniverse = Console.Out;
            ImprobabilityDrive = new StringWriter();

            // Capture everything from the very beginning so parallel universes
            // never accidentally leak into the reference universe
            if (!ReferenceEquals(Console.Out, ImprobabilityDrive))
            {
                Console.SetOut(ImprobabilityDrive);
            }

            try
            {
                IPositronicRuntime rt = PositronicAmbient.Current;
                rt.Babelfish = ImprobabilityDrive;
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
                    // Only run attributed entry on exit if it hasn't already run
                    if (Interlocked.CompareExchange(ref _hasRun, 1, 1) == 0)
                    {
                        RunAttributedEntryPointForTests();
                    }
                }
                finally
                {
                    AtTheRestaurant = false;
                }

                // Emit only if not already emitted and buffer has content
                string buf = ImprobabilityDrive.ToString();
                bool hasContent = !string.IsNullOrEmpty(buf);
                bool shouldEmit = hasContent && !s_EmittedOnce;
                if (shouldEmit)
                {
                    Console.SetOut(ReferenceUniverse);
                    ReferenceUniverse.Write(buf);
                    ReferenceUniverse.Flush();
                    s_EmittedOnce = true;
                }
            };
        }

        /// <summary>
        /// Just for our tests. <see cref="ImprobabilityDrive"/>.
        /// </summary>
        public static void RunAttributedEntryPointForTests()
        {
            if (!TryBeginRunOnce())
            {
                return;
            }

            // Ensure ProcessExit can emit if no other flush occurs
            SuppressEndOfUniverseReading = false;

            // Ensure a runtime exists
            if (!PositronicAmbient.IsInitialized)
            {
                InitialiseDefaultRuntime();
            }

            // Find exactly one [PositronicEntry]
            System.Collections.Generic.List<MethodInfo> entryPoints = AppDomain.CurrentDomain
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
            {
                throw new InvalidOperationException("No method marked with [PositronicEntry] was found. Please annotate a static method to use as the convergence entry point.");
            }

            if (entryPoints.Count > 1)
            {
                string methodNames = string.Join(Environment.NewLine, entryPoints.Select(m => $" - {m.DeclaringType.FullName}.{m.Name}"));
                throw new InvalidOperationException($"Multiple methods marked with [PositronicEntry] were found:{Environment.NewLine}{methodNames}{Environment.NewLine}Please mark only one method with [PositronicEntry].");
            }

            MethodInfo entryPoint = entryPoints[0];
            IPositronicRuntime rt = PositronicAmbient.Current;

            // If no variables exist yet, do a minimal "probe run" at reverse time
            // to allow the entry point to register its first PositronicVariable<T>.
            if (!rt.Registry.Any())
            {
                int savedEntropy = rt.Entropy;
                rt.Entropy = -1;

                // Suppress *probe* output so only converged prints are surfaced.
                TextWriter prevOut = Console.Out;
                try
                {
                    Console.SetOut(AethericRedirectionGrid.ImprobabilityDrive);
                    ParameterInfo[] pProbe = entryPoint.GetParameters();
                    object[] probeArgs = pProbe.Length == 0 ? null : new object[] { Array.Empty<string>() };
                    _ = entryPoint.Invoke(null, probeArgs);
                }
                finally
                {
                    // Discard anything the probe wrote and restore console.
                    _ = AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();
                    Console.SetOut(prevOut);
                    rt.Entropy = savedEntropy;
                }
            }

            if (!rt.Registry.Any())
            {
                throw new InvalidOperationException("No PositronicVariable was registered. Cannot determine a generic type for convergence.");
            }

            // Determine generic T from the last registered positronic variable
            IPositronicVariable lastVariable = rt.Registry.Last();
            Type genericArg = lastVariable.GetType().GetGenericArguments()[0];

            // Call PositronicVariable<T>.RunConvergenceLoop(rt, () => entryPoint(), …)
            MethodInfo runMethod = typeof(PositronicVariable<>)
                .MakeGenericType(genericArg)
                .GetMethod("RunConvergenceLoop", BindingFlags.Public | BindingFlags.Static);

            _ = runMethod.Invoke(
                null,
                new object[]
                {
                    rt,
                    () =>
                    {
                        ParameterInfo[] p = entryPoint.GetParameters();
                        object[] epArgs = p.Length == 0 ? null : new object[] { Array.Empty<string>() };
                        _ = entryPoint.Invoke(null, epArgs);
                    },
                    true,
                    true,
                    false
                });

            // Emit once explicitly and mark as emitted
            string buf = ImprobabilityDrive.ToString();
            if (!string.IsNullOrEmpty(buf))
            {
                Console.SetOut(ReferenceUniverse);
                ReferenceUniverse.Write(buf);
                ReferenceUniverse.Flush();
                SuppressEndOfUniverseReading = true;
                s_EmittedOnce = true;
            }
        }
    }
}
