using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Style
{
    /// <summary>
    /// Enforces the convention -
    /// https://github.com/ni/csharp/blob/main/docs/Coding-Conventions.md#f34-%EF%B8%8F-consider-splitting-long-chains-of-dotted-methodproperty-invocations
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LongChainDottedInvocationAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1017";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1017_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1017_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(
                AnalyzeSyntax,
                SyntaxKind.ExpressionStatement,
                SyntaxKind.EqualsValueClause,
                SyntaxKind.Argument,
                SyntaxKind.ArrowExpressionClause
                );
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var parentSyntaxNode = context.Node;

            // Find the invocation expression i.e
            // a method/delegate call or a property access
            // or a chain of them
            var invocationExpressionSyntax = parentSyntaxNode
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocationExpressionSyntax is null)
            {
                // This does not contain any invocations, bail out
                return;
            }

            // Find all invocations which involve brackets ()
            var nonNestedInvocationsWithParenthesis = invocationExpressionSyntax
                .DescendantNodes(syntaxNode => !syntaxNode.IsKind(SyntaxKind.Argument))
                .OfType<ArgumentListSyntax>();

            // There is a none or only a single call
            if (nonNestedInvocationsWithParenthesis.Count() < 2)
            {
                // No more refactoring required
                return;
            }

            // Find all end of lines in the expression
            var endOfLineTrivias = invocationExpressionSyntax
                .DescendantTrivia()
                .Where(syntaxTrivia => syntaxTrivia.Kind() == SyntaxKind.EndOfLineTrivia);

            // The expression is well split into multiple lines
            if (endOfLineTrivias.Count() >= nonNestedInvocationsWithParenthesis.Count())
            {
                // Don't ask for more refactoring
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, invocationExpressionSyntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
