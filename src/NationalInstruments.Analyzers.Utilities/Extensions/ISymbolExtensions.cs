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
        public static string? GetFullName(this ISymbol symbol)
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

            var fieldSymbol = symbol as IFieldSymbol;
            var propertySymbol = symbol as IPropertySymbol;
            if (fieldSymbol is not null || propertySymbol is not null)
            {
                ITypeSymbol? type = fieldSymbol?.Type ?? propertySymbol?.Type;
                prefix = type?.ToDisplayString(SymbolDisplayFormats.FullyQualifiedParameters);
            }

            // Always keep the removal of whitespace separate from the removal of the prefix. This will prevent
            // characters beyond the prefix from being removed.
            var symbolName = symbol.ToDisplayString(SymbolDisplayFormats.FullyQualifiedParameters)
                .TrimStart(prefix?.ToCharArray())
                .TrimStart();

            symbolNameBuilder.Append(symbolName);

            return symbolNameBuilder.ToString();
        }

        public static bool IsImplementationOfAnyImplicitInterfaceMember(this ISymbol symbol)
        {
            return IsImplementationOfAnyImplicitInterfaceMember<ISymbol>(symbol);
        }

        /// <summary>
        /// Checks if a given symbol implements an interface member implicitly
        /// </summary>
        public static bool IsImplementationOfAnyImplicitInterfaceMember<TSymbol>(this ISymbol symbol)
            where TSymbol : ISymbol
        {
            if (symbol.ContainingType != null)
            {
                foreach (INamedTypeSymbol interfaceSymbol in symbol.ContainingType.AllInterfaces)
                {
                    foreach (var interfaceMember in interfaceSymbol.GetMembers().OfType<TSymbol>())
                    {
                        if (IsImplementationOfInterfaceMember(symbol, interfaceMember))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a given symbol implements an interface member implicitly
        /// </summary>
        public static bool IsImplementationOfAnyImplicitInterfaceMember<TSymbol>(this ISymbol symbol, out TSymbol? interfaceMember)
            where TSymbol : ISymbol
        {
            if (symbol.ContainingType != null)
            {
                foreach (INamedTypeSymbol interfaceSymbol in symbol.ContainingType.AllInterfaces)
                {
                    foreach (var baseInterfaceMember in interfaceSymbol.GetMembers().OfType<TSymbol>())
                    {
                        if (IsImplementationOfInterfaceMember(symbol, baseInterfaceMember))
                        {
                            interfaceMember = baseInterfaceMember;
                            return true;
                        }
                    }
                }
            }

            interfaceMember = default;
            return false;
        }

        /// <summary>
        /// Checks if a given symbol implements an interface member explicitly
        /// </summary>
        public static bool IsImplementationOfAnyExplicitInterfaceMember(this ISymbol? symbol)
        {
            if (symbol is IMethodSymbol methodSymbol && !methodSymbol.ExplicitInterfaceImplementations.IsEmpty)
            {
                return true;
            }

            if (symbol is IPropertySymbol propertySymbol && !propertySymbol.ExplicitInterfaceImplementations.IsEmpty)
            {
                return true;
            }

            if (symbol is IEventSymbol eventSymbol && !eventSymbol.ExplicitInterfaceImplementations.IsEmpty)
            {
                return true;
            }

            return false;
        }

        public static bool IsImplementationOfInterfaceMember(this ISymbol symbol, ISymbol? interfaceMember)
        {
            return interfaceMember != null &&
                SymbolEqualityComparer.Default.Equals(symbol, symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember));
        }
    }
}
