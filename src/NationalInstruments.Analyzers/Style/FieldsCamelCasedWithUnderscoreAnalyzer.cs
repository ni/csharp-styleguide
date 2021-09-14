// Adapted from portions of the Roslynator source code, copyright (c) Josef Pihrt, licensed under the Apache License, Version 2.0.
// https://github.com/JosefPihrt/Roslynator

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Style
{
    /// <summary>
    /// Enforces the rule that mutable (non-const, non-readonly), private fields' names must both:
    ///  1. begin with a single underscore ('_')
    ///  2. have a lowercase or numeric character as the first non-underscore character
    /// To illustrate:
    ///     _myField (Good!)
    ///     myField (Bad!)
    ///     __myField (Bad!)
    ///     _MyField (Bad!)
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FieldsCamelCasedWithUnderscoreAnalyzer : NIDiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1001_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1001_Message), Resources.ResourceManager, typeof(Resources)),
            Category.Style,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal const string DiagnosticId = "NI1001";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static bool IsCamelCasePrefixedWithUnderscore(string value)
        {
            return value[0] != '_'
                ? false
                : value.Length == 1
                    ? true
                    : value[1] != '_' && !char.IsUpper(value[1]);
        }

        private void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;

            if (!field.IsConst
                && !field.IsReadOnly // NI-specific modification
                && !field.IsImplicitlyDeclared
                && field?.DeclaredAccessibility == Accessibility.Private
                && !string.IsNullOrEmpty(field.Name)
                && !IsCamelCasePrefixedWithUnderscore(field.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, field.Locations[0], field.Name));
            }
        }
    }
}
