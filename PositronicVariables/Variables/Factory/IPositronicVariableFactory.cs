using System;

namespace PositronicVariables.Variables.Factory
{
    public interface IPositronicVariableFactory
    {
        /// <summary>
        /// Rule 34 but for positronic variables: If it exists, there's a variable for it. If not, create one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="initialValue"></param>
        /// <returns></returns>
        PositronicVariable<T> GetOrCreate<T>(string id, T initialValue) where T : IComparable<T>;
        /// <summary>
        /// Rule 34 but for positronic variables: If it exists, there's a variable for it. If not, create one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        PositronicVariable<T> GetOrCreate<T>(string id) where T : IComparable<T>;
        /// <summary>
        /// Rule 34 but for positronic variables: If it exists, there's a variable for it. If not, create one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="initialValue"></param>
        /// <returns></returns>
        PositronicVariable<T> GetOrCreate<T>(T initialValue) where T : IComparable<T>;
    }
}
