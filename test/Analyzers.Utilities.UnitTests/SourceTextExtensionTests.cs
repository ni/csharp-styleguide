using System;
using System.Globalization;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests for <see cref="SourceTextExtensions"/>.
    /// </summary>
    public sealed class SourceTextExtensionTests
    {
        [Fact]
        public void NullSourceText_Parse_Throws()
        {
            SourceText sourceText = null;

            Assert.Throws<ArgumentNullException>("text", () => sourceText.Parse(stream => stream.ReadToEnd()));
        }

        [Fact]
        public void SourceText_ParseWithNullFunction_Throws()
        {
            var sourceText = SourceText.From(string.Empty);

            Assert.Throws<ArgumentNullException>("parser", () => sourceText.Parse<int>(null));
        }

        [Fact]
        public void SourceText_Parse_ReturnsParserFunctionOutput()
        {
            var sourceText = SourceText.From("text");

            var contents = sourceText.Parse(stream => stream.ReadToEnd().ToUpper(CultureInfo.InvariantCulture));

            Assert.Equal("TEXT", contents);
        }
    }
}
