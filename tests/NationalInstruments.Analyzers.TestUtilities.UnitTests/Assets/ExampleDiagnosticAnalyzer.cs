using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Utilities;

namespace NationalInstruments.Analyzers.TestUtilities.UnitTests.Assets
{
    /// <summary>
    /// Analyzer that reports diagnostics if a class contains constructors, fields set to string literals,
    /// methods, and/or properties. The various diagnostics take either no arguments, one argument, or
    /// multiple arguments.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Used for testing")]
    public sealed class ExampleDiagnosticAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "TEST1234";

        internal static readonly DiagnosticDescriptor NoArgumentRule = new DiagnosticDescriptor(
            DiagnosticId,
            "Title",
            "Message",
            "Category",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor OneArgumentRule = new DiagnosticDescriptor(
            DiagnosticId,
            "Title",
            "{0}",
            "Category",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ManyArgumentRule = new DiagnosticDescriptor(
            DiagnosticId,
            "Title",
            "Replace {0} in type {1} with {2}",
            "Category",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NoArgumentRule, OneArgumentRule, ManyArgumentRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            // NoArgumentRule
            context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);

            // OneArgumentRule
            context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);

            // ManyArgumentRule
            context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            // Report a diagnostic if there's a constructor
            var constructorSyntax = context.Node;
            var diagnostic = Diagnostic.Create(NoArgumentRule, constructorSyntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private void AnalyzeField(SyntaxNodeAnalysisContext context)
        {
            // Report a diagnostic for each field set to a string literal and include the literal in the message
            var fieldSyntax = context.Node;

            foreach (var literalSyntax in fieldSyntax.DescendantNodes().OfType<LiteralExpressionSyntax>())
            {
                var diagnostic = Diagnostic.Create(OneArgumentRule, literalSyntax.GetLocation(), literalSyntax.Token);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            // Report a diagnostic for each method and include its signature in the message
            var methodSyntax = context.Node;
            var diagnostic = Diagnostic.Create(OneArgumentRule, methodSyntax.GetLocation(), methodSyntax.ToString());
            context.ReportDiagnostic(diagnostic);
        }

        private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            // Report a diagnostic for each property set and include its identifier, type, and "private set" in the message
            var propertySyntax = (PropertyDeclarationSyntax)context.Node;

            var typeSyntax = propertySyntax.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var setSyntax = propertySyntax.DescendantNodes()
                .OfType<AccessorDeclarationSyntax>()
                .Where(x => x.Kind() == SyntaxKind.SetAccessorDeclaration).FirstOrDefault();

            if (typeSyntax == null || setSyntax == null)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(ManyArgumentRule, setSyntax.GetLocation(), $"{propertySyntax.Identifier}.set", typeSyntax.Identifier.Text, "private set");
            context.ReportDiagnostic(diagnostic);
        }
    }
}
