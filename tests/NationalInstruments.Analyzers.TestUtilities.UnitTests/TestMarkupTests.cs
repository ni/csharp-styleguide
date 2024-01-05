using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Analyzers.TestUtilities.Markers;
using NationalInstruments.Analyzers.TestUtilities.UnitTests.Assets;
using Xunit;

namespace NationalInstruments.Analyzers.TestUtilities.UnitTests
{
    /// <summary>
    /// Tests that <see cref="TestMarkup"/> successfully parses markup so that the original source
    /// is returned along with the relevant markers.
    /// </summary>
    public sealed class TestMarkupTests
    {
        private const string OneDiagnosticTextMarkup = @"
namespace <|My.Namespace|>
{
}";

        private const string OneDiagnosticArgumentMarkup = @"
namespace <?>My.Namespace
{
}";

        private const string OneDiagnosticPositionMarkup = @"
namespace <|>My.Namespace
{
}";

        private const string ManyDiagnosticTextsMarkup = @"
namespace <|My.Namespace|>
{
    class <|Program|>
    {
        public void <|Method|>(string name)
        {
        }
    }
}";

        private const string ManyDiagnosticArgumentsMarkup = @"
namespace <?>My.Namespace
{
    class <?>Program
    {
        public void <?>Method(string name)
        {
        }
    }
}";

        private const string ManyDiagnosticPositionsMarkup = @"
namespace <|>My.Namespace
{
    class <|>Program
    {
        public void <|>Method(string name)
        {
        }
    }
}";

        private static readonly Test OneDiagnosticTextTest = new Test(
            OneDiagnosticTextMarkup,
            new DiagnosticTextMarker(2, 11, "My.Namespace"));

        private static readonly Test ManyDiagnosticTextsTest = new Test(
            ManyDiagnosticTextsMarkup,
            new DiagnosticTextMarker(2, 11, "My.Namespace"),
            new DiagnosticTextMarker(4, 11, "Program"),
            new DiagnosticTextMarker(6, 21, "Method"));

        public static IEnumerable<object[]> DiagnosticTextTests =>
            new[]
            {
                new object[] { OneDiagnosticTextTest },
                new object[] { ManyDiagnosticTextsTest },
            };

        public static IEnumerable<object[]> OneDiagnosticMarkerTests =>
            new[]
            {
                new object[] { OneDiagnosticTextTest },
                new object[] { new Test(OneDiagnosticArgumentMarkup, new DiagnosticArgumentMarker(2, 11)) },
                new object[] { new Test(OneDiagnosticPositionMarkup, new DiagnosticPositionMarker(2, 11)) },
            };

        public static IEnumerable<object[]> ManyDiagnosticMarkersTests =>
            new[]
            {
                new object[] { ManyDiagnosticTextsTest },
                new object[]
                {
                    new Test(
                        ManyDiagnosticArgumentsMarkup,
                        new DiagnosticArgumentMarker(2, 11),
                        new DiagnosticArgumentMarker(4, 11),
                        new DiagnosticArgumentMarker(6, 21)),
                },
                new object[]
                {
                    new Test(
                        ManyDiagnosticPositionsMarkup,
                        new DiagnosticPositionMarker(2, 11),
                        new DiagnosticPositionMarker(4, 11),
                        new DiagnosticPositionMarker(6, 21)),
                },
            };

        [Theory]
        [MemberData(nameof(OneDiagnosticMarkerTests))]
        public void OneDiagnosticMarker_SourceExtracted(Test test)
        {
            const string ExpectedSource = @"
namespace My.Namespace
{
}";

            new TestMarkup().Parse(test.Markup, out var source);

            Assert.Equal(ExpectedSource, source);
        }

        [Theory]
        [MemberData(nameof(ManyDiagnosticMarkersTests))]
        public void ManyDiagnosticMarkers_SourceExtracted(Test test)
        {
            const string ExpectedSource = @"
namespace My.Namespace
{
    class Program
    {
        public void Method(string name)
        {
        }
    }
}";

            new TestMarkup().Parse(test.Markup, out var source);

            Assert.Equal(ExpectedSource, source);
        }

        [Theory]
        [MemberData(nameof(OneDiagnosticMarkerTests))]
        public void OneDiagnosticMarker_PositionCaptured(Test test)
        {
            var markers = new TestMarkup().Parse(test.Markup, out var source);

            Assert.Single(markers);
            Assert.Equal(test.ExpectedMarkers[0].Line, markers[0].Line);
            Assert.Equal(test.ExpectedMarkers[0].Column, markers[0].Column);
        }

        [Theory]
        [MemberData(nameof(ManyDiagnosticMarkersTests))]
        public void ManyDiagnosticMarkers_PositionsCaptured(Test test)
        {
            var markers = new TestMarkup().Parse(test.Markup, out var source);

            Assert.Equal(test.ExpectedMarkers.Count, markers.Count);

            for (var i = 0; i < markers.Count; ++i)
            {
                Assert.Equal(test.ExpectedMarkers[i].Line, markers[i].Line);
                Assert.Equal(test.ExpectedMarkers[i].Column, markers[i].Column);
            }
        }

