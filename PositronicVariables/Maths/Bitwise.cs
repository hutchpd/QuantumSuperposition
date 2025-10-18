using System;

namespace PositronicVariables.Maths
{
    /// <summary>
    /// Generic bitwise helpers for integral types. Uses dynamic to keep parity with Arithmetic helpers.
    /// </summary>
    public static class Bitwise
    {
        public static T And<T>(T a, T b) => (T)(object)(((dynamic)a) & ((dynamic)b));
        public static T Or<T>(T a, T b)  => (T)(object)(((dynamic)a) | ((dynamic)b));
        public static T Xor<T>(T a, T b) => (T)(object)(((dynamic)a) ^ ((dynamic)b));
        public static T Not<T>(T a)      => (T)(object)(~((dynamic)a));

        public static T ShiftLeft<T>(T a, int count)  => (T)(object)(((dynamic)a) << count);
        public static T ShiftRight<T>(T a, int count) => (T)(object)(((dynamic)a) >> count);
    }
}