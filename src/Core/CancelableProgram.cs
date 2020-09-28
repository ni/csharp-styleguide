using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NationalInstruments.Tools.Extensions;

namespace NationalInstruments.Tools
{
    public class CancelableProgram<T> : Cancelable, IProgram<T>
    {
        private IProgram<T> _originalProgram;

        public CancelableProgram(IProgram<T> originalProgram)
        {
            _originalProgram = originalProgram;
        }

        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<CancelableProgram<T>>();

        public virtual async Task<int> ExecuteAsync(T context, CancellationToken cancellationToken)
        {
            CancellationTokenSource.LinkTo(cancellationToken);

            int exitCode;

            using (new EscapeKeyMonitor(CancellationTokenSource))
            {
                exitCode = await _originalProgram.ExecuteAsync(context, CancellationToken).ConfigureAwait(false);
            }

            Logger.LogInformation("Exiting program with code " + exitCode.ToString(CultureInfo.InvariantCulture));

            return exitCode;
        }
    }
}
