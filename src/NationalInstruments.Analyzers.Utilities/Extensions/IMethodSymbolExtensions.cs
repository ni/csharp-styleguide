using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.Utilities.Extensions
{
    /// <summary>
    /// Class that contains helpful extensions to <see cref="IMethodSymbol"/>.
    /// </summary>
    /// <remarks>
    /// Microsoft has most of these extensions internally, but they haven't made them public.
    /// </remarks>
    public static class IMethodSymbolExtensions
    {
        /// <summary>
        /// Checks if the given method is a Finalizer implementation.
        /// </summary>
        /// <see cref="https://github.com/dotnet/roslyn-analyzers/blob/2af4cd81f7b5ac21569411f8dd5c93272c6dd19f/src/Utilities/Compiler/Extensions/IMethodSymbolExtensions.cs#L786"/>
        /// <param name="method">The method to check</param>
        public static bool IsFinalizer(this IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.Destructor)
            {
                return true; // for C#
            }

            if (method.Name != WellKnownMemberNames.DestructorName || !method.Parameters.IsEmpty || !method.ReturnsVoid)
            {
                return false;
            }

            IMethodSymbol? overridden = method.OverriddenMethod;

            if (method.ContainingType.SpecialType == SpecialType.System_Object)
            {
                // This is object.Finalize
                return true;
            }

            if (overridden is null)
            {
                return false;
            }

            for (IMethodSymbol? o = overridden.OverriddenMethod; o is not null; o = o.OverriddenMethod)
            {
                overridden = o;
            }

            return overridden?.ContainingType.SpecialType == SpecialType.System_Object; // it is object.Finalize
        }

        /// <summary>
        /// Get the original definitions.
        /// </summary>
        /// <see cref="https://github.com/dotnet/roslyn-analyzers/blob/2af4cd81f7b5ac21569411f8dd5c93272c6dd19f/src/Utilities/Compiler/Extensions/IMethodSymbolExtensions.cs#L786"/>
        /// <param name="methodSymbol">The method that may have original defintions.</param>
        /// <returns>An array of original definitions.</returns>
        public static ImmutableArray<IMethodSymbol> GetOriginalDefinitions(this IMethodSymbol methodSymbol)
        {
            ImmutableArray<IMethodSymbol>.Builder originalDefinitionsBuilder = ImmutableArray.CreateBuilder<IMethodSymbol>();

            if (methodSymbol.IsOverride && (methodSymbol.OverriddenMethod != null))
            {
                originalDefinitionsBuilder.Add(methodSymbol.OverriddenMethod);
            }

            if (!methodSymbol.ExplicitInterfaceImplementations.IsEmpty)
            {
                originalDefinitionsBuilder.AddRange(methodSymbol.ExplicitInterfaceImplementations);
            }

            var typeSymbol = methodSymbol.ContainingType;
            var methodSymbolName = methodSymbol.Name;

            originalDefinitionsBuilder.AddRange(typeSymbol.AllInterfaces
                .SelectMany(m => m.GetMembers(methodSymbolName))
                .OfType<IMethodSymbol>()
                .Where(m => methodSymbol.Parameters.Length == m.Parameters.Length
                            && methodSymbol.Arity == m.Arity
                            && typeSymbol.FindImplementationForInterfaceMember(m) != null));

            return originalDefinitionsBuilder.ToImmutable();
        }
    }
}
