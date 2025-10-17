using QuantumSuperposition.Core;
using QuantumSuperposition.Operators;
using System.Numerics;

namespace QuantumSuperposition.QuantumSoup
{

    /// <summary>
    /// Eigenstates<T> preserves original input keys in a dictionary Key->Value,
    /// because sometimes you just want your quantum states to stop changing the subject.
    /// </summary>
    public class Eigenstates<T> : QuantumSoup<T>, IEquatable<Eigenstates<T>>
    {
        protected override Func<T, bool> _valueValidator => v => !EqualityComparer<T>.Default.Equals(v, default);

        private Dictionary<T, T> _qDict;

        #region Constructors
        // Constructors that let you map values to themselves or to other values.
        // Also doubles as a safe space for anyone scared of full superposition commitment.

        public Eigenstates(IEnumerable<T> Items, IQuantumOperators<T> ops)
        {
            // same weight, then the waveform should collapse to the same value
            if (Items == null)
            {
                throw new ArgumentNullException(nameof(Items));
            }

            _ops = ops ?? throw new ArgumentNullException(nameof(ops));
            _qDict = Items.Distinct().ToDictionary(x => x, x => x);
        }

        public Eigenstates(IEnumerable<T> Items)
            : this(Items, QuantumOperatorsFactory.GetOperators<T>())
        {
        }

