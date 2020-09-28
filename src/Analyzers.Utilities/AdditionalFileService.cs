using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NationalInstruments.Tools.Analyzers.Utilities
{
    /// <summary>
    /// Service that allows analyzers to easily find, parse, and report errors with <see cref="AdditionalText" /> files.
    /// </summary>
    public sealed class AdditionalFileService : IAdditionalFileService
    {
        private readonly DiagnosticDescriptor _additionalFileParseRule;

        /// <summary>
        /// Constructor that accepts a collection of additional files and the <see cref="DiagnosticDescriptor"/> to use
        /// for a parsing error.
        /// </summary>
        /// <param name="additionalFiles">An immutable collection of <see cref="AdditionalText" /> files discovered by the caller.</param>
        /// <param name="additionalFileParseRule">A rule that contains information about how parsing errors should be reported.</param>
        public AdditionalFileService(ImmutableArray<AdditionalText> additionalFiles, DiagnosticDescriptor additionalFileParseRule)
        {
            _additionalFileParseRule = additionalFileParseRule;

            AdditionalFiles = additionalFiles;
            ParsingDiagnostics = new List<Diagnostic>();
        }

        /// <inheritdoc />
        public ImmutableArray<AdditionalText> AdditionalFiles { get; }

        /// <inheritdoc />
        public IList<Diagnostic> ParsingDiagnostics { get; }

        /// <inheritdoc />
        public IEnumerable<AdditionalText> GetFilesMatchingPattern(string fileNamePattern)
        {
            return AdditionalFiles.Where(file => Regex.IsMatch(Path.GetFileName(file.Path), fileNamePattern, RegexOptions.IgnoreCase));
        }

        /// <inheritdoc />
        public XElement ParseXmlFile(AdditionalText xmlFile, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var xml = string.Empty;

                if (!cancellationToken.IsCancellationRequested)
                {
                    xml = xmlFile.GetText(cancellationToken).ToString();
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    return XElement.Parse(xml);
                }
            }
            catch (XmlException ex)
            {
                ParsingDiagnostics.Add(Diagnostic.Create(_additionalFileParseRule, Location.None, ex.Message));
            }

            return null;
        }

        /// <summary>
        /// Uses the supplied <paramref name="compilationEndContext"/> to report any file-parsing-related diagnostics.
        /// </summary>
        /// <param name="compilationEndContext">A context provided from a compilation end action.</param>
        public void ReportAnyParsingDiagnostics(CompilationAnalysisContext compilationEndContext)
        {
            foreach (Diagnostic diagnostic in ParsingDiagnostics)
            {
                compilationEndContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
