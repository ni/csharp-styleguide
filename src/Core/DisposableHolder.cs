using System;

namespace NationalInstruments.Tools
{
    /// <summary>
    /// Helper class for creating <see cref="DisposableHolder{T}"/>.  More convenient than calling the constructor directly
    /// because generic parameters can be inferred.
    /// </summary>
    public static class DisposableHolder
    {
        /// <summary>
        /// Create a <see cref="DisposableHolder{T}"/>
        /// </summary>
        /// <typeparam name="T">Type of disposable to hold</typeparam>
        /// <param name="acquire">Function that acquires the disposable object</param>
        /// <returns>The disposable holder.</returns>
        public static DisposableHolder<T> Create<T>(Func<T> acquire)
            where T : class, IDisposable
        {
            return new DisposableHolder<T>(acquire);
        }
    }
}
