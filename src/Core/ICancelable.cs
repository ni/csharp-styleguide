using System;
using System.Threading;

namespace NationalInstruments.Tools
{
    public interface ICancelable
    {
        event EventHandler Canceled;

        CancellationToken CancellationToken { get; }

        void Cancel();
    }
}
