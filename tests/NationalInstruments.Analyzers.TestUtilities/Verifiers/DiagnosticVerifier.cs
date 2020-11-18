using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Threading;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using Xunit;

namespace NationalInstruments.Analyzers.TestUtilities.Verifiers
{
    /// <summary>
    /// Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        protected const TestValidationModes DefaultTestValidationMode = TestValidationModes.ValidateErrors;

        /// <summary>
        /// Get the CSharp analyzer being tested - to be implemented in non-abstract class
        /// </summary>
        protected virtual DiagnosticAnalyzer DiagnosticAnalyzer => null;

        /// <summary>
        /// The test will verify that the diagnostic messages exactly match the expected messages if <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Override if you want to verify the messages using less strict methods (e.g., Assert.Matches or Assert.Contains).
        /// </remarks>
        protected virtual bool VerifyMessages => true;

        /// <summary>
        /// Verifies that the compilation emits the diagnostics provided in <paramref name="expectedDiagnostics"/>.
        /// </summary>
        /// <param name="testFile">A test file belonging to a project.</param>
        /// <param name="expectedDiagnostics">The diagnostics expected to occur from running an analyzer on the compilation.</param>
        /// <returns>The actual diagnostics.</returns>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(TestFile testFile, params DiagnosticResult[] expectedDiagnostics)
        {
            return VerifyDiagnostics(testFile, additionalFile: null, expectedDiagnostics: expectedDiagnostics);
        }

        /// <inheritdoc cref="VerifyDiagnostics(TestFile, DiagnosticResult[])"/>
        /// <param name="additionalFile">A supporting test file that will appear to the analyzer as an "additional file".</param>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(TestFile testFile, AdditionalText additionalFile, params DiagnosticResult[] expectedDiagnostics)
        {
            return VerifyDiagnostics(testFile, new[] { additionalFile }.Where(x => x != null), expectedDiagnostics);
        }

        /// <inheritdoc cref="VerifyDiagnostics(TestFile, DiagnosticResult[])"/>
        /// <param name="additionalFiles">Supporting test files that will appear to the analyzer as "additional files".</param>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(TestFile testFile, IEnumerable<AdditionalText> additionalFiles, params DiagnosticResult[] expectedDiagnostics)
        {
            return VerifyDiagnostics(testFile, additionalFiles, null, expectedDiagnostics);
        }

        /// <inheritdoc cref="VerifyDiagnostics(TestFile, IEnumerable{AdditionalText}, DiagnosticResult[])"/>
        /// <param name="projectAdditionalFiles">Mapping of project names to the <see cref="AdditionalText"/> files they include.</param>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(TestFile testFile, IEnumerable<AdditionalText> additionalFiles, Dictionary<string, IEnumerable<AdditionalText>> projectAdditionalFiles, params DiagnosticResult[] expectedDiagnostics)
        {
            return VerifyDiagnostics(new[] { testFile }, additionalFiles, projectAdditionalFiles, expectedDiagnostics);
        }

        /// <inheritdoc cref="VerifyDiagnostics(TestFile, DiagnosticResult[])"/>
        /// <param name="testFiles">Test files belonging to a project.</param>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(TestFile[] testFiles, params DiagnosticResult[] expectedDiagnostics)
        {
            return VerifyDiagnostics(testFiles, additionalFile: null, expectedDiagnostics: expectedDiagnostics);
        }

        /// <inheritdoc cref="VerifyDiagnostics(TestFile, AdditionalText, DiagnosticResult[])"/>
        /// <inheritdoc cref="VerifyDiagnostics(TestFile[], DiagnosticResult[])" path="param"/>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(TestFile[] testFiles, AdditionalText additionalFile, params DiagnosticResult[] expectedDiagnostics)
        {
            return VerifyDiagnostics(testFiles, new[] { additionalFile }.Where(x => x != null), expectedDiagnostics);
        }