        public Eigenstates(IEnumerable<T> inputValues, Func<T, T> projection, IQuantumOperators<T> ops)
        {
            if (inputValues == null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            _ops = ops ?? throw new ArgumentNullException(nameof(ops));

            _qDict = inputValues.ToDictionary(x => x, projection);
        }

        public Eigenstates(IEnumerable<T> inputValues, Func<T, T> projection)
            : this(inputValues, projection, QuantumOperatorsFactory.GetOperators<T>())
        {
        }

        public Eigenstates(IEnumerable<(T value, Complex weight)> weightedItems, IQuantumOperators<T> ops)
        {
            if (weightedItems == null)
            {
                throw new ArgumentNullException(nameof(weightedItems));
            }

            _ops = ops ?? throw new ArgumentNullException(nameof(ops));

            Dictionary<T, Complex> dict = [];
            foreach ((T val, Complex w) in weightedItems)
            {
                if (!dict.ContainsKey(val))
                {
                    dict[val] = 0.0;
                }

                dict[val] += w;
            }

            _qDict = dict.Keys.ToDictionary(x => x, x => x);
            _weights = dict;
        }

        public Eigenstates(IEnumerable<(T value, Complex weight)> weightedItems)
            : this(weightedItems, QuantumOperatorsFactory.GetOperators<T>())
        {
        }

        internal Eigenstates(Dictionary<T, T> dict, IQuantumOperators<T> ops)
        {
            _ops = ops;
            _qDict = dict;
        }

        #endregion

        #region Filtering Mode
        // Toggle between "any of these are fine" and "they all better agree".
        // Like relationship statuses but for probability distributions.

        public Eigenstates<T> Any() { _eType = QuantumStateType.SuperpositionAny; return this; }
        public Eigenstates<T> All() { _eType = QuantumStateType.SuperpositionAll; return this; }

        #endregion

        #region Arithmetic Operations

        // When you want to do math to your eigenstates and still feel like a functional adult.
        // Avoids full combinatorial meltdown, unlike your weekend.
        // Also, it’s like a quantum blender: it mixes everything together but still keeps the labels.
        // Because who needs clarity when you can have chaos?

        /// <summary>
        /// Performs the specified operation on two Eigenstates<T> instances or an Eigenstates<T> and a scalar.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        private Eigenstates<T> Do_oper_type(Eigenstates<T> a, Eigenstates<T> b, Func<T, T, T> op)
        {
            // Use a dictionary to accumulate combined weights keyed by the computed result value.
            Dictionary<T, Complex> newWeights = [];

            // Loop through all weighted values from both operands.
            foreach ((T valA, Complex wA) in a.ToMappedWeightedValues())
            {
                foreach ((T valB, Complex wB) in b.ToMappedWeightedValues())
                {
                    T? newValue = op(valA, valB);
                    Complex combinedWeight = wA * wB;
                    newWeights[newValue] = newWeights.TryGetValue(newValue, out Complex existing)
                        ? existing + combinedWeight
                        : combinedWeight;
                }
            }

            // Create a new key->value mapping where each key maps to itself.
            Dictionary<T, T> newDict = newWeights.Keys.ToDictionary(x => x, x => x);
            Eigenstates<T> e = new(newDict, a._ops)
            {
                _weights = newWeights
            };
            return e;
        }

        /// <summary>
        /// Performs the specified operation on an Eigenstates<T> and a scalar.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        private Eigenstates<T> Do_oper_type(Eigenstates<T> a, T b, Func<T, T, T> op)
        {
            Dictionary<T, T> result = [];
            Dictionary<T, Complex>? newWeights = null;

            if (a._weights != null)
            {
                newWeights = [];
            }

            foreach (KeyValuePair<T, T> kvp in a._qDict)
            {
                T? newVal = op(kvp.Value, b);
                result[kvp.Key] = newVal;  // retain original key, update value

                if (newWeights != null)
                {
                    Complex wA = a._weights != null && a._weights.TryGetValue(kvp.Key, out Complex aw) ? aw : 1.0;
                    newWeights[kvp.Key] = wA;  // use original key
                }
            }

            Eigenstates<T> e = new(result, a._ops)
            {
                _weights = newWeights
            };
            return e;
        }

        /// <summary>
        /// Performs the specified operation on a scalar and an Eigenstates<T>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        private Eigenstates<T> Do_oper_type(T a, Eigenstates<T> b, Func<T, T, T> op)
        {
            Dictionary<T, T> result = [];
            Dictionary<T, Complex>? newWeights = null;

            if (b._weights != null)
            {
                newWeights = [];
            }

            foreach (KeyValuePair<T, T> kvp in b._qDict)
            {
                T? newVal = op(a, kvp.Value);
                result[kvp.Key] = newVal;  // retain original key, update value

                if (newWeights != null)
                {
                    Complex wB = b._weights != null && b._weights.TryGetValue(kvp.Key, out Complex bw) ? bw : 1.0;
                    newWeights[kvp.Key] = wB;  // use original key
                }
            }

            Eigenstates<T> e = new(result, b._ops)
            {
                _weights = newWeights
            };
            return e;
        }


        public static Eigenstates<T> operator %(T a, Eigenstates<T> b)
        {
            return b.Do_oper_type(a, b, b._ops.Mod);
        }

        public static Eigenstates<T> operator %(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_oper_type(a, b, a._ops.Mod);
        }

        public static Eigenstates<T> operator %(Eigenstates<T> a, T b)
        {
            return a.Do_oper_type(a, b, a._ops.Mod);
        }

        public static Eigenstates<T> operator +(T a, Eigenstates<T> b)
        {
            return b.Do_oper_type(a, b, b._ops.Add);
        }

        public static Eigenstates<T> operator +(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_oper_type(a, b, a._ops.Add);
        }

        public static Eigenstates<T> operator +(Eigenstates<T> a, T b)
        {
            return a.Do_oper_type(a, b, a._ops.Add);
        }

        public static Eigenstates<T> operator -(T a, Eigenstates<T> b)
        {
            return b.Do_oper_type(a, b, b._ops.Subtract);
        }

        public static Eigenstates<T> operator -(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_oper_type(a, b, a._ops.Subtract);
        }

        public static Eigenstates<T> operator -(Eigenstates<T> a, T b)
        {
            return a.Do_oper_type(a, b, a._ops.Subtract);
        }

        public static Eigenstates<T> operator *(T a, Eigenstates<T> b)
        {
            return b.Do_oper_type(a, b, b._ops.Multiply);
        }

        public static Eigenstates<T> operator *(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_oper_type(a, b, a._ops.Multiply);
        }

        public static Eigenstates<T> operator *(Eigenstates<T> a, T b)
        {
            return a.Do_oper_type(a, b, a._ops.Multiply);
        }

        public static Eigenstates<T> operator /(T a, Eigenstates<T> b)
        {
            return b.Do_oper_type(a, b, b._ops.Divide);
        }

        public static Eigenstates<T> operator /(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_oper_type(a, b, a._ops.Divide);
        }

        public static Eigenstates<T> operator /(Eigenstates<T> a, T b)
        {
            return a.Do_oper_type(a, b, a._ops.Divide);
        }

        #endregion

        #region Filtering (Comparison) Operators
        // Let’s you ask "which of you is actually greater than 5?" in a very judgmental way.
        // Returns a trimmed-down existential crisis with weights.

        private Eigenstates<T> Do_condition_type(Func<T, T, bool> condition, T value)
        {
            Dictionary<T, T> result = [];
            Dictionary<T, Complex>? newWeights = null;

            if (_weights != null)
            {
                newWeights = [];
            }

            foreach (KeyValuePair<T, T> kvp in _qDict)
            {
                if (condition(kvp.Value, value))
                {
                    result[kvp.Key] = kvp.Value;
                    if (newWeights != null)
                    {
                        Complex wA = _weights != null && _weights.TryGetValue(kvp.Key, out Complex aw) ? aw : 1.0;
                        newWeights[kvp.Key] = wA;
                    }
                }
            }
            Eigenstates<T> e = new(result, _ops)
            {
                _weights = newWeights
            };
            return e;
        }

        private Eigenstates<T> Do_condition_type(Func<T, T, bool> condition, Eigenstates<T> other)
        {
            Dictionary<T, T> result = [];
            Dictionary<T, Complex>? newWeights = null;

            if (_weights != null || other._weights != null)
            {
                newWeights = [];
            }

            foreach (KeyValuePair<T, T> kvp in _qDict)
            {
                foreach (KeyValuePair<T, T> kvp2 in other._qDict)
                {
                    if (condition(kvp.Value, kvp2.Value))
                    {
                        result[kvp.Key] = kvp.Value;
                        if (newWeights != null)
                        {
                            Complex wA = _weights != null && _weights.TryGetValue(kvp.Key, out Complex aw) ? aw : 1.0;
                            Complex wB = other._weights != null && other._weights.TryGetValue(kvp2.Key, out Complex bw) ? bw : 1.0;
                            newWeights[kvp.Key] = wA * wB;
                        }
                        break; // break on first match
                    }
                }
            }

            Eigenstates<T> e = new(result, _ops)
            {
                _weights = newWeights
            };
            return e;
        }

        public static Eigenstates<T> operator <=(Eigenstates<T> a, T b)
        {
            return a.Do_condition_type(a._ops.LessThanOrEqual, b);
        }

        public static Eigenstates<T> operator <=(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_condition_type(a._ops.LessThanOrEqual, b);
        }

        public static Eigenstates<T> operator >=(Eigenstates<T> a, T b)
        {
            return a.Do_condition_type(a._ops.GreaterThanOrEqual, b);
        }

        public static Eigenstates<T> operator >=(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_condition_type(a._ops.GreaterThanOrEqual, b);
        }

        public static Eigenstates<T> operator <(Eigenstates<T> a, T b)
        {
            return a.Do_condition_type(a._ops.LessThan, b);
        }

        public static Eigenstates<T> operator <(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_condition_type(a._ops.LessThan, b);
        }

        public static Eigenstates<T> operator >(Eigenstates<T> a, T b)
        {
            return a.Do_condition_type(a._ops.GreaterThan, b);
        }

        public static Eigenstates<T> operator >(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_condition_type(a._ops.GreaterThan, b);
        }

        public static Eigenstates<T> operator ==(Eigenstates<T> a, T b)
        {
            return a.Do_condition_type(a._ops.Equal, b);
        }

        public static Eigenstates<T> operator ==(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_condition_type(a._ops.Equal, b);
        }

        public static Eigenstates<T> operator !=(Eigenstates<T> a, T b)
        {
            return a.Do_condition_type(a._ops.NotEqual, b);
        }

        public static Eigenstates<T> operator !=(Eigenstates<T> a, Eigenstates<T> b)
        {
            return a.Do_condition_type(a._ops.NotEqual, b);
        }

        #endregion

        #region Weights / Output
        // The part where we pretend our quantum data is readable by humans.
        // Also provides debugging strings to impress people on code reviews.

        public IEnumerable<T> ToValues()
        {
            return _qDict.Keys;
        }

        /// <summary>
        /// Returns a string representation of the Eigenstates.
        /// </summary>
        /// <returns></returns>
        public override string? ToString()
        {
            List<T> distinctKeys = _qDict.Keys.Distinct().ToList();
            if (_weights == null || AllWeightsEqual(_weights))
            {
                // Return just the element if there is a single unique state
                return distinctKeys.Count == 1
                    ? distinctKeys.First().ToString()
                    : _eType == QuantumStateType.SuperpositionAny
                    ? $"any({string.Join(", ", distinctKeys)})"
                    : $"all({string.Join(", ", distinctKeys)})";
            }
            else
            {
                IEnumerable<string> pairs = ToMappedWeightedValues().Select(x => $"{x.value}:{x.weight}");
                return _eType == QuantumStateType.SuperpositionAny
                    ? $"any({string.Join(", ", pairs)})"
                    : $"all({string.Join(", ", pairs)})";
            }
        }

        /// <summary>
        /// Returns a collection of (mapped value, weight) pairs,
        /// where weights correspond to the original input keys.
        /// </summary>
        public IEnumerable<(T value, Complex weight)> ToMappedWeightedValues()
        {
            if (_weights == null)
            {
                foreach (T? k in _qDict.Keys.Distinct())
                {
                    yield return (_qDict[k], 1.0);
                }
            }
            else
            {
                foreach (KeyValuePair<T, Complex> kvp in _weights)
                {
                    yield return (_qDict[kvp.Key], kvp.Value);
                }
            }
        }


        /// <summary>
        /// Checks if all weights are equal. If so, congratulations — your data achieved perfect balance, Thanos-style.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private new bool AllWeightsEqual(Dictionary<T, Complex> dict)
        {
            if (dict.Count <= 1)
            {
                return true;
            }

            Complex first = dict.Values.First();
            return dict.Values.Skip(1).All(w => Complex.Abs(w - first) < 1e-14);
        }

        /// <summary>
        /// Checks if all weights are probably equal (i.e., squared magnitudes).
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private new bool AllWeightsProbablyEqual(Dictionary<T, Complex> dict)
        {
            if (dict.Count <= 1)
            {
                return true;
            }

            double firstProb = SquaredMagnitude(dict.Values.First());

            return dict.Values
                .Skip(1)
                .All(w => Math.Abs(SquaredMagnitude(w) - firstProb) < 1e-14);
        }

        private double SquaredMagnitude(Complex c)
        {
            return (c.Real * c.Real) + (c.Imaginary * c.Imaginary);
        }

        /// <summary>
        /// Returns a string representation of the Eigenstates,
        /// perfect for when your code works and you still don’t know why.
        /// </summary>
        /// <returns></returns>
        public string ToDebugString()
        {
            return _weights == null
                ? string.Join(", ",
                    _qDict.Select(kvp => $"{kvp.Key} => {kvp.Value}"))
                : string.Join(", ",
                    _qDict.Select(kvp =>
                    {
                        Complex w = _weights.TryGetValue(kvp.Key, out Complex val) ? val : 1.0;
                        return $"{kvp.Key} => {kvp.Value} (weight: {w})";
                    }));
        }

        #endregion

        #region Observation & Collapse, Mocking, Introspection

        public Guid? CollapseHistoryId => _collapseHistoryId;

        /// <summary>
        /// Observes (collapses) the Eigenstates with an optional random instance
        /// If mock collapse is enabled, it will return the forced value without changing state.
        /// Otherwise, perform a real probabilistic collapse.
        /// </summary>
        public override T Observe(Random? rng = null)
        {
            if (_mockCollapseEnabled)
            {
                return _mockCollapseValue == null
                    ? throw new InvalidOperationException("Mock collapse enabled but no mock value is set.")
                    : _mockCollapseValue;
            }

            if (_isActuallyCollapsed && _collapsedValue != null && _eType == QuantumStateType.CollapsedResult)
            {
                return _collapsedValue;
            }

            rng ??= Random.Shared;

            // If this was not called via Observe(int seed), then we need to generate a new default collapse ID.
            _collapseHistoryId ??= Guid.NewGuid();

            T picked = SampleWeighted(rng);

            if (QuantumConfig.ForbidDefaultOnCollapse && !_valueValidator(picked))
            {
                throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");
            }

            _collapsedValue = picked;
            _isActuallyCollapsed = true;
            Dictionary<T, T> newDict = new()
            { { picked, picked } };
            _qDict = newDict;
            if (_weights != null)
            {
                _weights = new Dictionary<T, Complex> { { picked, 1.0 } };
            }
            _eType = QuantumStateType.CollapsedResult;

            return picked;
        }

        /// <summary>
        /// Observes (collapses) using a supplied seed for deterministic behavior.
        /// </summary>
        public T Observe(int seed)
        {
            _lastCollapseSeed = seed;
            _collapseHistoryId = Guid.NewGuid();
            Random rng = new(seed);
            return Observe(rng);
        }

        /// <summary>
        /// Enables mock collapse for Eigenstates, forcing Observe() to return forcedValue.
        /// </summary>
        public Eigenstates<T> WithMockCollapse(T forcedValue)
        {
            _mockCollapseEnabled = true;
            _mockCollapseValue = forcedValue;
            return this;
        }

        /// <summary>
        /// Disables mock collapse for Eigenstates.
        /// </summary>
        public Eigenstates<T> WithoutMockCollapse()
        {
            _mockCollapseEnabled = false;
            _mockCollapseValue = default;
            return this;
        }

        /// <summary>
        /// Returns a string representation of the current eigenstates without collapsing.
        /// </summary>
        public string show_states()
        {
            return ToDebugString();
        }
        #endregion


        /// <summary>
        /// Indicates whether your states have developed opinions (i.e., weights).
        /// If false, they're blissfully indifferent.
        /// </summary>
        public new bool IsWeighted => _weights != null;

        public override IReadOnlyCollection<T> States => _qDict.Keys;

        /// <summary>
        /// Grabs the top N keys based on weight. 
        /// Sort of like picking favorites, but mathematically justified.
        /// </summary>
        public IEnumerable<T> TopNByWeight(int n)
        {
            if (!States.Any() || n <= 0)
            {
                return Enumerable.Empty<T>();
            }

            // If unweighted, each key is weight=1 => they are all "tied".
            // We'll just return them in natural order, limited to N.
            if (!IsWeighted)
            {
                return States.Take(n);
            }

            // Weighted => sort descending by weight
            return _weights!
                .OrderByDescending(kvp => kvp.Value.Magnitude * kvp.Value.Magnitude)
                .Take(n)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Filter your states by weight like you're trimming down party invites.
        /// Only show up if your weight is greater than 0.75, Chad."
        /// </summary>
        public Eigenstates<T> FilterByProbability(Func<Complex, bool> predicate)
        {
            Dictionary<T, T> newDict = [];
            Dictionary<T, Complex>? newWeights = null;

            if (IsWeighted)
            {
                newWeights = [];
                foreach ((T key, Complex wt) in _weights)
                {
                    if (predicate(wt))
                    {
                        newDict[key] = _qDict[key];
                        newWeights[key] = wt;
                    }
                }
            }
            else
            {
                // unweighted => each key has weight=1
                bool keep = predicate(1.0);
                if (keep)
                {
                    foreach (T? key in _qDict.Keys)
                    {
                        newDict[key] = _qDict[key];
                    }
                }
            }

            Eigenstates<T> e = new(newDict, _ops)
            {
                _weights = newWeights
            };
            return e;
        }

        /// <summary>
        /// Filter your states by amplitude like you're picking a favorite child.
        /// Useful if you're interested in real/imaginary structure instead of probability.
        /// </summary>
        /// <param name="amplitudePredicate">A predicate applied directly to the complex amplitude.</param>
        /// <returns>A filtered QuBit containing only states whose amplitude passes the test.</returns>
        public Eigenstates<T> FilterByAmplitude(Func<Complex, bool> amplitudePredicate)
        {
            Dictionary<T, T> newDict = [];
            Dictionary<T, Complex>? newWeights = null;

            if (IsWeighted)
            {
                newWeights = [];
                foreach ((T key, Complex amp) in _weights!)
                {
                    if (amplitudePredicate(amp))
                    {
                        newDict[key] = _qDict[key];
                        newWeights[key] = amp;
                    }
                }
            }
            else
            {
                // unweighted => treat each amplitude as 1 + 0i
                Complex defaultAmp = Complex.One;
                if (amplitudePredicate(defaultAmp))
                {
                    foreach (T? key in _qDict.Keys)
                    {
                        newDict[key] = _qDict[key];
                    }
                }
            }

            Eigenstates<T> e = new(newDict, _ops)
            {
                _weights = newWeights
            };
            return e;
        }

        /// <summary>
        /// Collapse the superposition by crowning the one true value.
        /// It's democracy, but weighted and quantum, so basically rigged.
        /// </summary>
        public T CollapseWeighted()
        {
            if (!States.Any())
            {
                throw new InvalidOperationException("No states available to collapse.");
            }

            if (!IsWeighted)
            {
                // fallback
                return _qDict.Keys.First();
            }
            else
            {
                T? key = _weights!.MaxBy(x => x.Value.Magnitude)!.Key;
                return key;
            }
        }

        /// <summary>
        /// Makes a unique hash that somehow encodes the weight of your guilt, I mean, states.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                foreach (T? k in _qDict.Keys.OrderBy(x => x))
                {
                    // Hash the key and its projected value
                    hash = (hash * 23) + (k?.GetHashCode() ?? 0);
                    hash = (hash * 23) + (_qDict[k]?.GetHashCode() ?? 0);

                    if (IsWeighted && _weights != null && _weights.TryGetValue(k, out Complex amp))
                    {
                        // Round both real and imaginary parts
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

        /// <summary>
        /// Returns a quick stats rundown so you can pretend you have control.
        /// </summary>
        public string WeightSummary()
        {
            if (!IsWeighted)
            {
                return "Weighted: false";
            }

            List<double> probs = _weights!.Values.Select(amp => amp.Magnitude * amp.Magnitude).ToList();
            double sum = probs.Sum();
            double max = probs.Max();
            double min = probs.Min();

            return $"Weighted: true (complex amplitudes), Total Prob: {sum:F4}, Max |amp|²: {max:F4}, Min |amp|²: {min:F4}";
        }

        /// <summary>
        /// Applies new weights to the same old states. 
        /// Like giving your data a glow-up without changing its personality.
        /// </summary>
        public Eigenstates<T> WithWeights(Dictionary<T, Complex> weights)
        {
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            Dictionary<T, Complex> filtered = [];
            foreach (KeyValuePair<T, Complex> kvp in weights)
            {
                if (_qDict.ContainsKey(kvp.Key))
                {
                    filtered[kvp.Key] = kvp.Value;
                }
            }

            // Create a new instance with the same key->value map, same ops
            Eigenstates<T> newEigen = new(new Dictionary<T, T>(_qDict), _ops)
            {
                _weights = filtered,
                _eType = _eType  // preserve the same quantum state type
            };
            return newEigen;
        }

        /// <summary>
        /// Returns a string representation of the Eigenstates,
        /// </summary>
        public string ToDebugString(bool includeCollapseMetadata = false)
        {
            string baseInfo = _weights == null
                ? string.Join(", ", _qDict.Select(kvp => $"{kvp.Key} => {kvp.Value}"))
                : string.Join(", ", _qDict.Select(kvp =>
                {
                    Complex w = _weights.TryGetValue(kvp.Key, out Complex val) ? val : 1.0;
                    return $"{kvp.Key} => {kvp.Value} (weight: {w})";
                }));

            return !includeCollapseMetadata
                ? baseInfo
                : $"{baseInfo}\nCollapsed: {_isActuallyCollapsed}, " +
                   $"Seed: {_lastCollapseSeed}, ID: {_collapseHistoryId}";
        }

        /// <summary>
        /// This quantum decision was final… but I copied it into a new universe where it wasn’t
        /// Cloning a collapsed QuBit resets its collapse status. The new instance retains the amplitudes and quantum state type, but is mutable and behaves as if never observed.
        /// </summary>
        public override QuantumSoup<T> Clone()
        {
            // Deep clone the key-to-value mapping.
            Dictionary<T, T> clonedDict = new(_qDict);

            // Clone the weights if available.
            Dictionary<T, Complex>? clonedWeights = null;
            if (_weights != null)
            {
                clonedWeights = new Dictionary<T, Complex>(_weights);
            }

            // Create a new Eigenstates<T> instance with the cloned dictionary and operators.
            Eigenstates<T> clone = new(clonedDict, _ops)
            {
                _weights = clonedWeights,
                _eType = _eType,
                // Optionally, copy over collapse-related metadata.
                _collapseHistoryId = _collapseHistoryId,
                _lastCollapseSeed = _lastCollapseSeed,
                _isActuallyCollapsed = _isActuallyCollapsed,
                _collapsedValue = _collapsedValue
            };

            return clone;
        }

        public Eigenstates<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            // New dictionary mapping the transformed (projected) values to themselves.
            Dictionary<TResult, TResult> newDict = [];

            // If the current eigenstates has weights, prepare a new weight dictionary;
            // otherwise, we'll remain unweighted.
            Dictionary<TResult, Complex>? newWeights = null;
            if (IsWeighted) // i.e. _weights != null
            {
                newWeights = [];
            }

            // Iterate over each value and its associated weight.
            foreach ((T value, Complex weight) in ToMappedWeightedValues())
            {
                TResult result = selector(value);

                if (newDict.ContainsKey(result))
                {
                    // If the result is already present, add the weight if applicable.
                    if (newWeights != null)
                    {
                        newWeights[result] += weight;
                    }
                }
                else
                {
                    newDict.Add(result, result);
                    newWeights?.Add(result, weight);
                }
            }

            // Construct a new Eigenstates<TResult> with the transformed dictionary.
            // We use QuantumOperatorsFactory to get the appropriate operators for TResult.
            Eigenstates<TResult> eigen = new(newDict, QuantumOperatorsFactory.GetOperators<TResult>())
            {
                // Copy over the state type so that any "All" or "Any" marker persists.
                _eType = _eType,
                // Set the new weights (if any)
                _weights = newWeights
            };
            return eigen;
        }

        public Eigenstates<T> Where(Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            Dictionary<T, T> newDict = _qDict
                .Where(kvp => predicate(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Dictionary<T, Complex>? newWeights = null;
            if (_weights != null)
            {
                newWeights = [];
                foreach (T? key in newDict.Keys)
                {
                    if (_weights.TryGetValue(key, out Complex w))
                    {
                        newWeights[key] = w;
                    }
                }
            }

            Eigenstates<T> eigen = new(newDict, _ops)
            {
                _eType = _eType,
                _weights = newWeights
            };
            return eigen;
        }


        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is Eigenstates<T> other && Equals(other));
        }
        public bool Equals(Eigenstates<T> other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Compare the keys in _qDict (which holds the states) using set equality.
            HashSet<T> thisKeys = [.. _qDict.Keys];
            HashSet<T> otherKeys = [.. other._qDict.Keys];
            if (!thisKeys.SetEquals(otherKeys))
            {
                return false;
            }

            // If weights exist, compare them with the desired tolerance.
            if (IsWeighted || other.IsWeighted)
            {
                // If one is weighted and the other isn't then they're not equal.
                if ((IsWeighted && !other.IsWeighted) || (!IsWeighted && other.IsWeighted))
                {
                    return false;
                }

                foreach (T? key in thisKeys)
                {
                    Complex w1 = _weights != null && _weights.TryGetValue(key, out Complex weight1) ? weight1 : Complex.One;
                    Complex w2 = other._weights != null && other._weights.TryGetValue(key, out Complex weight2) ? weight2 : Complex.One;
                    if (Complex.Abs(w1 - w2) > 1e-14) // adjust tolerance as needed
                    {
                        return false;
                    }
                }
            }

            return true;
        }


    }

}
