using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.Utilities
{
    /// <summary>
    /// Interface to a service that allows analyzers to easily find, parse, and report errors with <see cref="AdditionalText" /> files.
    /// </summary>
    public interface IAdditionalFileService
    {
        /// <summary>
        /// A collection of <see cref="AdditionalText" /> files discovered by the instantiator of this class.
        /// </summary>
        ImmutableArray<AdditionalText> AdditionalFiles { get; }

        /// <summary>
        /// Any parsing-specific diagnostics that occurred while parsing an <see cref="AdditionalText" /> document.
        /// </summary>
        IList<Diagnostic> ParsingDiagnostics { get; }

        /// <summary>
        /// Filters the collection of additional files on filenames matching the specified regex pattern: <paramref name="fileNamePattern"/>.
        /// </summary>
        /// <param name="fileNamePattern">A regular expression to match against a file's name.</param>
        /// <returns>An enumerable of <see cref="AdditionalText" /> files with names that match the <paramref name="fileNamePattern"/>.</returns>
        IEnumerable<AdditionalText> GetFilesMatchingPattern(string fileNamePattern);

        /// <summary>
        /// Parses the text contained within the supplied <paramref name="xmlFile"/> as XML.
        /// </summary>
        /// <param name="xmlFile">An <see cref="AdditionalText" /> file that should have XML contents.</param>
        /// <param name="cancellationToken">A token that allows text retrieval/parsing to be canceled prior to completion.</param>
        /// <returns>The <see cref="XElement" /> root node.</returns>
        XElement ParseXmlFile(AdditionalText xmlFile, CancellationToken cancellationToken);
    }
}
