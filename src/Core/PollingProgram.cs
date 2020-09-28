using System.Threading;
using System.Threading.Tasks;

namespace NationalInstruments.Tools
{
    public class PollingProgram<T> : IProgram<T>
        where T : IPollingRunnerContext
    {
        private readonly PollingRunner<T> _pollingRunner;

        public PollingProgram(IProgram<T> originalProgram)
        {
            _pollingRunner = new PollingRunner<T>(originalProgram.ExecuteAsync);
        }

        public async Task<int> ExecuteAsync(T context, CancellationToken cancellationToken)
        {
            await _pollingRunner.RunAsync(context, cancellationToken).ConfigureAwait(false);

            return 0;
        }
    }
}
