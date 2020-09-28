using Microsoft.CodeAnalysis;

namespace NationalInstruments.Tools.Analyzers.Utilities
{
    public static class WellKnownTypes
    {
#pragma warning disable CA1720 // Identifier contains type name
        public static INamedTypeSymbol Object(Compilation compilation) => compilation.GetTypeByMetadataName("System.Object");
#pragma warning restore CA1720 // Identifier contains type name

        public static INamedTypeSymbol IWeakEventListener(Compilation compilation) => compilation.GetTypeByMetadataName("System.Windows.IWeakEventListener");
    }
}
