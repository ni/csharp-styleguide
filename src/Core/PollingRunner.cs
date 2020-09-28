using System;
using System.Threading;
using System.Threading.Tasks;

namespace NationalInstruments.Tools
{
    public class PollingRunner<T>
        where T : IPollingRunnerContext
    {
        private readonly Func<T, CancellationToken, Task> _work;

        public PollingRunner(Func<T, CancellationToken, Task> work)
        {
            _work = work;
        }

        public async Task RunAsync(T context, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _work(context, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(context.PollTime, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
