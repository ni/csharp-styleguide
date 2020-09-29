using System;
using System.Collections.Immutable;
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
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DatabaseColumnsShouldBeNullableAnalyzer : NIDiagnosticAnalyzer
    {
        private const string DiagnosticId = "NI0017";

        private static readonly LocalizableString LocalizedTitle = new LocalizableResourceString(nameof(Resources.NI0017_Title), Resources.ResourceManager, typeof(Resources));

        public static DiagnosticDescriptor ValueTypeRule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.NI0017_Diagnostic_ValueType), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI0017_Description), Resources.ResourceManager, typeof(Resources)));

        public static DiagnosticDescriptor IEnumerableColumnRule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.NI0017_Diagnostic_IEnumerable), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI0017_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ValueTypeRule, IEnumerableColumnRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecutionIf(IsRunningInProduction);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                compilationStartContext.RegisterSyntaxNodeAction(AnalyzeClassDeclarations, SyntaxKind.ClassDeclaration);
            });
        }

        private void AnalyzeClassDeclarations(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var classSymbol = classDeclaration.GetDeclaredOrReferencedSymbol(context.SemanticModel) as INamedTypeSymbol;

            if (!classSymbol.IsOrInheritsFromClass("Microsoft.EntityFrameworkCore.DbContext"))
            {
                return;
            }

            var dbContextProperties = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();

            foreach (var property in dbContextProperties)
            {
                var propertyType = property.Type.GetDeclaredOrReferencedSymbol(context.SemanticModel) as INamedTypeSymbol;

                if (propertyType.ConstructedFrom.GetFullName() != "Microsoft.EntityFrameworkCore.DbSet<T>")
                {
                    continue;
                }

                var propertySymbol = property.GetDeclaredOrReferencedSymbol(context.SemanticModel) as IPropertySymbol;

                foreach (var type in propertyType.TypeArguments)
                {
                    CheckType(type as INamedTypeSymbol, propertySymbol, context);
                }
            }
        }

        private void CheckType(INamedTypeSymbol type, IPropertySymbol declaringProperty, SyntaxNodeAnalysisContext context, bool allowValueType = false)
        {
            if (type.SpecialType == SpecialType.System_String)
            {
                // String is special because it's a fundamental DB type
                return;
            }

            if (type.IsGenericType)
            {
                foreach (var typeArgument in type.TypeArguments)
                {
                    CheckType(typeArgument as INamedTypeSymbol, declaringProperty, context, true);
                }

                if (type.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                {
                    return;
                }
            }

            if (type.IsValueType)
            {
                if (!allowValueType)
                {
                    ReportDiagnostic(context, ValueTypeRule, type, declaringProperty);
                }

                return;
            }

            bool IsGenericEnumerable(INamedTypeSymbol x) => x.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
            if (type.AllInterfaces.Any(IsGenericEnumerable))
            {
                if (!type.ContainingNamespace.Name.StartsWith("System", StringComparison.Ordinal))
                {
                    ReportDiagnostic(context, IEnumerableColumnRule, type, declaringProperty);
                }

                // IEnumerables get converted to (one|many) to many relationships, and their properties won't actually be in the DB
                return;
            }

            foreach (var property in type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>())
            {
                CheckProperty(property, context);
            }
        }

        private void CheckProperty(IPropertySymbol property, SyntaxNodeAnalysisContext context)
        {
            bool AllowsValueType(AttributeData x) =>
                x.AttributeClass.ToString() == "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute"
                || x.AttributeClass.ToString() == "System.ComponentModel.DataAnnotations.KeyAttribute";

            if (property.GetAttributes().Any(AllowsValueType))
            {
                // [NotMapped] properties don't map to columns, so they can be ignored
                // [Key] properties are primary keys, which are allowed to be non-null
                return;
            }

            if (property.Name.ToUpperInvariant() == "ID" || property.Name.ToUpperInvariant() == property.ContainingType.Name.ToUpperInvariant() + "ID")
            {
                // Entity Framework Core will configure these as primary keys (https://www.learnentityframeworkcore.com/conventions#primary-key)
                return;
            }

            CheckType(property.Type as INamedTypeSymbol, property, context);
        }

        private void ReportDiagnostic(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, INamedTypeSymbol type, IPropertySymbol property)
        {
            var diagnostic = Diagnostic.Create(descriptor, property.Locations.First(), type.Name, property.Name, property.ContainingType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
