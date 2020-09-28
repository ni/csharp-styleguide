using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.LabVIEW.Tools.Helpers
{
    public static class QueueExtensions
    {
        public static IEnumerable<T> TakeAndRemove<T>(this Queue<T> queue, int count)
        {
            for (var i = 0; i < count && queue.Count > 0; i++)
            {
                yield return queue.Dequeue();
            }
        }

        public static IEnumerable<T> DequeueUntil<T>(this Queue<T> queue, Func<T, bool> predicate, bool includeMatch)
        {
            while (queue.Count() > 0)
            {
                var item = queue.Peek();

                if (predicate(item))
                {
                    if (includeMatch)
                    {
                        yield return queue.Dequeue();
                    }

                    break;
                }

                yield return queue.Dequeue();
            }
        }

        public static T TryDequeue<T>(this Queue<T> queue)
            where T : class
        {
            if (queue.Count() == 0)
            {
                return null;
            }

            return queue.Dequeue();
        }
    }
}
