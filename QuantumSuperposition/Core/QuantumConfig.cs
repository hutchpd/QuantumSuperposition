namespace QuantumSuperposition.Core
{
    public static class QuantumConfig
    {
        /// <summary>
        /// Set to true to disallow collapse if the resulting value equals default(T).
        /// </summary>
        public static bool ForbidDefaultOnCollapse { get; set; } = true;

        /// <summary>
        /// Set to true to allow non-observational arithmetic.
        /// </summary>
        public static bool EnableNonObservationalArithmetic { get; set; } = true;

        /// <summary>
        /// Set to true to enable commutative cache for faster operations.
        /// </summary>
        public static bool EnableCommutativeCache { get; set; } = true;

        /// <summary>
        /// If true, newly constructed QuantumGate instances validate approximate unitarity (M * M† ≈ I).
        /// </summary>
        public static bool ValidateUnitarity { get; set; } = true;

        /// <summary>
        /// Tolerance used for unitarity validation when <see cref="ValidateUnitarity"/> is enabled.
        /// </summary>
        public static double UnitarityTolerance { get; set; } = 1e-9;
    }
}
