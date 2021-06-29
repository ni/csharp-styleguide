using System;
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
    /// Enforces the convention: <see href="https://github.com/ni/csharp-styleguide#f34-%EF%B8%8F-do-split-chains-of-method-invocations-with-lambda-expressions-">
    /// [F.3.4] - DO Split chains of method invocations with lambda expressions</see>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ChainOfMethodsWithLambdasAnalyzer : NIDiagnosticAnalyzer
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
                AnalyzeExpression,
                SyntaxKind.ExpressionStatement,
                SyntaxKind.EqualsValueClause,
                SyntaxKind.Argument,
                SyntaxKind.ArrowExpressionClause);

            context.RegisterSyntaxNodeAction(
                AnalyzeArrayInitializer,
                SyntaxKind.ArrayInitializerExpression);
        }

        private static void AnalyzeExpression(SyntaxNodeAnalysisContext context)
        {
            var parentSyntaxNode = context.Node;

            // Find the invocation expression i.e
            // a method/delegate call or a property access
            // or a chain of them
            var invocationExpressionSyntax = parentSyntaxNode
                .DescendantNodes(IsNotArrayInitializerSyntax)
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            AnalyzeInvocationExpression(invocationExpressionSyntax, context.ReportDiagnostic);

            bool IsNotArrayInitializerSyntax(SyntaxNode syntaxNode) =>
                !syntaxNode.IsKind(SyntaxKind.ArrayInitializerExpression);
        }

        private static void AnalyzeArrayInitializer(SyntaxNodeAnalysisContext context)
        {
            var arrayInitializerSyntax = context.Node;

            // Find only direct child invocation expressions
            var invocationExpressionSyntaxes = arrayInitializerSyntax
                .ChildNodes()
                .OfType<InvocationExpressionSyntax>();

            // Analyze individual invocation expressions
            foreach (var invocationExpression in invocationExpressionSyntaxes)
            {
                AnalyzeInvocationExpression(invocationExpression, context.ReportDiagnostic);
            }
        }

        private static void AnalyzeInvocationExpression(InvocationExpressionSyntax invocationExpressionSyntax, Action<Diagnostic> reportDiagnostic)
        {
            if (invocationExpressionSyntax is null)
            {
                // This does not contain any invocations, bail out
                return;
            }

            // Find all non-nested arguments which contain a lambda expression
            var nonNestedArgumentsWithLambdas = invocationExpressionSyntax
                .DescendantNodes(IsOutOfScopeSyntax)
                .OfType<ArgumentListSyntax>()
                .Where(argument => argument.DescendantNodes().Any(IsLambdaExpression));

            // There is none or only a single call
            if (nonNestedArgumentsWithLambdas.Count() < 2)
            {
                // No more refactoring required
                return;
            }

            // The first argument can stay on the same line
            var successiveArgumentsWithLambdas = nonNestedArgumentsWithLambdas.Skip(1);

            foreach (var argument in successiveArgumentsWithLambdas)
            {
                // Get the parent invocation expression
                var parentInvocation = argument.Parent;

                // Find the method/delegate call
                var memberAccessExpression = parentInvocation?.ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

                if (memberAccessExpression is null)
                {
                    // unknown syntax, don't know what to do
                    throw new NotSupportedException();
                }

                // Get the dot operator which is used to call the method/delegate
                var dotToken = memberAccessExpression.ChildTokens().FirstOrDefault(token => token.IsKind(SyntaxKind.DotToken));

                // If the dot operator does not have a leading whitespace, report violation
                if (!dotToken.LeadingTrivia.Any(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia)))
                {
                    var diagnostic = Diagnostic.Create(Rule, invocationExpressionSyntax.GetLocation());
                    reportDiagnostic(diagnostic);
                    break;
                }
            }

            bool IsLambdaExpression(SyntaxNode syntaxNode) => syntaxNode.IsKind(SyntaxKind.SimpleLambdaExpression);

            bool IsOutOfScopeSyntax(SyntaxNode syntaxNode) => IsNotArgumentSyntax(syntaxNode) && IsNotArrayInitializerSyntax(syntaxNode);

            bool IsNotArgumentSyntax(SyntaxNode syntaxNode) => !syntaxNode.IsKind(SyntaxKind.Argument);

            bool IsNotArrayInitializerSyntax(SyntaxNode syntaxNode) => !syntaxNode.IsKind(SyntaxKind.ArrayInitializerExpression);
        }
    }
}
