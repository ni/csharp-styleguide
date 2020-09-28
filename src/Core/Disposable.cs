using System;
using System.Threading;

namespace NationalInstruments.Tools
{
    public abstract class Disposable : MarshalByRefObject, IDisposable
    {
        private int _disposed;

        public event EventHandler<EventArgs> Disposed;

        public event EventHandler<EventArgs> Disposing;

        public static IDisposable Empty => new EmptyDisposable();

        public bool IsDisposed => Interlocked.CompareExchange(ref _disposed, 0, 0) == 1;

        public static void DisposeAndNullOut<T>(ref T disposable)
            where T : class, IDisposable
        {
            if (disposable != null)
            {
                T tempDisposable = disposable;

                disposable = null;
                tempDisposable.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeManagedResources()
        {
        }

        protected virtual void DisposeUnmanagedResources()
        {
        }

        protected virtual void OnDisposed()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisposing()
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }

        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
        private void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return;
            }

            OnDisposing();

            if (disposing)
            {
                DisposeManagedResources();
            }

            DisposeUnmanagedResources();

            OnDisposed();
        }

        private class EmptyDisposable : Disposable
        {
        }
    }
}
