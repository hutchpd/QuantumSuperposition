namespace QuantumSuperposition.NoiseProperties
{
    /// <summary>
    /// 2x2 classical readout confusion matrix:
    /// [ P(meas 0 | actual 0)  P(meas 1 | actual 0) ]
    /// [ P(meas 0 | actual 1)  P(meas 1 | actual 1) ]
    /// </summary>
    public readonly struct ReadoutErrorMatrix : IEquatable<ReadoutErrorMatrix>
    {
        private const double RowSumTolerance = 1e-12;

        public double P00 { get; }
        public double P01 { get; }
        public double P10 { get; }
        public double P11 { get; }

        public static ReadoutErrorMatrix Identity { get; } = new(1.0, 0.0, 0.0, 1.0);

        public ReadoutErrorMatrix(double p00, double p01, double p10, double p11)
        {
            ValidateProbability(p00, nameof(p00));
            ValidateProbability(p01, nameof(p01));
            ValidateProbability(p10, nameof(p10));
            ValidateProbability(p11, nameof(p11));

            double row0 = p00 + p01;
            double row1 = p10 + p11;

            if (Math.Abs(row0 - 1.0) > RowSumTolerance)
                throw new ArgumentException($"Row 0 must sum to 1.0. Actual={row0:R}", nameof(p01));

            if (Math.Abs(row1 - 1.0) > RowSumTolerance)
                throw new ArgumentException($"Row 1 must sum to 1.0. Actual={row1:R}", nameof(p11));

            P00 = p00;
            P01 = p01;
            P10 = p10;
            P11 = p11;
        }

        /// <summary>
        /// Convenience: specify flip probabilities only.
        /// p01 = P(meas 1 | actual 0), p10 = P(meas 0 | actual 1)
        /// </summary>
        public static ReadoutErrorMatrix FromFlipProbabilities(double p01, double p10)
        {
            ValidateProbability(p01, nameof(p01));
            ValidateProbability(p10, nameof(p10));

            return new ReadoutErrorMatrix(
                p00: 1.0 - p01,
                p01: p01,
                p10: p10,
                p11: 1.0 - p10
            );
        }

        public double this[int actual, int measured] => (actual, measured) switch
        {
            (0, 0) => P00,
            (0, 1) => P01,
            (1, 0) => P10,
            (1, 1) => P11,
            _ => throw new ArgumentOutOfRangeException(nameof(actual), "Indices must be 0 or 1.")
        };

        public double[,] ToArray() => new[,]
        {
            { P00, P01 },
            { P10, P11 }
        };

        public bool Equals(ReadoutErrorMatrix other) =>
            P00.Equals(other.P00) && P01.Equals(other.P01) && P10.Equals(other.P10) && P11.Equals(other.P11);

        public override bool Equals(object? obj) => obj is ReadoutErrorMatrix other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(P00, P01, P10, P11);

        private static void ValidateProbability(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(paramName, value, "Probability must be finite.");

            if (value < 0.0 || value > 1.0)
                throw new ArgumentOutOfRangeException(paramName, value, "Probability must be in [0, 1].");
        }
    }
}
