namespace QuantumSuperposition.NoiseProperties
{
    /// <summary>
    /// Simple T1/T2 relaxation model parameters (time constants).
    /// Interpretation is left to the noise layer (added in step 2).
    /// </summary>
    public readonly struct ThermalRelaxation : IEquatable<ThermalRelaxation>
    {
        public TimeSpan T1 { get; }
        public TimeSpan T2 { get; }

        public ThermalRelaxation(TimeSpan t1, TimeSpan t2)
        {
            if (t1 <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(t1), t1, "T1 must be > 0.");
            if (t2 <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(t2), t2, "T2 must be > 0.");

            T1 = t1;
            T2 = t2;
        }

        public bool Equals(ThermalRelaxation other) => T1.Equals(other.T1) && T2.Equals(other.T2);
        public override bool Equals(object? obj) => obj is ThermalRelaxation other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(T1, T2);
    }
}
