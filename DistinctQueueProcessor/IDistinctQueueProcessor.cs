using System;
using System.Collections.Generic;

namespace DQP
{
    /// <summary>
    /// Interface for an Distinct Queue Processor
    /// </summary>
    /// <typeparam name="T">The type to capture for work.</typeparam>
    public interface IDistinctQueueProcessor<T>
    {
        /// <summary>
        /// A read only copy of the items currently being processed.
        /// </summary>
        IReadOnlyDictionary<string, T> ItemsProcessing { get; }

        /// <summary>
        /// A read only copy of the items currently in queue for processing.
        /// This includes items currently being processed as well.
        /// </summary>
        IReadOnlyDictionary<string, T> ItemsQueued { get; }

        /// <summary>
        /// Adds the given item to the processing schedule if the item isn't already scheduled.
        /// This operation does not block.
        /// Scheduled items are processed in linearly, but may complete out-of-order.
        /// </summary>
        void AddItem(T item);

        /// <summary>
        /// Checks the queue for the given item.
        /// Keys are based on the item T's ToString() implementation.
        /// </summary>
        bool CheckQueue(string key);

        /// <summary>
        /// Blocks the caller using Sleep(10) until all queued items are complete or the timeout is exceeded.
        /// Returns immediately if no items are queued or running.
        /// </summary>
        void WaitForCompletion(TimeSpan t);

        /// <summary>
        /// Blocks the caller using Sleep(10) until all queued items are complete.
        /// </summary>
        void WaitForCompletion();
    }
}