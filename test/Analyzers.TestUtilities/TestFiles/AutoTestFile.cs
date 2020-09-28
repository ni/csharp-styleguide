using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Tools.Analyzers.TestUtilities.Markers;
using NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.TestUtilities.TestFiles
{
    /// <summary>
    /// Fake for a project source file whose source code may contain markup denoting expected diagnostics.
    /// </summary>
    public struct AutoTestFile : IEquatable<AutoTestFile>, ITestFile
    {
        private readonly Rule[] _violatedRules;
        private readonly IList<SourceMarker> _markers;

        public AutoTestFile(string source, params Rule[] violatedRules)
            : this(DiagnosticVerifier.DefaultProjectName, source, violatedRules)
        {
        }

        public AutoTestFile(string projectName, string source, params Rule[] violatedRules)
            : this(null, projectName, source, violatedRules)
        {
        }

        public AutoTestFile(string name, string projectName, string source, params Rule[] violatedRules)
            : this(name, projectName, source, Enumerable.Empty<string>(), violatedRules)
        {
        }

        public AutoTestFile(string name, string projectName, string source, IEnumerable<string> referencedProjectNames, params Rule[] violatedRules)
        {
            Name = name;
            ProjectName = projectName;
            ReferencedProjectNames = referencedProjectNames;

            _violatedRules = violatedRules;
            _markers = new TestMarkup().Parse(source, out var cleanSource);

            Source = cleanSource;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string ProjectName { get; }

        /// <inheritdoc />
        public string Source { get; }

        /// <inheritdoc />
        public IEnumerable<string> ReferencedProjectNames { get; }

        /// <summary>
        /// <see cref="DiagnosticResult"/> instances expected to be reported from the provided markup.
        /// </summary>
        public IEnumerable<DiagnosticResult> ExpectedDiagnostics => GetExpectedDiagnostics(Name, _markers, _violatedRules);

        public static bool operator ==(AutoTestFile left, AutoTestFile right) => left.Equals(right);

        public static bool operator !=(AutoTestFile left, AutoTestFile right) => !(left == right);

        public override bool Equals(object obj) => !(obj is AutoTestFile) ? false : Equals((AutoTestFile)obj);

        public override int GetHashCode()
        {
            const int MagicValue = -1521134295;
            var hashCode = -1211575830;

            hashCode = (hashCode * MagicValue) + Name.GetHashCode();
            hashCode = (hashCode * MagicValue) + ProjectName?.GetHashCode() ?? 0;
            hashCode = (hashCode * MagicValue) + Source?.GetHashCode() ?? 0;
            hashCode = (hashCode * MagicValue) + ReferencedProjectNames?.GetHashCode() ?? 0;
            return (hashCode * MagicValue) + ExpectedDiagnostics?.GetHashCode() ?? 0;
        }

        public bool Equals(AutoTestFile other)
        {
            return Name == other.Name
                && ProjectName == other.ProjectName
                && Source == other.Source
                && ReferencedProjectNames == other.ReferencedProjectNames
                && ExpectedDiagnostics == other.ExpectedDiagnostics;
        }

        private static IEnumerable<DiagnosticResult> GetExpectedDiagnostics(string fileName, IList<SourceMarker> markers, params Rule[] violatedRules)
        {
            if ((!markers?.Any() ?? true) && (!violatedRules?.Any() ?? true))
            {
                yield break;
            }

            Assert.True(markers.Count == violatedRules.Length, "Number of markers should always equal the number of violated rules");

            for (var i = 0; i < markers.Count; ++i)
            {
                var rule = violatedRules[i];
                var marker = markers[i];
                var arguments = Enumerable.Empty<object>();

                if (marker is DiagnosticArgumentMarker)
                {
                    arguments = rule.Arguments;
                }
                else if (marker is DiagnosticTextMarker textMarker)
                {
                    arguments = new[] { textMarker.Text };
                }

                yield return DiagnosticVerifier.GetExpectedDiagnostic(
                    fileName ?? DiagnosticVerifier.DefaultFileName, marker.Line, marker.Column, rule.DiagnosticDescriptor, arguments.ToArray());
            }
        }
    }
}
