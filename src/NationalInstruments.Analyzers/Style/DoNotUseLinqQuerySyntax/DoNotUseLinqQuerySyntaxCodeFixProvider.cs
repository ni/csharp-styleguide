using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using NationalInstruments.Tools.Analyzers.Style.DoNotUseLinqQuerySyntax;

namespace NationalInstruments.Analyzers.Style.DoNotUseLinqQuerySyntax
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DoNotUseLinqQuerySyntaxCodeFixProvider))]
    [Shared]
    public sealed class DoNotUseLinqQuerySyntaxCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DoNotUseLinqQuerySyntaxAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var codeAction = CodeAction.Create(
                    "Rewrite LINQ expression using method syntax",
                    cancellationToken => RewriteLinqSyntaxAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                    equivalenceKey: nameof(DoNotUseLinqQuerySyntaxCodeFixProvider));

                context.RegisterCodeFix(codeAction, diagnostic);
            }

            return Task.CompletedTask;
        }

        private static async Task<Document> RewriteLinqSyntaxAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var rewriter = new QueryComprehensionToFluentRewriter(semanticModel);
            var oldExpression = root.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var newExpression = rewriter.Visit(oldExpression);
            editor.ReplaceNode(oldExpression, newExpression);

            return editor.GetChangedDocument();
        }
    }
}
