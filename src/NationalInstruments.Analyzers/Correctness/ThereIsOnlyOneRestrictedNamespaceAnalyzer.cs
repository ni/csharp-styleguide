using System;
using System.Collections.Immutable;
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
    /// Analyzer that reports a diagnostic if a type's namespace contains the text 'Restricted' but
    /// does not resolve to a namespace that begins with the text 'NationalInstruments.Restricted'.
    /// </summary>
    /// <remarks>
    /// The only restricted namespaces allowed are those that begin with 'NationalInstruments.Restricted.'
    /// </remarks>
    /// <example>
    /// <code>
    /// namespace MyApp.Restricted  // one solution would be NationalInstruments.Restricted.MyApp
    /// {
    ///     class Program
    ///     {
    ///     }
    /// }
    /// </code>
    /// </example>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ThereIsOnlyOneRestrictedNamespaceAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "LRN001";

        private const string RestrictedNamespacePrefix = "NationalInstruments.Restricted";

        public static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.LRN001_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.LRN001_Message), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.LRN001_Description), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: "https://nitalk.jiveon.com/docs/DOC-234077");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeNamespaceSyntax, SyntaxKind.NamespaceDeclaration);
        }

        private static bool IsNamespaceNameViolatingRule(string namespaceName)
        {
            return namespaceName != null
                && namespaceName.IndexOf("Restricted", StringComparison.OrdinalIgnoreCase) >= 0
                && !namespaceName.StartsWith(RestrictedNamespacePrefix, StringComparison.OrdinalIgnoreCase);
        }

        private void AnalyzeNamespaceSyntax(SyntaxNodeAnalysisContext context)
        {
            var namespaceSyntax = (NamespaceDeclarationSyntax)context.Node;

            // First, check the namespace syntax's name because it's faster
            if (IsNamespaceNameViolatingRule(namespaceSyntax.Name.ToString()))
            {
                // We can't immediately tell if there's a violation from the syntax alone as this 'Restricted'
                // namespace might be nested under the <c>NationalInstruments</c> namespace. To determine if that's
                // the case and there shouldn't be a violation, apply the same test against the fully-qualified
                // namespace name.
                var @namespace = namespaceSyntax.GetDeclaredOrReferencedSymbol(context.SemanticModel);
                var namespaceName = @namespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).TrimStart("global:".ToCharArray());

                if (IsNamespaceNameViolatingRule(namespaceName))
                {
                    var diagnostic = Diagnostic.Create(Rule, namespaceSyntax.GetLocation(), namespaceName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
