using System;
using System.Threading;

namespace NationalInstruments.Tools
{
    public interface IPollingRunnerContext
    {
        TimeSpan PollTime { get; }

        CancellationToken CancellationToken { get; }
    }
}
