using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Tools.Analyzers.Properties;
using NationalInstruments.Tools.Analyzers.Utilities;
using NationalInstruments.Tools.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Tools.Analyzers.Correctness
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotLockDirectlyOnPrivateMemberLockAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1016";

        public static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1016_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1016_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1016_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecutionIf(IsRunningInProduction);
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            analysisContext.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
        }

        private void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
        {
            var lockStatementSyntax = (LockStatementSyntax)context.Node;
            var lockTargetType = context.SemanticModel.GetTypeInfo(lockStatementSyntax.Expression).Type as INamedTypeSymbol;
            if (TypeIsPrivateMemberLock(lockTargetType))
            {
                var diagnostic = Diagnostic.Create(Rule, lockStatementSyntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool TypeIsPrivateMemberLock(INamedTypeSymbol type)
        {
            return type.IsOrInheritsFromClass("NationalInstruments.Core.PrivateMemberLock");
        }
    }
}
