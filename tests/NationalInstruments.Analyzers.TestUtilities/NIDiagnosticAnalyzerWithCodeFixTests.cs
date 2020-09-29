using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using NationalInstruments.Analyzers.Utilities;
using Xunit;

namespace NationalInstruments.Analyzers.TestUtilities
{
    /// <summary>
    /// Base class for tests against <see cref="NIDiagnosticAnalyzer">NIDiagnosticAnalyzers</see>
    /// and their code fix.
    /// </summary>
    /// <typeparam name="TAnalyzer">A Roslyn analyzer.</typeparam>
    /// <typeparam name="TCodeFix">A Roslyn codefix.</typeparam>
    public abstract class NIDiagnosticAnalyzerWithCodeFixTests<TAnalyzer, TCodeFix> : CodeFixVerifier
        where TAnalyzer : NIDiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        protected override DiagnosticAnalyzer DiagnosticAnalyzer { get; } = new TAnalyzer()
        {
            IsRunningInProduction = false,
        };

        protected override CodeFixProvider CodeFixProvider { get; } = new TCodeFix();

        protected IEnumerable<TestAdditionalDocument> GetAdditionalFiles(string fileNameBase, string extension, params string[] contents)
        {
            var i = 0;
            foreach (var content in contents)
            {
                yield return new TestAdditionalDocument($"{fileNameBase}{i++}.{extension}", content);
            }
        }

        protected Dictionary<string, IEnumerable<AdditionalText>> GetProjectAdditionalFiles(string project, params AdditionalText[] additionalFiles)
            => new Dictionary<string, IEnumerable<AdditionalText>> { { project, additionalFiles } };

        protected DiagnosticResult GetResult(DiagnosticDescriptor rule, params object[] messageArguments)
        {
            return GetResult(DefaultFileName, rule, messageArguments);
        }

        protected DiagnosticResult GetResult(string fileName, DiagnosticDescriptor rule, params object[] messageArguments)
        {
            return GetResultAt(fileName, -1, -1, rule, messageArguments);
        }

        protected DiagnosticResult GetResultAt(int line, int column, DiagnosticDescriptor rule, params object[] messageArguments)
        {
            // Each unit test creates its own unique project (by id), so using this constant filename
            // in each project does not prevent tests from running in parallel.
            return GetResultAt(DefaultFileName, line, column, rule, messageArguments);
        }

        protected DiagnosticResult GetResultAt(string fileName, int line, int column, DiagnosticDescriptor rule, params object[] messageArguments)
        {
            return GetExpectedDiagnostic(fileName, line, column, rule, messageArguments);
        }
    }
}
