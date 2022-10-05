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

            IMethodSymbol overridden = method.OverriddenMethod;

            if (method.ContainingType.SpecialType == SpecialType.System_Object)
            {
                // This is object.Finalize
                return true;
            }

            if (overridden == null)
            {
                return false;
            }

            for (IMethodSymbol o = overridden.OverriddenMethod; o != null; o = o.OverriddenMethod)
            {
                overridden = o;
            }

            return overridden.ContainingType.SpecialType == SpecialType.System_Object; // it is object.Finalize
        }
    }
}
