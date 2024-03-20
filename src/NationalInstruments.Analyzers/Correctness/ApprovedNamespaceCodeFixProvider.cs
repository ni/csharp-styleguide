using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
                var sourceTree = location.SourceTree;
                var namespaceName = sourceTree is not null
                    ? (await sourceTree.GetRootAsync(context.CancellationToken))
                        .FindNode(location.SourceSpan)
                        .ToString()
                    : null;
                var approvedNamespacesFilePaths = diagnostic.Properties.Where(kv => kv.Key.StartsWith("Path")).Select(kv => kv.Value);

                foreach (var path in approvedNamespacesFilePaths)
                {
                    if (namespaceName is not null)
                    {
                        context.RegisterCodeFix(new ApprovedNamespaceCodeAction(context, namespaceName, path), context.Diagnostics);
                    }
                }
            }
        }

        private class ApprovedNamespaceCodeAction : CodeAction
        {
            private readonly CodeFixContext _context;
            private readonly string _namespaceName;
            private readonly string _approvedNamespacesFilePath;
            private readonly string _title;

            public ApprovedNamespaceCodeAction(CodeFixContext context, string namespaceName, string? approvedNamespacesFilePath)
            {
                _context = context;
                _namespaceName = namespaceName;
                _approvedNamespacesFilePath = approvedNamespacesFilePath ?? "Unknown";
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

            [SuppressMessage(
                "MicrosoftCodeAnalysisCorrectness",
                "RS1035:The symbol 'File' is banned for use by analyzers: Do not do file IO in analyzers",
                Justification = "Existing working code.")]
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
