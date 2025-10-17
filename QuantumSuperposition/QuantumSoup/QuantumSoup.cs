using QuantumSuperposition.Core;
using QuantumSuperposition.Operators;
using System.Numerics;

namespace QuantumSuperposition.QuantumSoup
{


    /// <summary>
    /// Minestrone of quantum states, a soup of possibilities, a cauldron of uncertainty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class QuantumSoup<T> : IQuantumObservable<T>
    {
        protected Dictionary<T, Complex>? _weights;
        protected bool _isActuallyCollapsed;
        protected T? _collapsedValue;
        protected bool _mockCollapseEnabled;
        protected T? _mockCollapseValue;
        protected Guid? _collapseHistoryId;
        protected int? _lastCollapseSeed;
        protected virtual Func<T, bool> _valueValidator => _ => true;
        protected QuantumStateType _eType;
        protected IQuantumOperators<T> _ops = QuantumOperatorsFactory.GetOperators<T>();
        protected bool _weightsAreNormalised;
        protected static readonly double _tolerance = 1e-9;
        private bool _isFrozen => _isActuallyCollapsed;

        public abstract IReadOnlyCollection<T> States { get; }

        public bool IsWeighted => _weights != null;
        public bool IsActuallyCollapsed => _isActuallyCollapsed && _eType == QuantumStateType.CollapsedResult;
        public Guid? LastCollapseHistoryId => _collapseHistoryId;
        public int? LastCollapseSeed => _lastCollapseSeed;
        public QuantumStateType GetCurrentType()
        {
            return _eType;
        }

        public bool IsLocked { get; private set; } = false;

        public void Lock()
        {
            IsLocked = true;
        }

        public void Unlock()
        {
            IsLocked = false;
        }

        public void SetType(QuantumStateType t)
        {
            EnsureMutable();
            _eType = t;
        }


        /// <summary>
        /// Guard to ensure mutable operations are only allowed when not collapsed.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void EnsureMutable()
        {
            // _isFrozen checks for collapse; add _isLocked to enforce locking too.
            if (_isFrozen || IsLocked)
            {
                throw new InvalidOperationException("Cannot modify a locked or collapsed QuBit.");
            }
        }

        /// <summary>
        /// Normalises the amplitudes of the QuBit so that the sum of their squared magnitudes equals 1.
        /// </summary>
        public void NormaliseWeights()
        {
            if (_weights == null || _weightsAreNormalised)
            {
                return;
            }

            double totalProbability = _weights.Values.Select(a => a.Magnitude * a.Magnitude).Sum();
            if (totalProbability <= double.Epsilon)
            {
                return;
            }

            double normFactor = Math.Sqrt(totalProbability);
            foreach (T? k in _weights.Keys.ToList())
            {
                _weights[k] /= normFactor;
            }
            _weightsAreNormalised = true;
        }


        /// <summary>
        /// Reality check: converts quantum indecision into a final verdict.
        /// Basically forces the whole wavefunction to agree like it’s group therapy for particles.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool EvaluateAll()
        {
            SetType(QuantumStateType.SuperpositionAll);

            // More descriptive exception if empty
            return !States.Any()
                ? throw new InvalidOperationException(
                    $"No states to evaluate. IsCollapsed={_isActuallyCollapsed}, StateCount={States.Count}, Type={_eType}."
                )
                : States.All(state => !EqualityComparer<T>.Default.Equals(state, default));
        }

        public virtual IEnumerable<(T value, Complex weight)> ToWeightedValues()
        {
            if (_weights == null)
            {
                foreach (T? v in States.Distinct())
                {
                    yield return (v, Complex.One);
                }
            }
            else
            {
                foreach (KeyValuePair<T, Complex> kvp in _weights)
                {
                    yield return (kvp.Key, kvp.Value);
                }
            }
        }

        // Does a little dance, rolls a quantum die, picks a value.
        // May cause minor existential dread or major debugging regrets.
        public virtual T SampleWeighted(Random? rng = null)
        {
            rng ??= Random.Shared;
            if (!States.Any())
            {
                throw new InvalidOperationException("No states available.");
            }

            if (!IsWeighted)
            {
                return States.First();
            }

            NormaliseWeights();
            Dictionary<T, double> probabilities = _weights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Magnitude * kvp.Value.Magnitude);
            double totalProb = probabilities.Values.Sum();
            if (totalProb <= 1e-15)
            {
                return States.First();
            }

            double roll = rng.NextDouble() * totalProb;
            double cumulative = 0.0;
            foreach ((T key, double prob) in probabilities)
            {
                cumulative += prob;
                if (roll <= cumulative)
                {
                    return key;
                }
            }
            return probabilities.Last().Key;
        }

        public virtual QuantumSoup<T> WithWeights(Dictionary<T, Complex> weights, bool autoNormalise = false)
        {
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            Dictionary<T, Complex> filtered = [];
            foreach (KeyValuePair<T, Complex> kvp in weights)
            {
                if (States.Contains(kvp.Key))
                {
                    filtered[kvp.Key] = kvp.Value;
                }
            }
            QuantumSoup<T> clone = Clone();
            clone._weights = filtered;
            clone._weightsAreNormalised = false;
            if (autoNormalise)
            {
                clone.NormaliseWeights();
            }

            return clone;
        }

