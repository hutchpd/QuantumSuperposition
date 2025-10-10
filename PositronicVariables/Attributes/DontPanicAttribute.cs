using System;

namespace PositronicVariables.Attributes
{
    /// <summary>
    /// Marks a single static method as the convergence entry point.
    /// The engine will reflect for exactly one method bearing this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class DontPanicAttribute : Attribute
    {
    }
}
