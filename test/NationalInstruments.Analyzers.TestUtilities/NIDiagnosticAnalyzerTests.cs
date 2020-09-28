using NationalInstruments.Analyzers.Utilities;

namespace NationalInstruments.Analyzers.TestUtilities
{
    /// <summary>
    /// Base class for tests against <see cref="NIDiagnosticAnalyzer">NIDiagnosticAnalyzers</see>.
    /// </summary>
    /// <typeparam name="TAnalyzer">A Roslyn analyzer.</typeparam>
    public abstract class NIDiagnosticAnalyzerTests<TAnalyzer> : NIDiagnosticAnalyzerWithCodeFixTests<TAnalyzer, DefaultCodeFixProvider>
        where TAnalyzer : NIDiagnosticAnalyzer, new()
    {
    }
}