        [Theory]
        [MemberData(nameof(DiagnosticTextTests))]
        public void DiagnosticTextMarkers_TextCaptured(Test test)
        {
            var markers = new TestMarkup().Parse(test.Markup, out var source);

            Assert.Equal(test.ExpectedMarkers.Count, markers.Count);

            for (var i = 0; i < markers.Count; ++i)
            {
                Assert.IsType<DiagnosticTextMarker>(markers[i]);
                Assert.Equal(((DiagnosticTextMarker)test.ExpectedMarkers[i]).Text, ((DiagnosticTextMarker)markers[i]).Text);
            }
        }

        [Fact]
        public void OneEmbeddedDiagnosticTextMarker_TextCaptured()
        {
            const string Test = @"
namespace My.Namespace
{
    <|class Program
    {
        public void Method()
        {
        }

        <|class Foo
        {
        }|>
    }|>
}";

            const int ExpectedMarkerCount = 2;

            const string ExpectedText1 = @"class Program
    {
        public void Method()
        {
        }

        class Foo
        {
        }
    }";
            const string ExpectedText2 = @"class Foo
        {
        }";

            var markers = new TestMarkup().Parse(Test, out var source);

            Assert.Equal(ExpectedMarkerCount, markers.Count);

            foreach (var marker in markers)
            {
                Assert.IsType<DiagnosticTextMarker>(marker);
            }

            Assert.Equal(ExpectedText1, ((DiagnosticTextMarker)markers[0]).Text);
            Assert.Equal(ExpectedText2, ((DiagnosticTextMarker)markers[1]).Text);
        }

        [Fact]
        public void ManyEmbeddedDiagnosticTextMarkers_TextCaptured()
        {
            const string Test = @"
using System;

namespace My.Namespace
{
    <|class Program
    {
        <|public void Method(string name)
        {
            <|Console.WriteLine(name);|>
        }|>
    }|>
}";

            const int ExpectedMarkerCount = 3;

            const string ExpectedText1 = @"class Program
    {
        public void Method(string name)
        {
            Console.WriteLine(name);
        }
    }";
            const string ExpectedText2 = @"public void Method(string name)
        {
            Console.WriteLine(name);
        }";
            const string ExpectedText3 = @"Console.WriteLine(name);";

            var markers = new TestMarkup().Parse(Test, out var source);

            Assert.Equal(ExpectedMarkerCount, markers.Count);

            foreach (var marker in markers)
            {
                Assert.IsType<DiagnosticTextMarker>(marker);
            }

            Assert.Equal(ExpectedText1, ((DiagnosticTextMarker)markers[0]).Text);
            Assert.Equal(ExpectedText2, ((DiagnosticTextMarker)markers[1]).Text);
            Assert.Equal(ExpectedText3, ((DiagnosticTextMarker)markers[2]).Text);
        }

        [Fact]
        public void AllDiagnosticMarkers_MarkerDataCaptured()
        {
            const string Test = @"
namespace My.Namespace
{
    <|class Program
    {
        public void <?>Method(string name)
        {
            <|>Console.WriteLine(name);
        }
    }|>
}";

            const int ExpectedMarkerCount = 3;

            const string ExpectedText = @"class Program
    {
        public void Method(string name)
        {
            Console.WriteLine(name);
        }
    }";

            var expectedPositions = new List<Tuple<int, int>>()
            {
                Tuple.Create(4, 5),
                Tuple.Create(6, 21),
                Tuple.Create(8, 13),
            };

            var markers = new TestMarkup().Parse(Test, out var source);

            Assert.Equal(ExpectedMarkerCount, markers.Count);

            var textMarker = markers.First(x => x is DiagnosticTextMarker);
            Assert.Equal(ExpectedText, ((DiagnosticTextMarker)textMarker).Text);

            for (var i = 0; i < markers.Count; ++i)
            {
                var marker = markers[i];
                var expectedLine = expectedPositions[i].Item1;
                var expectedColumn = expectedPositions[i].Item2;

                Assert.Equal(expectedLine, marker.Line);
                Assert.Equal(expectedColumn, marker.Column);
            }
        }

        [Theory]
        [InlineData("<|class Program")]
        [InlineData("class Program|>")]
        public void MissingDiagnosticTextSyntax_ExceptionThrown(string classDeclaration)
        {
            var test = $@"
namespace My.Namespace
{{
    {classDeclaration}
    {{
    }}
}}";

            Assert.Throws<InvalidOperationException>(() => new TestMarkup().Parse(test, out var source));
        }

        [Fact]
        public void ExtremesDiagnosticTextSyntax_TextCaptured()
        {
            const int ExpectedMarkerCount = 1;
            const string Test = @"<|class Program { }|>";

            var markers = new TestMarkup().Parse(Test, out var source);

            Assert.NotNull(markers);
            Assert.Equal(ExpectedMarkerCount, markers.Count);
        }
    }
}
