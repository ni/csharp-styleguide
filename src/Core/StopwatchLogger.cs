using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace NationalInstruments.Tools
{
    public class StopwatchLogger : Disposable
    {
        private readonly string _timerName;
        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;
        private readonly Stopwatch _stopwatch;

        public StopwatchLogger(ILogger logger, LogLevel logLevel = LogLevel.Information, [CallerMemberName]string timerName = null)
        {
            _timerName = timerName;
            _logger = logger;
            _logLevel = logLevel;
            _logger.Log(_logLevel, "Starting timer {}", timerName);
            _stopwatch = Stopwatch.StartNew();
        }

        protected override void DisposeManagedResources()
        {
            if (_stopwatch != null)
            {
                _logger.Log(_logLevel, "Timer {} measured {} ms", _timerName, _stopwatch.Elapsed.TotalMilliseconds);
            }

            base.DisposeManagedResources();
        }
    }
}
