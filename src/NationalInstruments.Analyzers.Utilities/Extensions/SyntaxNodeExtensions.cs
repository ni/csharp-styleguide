using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NationalInstruments.Analyzers.Utilities.Extensions
{
    /// <summary>
    /// Class that contains helpful extensions to <see cref="SyntaxNode"/>.
    /// </summary>
    public static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Convenience method that returns the symbol for a <paramref name="node"/> without having to
        /// know which method to use: GetDeclaredSymbol or GetSymbolInfo.
        /// </summary>
        /// <remarks>
        /// Roslyn Analyzers' implementation:
        /// https://github.com/dotnet/roslyn-analyzers/blob/569bd373a4831d3035597197e02980b57602a7f2/src/Analyzer.Utilities/Extensions/SyntaxNodeExtensions.cs
        /// </remarks>
        /// <param name="node">Syntax node in a syntax tree.</param>
        /// <param name="model">Semantic model for the syntax tree containing the <paramref name="node"/>.</param>
        /// <returns>Symbol represented by the provided syntax.</returns>
        public static ISymbol GetDeclaredOrReferencedSymbol(this SyntaxNode node, SemanticModel model)
        {
            return node != null ? model.GetDeclaredSymbol(node) ?? model.GetSymbolInfo(node).Symbol : null;
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="syntax"/> represents a parameterized member invocation, <c>false</c> otherwise.
        /// </summary>
        /// <remarks>
        /// Types of parameterized member invocations include:
        /// - Method calls e.g. <code>var result = Method();</code>
        /// - Constructor calls e.g. <code>var instance = new Program();</code>
        /// - Element accesses e.g. <code>var exception = new Dictionary&lt;int, int&gt;()[1];</code>
        /// - Binary operator usages e.g. <code>var equal = 1 == 2;</code>
        /// - Unary operator usages e.g. <code>var five = +5;</code>
        /// </remarks>
        /// <param name="syntax">Any syntax node in a syntax tree.</param>
        /// <returns>True or false depending on whether the given syntax is a parameterized member invocation or not.</returns>
        public static bool IsParameterizedMemberInvocation(this SyntaxNode syntax)
        {
            return syntax is InvocationExpressionSyntax
                   || syntax is ObjectCreationExpressionSyntax
                   || syntax is ElementAccessExpressionSyntax
                   || syntax is BinaryExpressionSyntax
                   || syntax is PrefixUnaryExpressionSyntax;
        }
    }
}
