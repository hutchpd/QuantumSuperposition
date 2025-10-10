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
