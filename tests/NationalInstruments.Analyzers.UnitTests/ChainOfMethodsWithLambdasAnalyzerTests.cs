using System.Collections.Generic;
using NationalInstruments.Analyzers.Style;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    public class ChainOfMethodsWithLambdasAnalyzerTests : NIDiagnosticAnalyzerTests<ChainOfMethodsWithLambdasAnalyzer>
    {
        public static IEnumerable<object[]> WellSplitLambdasInVariousContexts =>
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
                .Select(g => g.First()).Distinct();
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
                .Select(g => g.First()).Distinct();
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
            .Select(g => g.First()).Distinct();

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
                     .Select(g => g.First()).Distinct());
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

        public static IEnumerable<object[]> PoorlySplitLambdasInVariousContexts =>
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
            var distinctSelectedSoftware = <|>selectedSoftwareContents.GroupBy(software => software.AliasName).Select(g => g.First()).Distinct();
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
            var distinctSelectedSoftware = <|>selectedSoftwareContents.GroupBy(software => software.AliasName).Select(g => g.First())
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
         <|>selectedSoftwareContents.GroupBy(software => software.AliasName).Select(g => g.First())
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
            softwareContent.Select(content => <|>content.Children.GroupBy(software => software.AliasName).Select(g => g.First()).Distinct());
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

        [Theory]
        [InlineData("softwareContent.Child().ToString().TrimEnd()")]
        [InlineData("softwareContent.Child().AliasName.TrimEnd().Length.ToString()")]
        public void NI1017_NoLambdas_NoDiagnostic(string expression)
        {
            var test = new AutoTestFile(
$@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{{
    class Test
    {{
        void Foo(ISoftwareContent softwareContent)
        {{
            {expression};
        }}

        private interface ISoftwareContent
        {{
            ISoftwareContent Child();

            string AliasName {{ get; }}
        }}
    }}
}}
");
            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("softwareContent.Children().Any(s => s.AliasName == \"T\")")]
        [InlineData("softwareContent.Children(x => true).Any()")]
        [InlineData("softwareContent.FirstChild.Children().First().AliasName.Any(s => s == 'A')")]
        [InlineData("softwareContent.FirstChild.Children(x => true).First().AliasName.Any()")]
        public void NI1017_OneLambda_NoDiagnostic(string expression)
        {
            var test = new AutoTestFile(
$@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{{
    class Test
    {{
        void Foo(ISoftwareContent softwareContent)
        {{
            {expression};
        }}

        private interface ISoftwareContent
        {{
            ISoftwareContent FirstChild {{ get; }}

            IEnumerable<ISoftwareContent> Children(Predicate<int> predicate = null);

            string AliasName {{ get; }}
        }}
    }}
}}
");
            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_PoorlySplitMultipleLambdasInMethodInvocations_EmitsDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(ISoftwareContent softwareContent)
        {
            <|>softwareContent.Children(x => true).Any(s => s.AliasName == ""T"");
        }

        private interface ISoftwareContent
        {
            IEnumerable<ISoftwareContent> Children(Predicate<int> predicate = null);

            string AliasName { get; }
        }
    }
}
",  GetNI1017Rule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_PoorlySplitMultipleLambdasAmongMixedInvocations_EmitsDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(ISoftwareContent softwareContent)
        {
            <|>softwareContent.FirstChild.Children(x => true).First().AliasName.Any(s => s == 'A');
        }

        private interface ISoftwareContent
        {
            ISoftwareContent FirstChild { get; }

            IEnumerable<ISoftwareContent> Children(Predicate<int> predicate = null);

            string AliasName { get; }
        }
    }
}
", GetNI1017Rule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_WellSplitMultipleLambdas_NoDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(ISoftwareContent softwareContent)
        {
            softwareContent.Children(x => true)
                .Any(s => s.AliasName == ""T"");
            softwareContent.FirstChild.Children(x => true).First().AliasName
                .Any(s => s == 'A');
        }

        private interface ISoftwareContent
        {
            ISoftwareContent FirstChild { get; }

            IEnumerable<ISoftwareContent> Children(Predicate<int> predicate = null);

            string AliasName { get; }
        }
    }
}
");
            VerifyDiagnostics(test);
        }

        [Theory]
        [MemberData(nameof(WellSplitLambdasInVariousContexts))]
        public void NI1017_WellSplitLambdasInVariousContexts_NoDiagnostic(string testString)
        {
            var test = new AutoTestFile(testString);

            VerifyDiagnostics(test);
        }

        [Theory]
        [MemberData(nameof(PoorlySplitLambdasInVariousContexts))]
        public void NI1017_PoorlySplitLambdasInVariousContexts_EmitsDiagnostic(string testString)
        {
            var test = new AutoTestFile(testString, GetNI1017Rule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_PoorlySplitMultiLineLambdas_EmitsDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    internal class Test
    {
        private void Foo(IEnumerable<int> objects)
        {
            <|>objects.Where(x =>
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
", GetNI1017Rule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_WellSplitMultiLineLambdas_NoDiagnostic()
        {
            var test = new AutoTestFile(
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
            .Select(x => $""num { x }"").First();
        }
    }
}
");
            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_PoorlySplitLambdasExtensionMethods_EmitsDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(ISoftwareContent softwareContent)
        {
            <|>softwareContent.Foo(x => true).Baz(x => true);
        }

    }
    public interface ISoftwareContent
    {
    }

    public static class ISoftwareContentExtensions
    {
        public static ISoftwareContent Foo(
            this ISoftwareContent softwareContent,
            Predicate<string> predicate) => softwareContent;

        public static ISoftwareContent Baz(
            this ISoftwareContent softwareContent,
            Predicate<string> predicate) => softwareContent;
    }
}
", GetNI1017Rule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_WellSplitLambdasExtensionMethods_NoDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(ISoftwareContent softwareContent)
        {
            softwareContent.Foo(x => true)
                .Baz(x => true);
        }

    }
    public interface ISoftwareContent
    {
    }

    public static class ISoftwareContentExtensions
    {
        public static ISoftwareContent Foo(
            this ISoftwareContent softwareContent,
            Predicate<string> predicate) => softwareContent;

        public static ISoftwareContent Baz(
            this ISoftwareContent softwareContent,
            Predicate<string> predicate) => softwareContent;
    }
}
");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_PoorlySplitLambdasDelegateInvocations_EmitsDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    public delegate ISoftwareContent Creator(Func<ISoftwareContent, string> func);

    class Test
    {

        void Foo(ISoftwareContent softwareContent)
        {
            <|>softwareContent.Foo(soft => ""first lambda"").Foo(soft => ""second lambda"");
        }

    }
    public interface ISoftwareContent
    {
        Creator Foo { get; }
    }
}
", GetNI1017Rule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_WellSplitLambdasDelegateInvocations_NoDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    public delegate ISoftwareContent Creator(Func<ISoftwareContent, string> func);

    class Test
    {

        void Foo(ISoftwareContent softwareContent)
        {
            softwareContent.Foo(soft => ""first lambda"")
                .Foo(soft => ""second lambda"");
        }

    }
    public interface ISoftwareContent
    {
        Creator Foo { get; }
    }
}
");
            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_WhitespaceBetweenMultipleLambdasInMethodInvocations_EmitsDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        void Foo(ISoftwareContent softwareContent)
        {
            <|>softwareContent         .Children(x => true)          .Any(s => s.AliasName == ""T"");
        }

        private interface ISoftwareContent
        {
            IEnumerable<ISoftwareContent> Children(Predicate<int> predicate = null);

            string AliasName { get; }
        }
    }
}
", GetNI1017Rule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_NoLambdasArrayInitializers_NoDiagnostic()
        {
            var test = new AutoTestFile(
@"
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        private static int MaxCounts(IEnumerable<int> numbers)
        {
            var max = new[]
            {
                numbers.Count(number => number == 1),
                numbers.Count(number => number == 2),
                numbers.Count(number => number == 3)
            }
            .Max();
            return max;
        }
    }
}
");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_PoorlySplitLambdasArrayInitializers_EmitsDiagnostic()
        {
            var test = new AutoTestFile(
@"

using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        private static int MaxCounts(IEnumerable<int> numbers, ISoftwareContent softwareContent)
        {
            var max = new[]
            {
                numbers.Count(number => number == 1),
                numbers.Count(number => number == 2),
                numbers.Count(number => number == 3),
                <|>softwareContent.Children(x => true).Count(s => s.AliasName == ""T"")
            }
            .Max();
            return max;
        }

        private interface ISoftwareContent
        {
            IEnumerable<ISoftwareContent> Children(Predicate<int> predicate = null);

            string AliasName { get; }
        }
    }
}
", GetNI1017Rule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1017_WellSplitLambdasArrayInitializers_EmitsDiagnostic()
        {
            var test = new AutoTestFile(
@"

using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.UnitTests
{
    class Test
    {
        private static int MaxCounts(IEnumerable<int> numbers, ISoftwareContent softwareContent)
        {
            var max = new[]
            {
                numbers.Count(number => number == 1),
                numbers.Count(number => number == 2),
                numbers.Count(number => number == 3),
                softwareContent.Children(x => true)
                    .Count(s => s.AliasName == ""T"")
            }
            .Max();
            return max;
        }

        private interface ISoftwareContent
        {
            IEnumerable<ISoftwareContent> Children(Predicate<int> predicate = null);

            string AliasName { get; }
        }
    }
}
");

            VerifyDiagnostics(test);
        }

        private Rule GetNI1017Rule()
        {
            return new Rule(ChainOfMethodsWithLambdasAnalyzer.Rule);
        }
    }
}
