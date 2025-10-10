using PositronicVariables.Runtime;
namespace PositronicVariables.Engine.Entropy
{
    /// <summary>
    /// The DefaultEntropyController is like the universe's mood ring, flipping between order and chaos with a dramatic flair.
    /// </summary>
    public class DefaultEntropyController : IEntropyController
    {
        private readonly IPositronicRuntime _runtime;
        public DefaultEntropyController(IPositronicRuntime runtime)
            => _runtime = runtime;

        public int Entropy => _runtime.Entropy;
        /// <summary>
        /// Winds the temporal crank backwards to appease the spiteful spirits of past simulations.
        /// </summary>
        public void Initialise() => _runtime.Entropy = -1;
        /// <summary>
        /// Flips the entropy switch, because even the universe needs to change its mind sometimes.
        /// </summary>
        public void Flip() => _runtime.Entropy = -_runtime.Entropy;
    }
}
