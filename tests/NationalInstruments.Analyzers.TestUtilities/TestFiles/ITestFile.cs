using System.Collections.Generic;

namespace NationalInstruments.Analyzers.TestUtilities.TestFiles
{
    /// <summary>
    /// Interface for fake project source files.
    /// </summary>
    public interface ITestFile
    {
        /// <summary>
        /// Name of the file.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Name of the project containing the file.
        /// </summary>
        string ProjectName { get; }

        /// <summary>
        /// Source code in the file.
        /// </summary>
        string Source { get; }

        /// <summary>
        /// Names of projects to reference.
        /// </summary>
        IEnumerable<string> ReferencedProjectNames { get; }
    }
}