        public virtual T CollapseWeighted()
        {
            if (!States.Any())
            {
                throw new InvalidOperationException("No states available for collapse.");
            }

            if (!IsWeighted)
            {
                return States.First();
            }

            T? key = _weights.MaxBy(x => x.Value.Magnitude)!.Key;
            return key;
        }

        protected bool AllWeightsEqual(Dictionary<T, Complex> dict)
        {
            if (dict.Count <= 1)
            {
                return true;
            }

            Complex first = dict.Values.First();
            return dict.Values.Skip(1).All(w => Complex.Abs(w - first) < 1e-14);
        }

        protected bool AllWeightsProbablyEqual(Dictionary<T, Complex> dict)
        {
            if (dict.Count <= 1)
            {
                return true;
            }

            double firstProb = dict.Values.First().Magnitude * dict.Values.First().Magnitude;
            return dict.Values.Skip(1).All(w => Math.Abs((w.Magnitude * w.Magnitude) - firstProb) < 1e-14);
        }

        /// <summary>
        /// Compares with the grace of a therapist and the precision of a passive-aggressive spreadsheet.
        /// Compare Squared Magnitudes (Probabilistic Equality)
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            QuantumSoup<T> other = (QuantumSoup<T>)obj;
            HashSet<T> mySet = States.Distinct().ToHashSet();
            HashSet<T> otherSet = other.States.Distinct().ToHashSet();
            if (!mySet.SetEquals(otherSet))
            {
                return false;
            }

            if (!IsWeighted && !other.IsWeighted)
            {
                return true;
            }

            foreach (T? s in mySet)
            {
                double p1 = 1.0, p2 = 1.0;
                if (_weights != null && _weights.TryGetValue(s, out Complex amp1))
                {
                    p1 = amp1.Magnitude * amp1.Magnitude;
                }

                if (other._weights != null && other._weights.TryGetValue(s, out Complex amp2))
                {
                    p2 = amp2.Magnitude * amp2.Magnitude;
                }

                if (Math.Abs(p1 - p2) > _tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks to see if two things are really the same, or just pretending to be.
        /// Compare Complex Amplitudes (Strict State Equality, as opposed to Probabilistic Equality)
        /// </summary>
        public virtual bool StrictlyEquals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            QuantumSoup<T> other = (QuantumSoup<T>)obj;
            HashSet<T> mySet = States.Distinct().ToHashSet();
            HashSet<T> otherSet = other.States.Distinct().ToHashSet();
            if (!mySet.SetEquals(otherSet))
            {
                return false;
            }

            if (!IsWeighted && !other.IsWeighted)
            {
                return true;
            }

            foreach (T? s in mySet)
            {
                Complex a1 = Complex.One, a2 = Complex.One;
                if (_weights != null && _weights.TryGetValue(s, out Complex w1))
                {
                    a1 = w1;
                }

                if (other._weights != null && other._weights.TryGetValue(s, out Complex w2))
                {
                    a2 = w2;
                }

                if ((a1 - a2).Magnitude > _tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (T? s in States.Distinct().OrderBy(x => x))
                {
                    hash = (hash * 23) + (s?.GetHashCode() ?? 0);
                    if (IsWeighted && _weights != null && _weights.TryGetValue(s, out Complex amp))
                    {
                        double real = Math.Round(amp.Real, 12);
                        double imag = Math.Round(amp.Imaginary, 12);
                        long realBits = BitConverter.DoubleToInt64Bits(real);
                        long imagBits = BitConverter.DoubleToInt64Bits(imag);
                        hash = (hash * 23) + realBits.GetHashCode();
                        hash = (hash * 23) + imagBits.GetHashCode();
                    }
                }
                return hash;
            }
        }

        public virtual string WeightSummary()
        {
            if (!IsWeighted)
            {
                return "Weighted: false";
            }

            double totalProbability = _weights.Values.Sum(a => a.Magnitude * a.Magnitude);
            double maxP = _weights.Values.Max(a => a.Magnitude * a.Magnitude);
            double minP = _weights.Values.Min(a => a.Magnitude * a.Magnitude);
            return $"Weighted: true, Sum(|amp|²): {totalProbability}, Max(|amp|²): {maxP}, Min(|amp|²): {minP}";
        }

        // I promise to pick you up from the airport, but I might never buy the car.
        /// <summary>
        /// The quantum clononing paradox: perfect cloning is actually forbidden, but we can still make copies.
        /// </summary>
        /// <returns></returns>
        public abstract QuantumSoup<T> Clone();

        public abstract T Observe(Random? rng = null);


        /// <summary>
        /// Exposes the interface for Qubit Observables
        /// </summary>
        public interface IQuantumObservable<T>

        {
            T Observe(Random? rng = null);
            T CollapseWeighted();
            T SampleWeighted(Random? rng = null);
            IEnumerable<(T value, Complex weight)> ToWeightedValues();
        }
    }
}
