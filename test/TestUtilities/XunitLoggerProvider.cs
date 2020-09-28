using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace NationalInstruments.Tools.TestUtilities
{
    public class XunitLoggerProvider : Disposable, ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, XunitLogger> _loggers = new ConcurrentDictionary<string, XunitLogger>();
        private ITestOutputHelper _testOutputHelper;

        public void SetTestOutputHelper(ITestOutputHelper outputHelper)
        {
            _testOutputHelper = outputHelper;

            foreach (var logger in _loggers.Values)
            {
                logger.TestOutputHelper = outputHelper;
            }
        }

        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, x => new XunitLogger(x) { TestOutputHelper = _testOutputHelper });
    }
}
