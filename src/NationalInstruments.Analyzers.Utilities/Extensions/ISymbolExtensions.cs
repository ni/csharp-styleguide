using System.Text;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.Utilities.Extensions
{
    /// <summary>
    /// Class that contains helpful extensions to <see cref="ISymbol"/>.
    /// </summary>
    public static class ISymbolExtensions
    {
        /// <summary>
        /// Returns a fully-qualified string representation of the given <paramref name="symbol"/>, including parameters.
        /// </summary>
        /// <param name="symbol">Any symbol.</param>
        /// <returns>
        /// A string representing all the aspects of a symbol that make it uniquely identifiable, such as its fully-qualified parameters.
        /// </returns>
        public static string GetFullName(this ISymbol symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            var containingType = symbol.ContainingType;
            var containingTypeName = containingType?.ToDisplayString(SymbolDisplayFormats.FullyQualifiedParameters) ?? string.Empty;

            var symbolNameBuilder = new StringBuilder(containingTypeName);
            if (symbolNameBuilder.Length > 0)
            {
                if (symbol is ITypeSymbol && containingType is ITypeSymbol)
                {
                    symbolNameBuilder.Append('+');
                }
                else
                {
                    symbolNameBuilder.Append('.');
                }
            }

            var prefix = string.Empty;

            // Remove the return type from a method's name e.g.
            // System.String System.String.Equals(System.String, System.String)
            // ^^^^^^^^^^^^^^
            if (symbol is IMethodSymbol method)
            {
                prefix = "void";
                if (!method.ReturnsVoid)
                {
                    prefix = method.ReturnType.ToDisplayString(SymbolDisplayFormats.FullyQualifiedParameters);
                }
            }

            IFieldSymbol field = null;
            IPropertySymbol property = null;
            if ((field = symbol as IFieldSymbol) != null || (property = symbol as IPropertySymbol) != null)
            {
                ITypeSymbol type = field?.Type ?? property?.Type;
                prefix = type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedParameters);
            }

            // Always keep the removal of whitespace separate from the removal of the prefix. This will prevent
            // characters beyond the prefix from being removed.
            var symbolName = symbol.ToDisplayString(SymbolDisplayFormats.FullyQualifiedParameters)
                .TrimStart(prefix.ToCharArray())
                .TrimStart();

            symbolNameBuilder.Append(symbolName);

            return symbolNameBuilder.ToString();
        }
    }
}
