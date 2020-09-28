using System.Threading;
using System.Threading.Tasks;

namespace NationalInstruments.Tools.Extensions
{
    public static class CancellationTokenExtensions
    {
        public static Task ToTaskAsync(this CancellationToken token)
        {
            var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(() => completionSource.SetCanceled());
            return completionSource.Task;
        }
    }
}
