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

            context.RegisterSyntaxNodeAction(AnalyzeExpressionStatementSyntax, SyntaxKind.ExpressionStatement);
            context.RegisterSyntaxNodeAction(AnalyzeEqualsValueClauseSyntax, SyntaxKind.EqualsValueClause);
            context.RegisterSyntaxNodeAction(AnalyzeArgumentSyntax, SyntaxKind.Argument);
            context.RegisterSyntaxNodeAction(AnalyzeArrowExpressionClauseSyntax, SyntaxKind.ArrowExpressionClause);
        }

        private static void AnalyzeExpressionStatementSyntax(SyntaxNodeAnalysisContext context)
        {
            var expressionStatementSyntax = (ExpressionStatementSyntax)context.Node;

            AnalyzeSyntax(expressionStatementSyntax, context);
        }

        private static void AnalyzeEqualsValueClauseSyntax(SyntaxNodeAnalysisContext context)
        {
            var equalsValueClauseSyntax = (EqualsValueClauseSyntax)context.Node;

            AnalyzeSyntax(equalsValueClauseSyntax, context);
        }

        private static void AnalyzeArrowExpressionClauseSyntax(SyntaxNodeAnalysisContext context)
        {
            var expressionStatementSyntax = (ArrowExpressionClauseSyntax)context.Node;

            AnalyzeSyntax(expressionStatementSyntax, context);
        }

        private static void AnalyzeArgumentSyntax(SyntaxNodeAnalysisContext context)
        {
            var argumentSyntax = (ArgumentSyntax)context.Node;

            AnalyzeSyntax(argumentSyntax, context);
        }

        private static void AnalyzeSyntax(SyntaxNode parentSyntaxNode, SyntaxNodeAnalysisContext context)
        {
            // Find the invocation expression i.e
            // a method/delegate call or a property access
            // or a chain of them
            var invocationExpressionSyntax = parentSyntaxNode
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocationExpressionSyntax is null)
            {
                // This does contain any invocations
                return;
            }

            // Find all invocations which involve brackets ()
            var nonNestedInvocationsWithParenthesis = invocationExpressionSyntax
                .DescendantNodes(syntaxNode => !syntaxNode.IsKind(SyntaxKind.Argument))
                .OfType<ArgumentListSyntax>();

            // There is a none or only a single call
            if (nonNestedInvocationsWithParenthesis.Count() < 2)
            {
                // No more refactoring required.
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
