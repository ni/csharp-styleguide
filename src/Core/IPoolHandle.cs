using System;

namespace NationalInstruments.Tools.Core
{
    public interface IPoolHandle<out T> : IDisposable
    {
        /// <summary>
        ///     Gets value.
        /// </summary>
        T Value { get; }

        /// <summary>
        ///     Asserts validity.
        /// </summary>
        void AssertValid();
    }
}
