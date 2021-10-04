using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness
{
    /// <summary>
    /// Analyzer that reports a diagnostic if an internal type is referenced that does not
    /// have the VisibleInternal attribute.
    /// </summary>
    /// <remarks>
    /// This analyzer is not complete, and is only enabled for tests! See https://dev.azure.com/ni/LabVIEW.Tools/_workitems/edit/148942/
    /// </remarks>
    /// <example>
    /// <code>
    /// Assembly A:
    /// using NationalInstruments.AssemblyB;
    ///
    /// namespace NationalInstruments.AssemblyA
    /// {
    ///     public class Program
    ///     {
    ///         internal InternalType InternalTypeInstance { get; set; }
    ///
    ///         internal void Method(InternalType typeInstance)
    ///         {
    ///             InternalType.StaticMethod();
    ///             typeInstance.RegularMethod();
    ///             typeInstance.InternalMethod();
    ///         }
    ///     }
    /// }
    ///
    /// Assembly B:
    /// namespace NationalInstruments.AssemblyB
    /// {
    ///     // If this attribute is not present on the type or method, we will report a diagnostic.
    ///     [VisibleInternalAttribute]
    ///     internal class InternalType
    ///     {
    ///         public static void StaticMethod()
    ///         {
    ///         }
    ///
    ///         public void RegularMethod()
    ///         {
    ///         }
    ///
    ///         [VisibleInternal]
    ///         internal void InternalMethod()
    ///         {
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReferencedInternalMustHaveVisibleInternalAttributeAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1009";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1009_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1009_Message), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: new LocalizableResourceString(nameof(Resources.NI1009_Description), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: "https://dev.azure.com/ni/LabVIEW.Tools/_workitems/edit/148942/");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.IdentifierName);
        }

        public void AnalyzeExpression(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode node = context.Node;
            ISymbol symbol = node.GetDeclaredOrReferencedSymbol(context.SemanticModel);
            if (symbol == null)
            {
                return;
            }

            if (!IsInternalOrFriendAccessibility(symbol.DeclaredAccessibility))
            {
                return;
            }

            IAssemblySymbol referencedAssembly = symbol.ContainingAssembly;
            IAssemblySymbol callingAssembly = context.ContainingSymbol.ContainingAssembly;
            if (referencedAssembly.Equals(callingAssembly, SymbolEqualityComparer.Default))
            {
                return;
            }

            IEnumerable<string> attributeNames = symbol.GetAttributes().Select(attribute => attribute.AttributeClass.Name);
            if (attributeNames.Contains("VisibleInternalAttribute", StringComparer.Ordinal))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), symbol.GetFullName()));
        }

        private static bool IsInternalOrFriendAccessibility(Accessibility accessibility)
        {
            return accessibility == Accessibility.ProtectedAndFriend
                   || accessibility == Accessibility.ProtectedAndInternal
                   || accessibility == Accessibility.Friend
                   || accessibility == Accessibility.Internal;
        }
    }
}
