using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness
{
    /// <summary>
    /// Analyzer that reports a diagnostic if the Boolean literal 'false' is returned from any
    /// implementation of IWeakEventListener's ReceiveWeakEvent method.
    /// </summary>
    /// <remarks>
    /// It is better to assert than return 'false'. Returning 'false' can cause crashes in some
    /// scenarios.
    /// </remarks>
    /// <example>
    /// class MyEvents : IWeakEventListener
    /// {
    ///     public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    ///     {
    ///         return false;
    ///     }
    /// }
    /// </example>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReceiveWeakEventMustReturnTrueAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1005";

        private const string ReceiveWeakEventMethodName = "ReceiveWeakEvent";

        public static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1005_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1005_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Resources.ResourceManager.GetString(nameof(Resources.NI1005_Description), CultureInfo.CurrentCulture));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeMethodSyntax, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodSyntax(SyntaxNodeAnalysisContext context)
        {
            var methodSyntax = (MethodDeclarationSyntax)context.Node;

            // Quickly abort if the method isn't named 'ReceiveWeakEvent'
            if (methodSyntax.Identifier.ValueText != ReceiveWeakEventMethodName)
            {
                return;
            }

            ISymbol receiveWeakEventMethod = WellKnownTypes.IWeakEventListener(context.Compilation)
                .GetMembers(ReceiveWeakEventMethodName).Single();

            // Is this method implementing IWeakEventListener's ReceiveWeakEvent?
            ISymbol method = context.SemanticModel.GetDeclaredSymbol(methodSyntax);
            ISymbol implementation = method.ContainingType.FindImplementationForInterfaceMember(receiveWeakEventMethod);
            if (implementation == null || !implementation.Equals(method))
            {
                return;
            }

            var returnStatementSyntaxes = methodSyntax.DescendantNodes().OfType<ReturnStatementSyntax>();
            foreach (var returnStatementSyntax in returnStatementSyntaxes)
            {
                // Does this return statement return a literal 'false'?
                var literalExpression = returnStatementSyntax.DescendantNodes().OfType<LiteralExpressionSyntax>().FirstOrDefault();
                if (literalExpression?.Kind() == SyntaxKind.FalseLiteralExpression)
                {
                    var diagnostic = Diagnostic.Create(Rule, literalExpression?.GetLocation() ?? returnStatementSyntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
