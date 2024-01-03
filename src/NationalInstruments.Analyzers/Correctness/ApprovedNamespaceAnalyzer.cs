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

        private ApprovedNamespaces? _approvedNamespaces;
        private ApprovedNamespaces? _approvedTestNamespaces;

        /// <summary>
        /// Rule for namespaces in production code
        /// </summary>
        public static readonly DiagnosticDescriptor ProductionRule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1800_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1800_Message), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1800_Description), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: string.Empty);

        /// <summary>
        /// Rule for namespaces in test/testutilities code
        /// </summary>
        public static readonly DiagnosticDescriptor TestRule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1800_TestTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1800_TestMessage), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1800_TestDescription), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: string.Empty);

        /// <summary>
        /// Rule for failure to read approved namespaces files.
        /// </summary>
        public static readonly DiagnosticDescriptor FileReadRule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1800_FileReadErrorTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1800_FileReadErrorMessage), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// Rule for missing approved namespaces files.
        /// </summary>
        public static readonly DiagnosticDescriptor MissingApprovalFilesRule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1800_MissingApprovalFilesErrorTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1800_MissingApprovalFilesErrorMessage), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
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

        private static bool IsTestNamespace(string namespaceName)
        {
            return _testNamespacePattern.IsMatch(namespaceName);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationStartContext)
        {
            var effectiveSeverity = ProductionRule.GetEffectiveSeverity(compilationStartContext.Compilation.Options);
            if (effectiveSeverity == Microsoft.CodeAnalysis.ReportDiagnostic.Suppress)
            {
                return;
            }

            InitializeApprovedNamespaces();

            if (!ApprovalFilesExist())
            {
                var diagnostic = Diagnostic.Create(MissingApprovalFilesRule, Location.None);
                compilationStartContext.RegisterCompilationEndAction(x => x.ReportDiagnostic(diagnostic));
            }
            else
            {
                compilationStartContext.RegisterSymbolAction(AnalyzeNamespace, SymbolKind.Namespace);
            }

            void AnalyzeNamespace(SymbolAnalysisContext context)
            {
                var symbol = context.Symbol;
                var namespaceName = symbol.ToDisplayString();
                if (TryGetNamespaceViolatingRule(namespaceName, out DiagnosticDescriptor rule, out var approvedNamespacesFilePath))
                {
                    foreach (var location in symbol.Locations)
                    {
                        ReportDiagnostic(context, namespaceName, location, rule, approvedNamespacesFilePath);
                    }
                }
            }

            void InitializeApprovedNamespaces()
            {
                var fileProvider = AdditionalFileProvider.FromOptions(compilationStartContext.Options);
                var approvedNamespacesFile = fileProvider.GetMatchingFiles("ApprovedNamespaces.txt").FirstOrDefault();
                var approvedTestNamespacesFile = fileProvider.GetMatchingFiles("ApprovedNamespaces.Tests.txt").FirstOrDefault();

                if (approvedNamespacesFile is not null)
                {
                    var sourceText = approvedNamespacesFile.GetText(compilationStartContext.CancellationToken);
                    if (sourceText is null || !compilationStartContext.TryGetValue(sourceText, _approvedNamespacesProvider, out _approvedNamespaces))
                    {
                        ReportFileReadDiagnostic(approvedNamespacesFile.Path);
                    }

                    if (_approvedNamespaces is not null)
                    {
                        _approvedNamespaces.Path = approvedNamespacesFile.Path;
                    }
                }

                if (approvedTestNamespacesFile is not null)
                {
                    var sourceText = approvedTestNamespacesFile.GetText(compilationStartContext.CancellationToken);
                    if (sourceText is null || !compilationStartContext.TryGetValue(sourceText, _approvedNamespacesProvider, out _approvedTestNamespaces))
                    {
                        ReportFileReadDiagnostic(approvedTestNamespacesFile.Path);
                    }

                    if (_approvedTestNamespaces is not null)
                    {
                        _approvedTestNamespaces.Path = approvedTestNamespacesFile.Path;
                    }
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
                        && _approvedTestNamespaces is not null
                        && !_approvedTestNamespaces.IsNamespaceApproved(namespaceName))
                    || (!isTestNamespace
                        && _approvedNamespaces is not null
                        && !_approvedNamespaces.IsNamespaceApproved(namespaceName));
            }

            bool TryGetNamespaceViolatingRule(string namespaceName, out DiagnosticDescriptor rule, out string approvedNamespacesFilePath)
            {
                var isTestNamespace = IsTestNamespace(namespaceName);
                rule = isTestNamespace ? TestRule : ProductionRule;
                approvedNamespacesFilePath = (isTestNamespace ? _approvedTestNamespaces?.Path : _approvedNamespaces?.Path) ?? "Unknown";
                return IsNamespaceNameViolatingRule(namespaceName, isTestNamespace);
            }

            void ReportDiagnostic(SymbolAnalysisContext context, string namespaceName, Location location, DiagnosticDescriptor rule, string approvedNamespacesFilePath)
            {
                var syntaxNode = location.SourceTree?.GetRoot().FindNode(location.SourceSpan);
                var isLeafNamespace = syntaxNode?.Parent is not QualifiedNameSyntax parent
                        || !(syntaxNode == parent.Left || parent.Parent is QualifiedNameSyntax);
                var namespaceDeclaration = syntaxNode?.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();

                if (isLeafNamespace && (namespaceDeclaration?.Members.Any(m => m is not NamespaceDeclarationSyntax) ?? false))
                {
                    var nameSyntax = namespaceDeclaration.Name;

                    var builder = ImmutableDictionary.CreateBuilder<string, string?>();
                    builder.Add("Path", approvedNamespacesFilePath);
                    var properties = builder.ToImmutable();
                    var diagnostic = Diagnostic.Create(rule, nameSyntax.GetLocation(), properties, namespaceName, approvedNamespacesFilePath);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            bool ApprovalFilesExist()
            {
                return _approvedNamespaces is object || _approvedTestNamespaces is object;
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

            public string? Path { get; set; }

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
