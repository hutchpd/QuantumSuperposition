using System;
using System.Numerics;
using QuantumSuperposition.Operators;

namespace QuantumSuperposition.QuantumSoup
{
    /// <summary>
    /// Specialised qubit constrained to the computational basis {|0>, |1>} with
    /// constructors for direct amplitude initialisation and Bloch sphere parameters.
    /// Internally uses QuBit<int> with weights { 0 -> alpha, 1 -> beta }.
    /// </summary>
    public sealed class PhysicsQubit : QuBit<int>
    {
        private static readonly IntOperators _intOps = new();

        /// <summary>
        /// Creates a PhysicsQubit with amplitudes alpha for |0> and beta for |1>.
        /// The amplitudes are normalised using the existing weight logic.
        /// </summary>
        public PhysicsQubit(Complex alpha, Complex beta)
            : base(new (int value, Complex weight)[] { (0, alpha), (1, beta) }, _intOps)
        {
            // Ensure amplitudes are normalised regardless of input
            NormaliseWeights();
        }

        /// <summary>
        /// Creates a PhysicsQubit from real/imaginary components of alpha and beta.
        /// </summary>
        public PhysicsQubit(double aRe, double aIm, double bRe, double bIm)
            : this(new Complex(aRe, aIm), new Complex(bRe, bIm))
        { }

        /// <summary>
        /// Creates a PhysicsQubit from Bloch sphere angles.
        /// |?> = cos(?/2)|0> + e^{i?} sin(?/2)|1>
        /// </summary>
        public PhysicsQubit(double theta, double phi)
            : this(
                  // alpha = cos(theta/2)
                  new Complex(Math.Cos(theta / 2.0), 0.0),
                  // beta  = e^{i phi} * sin(theta/2)
                  Complex.FromPolarCoordinates(Math.Sin(theta / 2.0), phi))
        { }

        /// <summary>
        /// Shortcut for |0> state.
        /// </summary>
        public static PhysicsQubit Zero => new PhysicsQubit(Complex.One, Complex.Zero);

        /// <summary>
        /// Shortcut for |1> state.
        /// </summary>
        public static PhysicsQubit One => new PhysicsQubit(Complex.Zero, Complex.One);
    }
}
