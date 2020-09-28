using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NationalInstruments.Tools
{
    public class SimpleDiCommandLineProgram<TContext, TProgram> : IProgram<string[]>
        where TProgram : IProgram<TContext>
        where TContext : class
    {
        private readonly ServiceCollection _services;
        private readonly Func<IEnumerable<Error>, int> _notParsedFunc;
        private Action<ServiceCollection> addServicesAction = null;

        public SimpleDiCommandLineProgram()
            : this((errors) => 1)
        {
        }

        public SimpleDiCommandLineProgram(Func<IEnumerable<Error>, int> notParsedFunc)
        {
            _services = new ServiceCollection();
            _notParsedFunc = notParsedFunc;
        }

        public SimpleDiCommandLineProgram<TContext, TProgram> AddServices(Action<ServiceCollection> action)
        {
            addServicesAction = action;
            return this;
        }

        public Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
        {
            try
            {
                addServicesAction?.Invoke(_services);

                var serviceProvider = _services.BuildServiceProvider();
                var program = serviceProvider.GetRequiredService<TProgram>();

                return Parser.Default
                    .ParseArguments<TContext>(args)
                    .MapResult(
                        async t => await program.ExecuteAsync(t, cancellationToken),
                        errors => Task.FromResult(_notParsedFunc(errors)));
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}
