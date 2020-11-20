using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness
{
    /// <summary>
    /// Analyzer that reports a diagnostic if a type's namespace is not from an approved list
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ApprovedNamespaceAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1800";

        private static readonly Regex _testNamespacePattern = new Regex(@".+(\.Tests|\.TestUtilities)");
        private static readonly SourceTextValueProvider<ApprovedNamespaces> _approvedNamespacesProvider = new SourceTextValueProvider<ApprovedNamespaces>(s => new ApprovedNamespaces(s));
        private static string _approvedNamespacesFilePath;
        private static string _approvedTestNamespacesFilePath;

        private ApprovedNamespaces _approvedNamespaces;
        private ApprovedNamespaces _approvedTestNamespaces;

        /// <summary>
        /// Rule for namespaces in production code
        /// </summary>
        public static DiagnosticDescriptor ProductionRule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1800_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1800_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNamespaces,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1800_Description), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: string.Empty);

        /// <summary>
        /// Rule for namespaces in test/testutilities code
        /// </summary>
        public static DiagnosticDescriptor TestRule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1800_TestTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1800_TestMessage), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNamespaces,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1800_TestDescription), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: string.Empty);

        /// <summary>
        /// Rule for failure to read approved namespaces files.
        /// </summary>
        public static DiagnosticDescriptor FileReadRule { get; } = new DiagnosticDescriptor(
            $"{DiagnosticId}_ReadError",
            new LocalizableResourceString(nameof(Resources.NI1800_FileReadErrorTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1800_FileReadErrorMessage), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNamespaces,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// Rule for missing approved namespaces files.
        /// </summary>
        public static DiagnosticDescriptor MissingApprovalFilesRule { get; } = new DiagnosticDescriptor(
            $"{DiagnosticId}_ReadError",
            new LocalizableResourceString(nameof(Resources.NI1800_MissingApprovalFilesErrorTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1800_MissingApprovalFilesErrorMessage), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNamespaces,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ProductionRule, TestRule, FileReadRule, MissingApprovalFilesRule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction && !InDebugMode);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        /// <summary>
        /// Adds a namespace to the approved namespaces list
        /// </summary>
        /// <param name="namespaceName">Namespace to be approved</param>
        internal static void ApproveNamespace(string namespaceName)
        {
            var namespacesFilePath = IsTestNamespace(namespaceName) ? _approvedTestNamespacesFilePath : _approvedNamespacesFilePath;
            var lines = File.ReadAllLines(namespacesFilePath);
            var namespaces = lines
                    .Concat(new[] { namespaceName })
                    .Select(x => x.Trim())
                    .OrderBy(x => x)
                    .Distinct();
            File.WriteAllLines(namespacesFilePath, namespaces);
        }

        private static bool IsTestNamespace(string namespaceName)
        {
            return _testNamespacePattern.IsMatch(namespaceName);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationStartContext)
        {
            InitializeApprovedNamespaces();
            compilationStartContext.RegisterSymbolAction(AnalyzeNamespace, SymbolKind.Namespace);

            if (!ApprovalFilesExist())
            {
                var diagnostic = Diagnostic.Create(MissingApprovalFilesRule, Location.None);
                compilationStartContext.RegisterCompilationEndAction(x => x.ReportDiagnostic(diagnostic));
            }

            void AnalyzeNamespace(SymbolAnalysisContext context)
            {
                var symbol = context.Symbol;
                var namespaceName = symbol.ToDisplayString();
                if (TryGetNamespaceViolatingRule(namespaceName, out DiagnosticDescriptor rule))
                {
                    foreach (var location in symbol.Locations)
                    {
                        ReportDiagnostic(context, namespaceName, location, rule);
                    }
                }
            }

            void InitializeApprovedNamespaces()
            {
                var fileProvider = AdditionalFileProvider.FromOptions(compilationStartContext.Options);
                var approvedNamespacesFile = fileProvider.GetMatchingFiles("ApprovedNamespaces.txt").FirstOrDefault();
                var approvedTestNamespacesFile = fileProvider.GetMatchingFiles("ApprovedNamespaces.Tests.txt").FirstOrDefault();

                _approvedNamespacesFilePath = null;
                if (approvedNamespacesFile != null)
                {
                    var sourceText = approvedNamespacesFile.GetText(compilationStartContext.CancellationToken);
                    if (!compilationStartContext.TryGetValue(sourceText, _approvedNamespacesProvider, out _approvedNamespaces))
                    {
                        ReportFileReadDiagnostic(approvedNamespacesFile.Path);
                    }

                    _approvedNamespacesFilePath = approvedNamespacesFile.Path;
                }

                _approvedTestNamespacesFilePath = null;
                if (approvedTestNamespacesFile != null)
                {
                    var sourceText = approvedTestNamespacesFile.GetText(compilationStartContext.CancellationToken);
                    if (!compilationStartContext.TryGetValue(sourceText, _approvedNamespacesProvider, out _approvedTestNamespaces))
                    {
                        ReportFileReadDiagnostic(approvedTestNamespacesFile.Path);
                    }

                    _approvedTestNamespacesFilePath = approvedTestNamespacesFile.Path;
                }

                void ReportFileReadDiagnostic(string filePath)
                {
                    var diagnostic = Diagnostic.Create(FileReadRule, Location.None, filePath);
                    compilationStartContext.RegisterCompilationEndAction(x => x.ReportDiagnostic(diagnostic));
                }
            }

            bool IsNamespaceNameViolatingRule(string namespaceName, bool isTestNamespace)
            {
                return (isTestNamespace
                        && _approvedTestNamespaces != null
                        && !_approvedTestNamespaces.IsNamespaceApproved(namespaceName))
                    || (!isTestNamespace
                        && _approvedNamespaces != null
                        && !_approvedNamespaces.IsNamespaceApproved(namespaceName));
            }

            bool TryGetNamespaceViolatingRule(string namespaceName, out DiagnosticDescriptor rule)
            {
                var isTestNamespace = IsTestNamespace(namespaceName);
                rule = isTestNamespace ? TestRule : ProductionRule;
                return IsNamespaceNameViolatingRule(namespaceName, isTestNamespace);
            }

            void ReportDiagnostic(SymbolAnalysisContext context, string namespaceName, Location location, DiagnosticDescriptor rule)
            {
                var syntaxNode = location.SourceTree.GetRoot().FindNode(location.SourceSpan);
                var isLeafNamespace = !(syntaxNode.Parent is QualifiedNameSyntax parent)
                        || !(syntaxNode == parent.Left || parent.Parent is QualifiedNameSyntax);
                var namespaceDeclaration = syntaxNode.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();

                if (isLeafNamespace && namespaceDeclaration.Members.Any(m => !(m is NamespaceDeclarationSyntax)))
                {
                    var nameSyntax = namespaceDeclaration.Name;

                    var diagnostic = Diagnostic.Create(rule, nameSyntax.GetLocation(), namespaceName);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            bool ApprovalFilesExist()
            {
                return !string.IsNullOrEmpty(_approvedNamespacesFilePath)
                    || !string.IsNullOrEmpty(_approvedTestNamespacesFilePath);
            }
        }

        /// <summary>
        /// Class to manage the approved namespaces and patterns
        /// </summary>
        private class ApprovedNamespaces
        {
            private readonly HashSet<string> _namespaces = new HashSet<string>();
            private readonly List<Regex> _namespacePatterns = new List<Regex>();

            public ApprovedNamespaces(SourceText sourceText)
            {
                _namespaces.Clear();
                _namespacePatterns.Clear();
                foreach (var line in sourceText.Lines)
                {
                    var trimmedLine = line.ToString().Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        if (trimmedLine.Contains('*') || trimmedLine.Contains('?'))
                        {
                            var regexString = Regex.Escape(trimmedLine)
                                .Replace(@"\*", ".*")
                                .Replace(@"\?", ".");
                            var pattern = new Regex(regexString, RegexOptions.Singleline | RegexOptions.Compiled);
                            _namespacePatterns.Add(pattern);
                        }
                        else
                        {
                            _namespaces.Add(trimmedLine);
                        }
                    }
                }
            }

            public bool IsNamespaceApproved(string namespaceName)
            {
                return !_namespaces.Any()
                    || _namespaces.Contains(namespaceName)
                    || NamespaceMatchesPattern(namespaceName, _namespacePatterns);
            }

            private bool NamespaceMatchesPattern(string namespaceName, List<Regex> namespacePatterns)
            {
                foreach (var pattern in namespacePatterns)
                {
                    if (pattern.IsMatch(namespaceName))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
