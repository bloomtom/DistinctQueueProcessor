using DQP;
using System;
using System.Collections.Generic;
using System.Text;

namespace DQP
{
    /// <summary>
    /// A simple implementation of DistinctQueueProcessor which converts the overridden methods Process and Error into actions taken in the constructor.
    /// </summary>
    public class ActionQueue<T> : DistinctQueueProcessor<T>
    {
        private readonly Action<T> process;
        private readonly Action<T, Exception> error;

        /// <param name="process">The action to invoke when an item is being processed. This action will be called from a threaded task.</param>
        /// <param name="error">The action to invoke if an exception is caught when processing an item. This action will be called from a threaded task.</param>
        public ActionQueue(Action<T> process, Action<T, Exception> error)
        {
            this.process = process;
            this.error = error;
        }

        protected override void Error(T item, Exception ex)
        {
            error.Invoke(item, ex);
        }

        protected override void Process(T item)
        {
            process.Invoke(item);
        }
    }
}
