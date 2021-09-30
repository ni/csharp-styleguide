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
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AwaitInReadLockOrTransactionAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1015";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            new LocalizableResourceString(nameof(Resources.NI1015_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NI1015_Message), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Resources.ResourceManager.GetString(nameof(Resources.NI1015_Description), CultureInfo.CurrentCulture));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
        }

        private static bool MethodCallBeginsTransaction(SimpleNameSyntax methodCalledSyntax)
        {
            var methodNameSyntax = methodCalledSyntax.Identifier.Text;
            // In the future, could look at Action callbacks for TransactTopLevel, TransactWhenPossible
            return methodNameSyntax == "BeginTransaction" || methodNameSyntax == "BeginTransactionIfNecessary";
        }

        private static bool TypeIsTransactionManager(INamedTypeSymbol type)
        {
            return type.IsOrImplementsInterface("NationalInstruments.SourceModel.ITransactionManager");
        }

        private static bool TypeIsTransactionRecruiter(INamedTypeSymbol type)
        {
            return type.IsOrImplementsInterface("NationalInstruments.SourceModel.ITransactionRecruiter");
        }

        private static bool TypeIsElement(INamedTypeSymbol type)
        {
            return type.IsOrInheritsFromClass("NationalInstruments.SourceModel.Element");
        }

        private static bool TypeIsTransactionServices(INamedTypeSymbol type)
        {
            return type.IsOrImplementsInterface("NationalInstruments.SourceModel.ITransactionServices");
        }

        private void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var usingStatementSyntax = (UsingStatementSyntax)context.Node;
            var containingMethodSyntax = usingStatementSyntax.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethodSyntax != null &&
                !containingMethodSyntax.Modifiers.Any(syntaxToken => syntaxToken.Kind() == SyntaxKind.AsyncKeyword))
            {
                return;
            }

            var usingAcquiresLock = false;
            var memberAccessSyntaxes = (usingStatementSyntax.Declaration?.DescendantNodes().OfType<MemberAccessExpressionSyntax>()).ToSafeEnumerable()
                .Concat((usingStatementSyntax.Expression?.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>()).ToSafeEnumerable());
            foreach (var memberAccessSyntax in memberAccessSyntaxes)
            {
                // If it's an array, pointer, or type parameter we can ignore it anyway, so cast to INamedTypeSymbol.
                if (context.SemanticModel.GetTypeInfo(memberAccessSyntax.Expression).Type is INamedTypeSymbol itemCalledOnType)
                {
                    var methodCalledNameSyntax = memberAccessSyntax.Name;
                    if (TypeIsTransactionManager(itemCalledOnType) && MethodCallBeginsTransaction(methodCalledNameSyntax))
                    {
                        usingAcquiresLock = true;
                        break;
                    }

                    if (TypeIsTransactionRecruiter(itemCalledOnType) && methodCalledNameSyntax.Identifier.Text == "DisableTransactionRecording")
                    {
                        usingAcquiresLock = true;
                        break;
                    }

                    if (TypeIsElement(itemCalledOnType) && methodCalledNameSyntax.Identifier.Text == "AcquireModelReadLock")
                    {
                        usingAcquiresLock = true;
                        break;
                    }

                    if (TypeIsTransactionServices(itemCalledOnType) && methodCalledNameSyntax.Identifier.Text == "AcquireModelReadLock")
                    {
                        usingAcquiresLock = true;
                        break;
                    }
                }
            }

            if (usingAcquiresLock)
            {
                var bodySyntax = usingStatementSyntax.Statement;
                foreach (var awaitExpressionSyntax in bodySyntax.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>())
                {
                    var diagnostic = Diagnostic.Create(Rule, awaitExpressionSyntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
