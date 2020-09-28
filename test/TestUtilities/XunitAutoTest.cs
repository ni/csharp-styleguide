using Xunit.Abstractions;

namespace NationalInstruments.Tools.TestUtilities
{
    public abstract class XunitAutoTest : Disposable
    {
        private static readonly XunitLoggerProvider LoggerProvider = new XunitLoggerProvider();

        static XunitAutoTest()
        {
            ApplicationLogging.LoggerFactory.AddProvider(LoggerProvider);
        }

        protected XunitAutoTest(ITestOutputHelper testOutputHelper)
        {
            LoggerProvider.SetTestOutputHelper(testOutputHelper);
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            LoggerProvider.SetTestOutputHelper(null);
        }
    }
}
