using System.Collections;
using System.Collections.Generic;

namespace NationalInstruments.Tools
{
    /// <summary>
    /// A lightweight HashSet implementation that retains the order, in which elements were added.
    /// </summary>
    /// <typeparam name="T">Type of items</typeparam>
    /// <remarks>Based on https://stackoverflow.com/questions/1552225/hashset-that-preserves-ordering/17853085#17853085. </remarks>
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public class OrderedHashSet<T> : IEnumerable<T>
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        private readonly LinkedList<T> _list = new LinkedList<T>();
        private readonly Dictionary<T, LinkedListNode<T>> _hashes = new Dictionary<T, LinkedListNode<T>>();

        public int Count => _hashes.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public bool Add(T item)
        {
            if (_hashes.ContainsKey(item))
            {
                return false;
            }

            var node = _list.AddLast(item);
            _hashes.Add(item, node);
            return true;
        }

        public bool Contains(T item)
        {
            return _hashes.ContainsKey(item);
        }

        public bool Remove(T item)
        {
            if (!_hashes.TryGetValue(item, out LinkedListNode<T> node))
            {
                return false;
            }

            _list.Remove(node);
            _hashes.Remove(item);
            return true;
        }

        public void Clear()
        {
            _hashes.Clear();
            _list.Clear();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            var newHashes = new HashSet<T>();
            foreach (var item in other)
            {
                if (_hashes.ContainsKey(item))
                {
                    newHashes.Add(item);
                }
            }

            var currentNode = _list.First;

            while (true)
            {
                if (currentNode == null)
                {
                    return;
                }

                var nextValue = currentNode.Next;
                if (!newHashes.Contains(currentNode.Value))
                {
                    _list.Remove(currentNode);
                    _hashes.Remove(currentNode.Value);
                }

                currentNode = nextValue;
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                if (_hashes.TryGetValue(item, out LinkedListNode<T> node))
                {
                    _list.Remove(node);
                    _hashes.Remove(item);
                }
            }
        }
    }
}
