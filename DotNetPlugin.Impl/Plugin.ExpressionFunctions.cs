using System;
using Dotx64Dbg;

namespace DotNetPlugin
{
    partial class Plugin
    {
        [ExpressionFunction("expr_no_input")]
        public nuint MyExpr1()
        {
            var th = Thread.Active;
            if (th != null)
            {
                return th.Nip;
            }
            else
                Console.WriteLine("No active thread");

            return 0;

        }

        [ExpressionFunction("expr_one_input")]
        public nuint MyExpr2(nuint a)
        {
            return a;

        }

        [ExpressionFunction("expr_two_inputs")]
        public nuint MyExpr3(nuint a, nuint b)
        {
            return a + b;

        }
    }
}
