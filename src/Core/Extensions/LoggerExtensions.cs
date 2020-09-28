using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace NationalInstruments.Tools.Extensions
{
    public static class LoggerExtensions
    {
        public static StopwatchLogger StartStopwatch(this ILogger logger, LogLevel logLevel = LogLevel.Information, [CallerMemberName] string timerName = null)
        {
            return new StopwatchLogger(logger, logLevel: logLevel, timerName: timerName);
        }
    }
}
