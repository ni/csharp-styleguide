using Microsoft.CodeAnalysis.Diagnostics;

namespace NationalInstruments.Analyzers.Utilities
{
    /// <summary>
    /// Base class for analyzers written specifically for NI.
    /// </summary>
    public abstract class NIDiagnosticAnalyzer : DiagnosticAnalyzer
    {
#if DEBUG
        protected const bool InDebugMode = true;
#else
        protected const bool InDebugMode = false;
#endif

        /// <summary>
        /// Boolean that indicates if this analyzer is running against real vs test code.
        /// </summary>
        public bool IsRunningInProduction { get; set; } = true;
    }
}
