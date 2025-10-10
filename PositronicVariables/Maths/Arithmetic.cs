using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Maths
{
    public static class Arithmetic
    {
        /// <summary>
        /// Sometimes maths is just adding two numbers together.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static T Add<T>(T x, T y)
            where T : INumber<T>
            => x + y;

        /// <summary>
        /// A fallback for dynamic types that don't support generic maths (e.g. Qubits)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static dynamic Add(dynamic x, dynamic y)
            => x + y;

        /// <summary>
        /// Who's even reading this far down? 1 - 1 = 0, right?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static T Subtract<T>(T x, T y)
            where T : ISubtractionOperators<T, T, T>
            => x - y;
        /// <summary>
        /// I hate dynamics, they're the worst.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static dynamic Subtract(dynamic x, dynamic y)
            => x - y;

        /// <summary>
        /// There's a video somewhere on the internet from the 90s of Carol Vorderman dressed as Madonna singing
        /// "I'm going to show you how to multiply", it's as easy as 1, 2, 3. I think about that video a lot.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static T Multiply<T>(T x, T y)
            where T : IMultiplyOperators<T, T, T>
            => x * y;
        /// <summary>
        /// Who are you and why are you using dynamics in 2025?
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static dynamic Multiply(dynamic x, dynamic y)
            => x * y;

        /// <summary>
        /// Division is the most controversial of the basic arithmetic operations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static T Divide<T>(T x, T y)
            where T : IDivisionOperators<T, T, T>
            => x / y;
        /// <summary>
        /// Seriously, stop using dynamics.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static dynamic Divide(dynamic x, dynamic y)
            => x / y;

        /// <summary>
        /// The remainder operation, also known as modulo, gives you the leftover part of a division.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static T Remainder<T>(T x, T y)
            where T : IModulusOperators<T, T, T>
            => x % y;
        /// <summary>
        /// Dynamic remainder, because why not.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static dynamic Remainder(dynamic x, dynamic y)
            => x % y;

        /// <summary>
        /// The mirror universe, we put a beard on every number.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <returns></returns>
        public static T Negate<T>(T x)
            where T : IUnaryNegationOperators<T, T>
            => -x;
        /// <summary>
        /// Dynamic negation, because some people just want to watch the world burn.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static dynamic Negate(dynamic x)
            => -x;

        /// <summary>
        /// Modulus operation that always returns a non-negative result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static T Modulus<T>(T x, T y)
            where T : IModulusOperators<T, T, T>
            => x % y;

        /// <summary>
        /// Dynamic modulus that always returns a non-negative result.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static dynamic Modulus(dynamic x, dynamic y)
        {
            if (x is double || x is float)
                return x - y * Math.Floor(x / y);
            return x % y;
        }

        /// <summary>
        /// Floor division, which rounds down to the nearest whole number.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static dynamic FloorDiv(dynamic x, dynamic y)
        {
            if (x is double || x is float || x is decimal)
                return Math.Floor(Convert.ToDouble(x) / Convert.ToDouble(y));
            return x / y;
        }

    }
}
