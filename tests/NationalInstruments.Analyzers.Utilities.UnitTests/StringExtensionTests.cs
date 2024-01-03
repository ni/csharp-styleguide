using System;
using System.Windows.Interop;
using NationalInstruments.Analyzers.Utilities.Extensions;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    public class StringExtensionTests
    {
        private const string ExampleText = "A quick brown fox";

        [Theory]
        [InlineData("A quick brown fox")]
        [InlineData("*A quick brown fox*")]
        [InlineData("*")]
        [InlineData("*brown fox")]
        [InlineData("A quick*")]
        [InlineData("* brown*")]
        [InlineData("*quick*fox")]
        [InlineData("*quick*wn*x")]
        public void MatchesWildcardPattern_PatternMatches_ReturnsTrue(string pattern)
        {
            Assert.True(ExampleText.MatchesWildcardPattern(pattern));
        }

        [Fact]
        public void MatchesWildcardPattern_PatternMatches_CaseWrong_ReturnsTrue()
        {
            Assert.True(ExampleText.MatchesWildcardPattern("a quick*"));
        }

        [Theory]
        [InlineData("A quick brown fo")] // missing last letter with no wildcard
        [InlineData("*a*z*")] // there is no 'z'
        [InlineData("*x*o*")] // characters are present but arranged wrong
        [InlineData(".*")] // regex syntax is escaped
        [InlineData("")] // maybe this will work?
        public void MatchesWildcardPattern_PatternDoesNotMatch_ReturnsFalse(string pattern)
        {
            Assert.False(ExampleText.MatchesWildcardPattern(pattern));
        }

        [Fact]
        public void MatchesWildcardPattern_PatternNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ExampleText.MatchesWildcardPattern(null));
        }

        [Theory]
        [InlineData(false)]
        public void MatchesWildcardPattern_InputNull_ThrowsArgumentNullException(bool argument)
        {
            var input = argument ? string.Empty : null;
            Assert.Throws<ArgumentNullException>(() => input.MatchesWildcardPattern("Anything"));
        }
    }
}
