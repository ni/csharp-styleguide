using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NationalInstruments.Tools
{
    // ReSharper disable LocalizableElement
    public class EscapeKeyMonitor : Disposable
    {
        private readonly CancellationTokenSource _internalTokenSource;
        private readonly CancellationTokenSource _tokenSource;
        private readonly Task _monitorTask;
        private IConsole _console;

        public EscapeKeyMonitor(CancellationTokenSource cancellationTokenSource)
            : this(new ConsoleInstance(), cancellationTokenSource)
        {
        }

        public EscapeKeyMonitor(IConsole console, CancellationTokenSource cancellationTokenSource)
        {
            if (!IsConsoleApplication())
            {
                throw new InvalidOperationException("Escape key monitor can be used only in console applications.");
            }

            _console = console;
            _tokenSource = cancellationTokenSource;
            _internalTokenSource = new CancellationTokenSource();
            _monitorTask = MonitorEscapeKeyAsync();
        }

        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<EscapeKeyMonitor>();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        protected override void DisposeManagedResources()
        {
            _internalTokenSource.Cancel();

            _monitorTask.Wait();

            _internalTokenSource.Dispose();
            base.DisposeManagedResources();
        }
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        private static bool IsConsoleApplication()
        {
            using (var standardInput = Console.OpenStandardInput(1))
            {
                return standardInput != Stream.Null;
            }
        }

        private async Task MonitorEscapeKeyAsync()
        {
            _console.WriteLine("Escape to exit");

            while (!_internalTokenSource.Token.IsCancellationRequested)
            {
                if (_console.KeyAvailable)
                {
                    ConsoleKeyInfo key = _console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        _console.WriteLine("Canceling");

                        Logger.LogInformation("!!! Canceling because the Escape key was pressed !!!");

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                        // Without the Task.Run, the cancellation call will execute and if the thing that is eventually cancelled does not spin
                        // up a different thread to execute Cancel, we will eventually get into the DisposeManagedResources call of this class
                        // which using the same thread the Cancel was called on, thus deadlock waiting for the _escapeKeyMonitor task to complete.
                        // We don't want to await the Task.Run call because that would essitially reintroduce the deadlock.
                        Task.Run(() => _tokenSource.Cancel());

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                        return;
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), _internalTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }
}
