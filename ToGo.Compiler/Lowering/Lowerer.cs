using System;
using System.Collections.Generic;
using ToGo.Compiler.Binder;

namespace ToGo.Compiler.Lowering;

public sealed class Lowerer
{
    public LoweredProgram Lower(BoundProgram program)
    {
        var antivals = new List<VariableSymbol>();
        var timeBlocks = new List<LoweredTimeBlock>();

        for (int i = 0; i < program.Statements.Count; i++)
        {
            var stmt = program.Statements[i];
            if (stmt is BoundAntivalDeclaration d)
            {
                antivals.Add(d.Variable);
                continue;
            }

            if (stmt is BoundTimeBlock tb)
            {
                timeBlocks.Add(LowerTimeBlock(tb));
                continue;
            }
        }

        return new LoweredProgram(antivals, timeBlocks);
    }

    private static LoweredTimeBlock LowerTimeBlock(BoundTimeBlock tb)
    {
        var assigns = new List<BoundAssignmentStatement>();
        var prints = new List<BoundExpression>();

        for (int i = 0; i < tb.Statements.Count; i++)
        {
            var s = tb.Statements[i];
            if (s is BoundAssignmentStatement a)
            {
                assigns.Add(a);
                continue;
            }

            if (s is BoundPrintStatement p)
            {
                prints.Add(p.Expression);
                continue;
            }
        }

        return new LoweredTimeBlock(assigns, prints);
    }
}
