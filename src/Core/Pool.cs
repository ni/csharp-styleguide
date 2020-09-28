using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NationalInstruments.Tools.Core
{
    // Source: https://github.com/microsoft/BuildXL/blob/master/Public/Src/Cache/ContentStore/Hashing/Pool.cs
    public class Pool<T> : Disposable
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;

        // Number of idle reserve instances to hold in the queue. -1 means unbounded
        private readonly int _maxReserveInstances;
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        /// <summary>
        /// Initializes an object pool
        /// </summary>
        /// <param name="factory">Func to create a new object for the pool</param>
        /// <param name="reset">Action to reset the state of the object for future reuse</param>
        /// <param name="maxReserveInstances">Number of idle reserve instances to keep. No bound when unset</param>
        public Pool(Func<T> factory, Action<T> reset = null, int maxReserveInstances = -1)
        {
            _factory = factory;
            _reset = reset;
            _maxReserveInstances = maxReserveInstances;
        }

        public int Size => _queue.Count;

        public virtual IPoolHandle<T> GetObject()
        {
            if (!_queue.TryDequeue(out var item))
            {
                item = _factory();
            }

            return new PoolHandle(this, item);
        }

        protected override void DisposeManagedResources()
        {
            foreach (var item in _queue)
            {
                (item as IDisposable)?.Dispose();
            }

            base.DisposeManagedResources();
        }

        private void Return(T item)
        {
            if ((_maxReserveInstances < 0) || (Size < _maxReserveInstances))
            {
                _reset?.Invoke(item);
                _queue.Enqueue(item);
            }
            else
            {
                // Still reset the item incase the reset logic has side effects other than cleanup for future reuse
                _reset?.Invoke(item);
            }
        }

        private struct PoolHandle : IPoolHandle<T>
        {
            private readonly Pool<T> _pool;
            private readonly T _value;
            private bool _disposed;

            public PoolHandle(Pool<T> pool, T value)
            {
                _pool = pool;
                _value = value;
                _disposed = false;
            }

            public T Value
            {
                get
                {
                    AssertValid();
                    return _value;
                }
            }

            public void AssertValid()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    try
                    {
                        _pool.Return(_value);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Nothing to return to...
                    }

                    _disposed = true;
                }
            }
        }
    }
}
