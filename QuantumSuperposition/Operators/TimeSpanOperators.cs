using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Because not every span of time feels the same, but math still applies.
    /// </summary>
    public class TimeSpanOperators : IQuantumOperators<TimeSpan>
    {
        public TimeSpan Add(TimeSpan a, TimeSpan b) => a + b;
        public TimeSpan Subtract(TimeSpan a, TimeSpan b) => a - b;
        public TimeSpan Multiply(TimeSpan a, TimeSpan b) => new TimeSpan(a.Ticks * b.Ticks);
        public TimeSpan Divide(TimeSpan a, TimeSpan b) => new TimeSpan(a.Ticks / b.Ticks);
        public TimeSpan Mod(TimeSpan a, TimeSpan b) => new TimeSpan(a.Ticks % b.Ticks);

        public bool GreaterThan(TimeSpan a, TimeSpan b) => a > b;
        public bool GreaterThanOrEqual(TimeSpan a, TimeSpan b) => a >= b;
        public bool LessThan(TimeSpan a, TimeSpan b) => a < b;
        public bool LessThanOrEqual(TimeSpan a, TimeSpan b) => a <= b;
        public bool Equal(TimeSpan a, TimeSpan b) => a == b;
        public bool NotEqual(TimeSpan a, TimeSpan b) => a != b;

        public bool IsAddCommutative => true;
    }
}
