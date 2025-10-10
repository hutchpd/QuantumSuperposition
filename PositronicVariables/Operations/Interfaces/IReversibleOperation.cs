using PositronicVariables.Engine.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Operations.Interfaces
{
    public interface IReversibleOperation<T> : IOperation
    {
        /// <summary>
        /// When time insists on moving forward, this little chap figures out how to make it march in reverse.
        /// (E.g. if you've divided by 9 in one universe, we smuggle you back with a ×9 in another.)
        /// </summary>
        T ApplyInverse(T result);
        /// <summary>
        /// Given the result of a forward operation, computes the forward value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        T ApplyForward(T value);
    }
}
