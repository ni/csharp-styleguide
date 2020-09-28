using Microsoft.Extensions.Logging;

namespace NationalInstruments.Tools
{
    public static class ApplicationLogging
    {
        public static ILoggerFactory LoggerFactory { get; } = new LoggerFactory();

        public static ILogger None => new EmptyLogger();

        public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

        public static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
    }
}
