using System;
using System.Threading;

namespace NationalInstruments.Tools
{
    public class Cancelable : Disposable, ICancelable
    {
        private CancellationTokenRegistration _cancellationTokenRegistration;

        public Cancelable()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        public Cancelable(CancellationToken cancellationToken)
            : this()
        {
            _cancellationTokenRegistration = cancellationToken.Register(OnCancellationTokenCanceled);
        }

        public event EventHandler Canceled;

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public bool IsCancellationComplete { get; private set; }

        public CancellationToken CancellationToken => CancellationTokenSource.Token;

        public bool IsCancellationRequested => CancellationTokenSource.IsCancellationRequested;

        public void Cancel()
        {
            ThrowIfDisposed();

            if (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Cancel();
            }

            OnCanceled();
        }

        public void ResetCancellationTokenSource()
        {
            ThrowIfDisposed();

            CancellationTokenSource.Dispose();

            CancellationTokenSource = new CancellationTokenSource();
        }

        public void RegisterCancellationToken(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            cancellationToken.Register(OnCancellationTokenCanceled);
        }

        protected virtual void OnCanceled()
        {
            if (IsDisposed)
            {
                return;
            }

            Canceled?.Invoke(this, EventArgs.Empty);

            IsCancellationComplete = true;
        }

        protected override void DisposeManagedResources()
        {
            try
            {
                _cancellationTokenRegistration.Dispose();
            }
            catch
            {
            }

            try
            {
                CancellationTokenSource.Dispose();
            }
            catch
            {
            }
        }

        protected void ThrowIfCancellationRequested()
        {
            ThrowIfDisposed();

            CancellationToken.ThrowIfCancellationRequested();
        }

        private void OnCancellationTokenCanceled()
        {
            if (IsDisposed)
            {
                return;
            }

            Cancel();
        }
    }
}
