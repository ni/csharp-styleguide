using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace NationalInstruments.Tools.TestUtilities
{
    public class XunitLogger : ILogger
    {
        private readonly string _categoryName;

        public XunitLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public ITestOutputHelper TestOutputHelper { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            TestOutputHelper?.WriteLine($"{_categoryName}: [{eventId}] {logLevel}: {formatter(state, exception)}");
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) => Disposable.Empty;
    }
}
