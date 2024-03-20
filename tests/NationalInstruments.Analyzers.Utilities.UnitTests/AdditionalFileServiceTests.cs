using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Threading;
using NationalInstruments.Analyzers.TestUtilities;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests that the <see cref="AdditionalFileService" /> allows any number of <see cref="AdditionalText" /> files to be matched and parsed
    /// with any errors caught and reported as necessary.
    /// </summary>
    public sealed class AdditionalFileServiceTests : UtilitiesTestBase
    {
        private const string FileParseRuleId = "NI0000";
        private const string ExampleXmlFileName = "a.xml";
        private const string InvalidXml = "<Root><Entry>1</Root>";
        private const string ValidXml = @"<Root>
    <Entry>1</Entry>
    <Entry>2</Entry>
    <Entry>3</Entry>
</Root>";

        public static DiagnosticDescriptor FileParseRule { get; } = new DiagnosticDescriptor(
            FileParseRuleId,
            "File parse error",
            "{0}",
            "National Instruments",
            DiagnosticSeverity.Error,
            true);

        [Fact]
        public void AdditionalFileService_GetFilesMatchingPattern_MatchesFound()
        {
            var additionalFiles = ImmutableArray.Create<AdditionalText>(
                new TestAdditionalDocument("a1.txt", string.Empty),
                new TestAdditionalDocument("aa.xml", string.Empty),
                new TestAdditionalDocument("a2.txt", string.Empty));

            var additionalFileService = new AdditionalFileService(additionalFiles, FileParseRule);
            var matchingFiles = additionalFileService.GetFilesMatchingPattern(@"a\d.txt").Select(x => x.Path).ToList();

            Assert.Equal(2, matchingFiles.Count);
            Assert.Contains("a1.txt", matchingFiles);
            Assert.Contains("a2.txt", matchingFiles);
        }

        [Fact]
        public void AdditionalFileService_ParseXmlFile_ValidXml_Parsed()
        {
            var additionalFile = new TestAdditionalDocument(
                ExampleXmlFileName,
                ValidXml);

            var additionalFileService = new AdditionalFileService(ImmutableArray.Create<AdditionalText>(additionalFile), FileParseRule);

            var parsedFile = additionalFileService.ParseXmlFile(additionalFile);

            Assert.Empty(additionalFileService.ParsingDiagnostics);
            Assert.Equal(3, parsedFile?.Elements("Entry").Count());
        }

        [Fact]
        public void AdditionalFileService_ParseXmlFile_InvalidXml_ParsingDiagnostic()
        {
            const string ExpectedExceptionMessage =
                "The 'Entry' start tag on line 1 position 8 does not match the end tag of 'Root'. Line 1, position 17.";

            var additionalFile = new TestAdditionalDocument(ExampleXmlFileName, InvalidXml);
            var additionalFileService = new AdditionalFileService(ImmutableArray.Create<AdditionalText>(additionalFile), FileParseRule);

            additionalFileService.ParseXmlFile(additionalFile);

            Assert.Single(additionalFileService.ParsingDiagnostics);
            Assert.Equal(FileParseRuleId, additionalFileService.ParsingDiagnostics[0].Id);
            Assert.Equal(ExpectedExceptionMessage, additionalFileService.ParsingDiagnostics[0].GetMessage());
        }

        [Fact]
        public void AdditionalFileService_ParseXmlFile_MultipleFilesWithInvalidXml_ParsingDiagnostics()
        {
            var additionalFiles = ImmutableArray.Create<AdditionalText>(
                new TestAdditionalDocument("a.xml", InvalidXml),
                new TestAdditionalDocument("b.xml", InvalidXml));

            var additionalFileService = new AdditionalFileService(additionalFiles, FileParseRule);

            foreach (var file in additionalFiles)
            {
                additionalFileService.ParseXmlFile(file);
            }

            Assert.Equal(2, additionalFileService.ParsingDiagnostics.Count);
        }

        [Fact]
        public void AdditionalFileService_ReportAnyConfigParseDiagnostics_Diagnostic()
        {
            VerifyCSharp(
                string.Empty,
                (tree, compilation) =>
                {
                    var additionalFile = new TestAdditionalDocument(ExampleXmlFileName, InvalidXml);
                    var compilationWithAnalyzers = compilation.WithAnalyzers(
                        ImmutableArray.Create<DiagnosticAnalyzer>(new TestAnalyzer()),
                        new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(additionalFile)));

                    var context = new JoinableTaskContext();
                    ImmutableArray<Diagnostic> diagnostics = new JoinableTaskFactory(context).Run(async () =>
                        await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync());

                    Assert.Single(diagnostics);
                    Assert.Equal(FileParseRuleId, diagnostics[0].Id);
                });
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class TestAnalyzer : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(FileParseRule);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterCompilationStartAction(compilationStartContext =>
                {
                    var additionalFileService = new AdditionalFileService(compilationStartContext.Options.AdditionalFiles, FileParseRule);

                    foreach (AdditionalText file in additionalFileService.GetFilesMatchingPattern(ExampleXmlFileName))
                    {
                        additionalFileService.ParseXmlFile(file);
                    }

                    // Register a random SyntaxNodeAction to avoid error RS1013: Start action has no registered non-end actions
                    compilationStartContext.RegisterSyntaxNodeAction(_ => { }, SyntaxKind.ClassDeclaration);
                    compilationStartContext.RegisterCompilationEndAction(additionalFileService.ReportAnyParsingDiagnostics);
                });
            }
        }
    }
}
