using System.Collections.Generic;
using NationalInstruments.Analyzers.Style;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    public class LongChainDottedInvocationAnalyzerTests : NIDiagnosticAnalyzerTests<LongChainDottedInvocationAnalyzer>
    {
        public static IEnumerable<object[]> WellSplitStatementInVariousContexts =>
            new[]
            {
                new object[]
                {
@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(IEnumerable<ISoftwareContent> selectedSoftwareContents)
        {
            var distinctSelectedSoftware = selectedSoftwareContents
                .GroupBy(software => software.AliasName)
                .Select(g => g.First())
                .Distinct();
        }

        private interface ISoftwareContent
        {
            string AliasName { get; }
        }
    }
}
"
                }, // inside a method
                new object[]
                {
                    @"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        Test(IEnumerable<ISoftwareContent> selectedSoftwareContents)
        {
            var distinctSelectedSoftware = selectedSoftwareContents
                .GroupBy(software => software.AliasName)
                .Select(g => g.First())
                .Distinct();
        }

        private interface ISoftwareContent
        {
            string AliasName { get; }
        }
    }
}
"
                }, // inside a constructor
                new object[]
                {
                    @"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        private IEnumerable<ISoftwareContent> selectedSoftwareContents = Enumerable.Empty<ISoftwareContent>();

        IEnumerable<ISoftwareContent> DistinctSoftwareContents =>
         selectedSoftwareContents
            .GroupBy(software => software.AliasName)
            .Select(g => g.First())
            .Distinct();

        private interface ISoftwareContent
        {
            string AliasName { get; }
        }
    }
}
"
                }, // inside a property getter
                new object[]
                {
                    @"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(IEnumerable<ISoftwareContent> softwareContent)
        {
            softwareContent.Select(content => content
                .Children.GroupBy(software => software.AliasName)
                     .Select(g => g.First())
                     .Distinct());
        }

        private interface ISoftwareContent
        {
            IEnumerable<ISoftwareContent> Children { get; }

            string AliasName { get; }
        }
    }
}
"
                } // inside an argument
            };

        public static IEnumerable<object[]> PoorlySplitStatementInVariousContextsWithDiagnosticLocation =>
            new[]
            {
                new object[]
                {
                    @"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(IEnumerable<ISoftwareContent> selectedSoftwareContents)
        {
            var distinctSelectedSoftware = selectedSoftwareContents.GroupBy(software => software.AliasName).Select(g => g.First()).Distinct();
        }

        private interface ISoftwareContent
        {
            string AliasName { get; }
        }
    }
}
", (11, 44)
                }, // inside a method
                new object[]
                {
                    @"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        Test(IEnumerable<ISoftwareContent> selectedSoftwareContents)
        {
            var distinctSelectedSoftware = selectedSoftwareContents.GroupBy(software => software.AliasName).Select(g => g.First())
                .Distinct();
        }

        private interface ISoftwareContent
        {
            string AliasName { get; }
        }
    }
}
", (11, 44)
                }, // inside a constructor
                new object[]
                {
                    @"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        private IEnumerable<ISoftwareContent> selectedSoftwareContents = Enumerable.Empty<ISoftwareContent>();

        IEnumerable<ISoftwareContent> DistinctSoftwareContents =>
         selectedSoftwareContents.GroupBy(software => software.AliasName).Select(g => g.First())
            .Distinct();

        private interface ISoftwareContent
        {
            string AliasName { get; }
        }
    }
}
", (12, 10)
                }, // inside a property getter
                new object[]
                {
                    @"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(IEnumerable<ISoftwareContent> softwareContent)
        {
            softwareContent.Select(content => content.Children.GroupBy(software => software.AliasName).Select(g => g.First()).Distinct());
        }

        private interface ISoftwareContent
        {
            IEnumerable<ISoftwareContent> Children { get; }

            string AliasName { get; }
        }
    }
}
", (11, 47)
                } // inside an argument
            };

        [Fact]
        public void NI1017_SingleInvocationInStatement_NoDiagnostic()
        {
            var test = new TestFile(@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(ISoftwareContent softwareContent)
        {
            softwareContent.Child.AliasName.TrimEnd();
        }

        private interface ISoftwareContent
        {
            ISoftwareContent Child { get; }

            string AliasName { get; }
        }
    }
}
");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_WellSplitStatementWithDifferentKindsOfInvocations_NoDiagnostic()
        {
            var test = new TestFile(@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        string Foo(ISoftwareContent softwareContent) => softwareContent
            .Child()
            .AliasName.Length
            .ToString();

        private interface ISoftwareContent
        {
            ISoftwareContent Child();

            string AliasName { get; }
        }
    }
}
");

            VerifyDiagnostics(test);
        }

        [Theory]
        [MemberData(nameof(WellSplitStatementInVariousContexts))]
        public void NI1017_WellSplitStatement_NoDiagnostic(string testString)
        {
            var test = new TestFile(testString);

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_PoorlySplitStatementWithDifferentKindsOfInvocations_EmitsDiagnostic()
        {
            var test = new TestFile(@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        string Foo(ISoftwareContent softwareContent) => softwareContent.Child().AliasName.Length.ToString();

        private interface ISoftwareContent
        {
            ISoftwareContent Child();

            string AliasName { get; }
        }
    }
}
");

            VerifyDiagnostics(test, GetNI1017ResultAt(9, 57));
        }

        [Theory]
        [MemberData(nameof(PoorlySplitStatementInVariousContextsWithDiagnosticLocation))]
        public void NI1017_PoorlySplitStatement_EmitsDiagnostic(string testString, (int Line, int Col) location)
        {
            var test = new TestFile(testString);

            VerifyDiagnostics(test, GetNI1017ResultAt(location.Line, location.Col));
        }

        [Fact]
        public void NI1017_PoorlySplitComplexStatement_EmitsDiagnostic()
        {
            var test = new TestFile(
@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    internal class Test
    {
        private void Foo(IEnumerable<int> objects)
        {
            objects.Where(x =>
            {
                if (x > 2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }).Where(x => x % 2 == 0).Select(x => $""num { x }"").First();
        }
    }
}
");

            VerifyDiagnostics(test, GetNI1017ResultAt(11, 13));
        }

        [Fact]
        public void NI1017_WellSplitComplexStatement_NoDiagnostic()
        {
            var test = new TestFile(
@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    internal class Test
    {
        private void Foo(IEnumerable<int> objects)
        {
            objects.Where(x =>
            {
                if (x > 2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            })
            .Where(x => x % 2 == 0)
            .Select(x => $""num { x }"")
            .First();
        }
    }
}
");
            VerifyDiagnostics(test);
        }

            private DiagnosticResult GetNI1017ResultAt(int line, int column)
        {
            return GetResultAt(line, column, LongChainDottedInvocationAnalyzer.Rule);
        }
    }
}
