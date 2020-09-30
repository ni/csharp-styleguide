using System;
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
    /// Analyzer that reports a diagnostic if a class marked with the
    /// <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestClass</c> attribute does not inherit from
    /// <c>NationalInstruments.Core.TestUtilities.AutoTest</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// using Microsoft.VisualStudio.TestTools.UnitTesting;
    ///
    /// [TestClass]
    /// class MyUnitTests   // needs to inherit from AutoTest
    /// {
    ///     [TestMethod]
    ///     public void MyTestMethod()
    ///     {
    ///     }
    /// }
    /// </code>
    /// </example>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestClassesMustInheritFromAutoTestAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1007";

        private const string TestClassAttributeTypeName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute";
        private const string NIAutoTestAttributeTypeName = "NationalInstruments.Core.TestUtilities.AutoTest";

        public static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1007_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1007_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Resources.ResourceManager.GetString(nameof(Resources.NI1007_Description), CultureInfo.CurrentCulture));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeClassSyntax, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClassSyntax(SyntaxNodeAnalysisContext context)
        {
            var classSyntax = (ClassDeclarationSyntax)context.Node;

            // Is the [TestClass] attribute applied to this class?
            // Note: although the attribute occurs above the class in code, it's a descendant node in the syntax tree
            var hasTestAttribute = classSyntax
                .DescendantNodes()
                .OfType<AttributeSyntax>()
                .Any(syntax => context.SemanticModel.GetTypeInfo(syntax).Type.ToString().Equals(TestClassAttributeTypeName, StringComparison.OrdinalIgnoreCase));

            if (!hasTestAttribute)
            {
                return;
            }

            // Yes, this is a [TestClass]. Does it inherit from NI's AutoTest?
            var testClass = classSyntax.GetDeclaredOrReferencedSymbol(context.SemanticModel) as INamedTypeSymbol;
            if (!testClass.GetBaseTypesAndThis().Any(x => x.ToString().Equals(NIAutoTestAttributeTypeName, StringComparison.OrdinalIgnoreCase)))
            {
                var diagnostic = Diagnostic.Create(Rule, classSyntax.GetLocation(), classSyntax.Identifier.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
