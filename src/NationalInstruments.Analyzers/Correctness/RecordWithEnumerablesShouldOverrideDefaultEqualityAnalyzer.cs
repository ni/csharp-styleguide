using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RecordWithEnumerablesShouldOverrideDefaultEqualityAnalyzer
        : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1019";

        public static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1019_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1019_Message), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1019_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSymbolAction(AnalyzeRecord, SymbolKind.NamedType);
        }

        private void AnalyzeRecord(SymbolAnalysisContext context)
        {
            var typeSymbol = (ITypeSymbol)context.Symbol;
            if (typeSymbol.IsRecord == false)
            {
                return;
            }

            var baseTypeProperties = GetBaseTypeProperties(typeSymbol)
                .ToImmutableHashSet(SymbolEqualityComparer.Default);

            var enumerableProperties = typeSymbol
                .GetPublicPropertySymbols()
                .Where(p => p.Type.IsEnumerable() && !baseTypeProperties.Contains(p))
                .ToImmutableArray();

            if (enumerableProperties.Length == 0)
            {
                // if the record does not have any enumerable properties,
                // then the default record equality implementation will work as expected.
                return;
            }

            if (typeSymbol.HasExplicitEquals())
            {
                // we don't need to check for GetHashCode because the built-in
                // C# warning CS0659 will flag code that implements Equals but not GetHashCode.
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, typeSymbol.Locations[0], typeSymbol.Name));
        }

        private ImmutableArray<IPropertySymbol> GetBaseTypeProperties(ITypeSymbol typeSymbol)
        {
            var baseType = typeSymbol.BaseType;
            if (baseType is null || baseType.Name == nameof(Object))
            {
                // For classes that don't have a base class declared,
                // the base type is not null but System.Object.
                return ImmutableArray<IPropertySymbol>.Empty;
            }

            return baseType.GetPublicPropertySymbols();
        }
    }
}
