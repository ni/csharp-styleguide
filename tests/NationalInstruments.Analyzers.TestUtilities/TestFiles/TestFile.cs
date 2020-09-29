using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;

namespace NationalInstruments.Analyzers.TestUtilities.TestFiles
{
    /// <summary>
    /// Fake for a project source file.
    /// </summary>
    public struct TestFile : IEquatable<TestFile>, ITestFile
    {
        public TestFile(string source)
            : this(DiagnosticVerifier.DefaultProjectName, source)
        {
        }

        public TestFile(string projectName, string source)
            : this(null, projectName, source)
        {
        }

        public TestFile(string name, string projectName, string source)
            : this(name, projectName, source, Enumerable.Empty<string>())
        {
        }

        public TestFile(string name, string projectName, string source, IEnumerable<string> referencedProjectNames)
        {
            Name = name;
            ProjectName = projectName;
            Source = source;
            ReferencedProjectNames = referencedProjectNames;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string ProjectName { get; }

        /// <inheritdoc />
        public string Source { get; }

        /// <inheritdoc />
        public IEnumerable<string> ReferencedProjectNames { get; }

        public static bool operator ==(TestFile left, TestFile right) => left.Equals(right);

        public static bool operator !=(TestFile left, TestFile right) => !(left == right);

        public override bool Equals(object obj) => !(obj is TestFile) ? false : Equals((TestFile)obj);

        public override int GetHashCode()
        {
            const int MagicValue = -1521134295;
            var hashCode = -1211575830;

            hashCode = (hashCode * MagicValue) + Name.GetHashCode();
            hashCode = (hashCode * MagicValue) + ProjectName?.GetHashCode() ?? 0;
            hashCode = (hashCode * MagicValue) + Source?.GetHashCode() ?? 0;
            return (hashCode * MagicValue) + ReferencedProjectNames?.GetHashCode() ?? 0;
        }

        public bool Equals(TestFile other)
        {
            return Name == other.Name
                && ProjectName == other.ProjectName
                && ReferencedProjectNames == other.ReferencedProjectNames;
        }
    }
}