        /// <inheritdoc cref="VerifyDiagnostics(TestFile, IEnumerable{AdditionalText}, DiagnosticResult[])"/>
        /// <inheritdoc cref="VerifyDiagnostics(TestFile[], DiagnosticResult[])" path="param"/>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(TestFile[] testFiles, IEnumerable<AdditionalText> additionalFiles, params DiagnosticResult[] expectedDiagnostics)
        {
            return VerifyDiagnostics(testFiles, additionalFiles, null, expectedDiagnostics);
        }

        /// <inheritdoc cref="VerifyDiagnostics(TestFile, IEnumerable{AdditionalText}, Dictionary{string, IEnumerable{AdditionalText}}, DiagnosticResult[])"/>
        /// <inheritdoc cref="VerifyDiagnostics(TestFile[], DiagnosticResult[])" path="param"/>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(TestFile[] testFiles, IEnumerable<AdditionalText> additionalFiles, Dictionary<string, IEnumerable<AdditionalText>> projectAdditionalFiles, params DiagnosticResult[] expectedDiagnostics)
        {
            var context = new JoinableTaskContext();
            var diagnostics = new JoinableTaskFactory(context).Run(
                async () => await GetSortedDiagnosticsAsync(DiagnosticAnalyzer, testFiles.Cast<ITestFile>().ToArray(), DefaultTestValidationMode, additionalFiles, projectAdditionalFiles));

            return VerifyDiagnosticResults(diagnostics, DiagnosticAnalyzer, expectedDiagnostics);
        }

        /// <summary>
        /// Verifies that the compilation emits the diagnostics parsed from the <paramref name="testFile"/>.
        /// </summary>
        /// <param name="testFile">A test file belonging to a project that may contain diagnostic markup.</param>
        /// <param name="additionalFile">A supporting test file that will appear to the analyzer as an "additional file".</param>
        /// <returns>The actual diagnostics.</returns>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(AutoTestFile testFile, AdditionalText additionalFile = null)
        {
            return VerifyDiagnostics(testFile, new[] { additionalFile }.Where(x => x != null));
        }

        /// <inheritdoc cref="VerifyDiagnostics(AutoTestFile, AdditionalText)"/>
        /// <param name="additionalFiles">Supporting test files that will appear to the analyzer as "additional files".</param>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(AutoTestFile testFile, IEnumerable<AdditionalText> additionalFiles)
        {
            return VerifyDiagnostics(testFile, additionalFiles, null);
        }

        /// <inheritdoc cref="VerifyDiagnostics(AutoTestFile, IEnumerable{AdditionalText})"/>
        /// <param name="projectAdditionalFiles">Mapping of project names to the <see cref="AdditionalText"/> files they include.</param>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(AutoTestFile testFile, IEnumerable<AdditionalText> additionalFiles, Dictionary<string, IEnumerable<AdditionalText>> projectAdditionalFiles)
        {
            return VerifyDiagnostics(new[] { testFile }, additionalFiles, projectAdditionalFiles);
        }

        /// <inheritdoc cref="VerifyDiagnostics(AutoTestFile, AdditionalText)"/>
        /// <param name="testFiles">Test files belonging to a project that may contain diagnostic markup.</param>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(AutoTestFile[] testFiles, AdditionalText additionalFile = null)
        {
            return VerifyDiagnostics(testFiles, new[] { additionalFile }.Where(x => x != null));
        }

        /// <inheritdoc cref="VerifyDiagnostics(AutoTestFile, IEnumerable{AdditionalText})"/>
        /// <inheritdoc cref="VerifyDiagnostics(AutoTestFile[], AdditionalText)" path="param"/>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(AutoTestFile[] testFiles, IEnumerable<AdditionalText> additionalFiles)
        {
            return VerifyDiagnostics(testFiles, additionalFiles, null);
        }

