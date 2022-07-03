using System;

namespace Dotx64Dbg
{
    public static class Expressions
    {
        /// <summary>
        /// Evaluates the given expression and results the evaluated value.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="value">Resulting value</param>
        /// <returns>True on success, false in case of errors</returns>
        /// <example>
        /// <code>
        /// ulong val = 0;
        /// if(Expression.TryEvaluate("rip", val)) { Console.WriteLine("Value of rip {0}", val); }
        /// </code>
        /// </example>

        public static bool TryEvaluate(string expr, out nuint value)
        {
            IntPtr val;
            var res = Native.Expressions.TryEvaluate(expr, out val);
            value = (nuint)(nint)val;
            return res;
        }

        /// <summary>
        /// Same as TryEvaluate except the function throws if the expression is invalid.
        /// </summary>
        /// <see cref="TryEvaluate"/>
        public static nuint Evaluate(string expr)
        {
            return (nuint)(nint)Native.Expressions.Evaluate(expr);
        }

        /// <summary>
        /// Formats the given the expression and results the formatted string.
        /// </summary>
        /// <param name="expr">Expression to format</param>
        /// <param name="value">Resulting formatted expression</param>
        /// <returns>True on success, false in case of any errors</returns>
        /// <example>
        /// <code>
        /// string formatted;
        /// if(Expression.TryFormat("rip = {rip}", formatted)) { Console.WriteLine("Formatted: {0}", formatted); }
        /// </code>
        /// </example>
        public static bool TryFormat(string expr, out string value)
        {
            return Native.Expressions.TryFormat(expr, out value);
        }

        /// <summary>
        /// Same as TryFormat except the function throws if the expression is invalid.
        /// </summary>
        /// <see cref="TryFormat"/>
        public static string Format(string expr)
        {
            return Native.Expressions.Format(expr);
        }

        /// <summary>
        /// Checks if the provided expression is valid.
        /// </summary>
        /// <param name="expr">Expression to validate</param>
        /// <returns>True if the expression is valid, false in case of errors</returns>
        public static bool IsValidExpression(string expr)
        {
            return Native.Expressions.IsValidExpression(expr);
        }
    }
}