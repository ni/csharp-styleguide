using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.Utilities.Extensions
{
    /// <summary>
    /// Class that contains helpful extensions to <see cref="ITypeSymbol"/>.
    /// </summary>
    /// <remarks>
    /// Microsoft has most of these extensions internally, but they haven't made them public.
    /// </remarks>
    public static class ITypeSymbolExtensions
    {
        /// <summary>
        /// Returns all ITypeSymbols that <paramref name="type"/> derives from including <paramref name="type"/> itself.
        /// </summary>
        /// <remarks>
        /// Roslyn Analyzers' implementation:
        /// https://github.com/dotnet/roslyn-analyzers/blob/master/src/Analyzer.Utilities/Extensions/ITypeSymbolExtensions.cs
        /// </remarks>
        /// <param name="type">Symbol that is derived from other symbols.</param>
        /// <returns>Generator of INamedTypeSymbols where the next symbol returned is the base type of the previous one</returns>
        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            ITypeSymbol? current = type;
            while (current is not null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static bool DerivesFrom(this ITypeSymbol? symbol, ITypeSymbol? candidateBaseType, bool baseTypesOnly = false, bool checkTypeParameterConstraints = true)
        {
            if (candidateBaseType is null || symbol is null)
            {
                return false;
            }

            if (!baseTypesOnly && candidateBaseType.TypeKind == TypeKind.Interface)
            {
                var allInterfaces = symbol.AllInterfaces.OfType<ITypeSymbol>();
                if (SymbolEqualityComparer.Default.Equals(candidateBaseType.OriginalDefinition, candidateBaseType))
                {
                    // Candidate base type is not a constructed generic type, so use original definition for interfaces.
                    allInterfaces = allInterfaces.Select(i => i.OriginalDefinition);
                }

                if (allInterfaces?.Contains(candidateBaseType, SymbolEqualityComparer.Default) ?? false)
                {
                    return true;
                }
            }

            if (checkTypeParameterConstraints && symbol.TypeKind == TypeKind.TypeParameter)
            {
                var typeParameterSymbol = (ITypeParameterSymbol)symbol;
                foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
                {
                    if (constraintType.DerivesFrom(candidateBaseType, baseTypesOnly, checkTypeParameterConstraints))
                    {
                        return true;
                    }
                }
            }

            while (symbol != null)
            {
                if (SymbolEqualityComparer.Default.Equals(symbol, candidateBaseType))
                {
                    return true;
                }

                symbol = symbol.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is of type <paramref name="interfaceName"/> or it implements
        /// an interface with that name.
        /// </summary>
        /// <param name="type">The symbol to inspect</param>
        /// <param name="interfaceName">The fully-qualified name of the interface type to check</param>
        /// <returns>Whether <paramref name="type"/> has the name <paramref name="interfaceName"/> or it implements
        /// an interface with that name.</returns>
        public static bool IsOrImplementsInterface(this ITypeSymbol type, string interfaceName)
        {
            return type.GetFullName() == interfaceName ||
                type.AllInterfaces.Any(namedType => namedType.GetFullName() == interfaceName);
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is of type <paramref name="className"/> or inherits from a base
        /// class with that name (at any level in the hierarchy)
        /// </summary>
        /// <param name="type">The symbol to inspect</param>
        /// <param name="className">The fully-qualified name of the base type to check</param>
        /// <returns>Whether <paramref name="type"/> is of type <paramref name="className"/> or inherits from a base
        /// class with that name (at any level in the hierarchy)</returns>
        public static bool IsOrInheritsFromClass(this ITypeSymbol type, string className)
        {
            return type.GetBaseTypesAndThis().OfType<INamedTypeSymbol>().Any(t => t.GetFullName() == className);
        }
    }
}