        /// <inheritdoc cref="VerifyDiagnostics(AutoTestFile, IEnumerable{AdditionalText}, Dictionary{string, IEnumerable{AdditionalText}})"/>
        /// <inheritdoc cref="VerifyDiagnostics(AutoTestFile[], AdditionalText)" path="param"/>
        protected IEnumerable<Diagnostic> VerifyDiagnostics(AutoTestFile[] testFiles, IEnumerable<AdditionalText> additionalFiles, Dictionary<string, IEnumerable<AdditionalText>> projectAdditionalFiles)
        {
            var context = new JoinableTaskContext();
            var diagnostics = new JoinableTaskFactory(context).Run(
                async () => await GetSortedDiagnosticsAsync(DiagnosticAnalyzer, testFiles.Cast<ITestFile>().ToArray(), DefaultTestValidationMode, additionalFiles, projectAdditionalFiles));
            var expectedDiagnostics = testFiles.SelectMany(x => x.ExpectedDiagnostics).ToArray();

            return VerifyDiagnosticResults(diagnostics, DiagnosticAnalyzer, expectedDiagnostics);
        }

        /// <summary>
        /// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
        /// </summary>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="diagnostic">The diagnostic that was found in the code</param>
        /// <param name="actual">The Location of the Diagnostic found in the code</param>
        /// <param name="expected">The DiagnosticResultLocation that should have been found</param>
        private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            Assert.True(
                actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                    expected.Path,
                    actualSpan.Path,
                    FormatDiagnostics(analyzer, diagnostic)));

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.Line)
                {
                    Assert.True(
                        false,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Line,
                            actualLinePosition.Line + 1,
                            FormatDiagnostics(analyzer, diagnostic)));
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.Column)
                {
                    Assert.True(
                        false,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Column,
                            actualLinePosition.Character + 1,
                            FormatDiagnostics(analyzer, diagnostic)));
                }
            }
        }

        /// <summary>
        /// Helper method to format a Diagnostic into an easily readable string
        /// </summary>
        /// <param name="analyzer">The analyzer that this verifier tests</param>
        /// <param name="diagnostics">The Diagnostics to be formatted</param>
        /// <returns>The Diagnostics formatted as a string</returns>
        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i].ToString());

                var analyzerType = analyzer.GetType();
                var rules = analyzer.SupportedDiagnostics;

                foreach (var rule in rules)
                {
                    if (rule != null && rule.Id == diagnostics[i].Id)
                    {
                        var location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat(CultureInfo.CurrentCulture, "GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                        }
                        else
                        {
                            Assert.True(
                                location.IsInSource,
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {0}\r\n",
                                    diagnostics[i]));

                            var resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ?
                                "GetCSharpResultAt" :
                                "GetBasicResultAt";
                            var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat(
                                CultureInfo.CurrentCulture,
                                "{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzerType.Name,
                                rule.Id);
                        }

                        if (i != diagnostics.Length - 1)
                        {
                            builder.Append(',');
                        }

                        builder.AppendLine();
                        break;
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
        /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
        /// </summary>
        /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
        /// <returns>The actual diagnostics.</returns>
        private IEnumerable<Diagnostic> VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, DiagnosticResult[] expectedResults)
        {
            var expectedCount = expectedResults.Length;
            var actualCount = actualResults.Count();

            if (expectedCount != actualCount)
            {
                var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";

                Assert.True(
                    false,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n",
                        expectedCount,
                        actualCount,
                        diagnosticsOutput));
            }

            for (var i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    if (actual.Location != Location.None)
                    {
                        Assert.True(
                            false,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Expected:\nA project diagnostic with No location\nActual:\n{0}",
                                FormatDiagnostics(analyzer, actual)));
                    }
                }
                else
                {
                    VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First());
                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Locations.Count - 1)
                    {
                        Assert.True(
                            false,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                expected.Locations.Count - 1,
                                additionalLocations.Length,
                                FormatDiagnostics(analyzer, actual)));
                    }

                    for (var j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
                    }
                }

                if (actual.Id != expected.Id)
                {
                    Assert.True(
                        false,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Id,
                            actual.Id,
                            FormatDiagnostics(analyzer, actual)));
                }

                if (actual.Severity != expected.Severity)
                {
                    Assert.True(
                        false,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Severity,
                            actual.Severity,
                            FormatDiagnostics(analyzer, actual)));
                }

                if (VerifyMessages)
                {
                    if (actual.GetMessage() != expected.Message)
                    {
                        Assert.True(
                            false,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Message,
                                actual.GetMessage(),
                                FormatDiagnostics(analyzer, actual)));
                    }
                }
            }

            return actualResults;
        }
    }
}
