using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PositronicVariables.Runtime
{

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
}
