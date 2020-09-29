using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness
{
    /// <summary>
    /// Analyzer that reports a diagnostic for each type (class, interface, enum, struct) in a namespace if the
    /// namespace does not start with the text 'NationalInstruments'.
    /// </summary>
    /// <remarks>
    /// Namespaces can be exempt from this rule if they're placed in an XML file with a name containing the text
    /// 'ExemptNamespaces'. The schema must conform to the following:
    /// <![CDATA[
    /// <ExemptNamespaces>
    ///     <Entry>My.Namespace.Name</Entry>
    ///     ...
    /// </ExemptNamespaces>
    /// ]]>
    /// </remarks>
    /// <example>
    /// namespace MyApp
    /// {
    ///     class Program   // violation, containing namespace does not start with 'NationalInstruments'
    ///     {
    ///     }
    /// }
    /// </example>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AllTypesInNationalInstrumentsNamespaceAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "LRT001";

        private const string CorrectNamespace = "NationalInstruments";

        private static readonly LocalizableString LocalizedTitle = new LocalizableResourceString(nameof(Resources.LRN001_Title), Resources.ResourceManager, typeof(Resources));

        public static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.LRT001_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.LRT001_Description), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: "https://nitalk.jiveon.com/docs/DOC-234077");

        public static DiagnosticDescriptor FileParseRule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.ParseError_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, FileParseRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                var additionalFileService = new AdditionalFileService(compilationStartContext.Options.AdditionalFiles, FileParseRule);
                var analyzer = new NamespaceAnalyzer(additionalFileService, compilationStartContext.CancellationToken);
                analyzer.LoadConfigurations("ExemptNamespaces");

                compilationStartContext.RegisterSyntaxNodeAction(analyzer.AnalyzeNamespaceDeclarations, SyntaxKind.NamespaceDeclaration);
                compilationStartContext.RegisterCompilationEndAction(additionalFileService.ReportAnyParsingDiagnostics);
            });
        }

        private class NamespaceAnalyzer : ConfigurableAnalyzer
        {
            private readonly IAdditionalFileService _additionalFileService;
            private readonly HashSet<string> _exemptNamespaces;

            public NamespaceAnalyzer(IAdditionalFileService additionalFileService, CancellationToken cancellationToken)
                : base(additionalFileService, cancellationToken)
            {
                _additionalFileService = additionalFileService;
                _exemptNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            public void AnalyzeNamespaceDeclarations(SyntaxNodeAnalysisContext context)
            {
                var namespaceSyntax = (NamespaceDeclarationSyntax)context.Node;
                var @namespace = namespaceSyntax.GetDeclaredOrReferencedSymbol(context.SemanticModel);
                var namespaceName = @namespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).TrimStart("global:".ToCharArray());

                // Bail out if this namespace already is/starts with 'NationalInstruments[.]' or is exempt
                if (Regex.IsMatch(namespaceName, string.Format(CultureInfo.InvariantCulture, @"^{0}(\s|\b)", CorrectNamespace), RegexOptions.IgnoreCase)
                    || _exemptNamespaces.Contains(namespaceName))
                {
                    return;
                }

                // Report a violation on each type in the invalid namespace
                foreach (var declarationSyntax in namespaceSyntax.ChildNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    var diagnostic = Diagnostic.Create(Rule, declarationSyntax.GetLocation(), declarationSyntax.Identifier.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }

            protected override void LoadConfigurations(XElement rootElement, string filePath)
            {
                if (TryGetRootElementDiagnostic(rootElement, "ExemptNamespaces", filePath, FileParseRule, out var diagnostic))
                {
                    _additionalFileService.ParsingDiagnostics.Add(diagnostic);
                }

                _exemptNamespaces.UnionWith(rootElement.Elements("Entry").Select(x => x.Value.Trim()));
            }
        }
    }
}
