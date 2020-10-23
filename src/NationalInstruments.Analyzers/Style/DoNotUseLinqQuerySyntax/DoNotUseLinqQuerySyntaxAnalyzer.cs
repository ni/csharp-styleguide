using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Style.DoNotUseLinqQuerySyntax
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotUseLinqQuerySyntaxAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1018";

        public static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1018_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1018_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeQueryExpression, SyntaxKind.QueryExpression);
        }

        private void AnalyzeQueryExpression(SyntaxNodeAnalysisContext context)
        {
            var syntaxNode = context.Node;
            var diagnostic = Diagnostic.Create(Rule, syntaxNode.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
