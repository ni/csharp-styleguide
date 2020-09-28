using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Tools.Analyzers.TestUtilities.TestFiles;

namespace NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers
{
    /// <summary>
    /// Class for turning strings into documents and getting the diagnostics on them.
    /// All methods are static.
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        public const string DefaultProjectName = "TestProject";
        // Similar to how the Roslyn Analyzers project obtains their default file path:
        // https://github.com/dotnet/roslyn-analyzers/blob/569bd373a4831d3035597197e02980b57602a7f2/src/Tools/AnalyzerCodeGenerator/template/src/test/Utilities/DiagnosticAnalyzerTestBase.cs#L36
        public const string DefaultFileName = DefaultFileNamePrefix + "0." + DefaultFileExtension;

        internal const string DefaultFileNamePrefix = "Test";
        internal const string DefaultFileExtension = "cs";

        private static readonly string AssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));
        private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.dll"));
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));
        private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.Runtime.dll"));
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        private static readonly MetadataReference WindowsBaseReference = MetadataReference.CreateFromFile(typeof(IWeakEventListener).Assembly.Location);
        private static readonly MetadataReference MsTestReference = MetadataReference.CreateFromFile(typeof(TestClassAttribute).Assembly.Location);

        /// <summary>
        /// Returns a <see cref="DiagnosticResult"/> created from the provided parameters.
        /// </summary>
        /// <param name="fileName">Name of the file where the diagnostic is expected.</param>
        /// <param name="line">Line where the diagnostic is expected to occur.</param>
        /// <param name="column">Column where the diagnostic is expected to occur.</param>
        /// <param name="rule">Rule that is being violated.</param>
        /// <param name="messageArguments">Any arguments that should be substituted into the rule's reported message.</param>
        /// <returns>A <see cref="DiagnosticResult"/> created from the provided parameters.</returns>
        internal static DiagnosticResult GetExpectedDiagnostic(string fileName, int line, int column, DiagnosticDescriptor rule, params object[] messageArguments)
        {
            return new DiagnosticResult(
                rule.Id,
                string.Format(CultureInfo.CurrentCulture, rule.MessageFormat.ToString(CultureInfo.CurrentCulture), messageArguments),
                rule.DefaultSeverity,
                new DiagnosticResultLocation(fileName, line, column));
        }

        /// <summary>
        /// Returns the diagnostics found in the given <paramref name="testFiles"/> for the given <paramref name="analyzer"/>.
        /// </summary>
        /// <param name="analyzer">The analyzer to be run on the sources.</param>
        /// <param name="testFiles">An array of <see cref="TestFile"/> that contain source and project information.</param>
        /// <param name="validationModes">Flags that specify which compilation diagnostics can cause a failure.</param>
        /// <param name="additionalFiles">Enumerable of <see cref="AdditionalText" /> that will appear to the analyzer as additional files.</param>
        /// <param name="projectAdditionalFiles">Mapping of project names to the <see cref="AdditionalText"/> files they include.</param>
        /// <returns>An <see cref="IEnumerable{Diagnostic}" /> that surfaced in the source code, sorted by Location.</returns>
        protected static async Task<IEnumerable<Diagnostic>> GetSortedDiagnosticsAsync(
            DiagnosticAnalyzer analyzer,
            IEnumerable<ITestFile> testFiles,
            TestValidationModes validationModes = DefaultTestValidationMode,
            IEnumerable<AdditionalText> additionalFiles = null,
            Dictionary<string, IEnumerable<AdditionalText>> projectAdditionalFiles = null)
        {
            return await GetSortedDiagnosticsAsync(
                analyzer,
                GetDocuments(testFiles),
                validationModes,
                additionalFiles,
                projectAdditionalFiles).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the diagnostics found in the given <paramref name="testFiles"/> for the given <paramref name="analyzer"/>.
        /// </summary>
        /// <param name="analyzer">The analyzer to be run on the sources.</param>
        /// <param name="documents">The <see cref="Document">Documents</see> that the analyzer will be run on.</param>
        /// <param name="validationModes">Flags that specify which compilation diagnostics can cause a failure.</param>
        /// <param name="additionalFiles">Enumerable of <see cref="AdditionalText"/> that will appear to the analyzer as additional files.</param>
        /// <param name="projectAdditionalFiles">Mapping of project name to the array of <see cref="AdditionalText"/> it includes.</param>
        /// <returns>An <see cref="IEnumerable{Diagnostic}" /> that surfaced in the source code, sorted by Location.</returns>
        protected static async Task<IEnumerable<Diagnostic>> GetSortedDiagnosticsAsync(
            DiagnosticAnalyzer analyzer,
            IEnumerable<Document> documents,
            TestValidationModes validationModes = DefaultTestValidationMode,
            IEnumerable<AdditionalText> additionalFiles = null,
            Dictionary<string, IEnumerable<AdditionalText>> projectAdditionalFiles = null)
        {
            return await GetSortedDiagnosticsFromDocumentsAsync(
                analyzer,
                documents,
                validationModes,
                additionalFiles,
                projectAdditionalFiles).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates project(s) and documents from <paramref name="testFiles"/>.
        /// </summary>
        /// <param name="testFiles">An array of <see cref="TestFile"/> that contain source and project information.</param>
        /// <returns>All documents created for all projects.</returns>
        protected static IEnumerable<Document> GetDocuments(IEnumerable<ITestFile> testFiles)
        {
            if (!testFiles.Any())
            {
                return Enumerable.Empty<Document>();
            }

            var projects = new List<Project>();
            var allDocuments = new List<Document>();
            var projectsAndTestFiles = testFiles.ToLookup(x => x.ProjectName, x => x);
            List<string> projectsInOrder = GetOrderToCreateProjects(projectsAndTestFiles);
            using (var workspace = new AdhocWorkspace())
            {
                var solution = workspace.CurrentSolution;
                foreach (var projectName in projectsInOrder)
                {
                    var project = CreateProject(ref solution, projectName, projectsAndTestFiles[projectName], projects);
                    projects.Add(project);

                    var documents = project.Documents.ToArray();

                    if (projectsAndTestFiles[projectName].Count() != documents.Length)
                    {
                        throw new InvalidOperationException("Number of sources did not match number of Documents created");
                    }

                    allDocuments.AddRange(documents);
                }
            }

            return allDocuments;
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents.</param>
        /// <param name="documents">The <see cref="Document">Documents</see> that the analyzer will be run on.</param>
        /// <param name="validationModes">Flags that specify which compilation diagnostics can cause a failure.</param>
        /// <param name="additionalFiles">Enumerable of <see cref="AdditionalText" /> that will appear to the analyzer as additional files.</param>
        /// <returns>An <see cref="IEnumerable{Diagnostic}" /> that surfaced in the source code, sorted by Location.</returns>
        private static async Task<IEnumerable<Diagnostic>> GetSortedDiagnosticsFromDocumentsAsync(
            DiagnosticAnalyzer analyzer,
            IEnumerable<Document> documents,
            TestValidationModes validationModes,
            IEnumerable<AdditionalText> additionalFiles,
            Dictionary<string, IEnumerable<AdditionalText>> projectAdditionalFiles)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var relevantDiagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync().ConfigureAwait(false);

                compilation = EnableDiagnostics(analyzer, compilation);

                if (validationModes != TestValidationModes.None)
                {
                    ValidateCompilation(compilation, validationModes);
                }

                var allProjectsFiles = additionalFiles ?? Enumerable.Empty<AdditionalText>();
                var allAdditionalFiles = projectAdditionalFiles != null && projectAdditionalFiles.TryGetValue(project.Name, out var thisProjectsFiles)
                    ? allProjectsFiles.Union(thisProjectsFiles)
                    : allProjectsFiles;
                var analyzerOptions = new AnalyzerOptions(ImmutableArray.Create(allAdditionalFiles.ToArray()));
                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer), analyzerOptions);

                var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
                foreach (var diagnostic in diagnostics)
                {
                    if (diagnostic.Location == Location.None || diagnostic.Location.IsInMetadata)
                    {
                        relevantDiagnostics.Add(diagnostic);
                    }
                    else
                    {
                        foreach (var document in documents)
                        {
                            var tree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);
                            if (tree == diagnostic.Location.SourceTree)
                            {
                                relevantDiagnostics.Add(diagnostic);
                            }
                        }
                    }
                }
            }

            return SortDiagnostics(relevantDiagnostics);
        }

        private static Compilation EnableDiagnostics(DiagnosticAnalyzer analyzer, Compilation compilation)
        {
            var specificOptions = compilation.Options.SpecificDiagnosticOptions;
            specificOptions = specificOptions.AddRange(
                analyzer.SupportedDiagnostics.Select(GetOptionToEnableDiagnostic).Where(x => x.Value != ReportDiagnostic.Default));
            compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(specificOptions));
            return compilation;
        }

        private static KeyValuePair<string, ReportDiagnostic> GetOptionToEnableDiagnostic(DiagnosticDescriptor descriptor)
        {
            var error = ReportDiagnostic.Default;
            if (!descriptor.IsEnabledByDefault)
            {
                switch (descriptor.DefaultSeverity)
                {
                    case DiagnosticSeverity.Error:
                        error = ReportDiagnostic.Error;
                        break;
                    case DiagnosticSeverity.Hidden:
                        error = ReportDiagnostic.Hidden;
                        break;
                    case DiagnosticSeverity.Info:
                        error = ReportDiagnostic.Info;
                        break;
                    case DiagnosticSeverity.Warning:
                        error = ReportDiagnostic.Warn;
                        break;
                    default:
                        break;
                }
            }

            return new KeyValuePair<string, ReportDiagnostic>(descriptor.Id, error);
        }

        /// <summary>
        /// Ensures no compilation diagnostics exist with severities that match what's specified in
        /// <paramref name="validationModes"/>
        /// </summary>
        /// <remarks>
        /// The idea is taken from the Roslyn Analyzers' project, though their implementation differs greatly (different APIs):
        /// https://github.com/dotnet/roslyn-analyzers/blob/569bd373a4831d3035597197e02980b57602a7f2/src/Test/Utilities/DiagnosticExtensions.cs
        /// </remarks>
        /// <param name="compilation">An object that allows us to obtain the warnings, errors, ... from a compilation</param>
        /// <param name="validationModes">Flags that specify which compilation diagnostics can cause a failure</param>
        private static void ValidateCompilation(Compilation compilation, TestValidationModes validationModes)
        {
            // Ignore the diagnostics about missing a main method (CS5001, BC30420) or not being able to find a type/namespace (CS0246)
            // Ids taken from: https://github.com/Vannevelj/RoslynTester/blob/master/RoslynTester/RoslynTester/Helpers/DiagnosticVerifier.cs
            var ignoredIds = new HashSet<string>(new[] { "CS5001", "BC30420", "CS0246" });

            var severityMapping = new Dictionary<DiagnosticSeverity, TestValidationModes>
            {
                { DiagnosticSeverity.Error, TestValidationModes.ValidateErrors },
                { DiagnosticSeverity.Warning, TestValidationModes.ValidateWarnings },
            };

            // Create a mapping from TestValidationMode to a list of compilation diagnostics. If no
            // compilation diagnostics exist for a given TestValidationMode, no entry is created
            var compileDiagnostics = compilation.GetDiagnostics()
                .Where(diagnostic =>
                    !ignoredIds.Contains(diagnostic.Id) &&
                    severityMapping.ContainsKey(diagnostic.Severity) &&
                    validationModes.HasFlag(severityMapping[diagnostic.Severity]))
                .GroupBy(diagnostic => diagnostic.Severity)
                .Select(g => new { Key = severityMapping[g.Key], Values = g.ToList() })
                .ToDictionary(g => g.Key, g => g.Values);

            if (compileDiagnostics.Keys.Any())
            {
                // Helper function that returns two different strings. The first looks like "1 error(s)" and the
                // second like "TestValidationModes.ValidateErrors". Both will be used to construct a helpful message
                var getMessageParts = new Func<TestValidationModes, string, Tuple<string, string>>((mode, whats) =>
                {
                    return validationModes.HasFlag(mode) && compileDiagnostics.ContainsKey(mode)
                        ? new Tuple<string, string>(
                            string.Format(CultureInfo.CurrentCulture, "{0} {1}", compileDiagnostics[mode].Count, whats),
                            string.Format(CultureInfo.CurrentCulture, "{0}.{1}", nameof(TestValidationModes), Enum.GetName(typeof(TestValidationModes), mode)))
                        : null;
                });

                var messages = new[]
                {
                    getMessageParts(TestValidationModes.ValidateErrors, "error(s)"),
                    getMessageParts(TestValidationModes.ValidateWarnings, "warning(s)"),
                }.Where(x => x != null).ToArray();

                // Create the helpful message
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    "\nTest compilation contains {0}. Disable {1} if these are expected:\n",
                    string.Join(", ", messages.Select(x => x.Item1)),
                    string.Join(" and/or ", messages.Select(x => x.Item2)));

                var builder = new StringBuilder(message);

                foreach (TestValidationModes mode in Enum.GetValues(typeof(TestValidationModes)))
                {
                    if (compileDiagnostics.ContainsKey(mode))
                    {
                        builder.Append(string.Format(CultureInfo.CurrentCulture, "\n {0}", string.Join("\n", compileDiagnostics[mode])));
                    }
                }

                Xunit.Assert.True(false, builder.ToString());
            }
        }

        /// <summary>
        /// Sort diagnostics by location in source document.
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        private static IEnumerable<Diagnostic> SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start);
        }

        private static List<string> GetOrderToCreateProjects(ILookup<string, ITestFile> projectGroup)
        {
            // For each project name, get the ReferencedProjectNames of each TestFile in the group, and remove duplicates.
            var projectsAndDependencies = projectGroup.Select(
                group => Tuple.Create(
                    group.Key,
                    new HashSet<string>(
                        group.SelectMany(testFile => testFile.ReferencedProjectNames)
                             .Where(name => !string.IsNullOrEmpty(name))
                             .Distinct())));
            var projectGraph = new Dictionary<string, HashSet<string>>();
            foreach (var projectEntry in projectsAndDependencies)
            {
                foreach (var dependency in projectEntry.Item2)
                {
                    if (!projectGroup.Contains(dependency))
                    {
                        throw new InvalidOperationException("unrecognized project dependency on " + dependency);
                    }
                }

                projectGraph[projectEntry.Item1] = projectEntry.Item2;
            }

            List<string> projectsInOrder = TopologicalSort(projectGraph);
            return projectsInOrder;
        }

        private static List<string> TopologicalSort(Dictionary<string, HashSet<string>> projectDependencyGraph)
        {
            // Based on Kahn's algorithm
            // https://en.wikipedia.org/wiki/Topological_sorting
            var projectDependencyGraphCopy = new Dictionary<string, HashSet<string>>(projectDependencyGraph);
            var nodesInOrder = new List<string>();
            var nodesWithNoIncomingEdges = new List<string>();
            {
                var edgeDestinations = new HashSet<string>(projectDependencyGraphCopy.Values.SelectMany(destination => destination));
                foreach (var node in projectDependencyGraphCopy.Keys)
                {
                    if (!edgeDestinations.Contains(node))
                    {
                        nodesWithNoIncomingEdges.Add(node);
                    }
                }

                foreach (var nodeWithNoIncomingEdge in nodesWithNoIncomingEdges)
                {
                    projectDependencyGraphCopy.Remove(nodeWithNoIncomingEdge);
                }
            }

            while (nodesWithNoIncomingEdges.Any())
            {
                var node = nodesWithNoIncomingEdges[0];
                nodesWithNoIncomingEdges.RemoveAt(0);
                nodesInOrder.Add(node);
                var nodesToRemoveFromGraph = new List<string>();
                foreach (var inboundNode in projectDependencyGraphCopy.Keys)
                {
                    HashSet<string> inboundNodeDestinations = projectDependencyGraphCopy[inboundNode];
                    inboundNodeDestinations.Remove(node);
                    if (inboundNodeDestinations.Count == 0)
                    {
                        nodesToRemoveFromGraph.Add(inboundNode);
                        nodesWithNoIncomingEdges.Add(inboundNode);
                    }
                }

                foreach (var nodeToRemoveFromGraph in nodesToRemoveFromGraph)
                {
                    projectDependencyGraphCopy.Remove(nodeToRemoveFromGraph);
                }
            }

            // Make sure there are no cycles
            foreach (var node in projectDependencyGraphCopy.Keys)
            {
                if (projectDependencyGraphCopy[node].Count > 0)
                {
                    throw new InvalidOperationException("Graph of project dependencies has a cycle!");
                }
            }

            // Topological sort returns the nodes with no incoming edges first (i.e. no dependencies)
            // We want these nodes to be last so they get created last.
            nodesInOrder.Reverse();
            return nodesInOrder;
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="solution">Solution to create the project in.</param>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="testFiles">A collection of <see cref="TestFile"/> that contain source and project information.</param>
        /// <param name="projectsToReference">An array of <see cref="Project"/>s that this project should reference.</param>
        /// <returns>A Project created out of the Documents created from the test files.</returns>
        private static Project CreateProject(ref Solution solution, string projectName, IEnumerable<ITestFile> testFiles, IEnumerable<Project> projectsToReference)
        {
            var projectId = ProjectId.CreateNewId(debugName: projectName);

            solution = solution
                .AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
                 .AddMetadataReference(projectId, CorlibReference)
                 .AddMetadataReference(projectId, SystemReference)
                 .AddMetadataReference(projectId, SystemCoreReference)
                 .AddMetadataReference(projectId, SystemRuntimeReference)
                 .AddMetadataReference(projectId, CSharpSymbolsReference)
                 .AddMetadataReference(projectId, CodeAnalysisReference)
                 .AddMetadataReference(projectId, WindowsBaseReference)
                 .AddMetadataReference(projectId, MsTestReference);

            foreach (var projectToReference in projectsToReference)
            {
                if (projectToReference.Solution.Id != solution.Id)
                {
                    throw new InvalidOperationException("Reference to project must be in the same solution");
                }

                solution = solution.AddProjectReference(projectId, new ProjectReference(projectToReference.Id));
            }

            var count = 0;
            foreach (var testFile in testFiles)
            {
                var fileName = testFile.Name ?? DefaultFileNamePrefix + count + "." + DefaultFileExtension;
                var documentId = DocumentId.CreateNewId(projectId, debugName: fileName);
                solution = solution.AddDocument(documentId, fileName, SourceText.From(testFile.Source));
                count++;
            }

            return solution.GetProject(projectId);
        }
    }
}
