using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DQP
{
    /// <summary>
    /// A multi-threaded queue processor base class. Duplicate items based on ToString() are discarded. Items are processed in linear order but may complete out-of-order.
    /// All available operations are fully thread-safe.
    /// </summary>
    /// <typeparam name="T">The type of item to handle. Dictionary keying is done on ToString(), so ensure it has a fast and unique implementation.</typeparam>
    public abstract class DistinctQueueProcessor<T> : IDistinctQueueProcessor<T>
    {
        /// <summary>
        /// A read only copy of the items currently in queue for processing.
        /// This includes items currently being processed as well.
        /// </summary>
        public IReadOnlyDictionary<string, T> ItemsQueued
        {
            get
            {
                return new ReadOnlyDictionary<string, T>(queueIndex);
            }
        }

        /// <summary>
        /// A read only copy of the items currently being processed.
        /// </summary>
        public IReadOnlyDictionary<string, T> ItemsProcessing
        {
            get
            {
                return new ReadOnlyDictionary<string, T>(itemsBeingProcessed);
            }
        }

        // Always ensure this is held before appending to the queue to ensure the queue and index are in sync.
        private readonly object queueAppendLock = new object();

        /// <summary>
        /// The number of workers to employ for handling queue items. Sane values are typically 1-[cpu cores].
        /// Setting this to zero will stall the creation of new workers. This can lead to a stalled queue if no workers are running and items are added.
        /// </summary>
        protected int Parallelization { get { return parallelization; } set { lock (queueAppendLock) { parallelization = value < 0 ? 0 : value; } } }
        private volatile int parallelization = 1;

        // Tracks how many threads are currently running. When zero, no conversion is taking place.
        protected int RunningWorkers { get { return runningWorkers; } }
        private volatile int runningWorkers = 0;

        // The FIFO structure for determining what item to process next.
        private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

        // Used for fast lookups of what's in the queue to avoid simultaneous double conversion.
        private readonly ConcurrentDictionary<string, T> queueIndex = new ConcurrentDictionary<string, T>();
        private readonly ConcurrentDictionary<string, T> itemsBeingProcessed = new ConcurrentDictionary<string, T>();

        /// <summary>
        /// Adds the given item to the processing schedule if the item isn't already scheduled.
        /// This operation does not block.
        /// Scheduled items are processed in linearly, but may complete out-of-order.
        /// </summary>
        public void AddItem(T item)
        {
            lock (queueAppendLock) // Ensures thread safety of the queue and index in cases where this method is called from multiple external threads.
            {
                if (!CheckQueue(item.ToString()) && queueIndex.TryAdd(item.ToString(), item))
                {
                    queue.Enqueue(item);

                    StartWorker();
                }
            }
        }

        /// <summary>
        /// Manually starts a single worker. This can be used to unstall the queue if items are added while parallelization is set to zero and no workers are running.
        /// </summary>
        protected void ManualStartWorker()
        {
            lock (queueAppendLock)
            {
                StartWorker();
            }
        }

        private void StartWorker()
        {
            // Start a worker if we're under out worker limit (Parallelization).
            int runningDiff = Parallelization - runningWorkers;
            if (runningDiff > 0)
            {
                RunWorker();
            }
        }

        /// <summary>
        /// Checks the queue for the given item.
        /// Keys are based on the item T's ToString() implementation.
        /// </summary>
        public bool CheckQueue(string key)
        {
            return queueIndex.ContainsKey(key);
        }

        private void RunWorker()
        {
            System.Threading.Interlocked.Increment(ref runningWorkers);
            Task.Run(() =>
            {
                try
                {
                    // Pop one item from the queue and convert it.
                    while (queue.TryDequeue(out T item))
                    {
                        itemsBeingProcessed.TryAdd(item.ToString(), item);

                        try
                        {
                            Process(item);
                        }
                        catch (Exception ex)
                        {
                            Error(item, ex);
                        }
                        finally
                        {
                            queueIndex.TryRemove(item.ToString(), out T x); // Item processed, no longer in queue.
                            itemsBeingProcessed.TryRemove(item.ToString(), out T y);
                        }
                    }
                }
                finally
                {
                    // Give up on more work coming in and exit.
                    System.Threading.Interlocked.Decrement(ref runningWorkers);
                }
            });
        }

        /// <summary>
        /// This must be overridden with an implementation that does something with an item when it's up for queue. The given item is dequeued upon return.
        /// This method will be called from a task thread; Take care to ensure thread safety.
        /// </summary>
        protected abstract void Process(T item);

        /// <summary>
        /// This is called whenever an exception is thrown internally when processing an item. Unhandled exceptions in Process(item) will also cause this to be called.
        /// This method will be called from a task thread; Take care to ensure thread safety.
        /// </summary>
        protected abstract void Error(T item, Exception ex);
    }
}
