// Adapted from portions of the Roslynator source code, copyright (c) Josef Pihrt, licensed under the Apache License, Version 2.0.
// https://github.com/JosefPihrt/Roslynator

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.Utilities
{
    /// <summary>
    /// Thin wrapper around Microsoft.CodeAnalysis.Rename.Renamer that renames a symbol in the context of the entire solution.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Renamer", Justification = "Using same name as wrapped type: Microsoft.CodeAnalysis.Rename.Renamer")]
    public static class Renamer
    {
        public static Task<Solution> RenameSymbolAsync(
            TextDocument document,
            ISymbol symbol,
            string newName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            Solution solution = document.Project.Solution;

            return Microsoft.CodeAnalysis.Rename.Renamer.RenameSymbolAsync(
                solution,
                symbol,
                newName,
                solution.Workspace.Options,
                cancellationToken);
        }
    }
}
