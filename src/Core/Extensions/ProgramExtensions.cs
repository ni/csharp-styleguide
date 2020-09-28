using System;
using System.Collections.Generic;
using CommandLine;
using NLog.Config;
using NLog.Extensions.Logging;

namespace NationalInstruments.Tools.Extensions
{
    public static class ProgramExtensions
    {
        public static IProgram<T> AddCancelable<T>(this IProgram<T> program)
        {
            return new CancelableProgram<T>(program);
        }

        public static IProgram<T> AddPolling<T>(this IProgram<T> program)
            where T : IPollingRunnerContext
        {
            return new PollingProgram<T>(program);
        }

        public static IProgram<T> AddNLog<T>(this IProgram<T> program)
        {
            ApplicationLogging.LoggerFactory.AddNLog();

            return program;
        }

        public static IProgram<T> ConfigureNLog<T>(this IProgram<T> program, LoggingConfiguration loggingConfiguration)
        {
            NLog.LogManager.Configuration = loggingConfiguration;

            return program;
        }
    }
}
