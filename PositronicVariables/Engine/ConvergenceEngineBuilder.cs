using System;
using System.Collections.Generic;
using System.Linq;

namespace PositronicVariables.Engine
{
    public class ConvergenceEngineBuilder<T>
        where T : IComparable<T>
    {
        private readonly List<Func<IImprobabilityEngine<T>, IImprobabilityEngine<T>>> _middlewares = [];

        /// <summary>
        /// Bolts a new questionable device onto the engine. Nobody asked what it does, and it's too late to stop now.
        /// </summary>
        public ConvergenceEngineBuilder<T> Use(Func<IImprobabilityEngine<T>, IImprobabilityEngine<T>> middleware)
        {
            _middlewares.Add(middleware);
            return this;
        }

        /// <summary>
        /// Summons the convergence abomination from its component horrors. Add enough decorators and it starts resembling sentience.
        /// </summary>
        public IImprobabilityEngine<T> Build(IImprobabilityEngine<T> core)
        {
            IImprobabilityEngine<T> engine = core;
            foreach (Func<IImprobabilityEngine<T>, IImprobabilityEngine<T>> middleware in _middlewares.Reverse<Func<IImprobabilityEngine<T>, IImprobabilityEngine<T>>>())
            {
                engine = middleware(engine);
            }
            return engine;
        }
    }
}
