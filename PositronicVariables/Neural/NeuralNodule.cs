using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Neural
{
    /// <summary>
    /// A quantum-aware neuron that collects Positronic inputs,
    /// applies an activation function, and fires an output into an alternate future.
    /// </summary>
    public class NeuralNodule<T> where T : struct, IComparable<T>
    {
        public List<PositronicVariable<T>> Inputs { get; } = new();
        public PositronicVariable<T> Output { get; }
        public Func<IEnumerable<T>, QuBit<T>> ActivationFunction { get; set; }

        private static readonly object s_initLock = new();
        /// <summary>
        /// Ensures that the ambient Positronic runtime is initialized.
        /// </summary>
        /// <returns></returns>
        private static IPositronicRuntime EnsureAmbientRuntime()
        {
            if (!PositronicAmbient.IsInitialized)
            {
                lock (s_initLock)
                {
                    if (!PositronicAmbient.IsInitialized)
                    {
                        var hb = Host.CreateDefaultBuilder()
                                     .ConfigureServices(s => s.AddPositronicRuntime());
                        PositronicAmbient.InitialiseWith(hb);
                    }
                }
            }
            return PositronicAmbient.Current;
        }


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
}
