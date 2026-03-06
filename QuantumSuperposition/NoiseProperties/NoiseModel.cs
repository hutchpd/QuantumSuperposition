namespace QuantumSuperposition.NoiseProperties
{
    /// <summary>
    /// Defines how ideal operations are perturbed by noise.
    /// This is intentionally passive data; the injection mechanism is added in step 2.
    /// </summary>
    public sealed class NoiseModel
    {
        private double _singleQubitErrorRate;
        private double _twoQubitErrorRate;

        /// <summary>
        /// Depolarizing probability applied to single-qubit gates.
        /// </summary>
        public double SingleQubitErrorRate
        {
            get => _singleQubitErrorRate;
            init
            {
                ValidateProbability(value, nameof(SingleQubitErrorRate));
                _singleQubitErrorRate = value;
            }
        }

        /// <summary>
        /// Noise probability applied to two-qubit gates (e.g., CNOT).
        /// </summary>
        public double TwoQubitErrorRate
        {
            get => _twoQubitErrorRate;
            init
            {
                ValidateProbability(value, nameof(TwoQubitErrorRate));
                _twoQubitErrorRate = value;
            }
        }

        /// <summary>
        /// 2x2 readout confusion matrix.
        /// </summary>
        public ReadoutErrorMatrix ReadoutErrorMatrix { get; init; } = ReadoutErrorMatrix.Identity;

        /// <summary>
        /// Optional T1/T2 relaxation parameters. Null means disabled.
        /// </summary>
        public ThermalRelaxation? ThermalRelaxation { get; init; }

        /// <summary>
        /// A convenient "no noise" model.
        /// </summary>
        public static NoiseModel None { get; } = new();

        public NoiseModel()
        {
        }

        public NoiseModel(
            double singleQubitErrorRate,
            double twoQubitErrorRate,
            ReadoutErrorMatrix? readoutErrorMatrix = null,
            ThermalRelaxation? thermalRelaxation = null)
        {
            SingleQubitErrorRate = singleQubitErrorRate;
            TwoQubitErrorRate = twoQubitErrorRate;
            ReadoutErrorMatrix = readoutErrorMatrix ?? ReadoutErrorMatrix.Identity;
            ThermalRelaxation = thermalRelaxation;
        }

        private static void ValidateProbability(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(paramName, value, "Probability must be finite.");

            if (value < 0.0 || value > 1.0)
                throw new ArgumentOutOfRangeException(paramName, value, "Probability must be in [0, 1].");
        }
    }
}
