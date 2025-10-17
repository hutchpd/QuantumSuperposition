using Microsoft.Extensions.DependencyInjection;
using PositronicVariables.Engine;
using PositronicVariables.Engine.Entropy;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Engine.Transponder;
using PositronicVariables.Runtime;
using PositronicVariables.Variables.Factory;
using System;

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

            _ = services.AddScoped<IImprobabilityEngine<T>>(sp =>
            {
                IPositronicRuntime runtime = sp.GetRequiredService<IPositronicRuntime>();

                ImprobabilityEngine<T> defaultEngine = new(
                    new DefaultEntropyController(runtime),
                    new RegretScribe<T>(),
                    new SubEthaOutputTransponder(runtime),
                    runtime,
                    new BureauOfTemporalRecords<T>()
                );

                ConvergenceEngineBuilder<T> builder = new();
                configure(builder);

                return builder.Build(defaultEngine);
            });

            return services;
        }
    }
}
