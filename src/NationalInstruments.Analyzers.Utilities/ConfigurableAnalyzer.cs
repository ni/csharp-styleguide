using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.Utilities
{
    /// <summary>
    /// Base class for analyzers that can be configured with external files.
    /// </summary>
    public abstract class ConfigurableAnalyzer : StatefulAnalyzer
    {
        private IAdditionalFileService _additionalFileService;

        /// <summary>
        /// Constructor that stores an implementation of <see cref="IAdditionalFileService"/>.
        /// </summary>
        /// <param name="additionalFileService">Service that allows additional files to be found and parsed.</param>
        /// <param name="cancellationToken">Object that indicates if a cancellation was requested or not.</param>
        protected ConfigurableAnalyzer(IAdditionalFileService additionalFileService, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _additionalFileService = additionalFileService;
        }

        /// <summary>
        /// Loads configuration values from files that match the given <paramref name="fileNamePattern"/> using the derived
        /// class's implementation of <see cref="LoadConfigurations(string, XElement)"/>.
        /// </summary>
        /// <param name="fileNamePattern">
        /// A regular expression to match against a file's name.
        /// </param>
        public void LoadConfigurations(string fileNamePattern)
        {
            foreach (var file in _additionalFileService.GetFilesMatchingPattern(fileNamePattern))
            {
                XElement configuration = _additionalFileService.ParseXmlFile(file, CancellationToken);
                if (configuration != null)
                {
                    LoadConfigurations(configuration, file.Path);
                }
            }
        }

        /// <summary>
        /// Loads configuration values from the parsed XML contained within <paramref name="rootElement"/>.
        /// </summary>
        /// <param name="rootElement">Root element of the parsed XML.</param>
        /// <param name="filePath">Full path to the XML file that was parsed.</param>
        protected abstract void LoadConfigurations(XElement rootElement, string filePath);

        /// <summary>
        /// Checks the root XML element's name against the <paramref name="expectedName"/> to sanity check the XML.
        /// </summary>
        /// <param name="rootElement">Root element of the XML.</param>
        /// <param name="expectedName">Expected name of the root element.</param>
        /// <param name="filePath">Full path to the XML file.</param>
        /// <param name="rule">Rule to use if the root element's name differs from the expected name.</param>
        /// <param name="diagnostic">Diagnostic to return as an out parameter if the root element's name differs from the expected name.</param>
        /// <returns></returns>
        protected bool TryGetRootElementDiagnostic(XElement rootElement, string expectedName, string filePath, DiagnosticDescriptor rule, out Diagnostic diagnostic)
        {
            if (rootElement.Name != expectedName)
            {
                diagnostic = Diagnostic.Create(rule, Location.None, $"{filePath} must have a root element of <{expectedName}>");
                return true;
            }

            diagnostic = null;
            return false;
        }
    }
}
