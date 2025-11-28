using Microsoft.Extensions.DependencyInjection;
using PositronicVariables.Engine;
using PositronicVariables.Engine.Entropy;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Engine.Transponder;
using PositronicVariables.Runtime;
using PositronicVariables.Variables.Factory;
using System;
using PositronicVariables.Engine.Coordinator;

namespace PositronicVariables.DependencyInjection
{
    public static class PositronicServiceCollectionExtensions
    {
        public static IServiceCollection AddPositronicRuntime(this IServiceCollection services)
        {
            return services.AddPositronicRuntime<double>(builder => { });
        }

        public static IServiceCollection AddPositronicRuntime<T>(
            this IServiceCollection services,
            Action<ConvergenceEngineBuilder<T>> configure)
            where T : IComparable<T>
        {
            _ = services.AddScoped<ScopedPositronicVariableFactory>();
            _ = services.AddScoped<IPositronicVariableFactory>(sp => sp.GetRequiredService<ScopedPositronicVariableFactory>());
            _ = services.AddScoped<IPositronicVariableRegistry>(sp => sp.GetRequiredService<ScopedPositronicVariableFactory>());
            _ = services.AddScoped<IPositronicRuntime, DefaultPositronicRuntime>();

            // Coordinator used by engine and tests
            _ = services.AddSingleton<ConvergenceCoordinator>();

            _ = services.AddScoped<IImprobabilityEngine<T>>(sp =>
            {
                IPositronicRuntime runtime = sp.GetRequiredService<IPositronicRuntime>();
                var coordinator = sp.GetRequiredService<ConvergenceCoordinator>();

                ImprobabilityEngine<T> defaultEngine = new(
                    new DefaultEntropyController(runtime),
                    new RegretScribe<T>(),
                    new SubEthaOutputTransponder(runtime),
                    runtime,
                    new BureauOfTemporalRecords<T>(),
                    coordinator
                );

                ConvergenceEngineBuilder<T> builder = new();
                configure(builder);

                return builder.Build(defaultEngine);
            });

            return services;
        }
    }
}
