using System;
using System.Numerics;

namespace PositronicVariables.Runtime
{
    internal static class NumericOps<T>
    {
        private static readonly TypeCode _code = Type.GetTypeCode(typeof(T));

        public static T Add(T a, T b)
        {
            switch (_code)
            {
                case TypeCode.Int32: return (T)(object)((int)(object)a + (int)(object)b);
                case TypeCode.Int64: return (T)(object)((long)(object)a + (long)(object)b);
                case TypeCode.Double: return (T)(object)((double)(object)a + (double)(object)b);
                case TypeCode.Single: return (T)(object)((float)(object)a + (float)(object)b);
                case TypeCode.Decimal: return (T)(object)((decimal)(object)a + (decimal)(object)b);
                case TypeCode.UInt32: return (T)(object)((uint)(object)a + (uint)(object)b);
                case TypeCode.UInt64: return (T)(object)((ulong)(object)a + (ulong)(object)b);
                case TypeCode.Int16: return (T)(object)((short)(object)a + (short)(object)b);
                case TypeCode.UInt16: return (T)(object)((ushort)(object)a + (ushort)(object)b);
                case TypeCode.Byte: return (T)(object)((byte)(object)a + (byte)(object)b);
                case TypeCode.SByte: return (T)(object)((sbyte)(object)a + (sbyte)(object)b);
                default:
                    try { dynamic da = a; dynamic db = b; return (T)(da + db); } catch { return b; }
            }
        }

        public static T Subtract(T a, T b)
        {
            switch (_code)
            {
                case TypeCode.Int32: return (T)(object)((int)(object)a - (int)(object)b);
                case TypeCode.Int64: return (T)(object)((long)(object)a - (long)(object)b);
                case TypeCode.Double: return (T)(object)((double)(object)a - (double)(object)b);
                case TypeCode.Single: return (T)(object)((float)(object)a - (float)(object)b);
                case TypeCode.Decimal: return (T)(object)((decimal)(object)a - (decimal)(object)b);
                case TypeCode.UInt32: return (T)(object)((uint)(object)a - (uint)(object)b);
                case TypeCode.UInt64: return (T)(object)((ulong)(object)a - (ulong)(object)b);
                case TypeCode.Int16: return (T)(object)((short)(object)a - (short)(object)b);
                case TypeCode.UInt16: return (T)(object)((ushort)(object)a - (ushort)(object)b);
                case TypeCode.Byte: return (T)(object)((byte)(object)a - (byte)(object)b);
                case TypeCode.SByte: return (T)(object)((sbyte)(object)a - (sbyte)(object)b);
                default:
                    try { dynamic da = a; dynamic db = b; return (T)(da - db); } catch { return a; }
            }
        }
    }
}
