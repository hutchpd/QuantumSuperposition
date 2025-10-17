using Microsoft.Extensions.DependencyInjection;
using PositronicVariables.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PositronicVariables.Variables.Factory
{
    public class ScopedPositronicVariableFactory : IPositronicVariableFactory, IPositronicVariableRegistry
    {
        private readonly IServiceProvider _provider;
        private IPositronicRuntime Runtime => _provider.GetRequiredService<IPositronicRuntime>();

        private readonly Dictionary<(Type, string), IPositronicVariable> _multiverseIndex
            = [];

        public ScopedPositronicVariableFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public PositronicVariable<T> GetOrCreate<T>(string id, T initialValue)
            where T : IComparable<T>
        {
            (Type, string id) key = (typeof(T), id);
            if (_multiverseIndex.TryGetValue(key, out IPositronicVariable existing))
            {
                return (PositronicVariable<T>)existing;
            }

            PositronicVariable<T> created = new(initialValue, Runtime);
            _multiverseIndex[key] = created;
            return created;
        }

        public PositronicVariable<T> GetOrCreate<T>(string id)
            where T : IComparable<T>
        {
            (Type, string id) key = (typeof(T), id);
            if (_multiverseIndex.TryGetValue(key, out IPositronicVariable existing))
            {
                return (PositronicVariable<T>)existing;
            }

            PositronicVariable<T> created = new(default, Runtime);
            _multiverseIndex[key] = created;
            return created;
        }

        public PositronicVariable<T> GetOrCreate<T>(T initialValue)
            where T : IComparable<T>
        {
            (Type, string) key = (typeof(T), "default");
            if (_multiverseIndex.TryGetValue(key, out IPositronicVariable existing))
            {
                return (PositronicVariable<T>)existing;
            }

            PositronicVariable<T> created = new(initialValue, Runtime);
            _multiverseIndex[key] = created;
            return created;
        }

        void IPositronicVariableRegistry.Add(IPositronicVariable v)
        {
            // pry T out of PositronicVariable<T> with reflection—a technique so eldritch even your toaster fears it
            Type t = v.GetType().GetGenericArguments()[0];
            (Type t, string) key = (t, Guid.NewGuid().ToString());
            _multiverseIndex[key] = v;
        }

        void IPositronicVariableRegistry.Clear()
        {
            _multiverseIndex.Clear();
        }

        public IEnumerator<IPositronicVariable> GetEnumerator()
        {
            return _multiverseIndex.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
