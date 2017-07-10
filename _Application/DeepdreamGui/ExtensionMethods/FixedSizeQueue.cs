using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepdreamGui.ExtensionMethods
{
    /// <summary>
    /// Fixed size Queue. Automatically dequeues Element when full.
    /// </summary>
    /// <typeparam name="T">Content Type</typeparam>
    public class FixedSizedQueue<T>
    {
        //ThreadSafe Queue, that automatically dequeues, when size is over limit
        //http://stackoverflow.com/questions/5852863/fixed-size-queue-which-automatically-dequeues-old-values-upon-new-enques

        ConcurrentQueue<T> q = new ConcurrentQueue<T>();

        /// <summary>
        /// Max numbers of Elements in Queue
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Add Elements to queue, dequeues Element when full
        /// </summary>
        /// <param name="obj">Object to enqueue</param>
        public void Enqueue(T obj)
        {
            q.Enqueue(obj);
            lock (this)
            {
                T overflow;
                while (q.Count > Limit && q.TryDequeue(out overflow)) ;
            }
        }

        /// <summary>
        /// Dequeue element
        /// </summary>
        /// <returns>dequeued element</returns>
        public T Dequeue()
        {
            T result;
            q.TryDequeue(out result);
            return result;
        }

        /// <summary>
        /// Number of elements in queue
        /// </summary>
        public int Count => q.Count;
    }
}
