using System.Globalization;
using System.Linq;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.UnitTests.Assets;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.TestUtilities.UnitTests
{
    /// <summary>
    /// Tests that the same expected diagnostics can be specified in markup as can be manually defined.
    /// </summary>
    public sealed class DiagnosticVerifierTests : NIDiagnosticAnalyzerTests<ExampleDiagnosticAnalyzer>
    {
        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_OnePositionMarker_SameDiagnostic()
        {
            const string TestTemplate = @"
class Program
{{
    {0}public Program()
    {{
    }}
}}";

            var sourceTest = new TestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<|>"),
                GetExampleDiagnosticConstructorRule());

            VerifyDiagnostics(sourceTest, GetExampleDiagnosticConstructorResultAt(4, 5));
            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_ManyPositionMarkers_SameDiagnostics()
        {
            const string TestTemplate = @"
class Program
{{
    {0}public Program()
    {{
    }}


    {0}public Program(int i)
    {{
    }}
}}";

            var sourceTest = new TestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<|>"),
                GetExampleDiagnosticConstructorRule(),
                GetExampleDiagnosticConstructorRule());

            VerifyDiagnostics(
                sourceTest,
                GetExampleDiagnosticConstructorResultAt(4, 5),
                GetExampleDiagnosticConstructorResultAt(9, 5));

            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_OneSubstitutionMarker_SameDiagnostic()
        {
            const string TestTemplate = @"
class Program
{{
    private string _field = {0}""foo"";
}}";

            var sourceTest = new TestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<?>"),
                GetExampleDiagnosticFieldRule(@"""foo"""));

            VerifyDiagnostics(sourceTest, GetExampleDiagnosticFieldResultAt(4, 29, @"""foo"""));
            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_ManySubstitutionMarkers_SameDiagnostics()
        {
            const string TestTemplate = @"
class Program
{{
    private string _field1 = {0}""foo1"";
    private string _field2 = {0}""foo2"";
}}";

            var sourceTest = new TestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<?>"),
                GetExampleDiagnosticFieldRule(@"""foo1"""),
                GetExampleDiagnosticFieldRule(@"""foo2"""));

            VerifyDiagnostics(
                sourceTest,
                GetExampleDiagnosticFieldResultAt(4, 30, @"""foo1"""),
                GetExampleDiagnosticFieldResultAt(5, 30, @"""foo2"""));

            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_OneSubstitutionMarker_ManyArguments_SameDiagnostics()
        {
            const string TestTemplate = @"
class Program
{{
    public int A {{ get; {0}set; }}
    public int B {{ get; {0}set; }}
}}";

            var sourceTest = new TestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<?>"),
                GetExampleDiagnosticPropertyRule("A.set", "Program", "private set"),
                GetExampleDiagnosticPropertyRule("B.set", "Program", "private set"));

            VerifyDiagnostics(
                sourceTest,
                GetExampleDiagnosticPropertyResultAt(4, 25, "A.set", "Program", "private set"),
                GetExampleDiagnosticPropertyResultAt(5, 25, "B.set", "Program", "private set"));

            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_OneTextMarker_SameDiagnostic()
        {
            const string TestTemplate = @"
class Program
{{
    private string _field = {0}""foo""{1};
}}";

            var sourceTest = new TestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty, string.Empty));
            var markupTest = new AutoTestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, "<|", "|>"), GetExampleDiagnosticFieldRule());

            VerifyDiagnostics(sourceTest, GetExampleDiagnosticFieldResultAt(4, 29, @"""foo"""));
            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_ManyTextMarkers_Multiline_SameDiagnostic()
        {
            const string TestTemplate = @"
class Program
{{
    {0}public int Method(string name)
    {{
        return name.Length;
    }}{1}
}}";

            var sourceTest = new TestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty, string.Empty));
            var markupTest = new AutoTestFile(string.Format(CultureInfo.InvariantCulture, TestTemplate, "<|", "|>"), GetExampleDiagnosticMethodRule());

            const string MethodSignature = @"public int Method(string name)
    {
        return name.Length;
    }";

            VerifyDiagnostics(sourceTest, GetExampleDiagnosticMethodResultAt(4, 5, MethodSignature));
            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_OneTextMarker_SameDiagnostics()
        {
            const string TestTemplate = @"
class Program
{{
    private string _field1 = {0}""foo1""{1};
    private string _field2 = {0}""foo2""{1};
}}";

            var sourceTest = new TestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<|", "|>"),
                GetExampleDiagnosticFieldRule(),
                GetExampleDiagnosticFieldRule());

            VerifyDiagnostics(
                sourceTest,
                GetExampleDiagnosticFieldResultAt(4, 30, @"""foo1"""),
                GetExampleDiagnosticFieldResultAt(5, 30, @"""foo2"""));

            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_MixedMarkers_SameRule_SameDiagnostics()
        {
            const string TestTemplate = @"
class Program
{{
    private string _field = {0}""foo"";    

    {1}public void Method(string name)
    {{
    }}{2}
}}";

            var sourceTest = new TestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty, string.Empty, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<?>", "<|", "|>"),
                GetExampleDiagnosticFieldRule(@"""foo"""),
                GetExampleDiagnosticMethodRule());

            const string MethodSignature = @"public void Method(string name)
    {
    }";

            VerifyDiagnostics(
                sourceTest,
                GetExampleDiagnosticFieldResultAt(4, 29, @"""foo"""),
                GetExampleDiagnosticMethodResultAt(6, 5, MethodSignature));

            VerifyDiagnostics(
                markupTest);
        }

        [Fact]
        public void VerifyCSharpDiagnosticsFromMarkup_MixedMarkers_DifferentRules_SameDiagnostics()
        {
            const string TestTemplate = @"
class Program
{{
    private string _field = {0}""foo"";    

    {1}public Program()
    {{
    }}
}}";

            var sourceTest = new TestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<?>", "<|>"),
                GetExampleDiagnosticFieldRule(@"""foo"""),
                GetExampleDiagnosticConstructorRule());

            VerifyDiagnostics(
                sourceTest,
                GetExampleDiagnosticFieldResultAt(4, 29, @"""foo"""),
                GetExampleDiagnosticConstructorResultAt(6, 5));

            VerifyDiagnostics(markupTest);
        }

        [Fact]
        public void GetExpectedDiagnosticsFromMarkup_MixedMarkers_DifferentRules_SameDiagnostics()
        {
            const string TestTemplate = @"
class Program
{{
    private string _field = {0}""foo"";    

    {1}public Program()
    {{
    }}
}}";

            var sourceTest = new TestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, string.Empty, string.Empty));
            var markupTest = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, TestTemplate, "<?>", "<|>"),
                GetExampleDiagnosticFieldRule(@"""foo"""),
                GetExampleDiagnosticConstructorRule());

            VerifyDiagnostics(
                sourceTest,
                GetExampleDiagnosticFieldResultAt(4, 29, @"""foo"""),
                GetExampleDiagnosticConstructorResultAt(6, 5));

            // Demonstrates that VerifyDiagnostics can still be called when markup is used.
            // This is useful when another rule that doesn't have a position in source is expected
            // e.g. file parse error.
            VerifyDiagnostics(new TestFile(markupTest.Source), markupTest.ExpectedDiagnostics.ToArray());
        }

        private DiagnosticResult GetExampleDiagnosticConstructorResultAt(int line, int column)
        {
            return GetResultAt(line, column, ExampleDiagnosticAnalyzer.NoArgumentRule);
        }

        private DiagnosticResult GetExampleDiagnosticFieldResultAt(int line, int column, string fieldName)
        {
            return GetResultAt(line, column, ExampleDiagnosticAnalyzer.OneArgumentRule, fieldName);
        }

        private DiagnosticResult GetExampleDiagnosticMethodResultAt(int line, int column, string methodSignature)
        {
            return GetResultAt(line, column, ExampleDiagnosticAnalyzer.OneArgumentRule, methodSignature);
        }

        private DiagnosticResult GetExampleDiagnosticPropertyResultAt(int line, int column, string existingAccessor, string typeName, string replacementAccessor)
        {
            return GetResultAt(line, column, ExampleDiagnosticAnalyzer.ManyArgumentRule, existingAccessor, typeName, replacementAccessor);
        }

        private Rule GetExampleDiagnosticConstructorRule()
        {
            return new Rule(ExampleDiagnosticAnalyzer.NoArgumentRule);
        }

        private Rule GetExampleDiagnosticFieldRule(string fieldName = null)
        {
            return new Rule(ExampleDiagnosticAnalyzer.OneArgumentRule, fieldName);
        }

        private Rule GetExampleDiagnosticMethodRule()
        {
            return new Rule(ExampleDiagnosticAnalyzer.OneArgumentRule);
        }

        private Rule GetExampleDiagnosticPropertyRule(string existingAccessor, string typeName, string replacementAccessor)
        {
            return new Rule(ExampleDiagnosticAnalyzer.ManyArgumentRule, existingAccessor, typeName, replacementAccessor);
        }
    }
}
