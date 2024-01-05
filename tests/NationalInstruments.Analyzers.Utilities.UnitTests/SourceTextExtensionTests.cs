using System;
using System.Globalization;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests for <see cref="SourceTextExtensions"/>.
    /// </summary>
    public sealed class SourceTextExtensionTests
    {
        [Fact]
        public void SourceText_ParseWithNullFunction_Throws()
        {
            var sourceText = SourceText.From(string.Empty);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>("parser", () => sourceText.Parse<int>(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
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
