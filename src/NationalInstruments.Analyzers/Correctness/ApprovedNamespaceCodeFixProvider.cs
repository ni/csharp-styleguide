using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using NationalInstruments.Analyzers.Properties;

namespace NationalInstruments.Analyzers.Correctness
{
    /// <summary>
    /// Fix for NI1017 - Unapproved namespace violation.
    /// Adds the namespace to the approved namespaces file.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ApprovedNamespaceCodeFixProvider))]
    [Shared]
    public class ApprovedNamespaceCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ApprovedNamespaceAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic != null)
            {
                var location = diagnostic.Location;
                var namespaceName = (await location.SourceTree.GetRootAsync(context.CancellationToken).ConfigureAwait(false))
                    .FindNode(location.SourceSpan)
                    .ToString();
                var approvedNamespacesFilePaths = diagnostic.Properties.Values;
                foreach (var path in approvedNamespacesFilePaths)
                {
                    context.RegisterCodeFix(new ApprovedNamespaceCodeAction(context, namespaceName, path), context.Diagnostics);
                }
            }
        }

        private class ApprovedNamespaceCodeAction : CodeAction
        {
            private readonly string _namespaceName;
            private readonly CodeFixContext _context;
            private readonly string _approvedNamespacesFilePath;
            private readonly string _title;

            public ApprovedNamespaceCodeAction(CodeFixContext context, string namespaceName, string approvedNamespacesFilePath)
            {
                _context = context;
                _namespaceName = namespaceName;
                _approvedNamespacesFilePath = approvedNamespacesFilePath;
                _title = string.Format(CultureInfo.InvariantCulture, Resources.NI1800_CodeFixTitleFormat, approvedNamespacesFilePath);
            }

            public override string Title => _title;

            public override string EquivalenceKey => _namespaceName;

            protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                // preview is not supported
                return Task.FromResult(Enumerable.Empty<CodeActionOperation>());
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                ApproveNamespace(_namespaceName, _approvedNamespacesFilePath);
                return Task.FromResult(_context.Document);
            }

            private void ApproveNamespace(string namespaceName, string namespacesFilePath)
            {
                var lines = File.ReadAllLines(namespacesFilePath);
                var namespaces = lines
                        .Concat(new[] { namespaceName })
                        .Select(x => x.Trim())
                        .OrderBy(x => x)
                        .Distinct();
                File.WriteAllLines(namespacesFilePath, namespaces);
            }
        }
    }
}
