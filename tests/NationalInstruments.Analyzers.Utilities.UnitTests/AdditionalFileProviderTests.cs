using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Analyzers.TestUtilities;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests for <see cref="AdditionalFileProvider"/>.
    /// </summary>
    public sealed class AdditionalFileProviderTests
    {
        [Theory]
        [InlineData]
        [InlineData("b.txt")]
        [InlineData("a.bat")]
        public void DesiredFileMissing_GetFile_ReturnsNull(params string[] fileNames)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(fileNames));

            var file = fileProvider.GetFile("a.txt");

            Assert.Null(file);
        }

        [Theory]
        [InlineData("a.txt")]
        [InlineData("a.txt", "b.txt")]
        [InlineData("b.txt", "a.txt")]
        [InlineData("a.bat", "a.txt")]
        public void DesiredFilePresent_GetFile_ReturnsFile(params string[] fileNames)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(fileNames));

            var file = fileProvider.GetFile("a.txt");

            Assert.NotNull(file);
            Assert.Equal("a.txt", file?.Path);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("a.+")]
        [InlineData("a.tx")]
        public void DesiredFilePresent_GetFileWithoutExactName_ReturnsNull(string fileName)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles("a.txt"));

            var file = fileProvider.GetFile(fileName);

            Assert.Null(file);
        }

        [Fact]
        public void DesiredFilePresentMoreThanOnce_GetFile_ReturnsFirstFile()
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(("a.txt", "1"), ("b.txt", "2"), ("a.txt", "3")));

            var file = fileProvider.GetFile("a.txt");

            Assert.NotNull(file);
            Assert.Equal("a.txt", file?.Path);
            Assert.Equal("1", file?.GetText()?.ToString());
        }

        [Theory]
        [InlineData(new string[] { }, ".")]
        [InlineData(new[] { "a.txt" }, "c")]
        public void MatchingFilesMissing_GetMatchingFiles_ReturnsEmptyEnumerable(IEnumerable<string> fileNames, string pattern)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(fileNames.ToArray()));

            var files = fileProvider.GetMatchingFiles(pattern);

            Assert.Empty(files);
        }

        [Theory]
        [InlineData(new[] { "a.txt" }, "a", new[] { "a.txt" })]
        [InlineData(new[] { "a.txt", "b.txt" }, @"\w\.", new[] { "a.txt", "b.txt" })]
        [InlineData(new[] { "a.txt", "b.txt", "c.bat" }, @"\.txt", new[] { "a.txt", "b.txt" })]
        public void MatchingFilesPresent_GetMatchingFiles_ReturnsMatchingFiles(IEnumerable<string> fileNames, string pattern, IEnumerable<string> expectedFileNames)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(fileNames.ToArray()));

            var files = fileProvider.GetMatchingFiles(pattern);

            Assert.Equal(expectedFileNames, files.Select(x => x.Path));
        }

        private static TestAdditionalDocument[] CreateAdditionalFiles(params (string FileName, string Content)[] fileNameAndContentGroups)
            => fileNameAndContentGroups.Select(x => CreateAdditionalFile(x.FileName, x.Content)).ToArray();

        private static TestAdditionalDocument[] CreateAdditionalFiles(params string[] fileNames)
            => fileNames.Select(x => CreateAdditionalFile(x)).ToArray();

        private static TestAdditionalDocument CreateAdditionalFile(string fileName, string content = "")
            => new TestAdditionalDocument(fileName, content);
    }
}
