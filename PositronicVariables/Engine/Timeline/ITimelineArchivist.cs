using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;

namespace PositronicVariables.Engine.Timeline
{
    public interface ITimelineArchivist<T>
        where T : IComparable<T>
    {
        /// <summary>
        /// Metatron writes, he knows not why - but it's important.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="newSlice"></param>
        void SnapshotAppend(PositronicVariable<T> variable, QuBit<T> newSlice);
        /// <summary>
        /// This is why he wrote it down - to remember.
        /// </summary>
        void RestoreLastSnapshot();
        /// <summary>
        /// A bookmark in the timeline, for when you need to go back.
        /// </summary>
        /// <param name="hook"></param>
        void RegisterTimelineAppendedHook(Action hook);
        /// <summary>
        /// Vitally important: otherwise the timeline would grow without bound.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="mergedSlice"></param>
        void ReplaceLastSlice(PositronicVariable<T> variable, QuBit<T> mergedSlice);
        /// <summary>
        /// When the universe insists on a specific outcome, we must comply.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="slice"></param>
        void OverwriteBootstrap(PositronicVariable<T> variable, QuBit<T> slice);
        /// <summary>
        /// For when the timeline needs a fresh start.
        /// </summary>
        void ClearSnapshots();

    }
}
