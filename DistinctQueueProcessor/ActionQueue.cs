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

        /// <summary>
        /// This is called whenever an exception is thrown internally when processing an item, and will run the error action given in the constructor. Unhandled exceptions in Process(item) will also cause this to be called.
        /// This method will be called from a task thread; Take care to ensure thread safety.
        /// </summary>
        protected override void Error(T item, Exception ex)
        {
            error.Invoke(item, ex);
        }

        /// <summary>
        /// This will run the process action given in the constructor on items when they enter the run queue. The given item is dequeued upon return.
        /// This method will be called from a task thread; Take care to ensure thread safety.
        /// </summary>
        protected override void Process(T item)
        {
            process.Invoke(item);
        }
    }
}
