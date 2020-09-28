using System;
using System.Threading;

namespace NationalInstruments.Tools.Extensions
{
    /// <summary>
    /// Extensions on <see cref="CancellationTokenSource"/>
    /// </summary>
    public static class CancellationTokenSourceExtensions
    {
        /// <summary>
        /// Convenience function to tie a <see cref="CancellationTokenSource"/> to another token, so when that token
        /// is canceled, it will automatically cancel this source as well.
        /// </summary>
        /// <param name="source">The source we want to cancel when <paramref name="token"/> is canceled.</param>
        /// <param name="token">The token we want to observe, and cancel <paramref name="source"/> when it is canceled.</param>
        /// <returns>CancellationTokenRegistration</returns>
        public static CancellationTokenRegistration LinkTo(this CancellationTokenSource source, CancellationToken token)
        {
            return token.Register(
                () =>
                {
                    try
                    {
                        source.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Just ignore this exception.  No way to unregister from a token.
                    }
                });
        }
    }
}
