// Adapted from portions of the Roslynator source code, copyright (c) Josef Pihrt, licensed under the Apache License, Version 2.0.
// https://github.com/JosefPihrt/Roslynator

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace NationalInstruments.Analyzers.Style
{
    /// <summary>
    /// Fixes a violation of the associated rule by renaming the field to have exactly one leading underscore, and converts the next character to lowercase, if necessary.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldsCamelCasedWithUnderscoreCodeFixProvider))]
    [Shared]
    public sealed class FieldsCamelCasedWithUnderscoreCodeFixProvider : CodeFixProvider
    {
        public override sealed ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(FieldsCamelCasedWithUnderscoreAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            VariableDeclaratorSyntax declarator = root
                .FindNode(context.Span, getInnermostNodeForTie: true)
                ?.FirstAncestorOrSelf<VariableDeclaratorSyntax>();

            if (declarator == null)
            {
                return;
            }

            SemanticModel semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            ISymbol symbol = semanticModel.GetDeclaredSymbol(declarator, context.CancellationToken);

            var oldName = declarator.Identifier.ValueText;
            var newName = EnsureUniqueMemberName(FixName(oldName), declarator.Identifier.SpanStart, semanticModel, context.CancellationToken);

            var codeAction = CodeAction.Create(
                $"Rename '{oldName}' to '{newName}'",
                cancellationToken => Renamer.RenameSymbolAsync(context.Document.Project.Solution, symbol, new SymbolRenameOptions(), newName, cancellationToken),
                equivalenceKey: oldName + newName);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static string FixName(string value)
        {
            var prefix = "_";

            if (value.Length <= 0)
            {
                return prefix;
            }

            var sb = new StringBuilder(prefix, value.Length + prefix.Length);

            // Skip leading underscore(s). We've already added one to our string builder.
            var i = 0;
            for (; i < value.Length && value[i] == '_'; ++i)
            {
            }

            var firstNonUnderscore = char.IsUpper(value[i]) ? char.ToLower(value[i], CultureInfo.CurrentCulture) : value[i];
            sb.Append(firstNonUnderscore);

            i++;

            sb.Append(value, i, value.Length - i);

            return sb.ToString();
        }

        private static string EnsureUniqueMemberName(
            string baseName,
            int position,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            ISymbol symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
            INamedTypeSymbol asNamedTypeSymbol = null;

            while (symbol != null && (asNamedTypeSymbol = symbol as INamedTypeSymbol) == null)
            {
                symbol = symbol.ContainingSymbol;
            }

            IList<ISymbol> symbols = asNamedTypeSymbol?.GetMembers() ?? semanticModel.LookupSymbols(position);

            var suffix = 2;
            var name = baseName;

            while (!symbols.Any(x => string.Equals(x.Name, name, StringComparison.Ordinal)))
            {
                name = baseName + suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }

            return name;
        }
    }
}
