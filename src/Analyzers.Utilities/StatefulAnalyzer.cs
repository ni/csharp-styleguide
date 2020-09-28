using System.Threading;

namespace NationalInstruments.Tools.Analyzers.Utilities
{
    /// <summary>
    /// Base class for "stateful" analyzers as patterned <see href="https://github.com/dotnet/roslyn/blob/master/src/Samples/CSharp/Analyzers/CSharpAnalyzers/CSharpAnalyzers/StatefulAnalyzers/CompilationStartedAnalyzerWithCompilationWideAnalysis.cs">here</see>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Stateful", Justification = "Not misspelled and makes the most sense")]
    public abstract class StatefulAnalyzer
    {
        /// <summary>
        /// Constructor that stores a compilation's cancellation token.
        /// </summary>
        /// <param name="cancellationToken">Object that indicates if a cancellation was requested or not.</param>
        protected StatefulAnalyzer(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Token that informs the analyzer a cancellation was requested (controlled by the compilation).
        /// </summary>
        protected CancellationToken CancellationToken { get; }
    }
}
