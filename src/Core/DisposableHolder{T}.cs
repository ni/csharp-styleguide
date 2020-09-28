using System;

namespace NationalInstruments.Tools
{
    /// <summary>
    /// A utility class that allows a using block to temporarily hold on to an <see cref="IDisposable"/>.
    /// See remarks for details.
    /// </summary>
    /// <remarks>
    /// This is a useful class to correct CA2000 violations.  You can use it when a simple using block around
    /// the disposable itself is insufficient, usually because you want to be able to exit the current scope when the
    /// object is still alive.  Consider this code:
    /// <code>
    /// Foo CreateFoo()
    /// {
    ///     Foo foo = new Foo(); // Foo implements IDisposable
    ///
    ///     foo.Initialize();
    ///     return foo;
    /// }
    /// </code>
    /// The above code gives a CA2000 violation, because if Initialize() throws, you leak a foo.  "Fixing" it by simply
    /// putting in a using block clears the violation but is clearly wrong:
    /// <code>
    /// Foo CreateFoo()
    /// {
    ///     using(Foo foo = new Foo()) // Foo implements IDisposable
    ///     {
    ///         foo.Initialize();
    ///         return foo;
    ///     }
    /// }
    /// </code>
    /// Oops.  You just disposed the foo you're supposed to be creating and handing off to the client.  So no
    /// dice there.  The fix to this issue suggested
    /// by Microsoft involves some verbose and ugly boilerplate code involving a try/finally, but this class provides
    /// a prettier alternative.  Here is how you would handle the above case:
    /// <code>
    /// Foo CreateFoo()
    /// {
    ///     using(var holder = new DisposableHolder&lt;Foo&gt;(() =&gt; new Foo()) // Foo implements IDisposable
    ///     {
    ///         holder.Value.Initialize();
    ///         return holder.Release();
    ///     }
    /// }
    /// </code>
    /// That's almost as pretty as the (incorrect) second using block example, but actually works, because Release()
    /// "detaches" foo from the holder before returning it, but will still properly dispose it if Initialize() throws.
    /// </remarks>
    /// <typeparam name="T">The type of the thing being held, must implement <see cref="IDisposable"/></typeparam>
    public class DisposableHolder<T> : Disposable
        where T : class, IDisposable
    {
        private T _disposable;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableHolder{T}"/> class.
        /// </summary>
        /// <param name="acquire">The function that creates the disposable held object.</param>
        public DisposableHolder(Func<T> acquire)
        {
            _disposable = acquire();
        }

        /// <summary>
        /// Gets the held object acquired in the constructor.
        /// </summary>
        public T Value
        {
            get
            {
                ThrowIfDisposed();

                return _disposable;
            }
        }

        /// <summary>
        /// Release the held object (so that the disposal of the holder will no longer dispose the object) and
        /// return it.
        /// </summary>
        /// <returns>The held object.</returns>
        /// <exception cref="InvalidOperationException">Thrown if you try to release an already-released
        /// object.</exception>
        public T Release()
        {
            ThrowIfDisposed();

            if (_disposable == null)
            {
                throw new InvalidOperationException("Tried to release when the contained object is null.");
            }

            T temp = _disposable;

            _disposable = null;
            return temp;
        }

        /// <summary>
        /// Release the current owned object if any (as if we called <see cref="Release"/>,
        /// and then assume ownership of an object owned by another disposable holder.
        /// </summary>
        /// <param name="holder2">Other holder that we want to "steal" ownership from.</param>
        /// <returns>The object being held</returns>
        public T ReleaseAndAssumeOwnershipOf(DisposableHolder<T> holder2)
        {
            ThrowIfDisposed();

            T temp = _disposable;

            _disposable = holder2.Release();
            return temp;
        }

        /// <summary>
        /// Release the current object (as if you called <see cref="Release"/>) then
        /// acquire a new object.  This is exception safe so that if <paramref name="acquire"/>
        /// throws, we retain ownership of the old object.
        /// </summary>
        /// <param name="acquire">Function called to get the thing we want to take ownership of.</param>
        /// <returns>The old value as it would have been returned from <see cref="Release"/></returns>
        public T ReleaseAndReacquire(Func<T> acquire)
        {
            ThrowIfDisposed();

            T temp = _disposable;
            T temp2 = acquire();

            _disposable = temp2;
            return temp;
        }

        /// <inheritdoc />
        protected override void DisposeManagedResources()
        {
            _disposable?.Dispose();
        }
    }
}
