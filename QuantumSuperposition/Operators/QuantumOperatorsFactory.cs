using QuantumSuperposition.Core;
using System.Collections.Concurrent;
using System.Numerics;

namespace QuantumSuperposition.Operators
{
    public static class QuantumOperatorsFactory
    {
        private static readonly ConcurrentDictionary<Type, object> _cache = new();

        static QuantumOperatorsFactory()
        {
            Register<int>(() => new IntOperators());
            Register<Complex>(() => new ComplexOperators());
            Register<bool>(() => new BooleanOperators());
            Register<string>(() => new StringOperators());
            Register<float>(() => new FloatOperators());
            Register<double>(() => new DoubleOperators());
            Register<decimal>(() => new DecimalOperators());
            Register<byte>(() => new ByteOperators());
            Register<sbyte>(() => new SByteOperators());
            Register<short>(() => new ShortOperators());
            Register<ushort>(() => new UShortOperators());
            Register<uint>(() => new UIntOperators());
            Register<long>(() => new LongOperators());
            Register<ulong>(() => new ULongOperators());
            Register<char>(() => new CharOperators());
            Register<DateTime>(() => new DateTimeOperators());
            Register<TimeSpan>(() => new TimeSpanOperators());
            Register<Guid>(() => new GuidOperators());
            Register<Uri>(() => new UriOperators());
            Register<Version>(() => new VersionOperators());

        }

        public static void Register<T>(Func<IQuantumOperators<T>> factory)
        {
            _cache[typeof(T)] = factory;
        }

        public static IQuantumOperators<T> GetOperators<T>()
        {
            if (_cache.TryGetValue(typeof(T), out object? factoryObj))
            {
                Func<IQuantumOperators<T>> factory = (Func<IQuantumOperators<T>>)factoryObj;
                return factory();
            }

            // fallback: dynamically create ExpressionOperators<T>
            return new ExpressionOperators<T>();
        }
    }
}
