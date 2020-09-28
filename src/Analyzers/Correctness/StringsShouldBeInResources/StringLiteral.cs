using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NationalInstruments.Tools.Analyzers.Correctness.StringsShouldBeInResources
{
    public class StringLiteral
    {
        /// <summary>
        /// Object that bundles a string literal value with its <see cref="SyntaxNode"/>.
        /// </summary>
        /// <param name="value">Any string literal.</param>
        /// <param name="syntax">Syntax node containing the string literal.</param>
        public StringLiteral(string value, SyntaxNode syntax)
        {
            Value = value;
            Syntax = syntax;
        }

        public string Value { get; }

        public SyntaxNode Syntax { get; }

        public static IEnumerable<StringLiteral> GetStringLiterals(IEnumerable<SyntaxNode> nodes)
        {
            // Handles both $[@]"" and [@]"" strings
            foreach (var node in nodes)
            {
                if (node is InterpolatedStringExpressionSyntax interpolatedStringSyntax)
                {
                    yield return new StringLiteral(interpolatedStringSyntax.Contents.ToString(), interpolatedStringSyntax);
                }

                if (node is LiteralExpressionSyntax literalSyntax && literalSyntax.Kind() == SyntaxKind.StringLiteralExpression)
                {
                    yield return new StringLiteral(literalSyntax.Token.ValueText, literalSyntax);
                }
            }
        }
    }
}
