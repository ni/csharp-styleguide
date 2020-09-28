using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace NationalInstruments.Tools
{
    public class MultiVerbCommandLineProgram : IProgram<string[]>
    {
        private readonly Dictionary<Type, Func<object, CancellationToken, Task<int>>> _callbacks;
        private readonly Func<IEnumerable<Error>, int> _notParsedFunc;

        public MultiVerbCommandLineProgram()
            : this((errors) => 1)
        {
        }

        public MultiVerbCommandLineProgram(Func<IEnumerable<Error>, int> notParsedFunc)
        {
            _notParsedFunc = notParsedFunc;
            _callbacks = new Dictionary<Type, Func<object, CancellationToken, Task<int>>>();
        }

        public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
        {
            object options = null;
            Func<object, CancellationToken, Task<int>> callback = null;

            var exitCode = 0;

            Parser.Default.ParseArguments(args, _callbacks.Keys.ToArray())
                .WithParsed((obj) =>
                {
                    callback = _callbacks[obj.GetType()];
                    options = obj;
                })
                .WithNotParsed((errors) => exitCode = _notParsedFunc(errors));

            return callback == null ? exitCode : await callback(options, cancellationToken).ConfigureAwait(false);
        }

        public MultiVerbCommandLineProgram WithVerb<T>(IProgram<T> program)
        {
            VerifyVerb<T>();

            _callbacks.Add(typeof(T), (object context, CancellationToken token) => program.ExecuteAsync((T)context, token));

            return this;
        }

        private void VerifyVerb<T>()
        {
            var type = typeof(T);
            var verbType = typeof(VerbAttribute);

            var verbAttribute = type.GetCustomAttributes(typeof(VerbAttribute)).SingleOrDefault();

            if (verbAttribute == null)
            {
                throw new ArgumentException(type.FullName + " is not a verb! Add " + verbType.FullName + " to it.");
            }
        }
    }
}
