using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using NationalInstruments.Analyzers.TestUtilities;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests that the <see cref="ConfigurableAnalyzer.LoadConfigurations"/> method allows data to be read and stored from configuration files.
    /// </summary>
    public sealed class ConfigurableAnalyzerTests
    {
        private const string FileParseRuleId = "NI0001";

        public static DiagnosticDescriptor FileParseRule { get; } = new DiagnosticDescriptor(
            FileParseRuleId,
            "File parse error",
            "{0}",
            "National Instruments",
            DiagnosticSeverity.Error,
            true);

        [Fact]
        public void LoadConfigurations_MultipleFiles_ConfigurationsLoad()
        {
            string[] expectedEntries = { "A", "B" };

            var configurationFiles = ImmutableArray.Create<AdditionalText>(
                new TestAdditionalDocument("first.xml", $"<Data><Entry>{expectedEntries[0]}</Entry></Data>"),
                new TestAdditionalDocument("second.xml", $"<Data><Entry>{expectedEntries[1]}</Entry></Data>"),
                new TestAdditionalDocument("third.txt", "<Data><Entry>C</Entry></Data>"));

            var additionalFileService = new AdditionalFileService(configurationFiles, FileParseRule);
            var testAnalyzer = new TestAnalyzer(additionalFileService, CancellationToken.None);

            testAnalyzer.LoadConfigurations(@".+\.xml");

            Assert.Equal(expectedEntries, testAnalyzer.Entries);
        }

        private class TestAnalyzer : ConfigurableAnalyzer
        {
            public TestAnalyzer(IAdditionalFileService additionalFileService, CancellationToken cancellationToken)
                : base(additionalFileService, cancellationToken)
            {
                Entries = new HashSet<string>();
            }

            public HashSet<string> Entries { get; private set; }

            protected override void LoadConfigurations(XElement rootElement, string filePath)
            {
                Entries.UnionWith(rootElement.Elements("Entry").Select(x => x.Value.Trim()));
            }
        }
    }
}
