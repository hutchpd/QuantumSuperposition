using PositronicVariables.Engine.Transponder;
using PositronicVariables.Variables.Factory;
using System;
using System.IO;

namespace PositronicVariables.Runtime
{
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


        public DefaultPositronicRuntime()
        {
            Babelfish = AethericRedirectionGrid.ImprobabilityDrive;

            // Create a minimal fake IServiceProvider
            FallbackServiceProvider provider = new(this);
            ScopedPositronicVariableFactory scoped = new(provider);

            Factory = scoped;
            Variables = scoped;
            Registry = scoped;

            Reset();
            PositronicAmbient.Current = this;
        }

        private class FallbackServiceProvider(IPositronicRuntime runtime) : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return serviceType == typeof(IPositronicRuntime)
                    ? (object)runtime
                    : throw new InvalidOperationException($"Service {serviceType.Name} not available in fallback provider.");
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
        public void FireAllConverged()
        {
            OnAllConverged?.Invoke();
        }
    }
}
