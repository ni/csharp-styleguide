using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1019_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1019_Message), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1019_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
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

            var enumerableProperties = typeSymbol
                .GetPublicPropertySymbols()
                .Where(p => p.Type.IsEnumerable())
                .ToImmutableArray();

            if (enumerableProperties.Length == 0)
            {
                return;
            }

            // check if the record type has implemented its own Equality methods
            foreach (var location in typeSymbol.Locations)
            {
                var rootNode = (CompilationUnitSyntax)location.SourceTree.GetRoot();
                var recordDeclarationNode = rootNode.Members
                    .OfType<RecordDeclarationSyntax>()
                    .FirstOrDefault();
                if (recordDeclarationNode is null)
                {
                    continue;
                }

                if (recordDeclarationNode.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Any(m => m.Identifier.Text == "Equals"))
                {
                    // we don't need to check for GetHashCode because the built-in C# analyzer will
                    // flag code that implements Equals but not GetHashCode.
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, typeSymbol.Locations[0], typeSymbol.Name));
        }
    }
}
