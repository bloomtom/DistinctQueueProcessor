using System.Collections.Generic;

namespace DQP
{
    public interface IDistinctQueueProcessor<T>
    {
        IReadOnlyDictionary<string, T> ItemsProcessing { get; }
        IReadOnlyDictionary<string, T> ItemsQueued { get; }

        void AddItem(T item);
        bool CheckQueue(string key);
    }
}