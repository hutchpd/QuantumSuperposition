using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Engine.Transponder
{
    public interface ISubEthaTransponder
    {
        void Redirect();
        void Restore();
    }
}
