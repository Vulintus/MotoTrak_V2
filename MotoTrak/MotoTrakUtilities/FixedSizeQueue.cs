using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakUtilities
{
    public class FixedSizedQueue<T>
    {
        ConcurrentQueue<T> q = new ConcurrentQueue<T>();

        public int Limit { get; set; }

        public int Count
        {
            get
            {
                return q.Count;
            }
        }

        public bool IsFull
        {
            get
            {
                return q.Count == Limit;
            }
        }

        public List<T> ListClone
        {
            get
            {
                List<T> copy;

                lock (this)
                {
                    copy = q.ToList();
                }

                return copy;
            }
        }

        public void Enqueue(T obj)
        {
            q.Enqueue(obj);
            lock (this)
            {
                T overflow;
                while (q.Count > Limit && q.TryDequeue(out overflow));
            }
        }

        public void Clear ()
        {
            T item;
            while (!q.IsEmpty)
            {
                bool success = q.TryDequeue(out item);
            }
        }
    }
}
