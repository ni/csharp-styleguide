using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace NationalInstruments.Tools
{
    public class SimpleCommandLineProgram<T> : IProgram<string[]>
    {
        private readonly IProgram<T> _program;
        private readonly Func<IEnumerable<Error>, int> _notParsedFunc;

        public SimpleCommandLineProgram(IProgram<T> program)
            : this(program, (errors) => 1)
        {
        }

        public SimpleCommandLineProgram(IProgram<T> program, Func<IEnumerable<Error>, int> notParsedFunc)
        {
            _program = program;
            _notParsedFunc = notParsedFunc;
        }

        public Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(Parser.Default.ParseArguments<T>(args)
                    .MapResult(t => _program.ExecuteAsync(t, cancellationToken).Result, _notParsedFunc));
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}
