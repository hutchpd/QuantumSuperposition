using PositronicVariables.Variables.Factory;
using System;
using System.IO;

namespace PositronicVariables.Runtime
{
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
}
