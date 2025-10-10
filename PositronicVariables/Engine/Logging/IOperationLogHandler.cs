using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Engine.Logging
{
    public interface IOperationLogHandler<T>
    {
        void Record(IOperation op);
        void UndoLastForwardCycle();
        void Clear();
        bool SawForwardWrite { get; set; }
    }
}
