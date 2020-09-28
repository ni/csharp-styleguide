using Microsoft.CodeAnalysis.Diagnostics;

namespace NationalInstruments.Tools.Analyzers.Utilities.Extensions
{
    /// <summary>
    /// Class that contains useful extensions to <see cref="AnalysisContext"/>.
    /// </summary>
    public static class AnalysisContextExtensions
    {
        /// <summary>
        /// Enables concurrent execution if the <paramref name="enable"/> flag is <c>True</c>.
        /// </summary>
        /// <param name="analysisContext">Context for initializing an analyzer.</param>
        /// <param name="enable">Whether concurrent execution should be enabled or not.</param>
        public static void EnableConcurrentExecutionIf(this AnalysisContext analysisContext, bool enable)
        {
            if (enable)
            {
                analysisContext.EnableConcurrentExecution();
            }
        }
    }
}
