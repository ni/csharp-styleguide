using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.Utilities
{
    /// <summary>
    /// Class that contains various <see cref="SymbolDisplayFormat"/>s for obtaining
    /// different string representations of a symbol.
    /// </summary>
    public static class SymbolDisplayFormats
    {
        /// <summary>
        /// <see cref="SymbolDisplayFormat"/> for returning a symbol's name with its parameters fully-qualified.
        /// </summary>
        public static SymbolDisplayFormat FullyQualifiedParameters { get; } = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
            localOptions: SymbolDisplayLocalOptions.IncludeType,
            parameterOptions: SymbolDisplayParameterOptions.IncludeOptionalBrackets | SymbolDisplayParameterOptions.IncludeType);
    }
}
