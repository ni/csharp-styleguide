using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.Threading;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using Xunit;

namespace NationalInstruments.Analyzers.TestUtilities.Verifiers
{
    /// <summary>
    /// Superclass of all Unit tests made for diagnostics with codefixes.
    /// Contains methods used to verify correctness of codefixes
    /// </summary>
    public abstract partial class CodeFixVerifier : DiagnosticVerifier
    {
        /// <summary>
        /// Returns the codefix being tested (C#) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeFixProvider to be used for CSharp code</returns>
        protected virtual CodeFixProvider? CodeFixProvider => null;

        /// <summary>
        /// Called to test a C# codefix when applied on the inputted string as a source
        /// </summary>
        /// <param name="testBeforeFix">A test file belonging to a project that may contain markup and should contain diagnostics.</param>
        /// <param name="testAfterFix">A test file belonging to a project whose source should be "fixed".</param>
        /// <param name="additionalTestFiles">Other C# source files used for a test.</param>
        /// <param name="additionalFiles">Supporting test files that will appear to the analyzer as "additional files".</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        protected void VerifyFix(
            ITestFile testBeforeFix,
            TestFile testAfterFix,
            IEnumerable<ITestFile>? additionalTestFiles = null,
            IEnumerable<AdditionalText>? additionalFiles = null,
            int? codeFixIndex = null,
            bool allowNewCompilerDiagnostics = false)
        {
            var context = new JoinableTaskContext();
            new JoinableTaskFactory(context).Run(
                async () => await VerifyFixAsync(
                    DiagnosticAnalyzer,
                    CodeFixProvider,
                    testBeforeFix,
                    testAfterFix,
                    additionalTestFiles,
                    additionalFiles,
                    codeFixIndex,
                    allowNewCompilerDiagnostics));
        }

        /// <summary>
        /// General verifier for codefixes.
        /// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
        /// Then gets the string after the codefix is applied and compares it with the expected result.
        /// Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
        /// </summary>
        /// <param name="analyzer">The analyzer to be applied to the source code</param>
        /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
        /// <param name="testBeforeFix">A test file belonging to a project that may contain markup and should contain diagnostics.</param>
        /// <param name="testAfterFix">A test file belonging to a project whose source should be "fixed".</param>
        /// <param name="additionalTestFiles">Other C# source files used for a test.</param>
        /// <param name="additionalFiles">Supporting test files that will appear to the analyzer as "additional files".</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        private static async Task VerifyFixAsync(
            DiagnosticAnalyzer? analyzer,
            CodeFixProvider? codeFixProvider,
            ITestFile testBeforeFix,
            TestFile testAfterFix,
            IEnumerable<ITestFile>? additionalTestFiles,
            IEnumerable<AdditionalText>? additionalFiles,
            int? codeFixIndex,
            bool allowNewCompilerDiagnostics)
        {
            var testFiles = new ITestFile[] { testBeforeFix };
            testFiles = testFiles.Concat(additionalTestFiles ?? Enumerable.Empty<ITestFile>()).ToArray();
            var document = GetDocuments(testFiles).First();
            var analyzerDiagnostics = (await GetSortedDiagnosticsAsync(
                analyzer,
                testFiles,
                additionalFiles: additionalFiles).ConfigureAwait(false)).ToList();
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(document).ConfigureAwait(false);
            var attempts = analyzerDiagnostics.Count;

            for (var i = 0; i < attempts; ++i)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostics.FirstOrDefault(), (a, d) => actions.Add(a), CancellationToken.None);
                await (codeFixProvider?.RegisterCodeFixesAsync(context) ?? Task.CompletedTask).ConfigureAwait(false);

                if (!actions.Any())
                {
                    break;
                }

                if (codeFixIndex != null)
                {
                    document = await ApplyFixAsync(document, actions.ElementAt((int)codeFixIndex)).ConfigureAwait(false);
                    break;
                }

                document = await ApplyFixAsync(document, actions.ElementAt(0)).ConfigureAwait(false);
                if (document is null)
                {
                    break;
                }

                analyzerDiagnostics = (await GetSortedDiagnosticsAsync(
                    analyzer,
                    new[] { document },
                    additionalFiles: additionalFiles).ConfigureAwait(false)).ToList();

                var compilerDiagnosticsNow = await GetCompilerDiagnosticsAsync(document).ConfigureAwait(false);
                var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, compilerDiagnosticsNow);

                // check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                    var updatedCompilerDiagnostics = await GetCompilerDiagnosticsAsync(document).ConfigureAwait(false);
                    if (syntaxRoot is not null)
                    {
                        document = document.WithSyntaxRoot(Formatter.Format(syntaxRoot, Formatter.Annotation, document.Project.Solution.Workspace));
                    }
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, updatedCompilerDiagnostics);

                    Assert.Fail(string.Format(
                        CultureInfo.InvariantCulture,
                        "Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
                        string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
                        syntaxRoot?.ToFullString()));
                }

                // check if there are analyzer diagnostics left after the code fix
                if (!analyzerDiagnostics.Any())
                {
                    break;
                }
            }

            // after applying all of the code fixes, compare the resulting string to the inputted one
            var actual = await GetStringFromDocumentAsync(document).ConfigureAwait(false);
            Assert.Equal(testAfterFix.Source, actual);

            // TODO: Enable if a codefix move the file into a different project
            // Assert.Equal(testAfterFix.ProjectName, document.Project.Name);
        }
    }
}
